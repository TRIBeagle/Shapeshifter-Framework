// ShapeshifterFramework | Patches | Patch_Verb_ShootBeam.cs
// 목적 : 무기 없이 맨몸(변신 폼)에서 광선(Beam) 공격을 발사할 때 발생하는 바닐라의 치명적 크래시(NRE)를 방지.
// 용도 : 바닐라 광선 로직은 무조건 무기(EquipmentSource.def)를 참조하려 하므로, 무기가 없을 경우 이를 Prefix로 가로채어 직접 데미지 계산을 수행하고 원본 로직을 스킵(return false)시켜 게임 멈춤을 완벽히 방어함.

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch]
    internal static class Patch_Verb_ShootBeam
    {
        // vanilla-private: Verb_ShootBeam.pathCells (RimWorld 1.6) — 바닐라 버전 변경 시 확인 필요
        private static readonly FieldInfo PathCellsFI;
        private static readonly AccessTools.FieldRef<Verb_ShootBeam, HashSet<IntVec3>> PathCellsRef;

        static Patch_Verb_ShootBeam()
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
                // 바닐라의 ApplyDamage 내부에서 EquipmentSource.def를 참조하므로,
                // EquipmentSource가 null이면 NRE가 발생합니다.
                // 원본 코드를 통째로 복제하지 않고, 해당 상황에서만 실행을 안전하게 취소(또는 커스텀 데미지 적용 후 취소)합니다.
                if (__instance != null && __instance.EquipmentSource == null)
                {
                    if (thing != null && __instance.verbProps != null && __instance.verbProps.beamDamageDef != null)
                    {
                        // pathCells 카운트 획득 시도
                        int cellCount = 1;
                        if (PathCellsRef != null)
                        {
                            var cells = PathCellsRef(__instance);
                            if (cells != null && cells.Count > 0) cellCount = cells.Count;
                        }
                        else if (PathCellsFI != null)
                        {
                            var cells = PathCellsFI.GetValue(__instance) as HashSet<IntVec3>;
                            if (cells != null && cells.Count > 0) cellCount = cells.Count;
                        }
                        // pathCells 접근 실패 시 cellCount=1 폴백 — 데미지가 부정확할 수 있음
                        if (cellCount <= 1 && PathCellsRef == null && PathCellsFI == null)
                            Log.WarningOnce("[SSF] Verb_ShootBeam: pathCells field not accessible. Beam damage may be inaccurate.", 0x5346_4245);

                        float damage = __instance.verbProps.beamTotalDamage > 0
                            ? (__instance.verbProps.beamTotalDamage / cellCount) * damageFactor
                            : __instance.verbProps.beamDamageDef.defaultDamage * damageFactor;

                        DamageInfo dinfo = new DamageInfo(__instance.verbProps.beamDamageDef, damage,
                                                          __instance.verbProps.beamDamageDef.defaultArmorPenetration,
                                                          -1f, __instance.Caster, null, null,
                                                          DamageInfo.SourceCategory.ThingOrUnknown, thing);
                        thing.TakeDamage(dinfo);
                    }
                    return false; // 데미지만 주고 원본 스킵 (NRE 방지)
                }

                return true; // 정상적인 경우 원본 그대로 실행 (타 모드 호환성 보장)
            }
            catch (Exception e)
            {
                Log.Error($"[SSF] Patch_Verb_ShootBeam_EquipmentSafe failed: {e}");
                return true;
            }
        }
    }
}
