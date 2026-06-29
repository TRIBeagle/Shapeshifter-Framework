// ShapeshifterFramework | Patches | Patch_DamageWorker_AddInjury_SourceLabel.cs
// 목적 : 변신 중 맨손 공격 시 상처 라벨의 종족명을 폼 이름으로 교체.
// 용도 : 바닐라는 DamageInfo.Weapon = CasterPawn.def (예: Human)로 설정하여
//        상처 라벨이 "물림 (인간 이빨)"로 표시됨. 변신 중이면 Postfix에서
//        sourceLabel을 form.label로 교체 → "물림 (곰 변신 이빨)" 형태로 표시.
// 주의 : weaponInt(sourceDef)는 건드리지 않음 — null이면 괄호 표시 자체가 안 됨.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>DamageWorker_AddInjury Postfix: 상처의 sourceLabel을 폼 이름으로 교체.</summary>
    [HarmonyPatch]
    internal static class Patch_DamageWorker_AddInjury_SourceLabel
    {
        private static MethodBase _target;

        static bool Prepare()
        {
            _target = AccessTools.Method(typeof(DamageWorker_AddInjury), "ApplyToPawn");
            if (_target == null)
                Log.Warning("[SSF] DamageWorker_AddInjury.ApplyToPawn not found - patch skipped (RimWorld version change?)");
            return _target != null;
        }

        static MethodBase TargetMethod() => _target;

        [HarmonyPostfix]
        static void Postfix(DamageInfo dinfo, Pawn pawn)
        {
            var attacker = dinfo.Instigator as Pawn;
            if (attacker == null) return;

            if (!ShapeshiftRegistry.TryGet(attacker, out _, out var form)) return;
            if (form.tools == null || form.tools.Count == 0) return;

            // 장비 공격이면 스킵 (Weapon이 공격자 종족 def가 아닌 경우 = 장비 사용 중)
            if (dinfo.Weapon != null && dinfo.Weapon != attacker.def) return;

            string formLabel = form.LabelCap.NullOrEmpty() ? form.defName : form.LabelCap.Resolve();

            // 이번 타격으로 추가된 부상 hediff의 sourceLabel을 폼 이름으로 교체
            // sourceDef(weaponInt)는 유지 → 바닐라 LabelInBrackets에서 "폼이름 도구이름" 형태로 표시
            if (pawn.health?.hediffSet?.hediffs == null) return;
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i] is Hediff_Injury injury && injury.sourceLabel == attacker.def.label)
                {
                    injury.sourceLabel = formLabel;
                }
            }
        }
    }
}
