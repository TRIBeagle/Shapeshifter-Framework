using ShapeshifterFramework.Extensions;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Projectiles
{
    public class Projectile_Polymorph : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // 방패에 막힌 경우: 효과 없이 종료
            if (blockedByShield)
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            // 1) XML 확장 읽기 (필수: formDefName)
            var ext = def?.GetModExtension<PolymorphProjectileExtension>();
            if (ext == null || string.IsNullOrEmpty(ext.formDefName))
            {
                Log.Warning("[SSF] Polymorph projectile missing extension or formDefName.");
                base.Impact(hitThing, blockedByShield);
                return;
            }

            // 2) 직격 대상 처리
            if (hitThing is Pawn p && !p.Dead)
            {
                ShapeshiftTargetUtility.TryShiftPawn(p, ext.formDefName, ext.successChance);
            }

            // 3) AoE 처리(있다면) — base.Impact() 호출 전에 Map 사용
            if (ext.aoeRadius > 0.01f && Map != null)
            {
                foreach (var t in GenRadial.RadialDistinctThingsAround(Position, Map, ext.aoeRadius, true))
                {
                    if (t is Pawn pp && !pp.Dead)
                        ShapeshiftTargetUtility.TryShiftPawn(pp, ext.formDefName, ext.successChance);
                }
            }

            // 마지막에 원본 Impact 처리(사운드/이펙트/파괴)
            base.Impact(hitThing, blockedByShield);
        }
    }
}
