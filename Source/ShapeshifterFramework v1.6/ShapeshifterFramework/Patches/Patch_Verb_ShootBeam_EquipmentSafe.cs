// .NET Framework 4.8 / C# 7.3
// Patch_Verb_ShootBeam_EquipmentSafe.cs
// 목적: 변신 폼처럼 장비 없이(EquipmentSource=null) Verb_ShootBeam 사용 시
//       ApplyDamage 내부의 base.EquipmentSource.def 접근으로 인한 NRE를 예방.
// 방식: Prefix로 원본 ApplyDamage를 대체하여 null-safe로 동일 동작 수행.

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch]
    public static class Patch_Verb_ShootBeam_EquipmentSafe
    {
        // private void ApplyDamage(Thing thing, IntVec3 sourceCell, float damageFactor = 1f)
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Verb_ShootBeam),
                "ApplyDamage",
                new Type[] { typeof(Thing), typeof(IntVec3), typeof(float) }
            );
        }

        static bool Prefix(Verb_ShootBeam __instance, Thing thing, IntVec3 sourceCell, float damageFactor)
        {
            try
            {
                if (thing == null) return false;

                // 필요한 필드/프로퍼티 접근
                var verbProps = __instance.verbProps;                 // VerbProperties
                var caster = __instance.Caster;                    // Thing
                var map = caster?.Map;
                if (map == null || verbProps == null) return false;

                // 원본: InterpolatedPosition 기반 LOS 보정
                IntVec3 intVec = __instance.InterpolatedPosition.Yto0().ToIntVec3();
                IntVec3 intVec2 = GenSight.LastPointOnLineOfSight(
                    sourceCell, intVec,
                    (IntVec3 c) => c.InBounds(map) && c.CanBeSeenOverFast(map),
                    skipFirstCell: true
                );
                if (intVec2.IsValid) intVec = intVec2;

                // 필요한 값 체크
                if (verbProps.beamDamageDef == null) return false;

                // 무기(def) null-safe
                ThingDef equipmentDef = __instance.EquipmentSource != null ? __instance.EquipmentSource.def : null;

                // 로그/각도
                float angleFlat = (__instance.CurrentTarget.Cell - caster.Position).AngleFlat;
                var log = new BattleLogEntry_RangedImpact(caster, thing, __instance.CurrentTarget.Thing, equipmentDef, null, null);

                // pathCells.Count 접근(사설 필드)
                int pathCount = 1;
                var fPathCells = AccessTools.Field(typeof(Verb_ShootBeam), "pathCells");
                var pathCellsObj = fPathCells?.GetValue(__instance) as System.Collections.ICollection;
                if (pathCellsObj != null && pathCellsObj.Count > 0) pathCount = pathCellsObj.Count;

                // DamageInfo 구성(원본과 동일, 단 equipmentDef null-safe)
                DamageInfo dinfo;
                if (verbProps.beamTotalDamage > 0f)
                {
                    float perCell = verbProps.beamTotalDamage / (float)pathCount;
                    perCell *= damageFactor;
                    dinfo = new DamageInfo(
                        verbProps.beamDamageDef, perCell, verbProps.beamDamageDef.defaultArmorPenetration,
                        angleFlat, caster, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown,
                        __instance.CurrentTarget.Thing
                    );
                }
                else
                {
                    float amount = (float)verbProps.beamDamageDef.defaultDamage * damageFactor;
                    dinfo = new DamageInfo(
                        verbProps.beamDamageDef, amount, verbProps.beamDamageDef.defaultArmorPenetration,
                        angleFlat, caster, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown,
                        __instance.CurrentTarget.Thing
                    );
                }

                thing.TakeDamage(dinfo).AssociateWithLog(log);

                // 화재 처리(원본과 동일 로직; chance 커브 null-safe)
                if (thing.CanEverAttachFire())
                {
                    float chance = (verbProps.flammabilityAttachFireChanceCurve == null)
                        ? verbProps.beamChanceToAttachFire
                        : verbProps.flammabilityAttachFireChanceCurve.Evaluate(thing.GetStatValue(StatDefOf.Flammability));
                    if (Rand.Chance(chance))
                        thing.TryAttachFire(verbProps.beamFireSizeRange.RandomInRange, caster);
                }
                else if (Rand.Chance(verbProps.beamChanceToStartFire))
                {
                    FireUtility.TryStartFireIn(intVec, map, verbProps.beamFireSizeRange.RandomInRange, caster, verbProps.flammabilityAttachFireChanceCurve);
                }

                // 원본 스킵
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"[SSF] ShootBeam safe patch failed: {e}");
                // 오류 시 원본 호출(최후의 수단)
                return true;
            }
        }
    }
}
