// ShapeshifterFramework | Compat | Compat_FacialAnimation_OverridesHook.cs
// 목적   : Facial Animation과의 연동을 Harmony 훅으로 처리하여 변신/해제/로드 시점에만 개입,
//          폼에 지정된 FA FaceTypeDef와 눈 색상(Color)을 적용·복원한다.
// 용도   : CompShapeshifter의 ApplyForm/RemoveForm/PostExposeData를 후킹해 백업-오버라이드-원복을 수행.
// 변경   : 2025-09-22 v1.0 — 프로젝트 주석 규칙 적용(주석만 정리, 로직 변경 없음).
// 저장   : GameComponent(FAStateStore)에 Pawn별 Backup을 딥세이브. PostLoadInit에 정리(Cleanup).
// 주의   : *TypeDef는 defName만 사용(백호환 미사용). 동일 오류(id)는 한 번만 경고(중복 억제).

using HarmonyLib;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Compat
{
    #region Facial Animation Overrides (validation/backup/apply/restore)

    /// <summary>
    /// Facial Animation 유틸리티:
    /// - 시작 시 전체 폼 유효성 검증(존재하지 않는 FaceTypeDef defName 감지, 동일 오류 1회 보고)
    /// - 현재 Pawn의 FA 상태 백업/적용/원복(눈 색상 포함, DirtyFlag 갱신)
    /// - 변신 시점에만 Harmony 훅으로 개입(CompShapeshifter와 코드 의존 분리)
    /// </summary>
    internal static class FacialAnimationCompat
    {
        /// <summary>외부 모드 활성/감지 상태.</summary>
        internal static bool Active => CompatManager.FA.IsActive;

        /// <summary>
        /// [시작 단계] 모든 ShapeshiftFormDef에 대해 FA 타입 defName 유효성을 검증한다.
        /// 동일한 문제는 한 번만 Report(중복 경고 억제).
        /// </summary>
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

        /// <summary>
        /// 컨트롤러 별 대상 Def 타입을 추론하여 defName을 검증한다.
        /// 실패 시 동일 id에 대해 한 번만 경고.
        /// </summary>
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

        /// <summary>
        /// Pawn의 현재 FA 상태를 Backup에 저장한다(Def 선택 + 눈 색상).
        /// </summary>
        public sealed class Backup : IExposable
        {
            // face type defName
            public string head, eyeball, lid, brow, mouth, skin;

            // colors + 존재 플래그(완전투명 보존)
            public Color? eyeColor;     // FaceColor
            public Color? eyeColor2;    // FaceSecondColor
            internal bool eyeColorSet;
            internal bool eyeColor2Set;

            /// <summary>세이브/로드. 색상 존재 여부 플래그를 별도로 보존.</summary>
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

                if (Scribe.mode != LoadSaveMode.Saving)
                {
                    eyeColor = eyeColorSet ? (Color?)c1 : null;
                    eyeColor2 = eyeColor2Set ? (Color?)c2 : null;
                }
            }

            /// <summary>백업 내용이 비었는지 여부.</summary>
            public bool IsEmpty =>
                head == null && eyeball == null && lid == null &&
                brow == null && mouth == null && skin == null &&
                !eyeColor.HasValue && !eyeColor2.HasValue;

            /// <summary>백업 초기화.</summary>
            public void Clear()
            {
                head = eyeball = lid = brow = mouth = skin = null;
                eyeColor = eyeColor2 = null;
                eyeColorSet = eyeColor2Set = false;
            }
        }

        /// <summary>
        /// Pawn의 현재 FA 타입/색상을 Backup에 저장한다.
        /// </summary>
        /// <param name="pawn">대상 Pawn</param>
        /// <param name="dst">목적지 백업</param>
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

        /// <summary>
        /// 폼에 지정된 항목만 FA에 적용한다(DefName → Def 조회, 색상 적용 시 DirtyFlag 갱신).
        /// </summary>
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

        /// <summary>
        /// 백업 기준으로 Pawn의 FA 상태를 복원한다(Def/색상, DirtyFlag 포함).
        /// </summary>
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

        #region helpers

        /// <summary>Pawn에서 특정 FA 컨트롤러 컴프를 이름 규칙으로 찾는다.</summary>
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

        /// <summary>현재 faceType/FaceType에서 Def를 읽어 defName을 반환.</summary>
        private static string GetFADefName(Pawn pawn, string controller)
        {
            var comp = FindFAControllerComp(pawn, controller);
            if (comp == null) return null;

            Def cur = ShapeshiftReflectionCache.GetInstanceField<Def>(comp, "faceType");
            if (cur == null)
                cur = ShapeshiftReflectionCache.GetInstanceProperty<Def>(comp, "FaceType");

            return cur != null ? cur.defName : null;
        }

        /// <summary>컨트롤러명 → 대상 Def 타입 매핑.</summary>
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

        /// <summary>
        /// DefName으로 대상 Def를 조회해 컨트롤러의 faceType에 적용하고, DirtyFlag를 세운다.
        /// 실패/미존재/타입해석 실패는 동일 id 1회만 경고.
        /// </summary>
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

        /// <summary>컨트롤러의 DirtyFlag를 true로 설정(필드/프로퍼티 폴백).</summary>
        private static void MarkDirty(object controllerComp)
        {
            if (controllerComp == null) return;

            if (!ShapeshiftReflectionCache.TrySetInstanceField(controllerComp, "dirtyFlag", true))
                ShapeshiftReflectionCache.TrySetInstanceProperty(controllerComp, "DirtyFlag", true);
        }

        /// <summary>동일 오류 id는 한 번만 Failed 로그를 남긴다.</summary>
        private static void ReportOnceFailed(string id, string reason)
        {
            if (!CompatManager.FA.HasFailed(id))
                CompatManager.FA.Failed(id, reason);
        }

        #endregion
    }

    #endregion

    #region Save store: Pawn → Backup (GameComponent)

    /// <summary>
    /// Pawn별 Facial Animation 백업 저장소.
    /// - 딥세이브/로드 지원(Reference/Deep 혼합), PostLoadInit에 dangling 참조 정리.
    /// </summary>
    internal sealed class FAStateStore : GameComponent
    {
        /// <summary>게임 컴포넌트 생성자(싱글턴 할당).</summary>
        public FAStateStore(Game game) { Inst = this; }

        /// <summary>싱글턴 인스턴스.</summary>
        public static FAStateStore Inst { get; private set; }

        private Dictionary<Pawn, FacialAnimationCompat.Backup> map =
            new Dictionary<Pawn, FacialAnimationCompat.Backup>();

        // Scribe가 Dict<ref, deep>를 처리할 수 있도록 tmp 리스트 필요
        private List<Pawn> tmpKeys;
        private List<FacialAnimationCompat.Backup> tmpVals;

        /// <summary>세이브/로드 구현. 로드 완료 후 정리(Cleanup).</summary>
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref map, "faBackups",
                LookMode.Reference, LookMode.Deep, ref tmpKeys, ref tmpVals);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Cleanup();
        }

        // 게임 중에도 60,000틱(1일)마다 죽은 폰 찌꺼기 청소
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                Cleanup();
            }
        }

        /// <summary>null/Destroyed Pawn 키 제거.</summary>
        private void Cleanup()
        {
            // null/Destroyed 참조 정리
            var remove = new List<Pawn>();
            foreach (var kv in map)
                if (kv.Key == null || kv.Key.Destroyed) remove.Add(kv.Key);
            for (int i = 0; i < remove.Count; i++) map.Remove(remove[i]);
        }

        /// <summary>백업을 가져오거나 새로 만든다.</summary>
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

        /// <summary>백업을 시도해 얻는다.</summary>
        public bool TryGet(Pawn p, out FacialAnimationCompat.Backup b)
        {
            if (p != null && map.TryGetValue(p, out b) && b != null) return true;
            b = null; return false;
        }

        /// <summary>백업 제거.</summary>
        public void Remove(Pawn p)
        {
            if (p != null) map.Remove(p);
        }
    }

    #endregion

    #region Harmony hooks: CompShapeshifter ↔ FacialAnimationCompat

    /// <summary>
    /// CompShapeshifter의 라이프사이클에 맞춰 FacialAnimationCompat을 호출하는 Harmony 훅들.
    /// - ApplyForm(Postfix): 백업 → 폼 오버라이드 적용
    /// - RemoveForm(Prefix): 백업 기준 원복 → 제거
    /// - PostExposeData(Postfix): PostLoadInit & 변신 상태면 오버라이드 재적용
    /// </summary>
    [HarmonyPatch]
    internal static class Compat_FacialAnimation_OverridesHook
    {
        private static bool counted;

        /// <summary>
        /// 원본: <c>CompShapeshifter.ApplyForm(ShapeshiftFormDef, string)</c> — <b>Postfix</b>.
        /// 처음 준비 시 한 번만 패치 카운트 기록.
        /// </summary>
        [HarmonyPatch(typeof(CompShapeshifter), "ApplyForm", new System.Type[] { typeof(ShapeshiftFormDef), typeof(string) })]
        static class Patch_ApplyForm2
        {
            /// <summary>FA 비활성 시 패치 비적용. 최초 1회 Patched 기록.</summary>
            static bool Prepare()
            {
                if (!CompatManager.FA.IsActive) return false;
                if (!counted) { counted = true; CompatManager.FA.Patched("OverridesHook"); }
                return true;
            }

            /// <summary>변신 직후: 현 상태 백업 후 폼 오버라이드 적용.</summary>
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
                    // [안전] 훅 예외는 동일 id로 1회만 보고
                    if (!CompatManager.FA.HasFailed("OverridesHook:ApplyForm2:Exception"))
                        CompatManager.FA.Failed("OverridesHook:ApplyForm2:Exception", e.Message);
                }
            }
        }

        /// <summary>
        /// 원본: <c>CompShapeshifter.RemoveForm()</c> — <b>Prefix</b>.
        /// 제거 전 백업 기준으로 원복, 백업 제거.
        /// </summary>
        [HarmonyPatch(typeof(CompShapeshifter), "RemoveForm")]
        static class Patch_RemoveForm
        {
            static bool Prepare() => CompatManager.FA.IsActive;

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
                    if (!CompatManager.FA.HasFailed("OverridesHook:RemoveForm:Exception"))
                        CompatManager.FA.Failed("OverridesHook:RemoveForm:Exception", e.Message);
                }
            }
        }

        /// <summary>
        /// 원본: <c>CompShapeshifter.PostExposeData()</c> — <b>Postfix</b>.
        /// PostLoadInit 이후 이미 변신 상태면 폼 오버라이드를 재적용.
        /// </summary>
        [HarmonyPatch(typeof(CompShapeshifter), "PostExposeData")]
        static class Patch_PostExposeData
        {
            static bool Prepare() => CompatManager.FA.IsActive;

            static void Postfix(CompShapeshifter __instance)
            {
                try
                {
                    // [저장] 저장 불러온 뒤 이미 변신 상태면 재적용
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
                    if (!CompatManager.FA.HasFailed("OverridesHook:PostExposeData:Exception"))
                        CompatManager.FA.Failed("OverridesHook:PostExposeData:Exception", e.Message);
                }
            }
        }
    }

    #endregion
}
