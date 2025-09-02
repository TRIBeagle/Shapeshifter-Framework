// Compat_FacialAnimation_OverridesHook.cs
// 목적/특징:
//  - 한 파일 = 한 패치 카운트("OverridesHook").
//  - CompShapeshifter와 완전 분리: Harmony 훅으로 변신/해제/로드 후에만介入.
//  - 시작 시 전체 폼의 *TypeDef defName 유효성 검증 → Report에서 출력.
//  - 변신 시 동일 오류(같은 id)는 "중복 경고 억제": 시작 때 이미 보고된 건 다시 찍지 않음.
//  - Color는 바닐라 ColorInt? → ToColor, dirty 갱신은 필드→프로퍼티 폴백.
//
// 주의: *TypeDef defName만 사용. (백호환 미사용)

using HarmonyLib;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Compat
{
    // ────────────────────────────────────────────────────────────────
    // Facial Animation 유틸 (8개 오버라이드 + 시작 시 검증 + 중복 억제)
    internal static class FacialAnimationCompat
    {
        internal static bool Active => ShapeshiftCompat.FA.IsActive;

        // ── 시작 시 전체 폼 유효성 검증 ──────────────────────────────────────────
        // 같은 문제는 시작 로그에 한 번만 표시하고, 이후 변신 시에는 중복 경고 억제
        internal static void ValidateAllForms()
        {
            if (!Active) return;

            var all = DefDatabase<ShapeshiftFormDef>.AllDefsListForReading;
            if (all == null) return;

            for (int i = 0; i < all.Count; i++)
            {
                var f = all[i];
                if (f == null) continue;

                ValidateDefField("HeadControllerComp", f.faHeadTypeDef, f);
                ValidateDefField("EyeballControllerComp", f.faEyeballTypeDef, f);
                ValidateDefField("LidControllerComp", f.faLidTypeDef, f);
                ValidateDefField("BrowControllerComp", f.faBrowTypeDef, f);
                ValidateDefField("MouthControllerComp", f.faMouthTypeDef, f);
                ValidateDefField("SkinControllerComp", f.faSkinTypeDef, f);
            }
        }

        private static void ValidateDefField(string controller, string defName, ShapeshiftFormDef owner)
        {
            if (string.IsNullOrEmpty(defName)) return;

            var defType = MapControllerToDefType(controller);
            if (defType == null)
            {
                ReportOnceFailed("ResolveDefTypeFailed:" + controller,
                    "cannot resolve target Def type");
                return;
            }

            var target = GenDefDatabase.GetDef(defType, defName, false);
            if (target == null)
            {
                // 시작 로그용: 폼명 포함, id는 컨트롤러+defName으로 고정
                ReportOnceFailed("InvalidFA:" + controller + ":" + defName,
                    $"def '{defName}' not found for {defType.Name} (in form {owner.defName})");
            }
        }

        // ── 세이브/로드용 백업 덩어리 ──────────────────────────────────────────
        public sealed class Backup : IExposable
        {
            // face type defName
            public string head, eyeball, lid, brow, mouth, skin;

            // colors + 존재 플래그(완전투명 보존)
            public Color? eyeColor;     // FaceColor
            public Color? eyeColor2;    // FaceSecondColor
            internal bool eyeColorSet;
            internal bool eyeColor2Set;

            public void ExposeData()
            {
                Scribe_Values.Look(ref head, "faHead");
                Scribe_Values.Look(ref eyeball, "faEyeball");
                Scribe_Values.Look(ref lid, "faLid");
                Scribe_Values.Look(ref brow, "faBrow");
                Scribe_Values.Look(ref mouth, "faMouth");
                Scribe_Values.Look(ref skin, "faSkin");

                Scribe_Values.Look(ref eyeColorSet, "faEyeColor_set", false);
                Scribe_Values.Look(ref eyeColor2Set, "faEyeColor2_set", false);

                var c1 = eyeColor ?? default(Color);
                var c2 = eyeColor2 ?? default(Color);
                Scribe_Values.Look(ref c1, "faEyeColor");
                Scribe_Values.Look(ref c2, "faEyeColor2");

                eyeColor = eyeColorSet ? (Color?)c1 : null;
                eyeColor2 = eyeColor2Set ? (Color?)c2 : null;
            }

            public bool IsEmpty =>
                head == null && eyeball == null && lid == null &&
                brow == null && mouth == null && skin == null &&
                !eyeColor.HasValue && !eyeColor2.HasValue;

            public void Clear()
            {
                head = eyeball = lid = brow = mouth = skin = null;
                eyeColor = eyeColor2 = null;
                eyeColorSet = eyeColor2Set = false;
            }
        }

        // 현재 상태 백업
        internal static void BackupCurrent(Pawn pawn, Backup dst)
        {
            if (!Active || pawn == null || dst == null) return;

            dst.head = GetFADefName(pawn, "HeadControllerComp");
            dst.eyeball = GetFADefName(pawn, "EyeballControllerComp");
            dst.lid = GetFADefName(pawn, "LidControllerComp");
            dst.brow = GetFADefName(pawn, "BrowControllerComp");
            dst.mouth = GetFADefName(pawn, "MouthControllerComp");
            dst.skin = GetFADefName(pawn, "SkinControllerComp");

            var eyeComp = FindFAControllerComp(pawn, "EyeballControllerComp");
            if (eyeComp != null)
            {
                var c1 = ShapeshiftReflectionCache.GetInstanceProperty<Color>(eyeComp, "FaceColor");
                var c2 = ShapeshiftReflectionCache.GetInstanceProperty<Color>(eyeComp, "FaceSecondColor");
                if (c1 != default(Color)) { dst.eyeColor = c1; dst.eyeColorSet = true; }
                if (c2 != default(Color)) { dst.eyeColor2 = c2; dst.eyeColor2Set = true; }
            }
        }

        // 폼에 지정된 항목만 적용
        internal static void ApplyOverrides(Pawn pawn, ShapeshiftFormDef form)
        {
            if (!Active || pawn == null || form == null) return;

            ApplyDefByName(pawn, "HeadControllerComp", form.faHeadTypeDef);
            ApplyDefByName(pawn, "EyeballControllerComp", form.faEyeballTypeDef);
            ApplyDefByName(pawn, "LidControllerComp", form.faLidTypeDef);
            ApplyDefByName(pawn, "BrowControllerComp", form.faBrowTypeDef);
            ApplyDefByName(pawn, "MouthControllerComp", form.faMouthTypeDef);
            ApplyDefByName(pawn, "SkinControllerComp", form.faSkinTypeDef);

            // 눈 색상 (ColorInt? → Color)
            if (form.faEyeColor.HasValue || form.faEyeColor2.HasValue)
            {
                var eyeComp = FindFAControllerComp(pawn, "EyeballControllerComp");
                if (eyeComp != null)
                {
                    bool any = false;

                    if (form.faEyeColor.HasValue)
                    {
                        var c1 = form.faEyeColor.Value.ToColor;
                        if (!ShapeshiftReflectionCache.TrySetInstanceProperty(eyeComp, "FaceColor", c1))
                            ReportOnceFailed("EyeColor:MissingProperty:FaceColor",
                                "property FaceColor not found or not writable");
                        else any = true;
                    }

                    if (form.faEyeColor2.HasValue)
                    {
                        var c2 = form.faEyeColor2.Value.ToColor;
                        if (!ShapeshiftReflectionCache.TrySetInstanceProperty(eyeComp, "FaceSecondColor", c2))
                            ReportOnceFailed("EyeColor2:MissingProperty:FaceSecondColor",
                                "property FaceSecondColor not found or not writable");
                        else any = true;
                    }

                    if (any) MarkDirty(eyeComp);
                }
                else
                {
                    ReportOnceFailed("EyeColor:EyeballControllerMissing",
                        "EyeballControllerComp not found on pawn");
                }
            }
        }

        // 백업 기준 원복
        internal static void Restore(Pawn pawn, Backup src)
        {
            if (!Active || pawn == null || src == null || src.IsEmpty) return;

            ApplyDefByName(pawn, "HeadControllerComp", src.head);
            ApplyDefByName(pawn, "EyeballControllerComp", src.eyeball);
            ApplyDefByName(pawn, "LidControllerComp", src.lid);
            ApplyDefByName(pawn, "BrowControllerComp", src.brow);
            ApplyDefByName(pawn, "MouthControllerComp", src.mouth);
            ApplyDefByName(pawn, "SkinControllerComp", src.skin);

            if (src.eyeColor.HasValue || src.eyeColor2.HasValue)
            {
                var eyeComp = FindFAControllerComp(pawn, "EyeballControllerComp");
                if (eyeComp != null)
                {
                    bool any = false;

                    if (src.eyeColor.HasValue)
                    {
                        if (!ShapeshiftReflectionCache.TrySetInstanceProperty(eyeComp, "FaceColor", src.eyeColor.Value))
                            ReportOnceFailed("RestoreEyeColor:MissingProperty:FaceColor",
                                "FaceColor property set failed");
                        else any = true;
                    }
                    if (src.eyeColor2.HasValue)
                    {
                        if (!ShapeshiftReflectionCache.TrySetInstanceProperty(eyeComp, "FaceSecondColor", src.eyeColor2.Value))
                            ReportOnceFailed("RestoreEyeColor2:MissingProperty:FaceSecondColor",
                                "FaceSecondColor property set failed");
                        else any = true;
                    }

                    if (any) MarkDirty(eyeComp);
                }
            }
        }

        // ── 내부 유틸 ────────────────────────────────────────────────────────────

        private static ThingComp FindFAControllerComp(Pawn pawn, string controllerSuffix)
        {
            if (pawn == null) return null;
            var list = (pawn as ThingWithComps)?.AllComps;
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i]; if (c == null) continue;
                var full = c.GetType().FullName;
                if (!string.IsNullOrEmpty(full)
                    && full.StartsWith("FacialAnimation", System.StringComparison.Ordinal)
                    && full.EndsWith(controllerSuffix, System.StringComparison.Ordinal))
                    return c;
            }
            return null;
        }

        private static string GetFADefName(Pawn pawn, string controller)
        {
            var comp = FindFAControllerComp(pawn, controller);
            if (comp == null) return null;

            Def cur = ShapeshiftReflectionCache.GetInstanceField<Def>(comp, "faceType");
            if (cur == null)
                cur = ShapeshiftReflectionCache.GetInstanceProperty<Def>(comp, "FaceType");

            return cur != null ? cur.defName : null;
        }

        private static System.Type MapControllerToDefType(string controller)
        {
            string name = null;
            if (controller == "HeadControllerComp") name = "FacialAnimation.HeadTypeDef";
            else if (controller == "EyeballControllerComp") name = "FacialAnimation.EyeballTypeDef";
            else if (controller == "LidControllerComp") name = "FacialAnimation.LidTypeDef";
            else if (controller == "BrowControllerComp") name = "FacialAnimation.BrowTypeDef";
            else if (controller == "MouthControllerComp") name = "FacialAnimation.MouthTypeDef";
            else if (controller == "SkinControllerComp") name = "FacialAnimation.SkinTypeDef";
            return string.IsNullOrEmpty(name) ? null : ShapeshiftReflectionCache.TryType(name);
        }

        private static void ApplyDefByName(Pawn pawn, string controller, string defName)
        {
            if (string.IsNullOrEmpty(defName)) return;

            var comp = FindFAControllerComp(pawn, controller);
            if (comp == null)
            {
                ReportOnceFailed("ControllerMissing:" + controller, "controller comp not found");
                return;
            }

            // target Def 타입: 현재 faceType 우선, 없으면 매핑
            var defType = (System.Type)null;
            Def cur = ShapeshiftReflectionCache.GetInstanceField<Def>(comp, "faceType");
            if (cur != null) defType = cur.GetType();
            if (defType == null) defType = MapControllerToDefType(controller);
            if (defType == null)
            {
                ReportOnceFailed("ResolveDefTypeFailed:" + controller, "cannot resolve target Def type");
                return;
            }

            var target = GenDefDatabase.GetDef(defType, defName, false);
            if (target == null)
            {
                // 변신 시: 시작에 이미 보고된 동일 오류면 다시 찍지 않음
                ReportOnceFailed("InvalidFA:" + controller + ":" + defName,
                    $"def '{defName}' not found for {defType.Name}");
                return;
            }

            if (!ShapeshiftReflectionCache.TrySetInstanceField(comp, "faceType", target))
            {
                ReportOnceFailed("SetFaceTypeFailed:" + controller,
                    "set faceType failed (field missing or type mismatch)");
                return;
            }

            MarkDirty(comp);
        }

        private static void MarkDirty(object controllerComp)
        {
            if (controllerComp == null) return;

            if (!ShapeshiftReflectionCache.TrySetInstanceField(controllerComp, "dirtyFlag", true))
                ShapeshiftReflectionCache.TrySetInstanceProperty(controllerComp, "DirtyFlag", true);
        }

        // "한 번만 실패 보고" 도우미
        private static void ReportOnceFailed(string id, string reason)
        {
            if (!ShapeshiftCompat.FA.HasFailed(id))
                ShapeshiftCompat.FA.Failed(id, reason);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 백업 저장소(GameComponent) : Pawn → Backup
    internal sealed class FAStateStore : GameComponent
    {
        public FAStateStore(Game game) { Inst = this; }
        public static FAStateStore Inst { get; private set; }

        private Dictionary<Pawn, FacialAnimationCompat.Backup> map =
            new Dictionary<Pawn, FacialAnimationCompat.Backup>();

        // Scribe가 Dict<ref, deep>를 처리할 수 있도록 tmp 리스트 필요
        private List<Pawn> tmpKeys;
        private List<FacialAnimationCompat.Backup> tmpVals;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref map, "faBackups",
                LookMode.Reference, LookMode.Deep, ref tmpKeys, ref tmpVals);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Cleanup();
        }

        private void Cleanup()
        {
            // null/Destroyed 참조 정리
            var remove = new List<Pawn>();
            foreach (var kv in map)
                if (kv.Key == null || kv.Key.Destroyed) remove.Add(kv.Key);
            for (int i = 0; i < remove.Count; i++) map.Remove(remove[i]);
        }

        public FacialAnimationCompat.Backup GetOrCreate(Pawn p)
        {
            if (p == null) return null;
            if (!map.TryGetValue(p, out var b) || b == null)
            {
                b = new FacialAnimationCompat.Backup();
                map[p] = b;
            }
            return b;
        }

        public bool TryGet(Pawn p, out FacialAnimationCompat.Backup b)
        {
            if (p != null && map.TryGetValue(p, out b) && b != null) return true;
            b = null; return false;
        }

        public void Remove(Pawn p)
        {
            if (p != null) map.Remove(p);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Harmony 훅: CompShapeshifter ←→ FacialAnimationCompat 연결
    [HarmonyPatch]
    internal static class Compat_FacialAnimation_OverridesHook
    {
        static bool counted;

        // ApplyForm(ShapeshiftFormDef, string prev)
        [HarmonyPatch(typeof(CompShapeshifter), "ApplyForm", new System.Type[] { typeof(ShapeshiftFormDef), typeof(string) })]
        static class Patch_ApplyForm2
        {
            static bool Prepare()
            {
                if (!ShapeshiftCompat.FA.IsActive) return false;
                if (!counted) { counted = true; ShapeshiftCompat.FA.Patched("OverridesHook"); }
                return true;
            }

            static void Postfix(CompShapeshifter __instance, ShapeshiftFormDef form)
            {
                try
                {
                    var pawn = __instance?.parent as Pawn;
                    if (pawn == null || form == null) return;

                    var store = Current.Game?.GetComponent<FAStateStore>();
                    var backup = store?.GetOrCreate(pawn);
                    if (backup == null) return;

                    FacialAnimationCompat.BackupCurrent(pawn, backup);
                    FacialAnimationCompat.ApplyOverrides(pawn, form);
                }
                catch (Exception e)
                {
                    // 훅 예외는 반복될 수 있으므로 동일 id로 1회만 보고
                    if (!ShapeshiftCompat.FA.HasFailed("OverridesHook:ApplyForm2:Exception"))
                        ShapeshiftCompat.FA.Failed("OverridesHook:ApplyForm2:Exception", e.Message);
                }
            }
        }

        // RemoveForm()
        [HarmonyPatch(typeof(CompShapeshifter), "RemoveForm")]
        static class Patch_RemoveForm
        {
            static bool Prepare() => ShapeshiftCompat.FA.IsActive;

            static void Prefix(CompShapeshifter __instance)
            {
                try
                {
                    var pawn = __instance?.parent as Pawn;
                    if (pawn == null) return;

                    var store = Current.Game?.GetComponent<FAStateStore>();
                    if (store != null && store.TryGet(pawn, out var backup) && backup != null)
                    {
                        FacialAnimationCompat.Restore(pawn, backup);
                        backup.Clear();
                        store.Remove(pawn);
                    }
                }
                catch (Exception e)
                {
                    if (!ShapeshiftCompat.FA.HasFailed("OverridesHook:RemoveForm:Exception"))
                        ShapeshiftCompat.FA.Failed("OverridesHook:RemoveForm:Exception", e.Message);
                }
            }
        }

        // PostExposeData()
        [HarmonyPatch(typeof(CompShapeshifter), "PostExposeData")]
        static class Patch_PostExposeData
        {
            static bool Prepare() => ShapeshiftCompat.FA.IsActive;

            static void Postfix(CompShapeshifter __instance)
            {
                try
                {
                    // 저장 불러온 뒤 이미 변신 상태면 재적용
                    if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance != null && __instance.isTransformed && __instance.currentForm != null)
                    {
                        var pawn = __instance.parent as Pawn;
                        if (pawn == null) return;

                        var store = Current.Game?.GetComponent<FAStateStore>();
                        var backup = store?.GetOrCreate(pawn);
                        if (backup == null) return;

                        FacialAnimationCompat.ApplyOverrides(pawn, __instance.currentForm);
                    }
                }
                catch (Exception e)
                {
                    if (!ShapeshiftCompat.FA.HasFailed("OverridesHook:PostExposeData:Exception"))
                        ShapeshiftCompat.FA.Failed("OverridesHook:PostExposeData:Exception", e.Message);
                }
            }
        }
    }
}
