// ShapeshifterFramework | Compat | Compat_FacialAnimation_OverridesHook.cs
// 변신 시 FA FaceTypeDef/눈 색상을 폼 데이터로 덮어씌우고 해제 시 원복.
// FAStateStore: Pawn별 원본 FA 상태 딥세이브, 주기 청소.
// Harmony Hooks: ApplyForm/RemoveForm/PostExposeData에 개입. 오류는 동일 id당 1회만 경고.

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

    /// <summary>FA 유틸리티: 폼 유효성 검증, FA 상태 백업/적용/원복.</summary>
    internal static class FacialAnimationCompat
    {
        /// <summary>FA 활성 여부.</summary>
        internal static bool Active => CompatManager.FA.IsActive;

        /// <summary>모든 ShapeshiftFormDef의 FA defName 유효성 검증.</summary>
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

        /// <summary>컨트롤러별 Def 타입 추론 후 defName 검증.</summary>
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
                // id는 컨트롤러+defName으로 고정
                ReportOnceFailed("InvalidFA:" + controller + ":" + defName,
                    $"def '{defName}' not found for {defType.Name} (in form {owner.defName})");
            }
        }

        /// <summary>Pawn의 FA 상태 백업(Def + 눈 색상).</summary>
        public sealed class Backup : IExposable
        {
            public string head, eyeball, lid, brow, mouth, skin;

            // 눈 색상 + 존재 플래그
            public Color? eyeColor;     // FaceColor
            public Color? eyeColor2;    // FaceSecondColor
            internal bool eyeColorSet;
            internal bool eyeColor2Set;

            /// <summary>세이브/로드.</summary>
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

        /// <summary>Pawn의 현재 FA 타입/색상을 백업.</summary>
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

        /// <summary>폼 지정 항목을 FA에 적용(Def 조회 + 색상 + DirtyFlag).</summary>
        internal static void ApplyOverrides(Pawn pawn, ShapeshiftFormDef form)
        {
            if (!Active || pawn == null || form == null) return;

            ApplyDefByName(pawn, "HeadControllerComp", form.faHeadTypeDef);
            ApplyDefByName(pawn, "EyeballControllerComp", form.faEyeballTypeDef);
            ApplyDefByName(pawn, "LidControllerComp", form.faLidTypeDef);
            ApplyDefByName(pawn, "BrowControllerComp", form.faBrowTypeDef);
            ApplyDefByName(pawn, "MouthControllerComp", form.faMouthTypeDef);
            ApplyDefByName(pawn, "SkinControllerComp", form.faSkinTypeDef);

            // 눈 색상 적용
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

        /// <summary>백업 기준으로 FA 상태 복원.</summary>
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

        /// <summary>Pawn에서 FA 컨트롤러 컴프를 이름으로 탐색.</summary>
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

        /// <summary>faceType에서 defName 반환.</summary>
        private static string GetFADefName(Pawn pawn, string controller)
        {
            var comp = FindFAControllerComp(pawn, controller);
            if (comp == null) return null;

            Def cur = ShapeshiftReflectionCache.GetInstanceField<Def>(comp, "faceType");
            if (cur == null)
                cur = ShapeshiftReflectionCache.GetInstanceProperty<Def>(comp, "FaceType");

            return cur != null ? cur.defName : null;
        }

        /// <summary>컨트롤러명에서 Def 타입 매핑.</summary>
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

        /// <summary>DefName으로 Def 조회 후 faceType에 적용.</summary>
        private static void ApplyDefByName(Pawn pawn, string controller, string defName)
        {
            if (string.IsNullOrEmpty(defName)) return;

            var comp = FindFAControllerComp(pawn, controller);
            if (comp == null)
            {
                ReportOnceFailed("ControllerMissing:" + controller, "controller comp not found");
                return;
            }

            // Def 타입: faceType 우선, 없으면 매핑
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
                // 동일 오류 중복 보고 방지
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

        /// <summary>DirtyFlag를 true로 설정.</summary>
        private static void MarkDirty(object controllerComp)
        {
            if (controllerComp == null) return;

            if (!ShapeshiftReflectionCache.TrySetInstanceField(controllerComp, "dirtyFlag", true))
                ShapeshiftReflectionCache.TrySetInstanceProperty(controllerComp, "DirtyFlag", true);
        }

        /// <summary>동일 id 1회만 Failed 기록.</summary>
        private static void ReportOnceFailed(string id, string reason)
        {
            if (!CompatManager.FA.HasFailed(id))
                CompatManager.FA.Failed(id, reason);
        }

        #endregion
    }

    #endregion

    #region Save store: Pawn → Backup (GameComponent)

    /// <summary>Pawn별 FA 백업 저장소. 딥세이브/로드 지원.</summary>
    internal sealed class FAStateStore : GameComponent
    {
        public FAStateStore(Game game) { Inst = this; }

        public static FAStateStore Inst { get; private set; }

        private Dictionary<Pawn, FacialAnimationCompat.Backup> map =
            new Dictionary<Pawn, FacialAnimationCompat.Backup>();

        // Scribe Dict<ref, deep> 처리용 tmp
        private List<Pawn> tmpKeys;
        private List<FacialAnimationCompat.Backup> tmpVals;

        // GC 할당 방지용 재활용 버퍼
        private List<Pawn> _removeBuffer = new List<Pawn>();

        /// <summary>세이브/로드. 로드 완료 후 Cleanup.</summary>
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref map, "faBackups",
                LookMode.Reference, LookMode.Deep, ref tmpKeys, ref tmpVals);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Cleanup();
        }

        // 60,000틱(1일)마다 죽은 폰 청소
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                Cleanup();
            }
        }

        /// <summary>null/Destroyed Pawn 제거.</summary>
        private void Cleanup()
        {
            _removeBuffer.Clear();
            foreach (var kv in map)
            {
                if (kv.Key == null || kv.Key.Destroyed || kv.Value == null)
                    _removeBuffer.Add(kv.Key);
            }

            for (int i = 0; i < _removeBuffer.Count; i++)
            {
                var key = _removeBuffer[i];
                if (key != null)
                {
                    map.Remove(key);
                }
            }
            _removeBuffer.Clear();
        }

        /// <summary>백업 조회 또는 생성.</summary>
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

        /// <summary>백업 조회 시도.</summary>
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

    #endregion

    #region Harmony hooks: CompShapeshifter ↔ FacialAnimationCompat

    /// <summary>CompShapeshifter 라이프사이클에 FA 백업/적용/원복 훅.</summary>
    [HarmonyPatch]
    internal static class Compat_FacialAnimation_OverridesHook
    {
        private static bool counted;

        /// <summary>ApplyForm Postfix.</summary>
        [HarmonyPatch(typeof(CompShapeshifter), "ApplyForm", new System.Type[] { typeof(ShapeshiftFormDef), typeof(string), typeof(System.Collections.Generic.List<Verse.Thing>) })]
        static class Patch_ApplyForm2
        {
            /// <summary>FA 비활성 시 패치 비적용.</summary>
            static bool Prepare()
            {
                if (!CompatManager.FA.IsActive) return false;
                if (!counted) { counted = true; CompatManager.FA.Patched("OverridesHook"); }
                return true;
            }

            /// <summary>변신 직후 백업 후 오버라이드 적용.</summary>
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
                    // 훅 예외는 1회만 보고
                    if (!CompatManager.FA.HasFailed("OverridesHook:ApplyForm2:Exception"))
                        CompatManager.FA.Failed("OverridesHook:ApplyForm2:Exception", e.Message);
                }
            }
        }

        /// <summary>RemoveForm Prefix: 백업 기준 원복 후 제거.</summary>
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

        /// <summary>PostExposeData Postfix: 로드 후 변신 상태면 오버라이드 재적용.</summary>
        [HarmonyPatch(typeof(CompShapeshifter), "PostExposeData")]
        static class Patch_PostExposeData
        {
            static bool Prepare() => CompatManager.FA.IsActive;

            static void Postfix(CompShapeshifter __instance)
            {
                try
                {
                    // 로드 후 변신 상태면 재적용
                    if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance != null && __instance.isTransformed && __instance.currentForm != null)
                    {
                        var pawn = __instance.parent as Pawn;
                        if (pawn == null) return;

                        var store = Current.Game?.GetComponent<FAStateStore>();
                        // 빈 백업 방지, 없으면 경고
                        if (store == null || !store.TryGet(pawn, out var backup) || backup == null || backup.IsEmpty)
                        {
                            Log.Warning($"[SSF] FA backup missing for transformed pawn {pawn.Name}. Facial revert might fail later.");
                        }

                        // 오버라이드 적용
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