// 목적: EquipmentSource가 null인 Verb_ShootBeam에서 NRE 방지 (base.EquipmentSource.def 접근)
// 근거: 바닐라 1.6 Verb_ShootBeam.pathCells는 HashSet<IntVec3>
// 최적화: pathCells 접근은 FieldRef 우선, 실패 시 FieldInfo 폴백(핫패스에서 예외로 모드 로딩 죽는 것 방지)

using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch]
    internal static class Patch_Verb_ShootBeam_EquipmentSafe
    {
        private static readonly FieldInfo PathCellsFI;
        private static readonly AccessTools.FieldRef<Verb_ShootBeam, HashSet<IntVec3>> PathCellsRef;

        static Patch_Verb_ShootBeam_EquipmentSafe()
        {
            // FieldInfo는 항상 확보 시도
            PathCellsFI = AccessTools.Field(typeof(Verb_ShootBeam), "pathCells");

            // FieldRef는 타입이 정확히 맞을 때만 성공. 실패해도 모드 로딩을 죽이지 않기 위해 try/catch.
            try
            {
                PathCellsRef = AccessTools.FieldRefAccess<Verb_ShootBeam, HashSet<IntVec3>>("pathCells");
            }
            catch
            {
                PathCellsRef = null;
            }
        }

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
                if (__instance == null) return true; // 원본
                if (thing == null) return false;     // 바닐라도 바로 return

                var caster = __instance.Caster;
                if (caster == null) return true;

                var map = caster.Map;
                if (map == null) return true;

                var verbProps = __instance.verbProps;
                if (verbProps == null || verbProps.beamDamageDef == null) return false;

                // 바닐라 동일: LOS 마지막 셀 보정
                IntVec3 intVec = __instance.InterpolatedPosition.Yto0().ToIntVec3();
                IntVec3 intVec2 = GenSight.LastPointOnLineOfSight(
                    sourceCell,
                    intVec,
                    (IntVec3 c) => c.InBounds(map) && c.CanBeSeenOverFast(map),
                    skipFirstCell: true
                );
                if (intVec2.IsValid) intVec = intVec2;

                // NRE 핵심: EquipmentSource가 null일 수 있음
                ThingDef equipmentDef = __instance.EquipmentSource != null ? __instance.EquipmentSource.def : null;

                float angleFlat = (__instance.CurrentTarget.Cell - caster.Position).AngleFlat;
                var log = new BattleLogEntry_RangedImpact(caster, thing, __instance.CurrentTarget.Thing, equipmentDef, null, null);

                int cellCount = 1;
                HashSet<IntVec3> pathCells = null;

                if (PathCellsRef != null)
                {
                    try { pathCells = PathCellsRef(__instance); } catch { pathCells = null; }
                }
                if (pathCells == null && PathCellsFI != null)
                {
                    try { pathCells = PathCellsFI.GetValue(__instance) as HashSet<IntVec3>; } catch { pathCells = null; }
                }

                if (pathCells != null && pathCells.Count > 0) cellCount = pathCells.Count;

                DamageInfo dinfo;
                if (verbProps.beamTotalDamage > 0f)
                {
                    float num = verbProps.beamTotalDamage / (float)cellCount;
                    num *= damageFactor;
                    dinfo = new DamageInfo(
                        verbProps.beamDamageDef,
                        num,
                        verbProps.beamDamageDef.defaultArmorPenetration,
                        angleFlat,
                        caster,
                        null,
                        equipmentDef,
                        DamageInfo.SourceCategory.ThingOrUnknown,
                        __instance.CurrentTarget.Thing
                    );
                }
                else
                {
                    float amount = (float)verbProps.beamDamageDef.defaultDamage * damageFactor;
                    dinfo = new DamageInfo(
                        verbProps.beamDamageDef,
                        amount,
                        verbProps.beamDamageDef.defaultArmorPenetration,
                        angleFlat,
                        caster,
                        null,
                        equipmentDef,
                        DamageInfo.SourceCategory.ThingOrUnknown,
                        __instance.CurrentTarget.Thing
                    );
                }

                thing.TakeDamage(dinfo).AssociateWithLog(log);

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

                return false; // 원본 스킵
            }
            catch (Exception e)
            {
                Log.Error($"[SSF] Patch_Verb_ShootBeam_EquipmentSafe failed: {e}");
                return true; // 예외 시 원본으로 폴백
            }
        }
    }
}
