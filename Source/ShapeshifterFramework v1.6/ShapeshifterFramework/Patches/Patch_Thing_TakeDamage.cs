// ShapeshifterFramework | Patches | Patch_Thing_TakeDamage.cs
// 목적 : 변신 중 맨손 공격 시 상처 라벨의 종족명을 폼 이름으로 교체.
// 용도 : 바닐라는 DamageInfo.Weapon = CasterPawn.def (예: Human)로 설정하여
//        상처 라벨이 "인간 teeth"로 표시됨. 변신 중이면 weapon을 null로 비워
//        바닐라 DamageWorker_AddInjury가 sourceLabel에 빈 문자열을 넣도록 한 후,
//        Postfix에서 sourceLabel을 form.label로 교체 → "bear form teeth" 형태로 표시.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>TakeDamage Prefix: 변신 중 맨손 공격 시 weaponInt를 null로 비움.</summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    internal static class Patch_Thing_TakeDamage
    {
        private static readonly FieldInfo weaponIntField = AccessTools.Field(typeof(DamageInfo), "weaponInt");

        static Patch_Thing_TakeDamage()
        {
            if (weaponIntField == null)
                Log.Warning("[SSF] Reflection failed: DamageInfo.weaponInt not found. TakeDamage wound-label patch disabled.");
        }

        static void Prefix(ref DamageInfo dinfo)
        {
            if (weaponIntField == null) return;

            var pawn = dinfo.Instigator as Pawn;
            if (pawn == null) return;

            if (!ShapeshiftRegistry.TryGet(pawn, out _, out var form)) return;
            if (form.tools == null || form.tools.Count == 0) return;

            // 장비로 공격 중이면 건드리지 않음
            if (dinfo.Weapon != null && dinfo.Weapon != pawn.def) return;

            // weaponInt를 null로 비워서 바닐라가 sourceLabel에 빈 문자열을 넣도록 유도
            // → Postfix에서 폼 이름으로 교체
            object boxed = dinfo;
            weaponIntField.SetValue(boxed, null);
            dinfo = (DamageInfo)boxed;
        }
    }

    /// <summary>DamageWorker_AddInjury Postfix: 상처의 sourceLabel을 폼 이름으로 교체.</summary>
    [HarmonyPatch]
    internal static class Patch_DamageWorker_AddInjury_SourceLabel
    {
        // DamageWorker_AddInjury.ApplyToPawn은 private — 리플렉션으로 타겟 지정
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(DamageWorker_AddInjury), "ApplyToPawn");
        }

        [HarmonyPostfix]
        static void Postfix(DamageInfo dinfo, Pawn pawn)
        {
            // dinfo.Instigator가 변신 중인 폰인지 확인
            var attacker = dinfo.Instigator as Pawn;
            if (attacker == null) return;

            if (!ShapeshiftRegistry.TryGet(attacker, out _, out var form)) return;
            if (form.tools == null || form.tools.Count == 0) return;

            // 장비 공격이면 스킵
            if (dinfo.Weapon != null && dinfo.Weapon != attacker.def) return;

            // 폼 라벨을 상처 소스로 사용
            string formLabel = form.LabelCap.NullOrEmpty() ? form.defName : form.LabelCap.Resolve();

            // 이번 타격으로 추가된 부상 hediff의 sourceLabel을 폼 이름으로 교체
            if (pawn.health?.hediffSet?.hediffs == null) return;
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i] is Hediff_Injury injury && string.IsNullOrEmpty(injury.sourceLabel))
                {
                    injury.sourceLabel = formLabel;
                }
            }
        }
    }
}
