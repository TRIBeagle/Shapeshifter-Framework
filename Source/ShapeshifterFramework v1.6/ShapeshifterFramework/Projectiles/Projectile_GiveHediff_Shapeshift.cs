// ShapeshifterFramework | Projectiles | Projectile_GiveHediff_Shapeshift.cs
// 목적 : GiveHediffProjectileExtension_Shapeshift의 설정을 읽어, 피격 대상에게 HediffDef를 부여하는 커스텀 투사체 로직.
// 용도 : 투사체 명중(Impact) 시 방패(Shield)에 막혔는지 1차 검증 후, 직격 대상 및 aoeRadius 반경 내의 모든 살아있는 폰(Pawn)을 탐색(GenRadial)하여 hediff를 부여함.
//        hediffDef 기반 GiveShiftHediff 호출.
// 주의 : 투사체의 기본 이펙트나 파괴 로직을 정상 수행하기 위해, 변신 처리가 끝난 후 마지막에 반드시 base.Impact()를 호출하도록 설계됨.
// AoE 팩션 필터: affectAllies=false(기본값)이면 시전자에게 적대적인 폰만 적용. true이면 모든 폰.

using RimWorld;
using ShapeshifterFramework.Extensions;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Projectiles
{
    public class Projectile_GiveHediff_Shapeshift : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // 방패에 막힌 경우: 효과 없이 종료
            if (blockedByShield)
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            // 1) XML 확장 읽기 (hediffDef 필수)
            var ext = def?.GetModExtension<GiveHediffProjectileExtension_Shapeshift>();
            if (ext == null || ext.hediffDef == null)
            {
                Log.Warning("[SSF] GiveHediff projectile missing extension or hediffDef.");
                base.Impact(hitThing, blockedByShield);
                return;
            }

            var casterPawn = Launcher as Pawn;

            // 2) 직격 대상 처리
            if (hitThing is Pawn p && !p.Dead)
            {
                ApplyHediffToTarget(p, ext);
            }

            // 3) AoE 처리 — 팩션 필터 적용
            if (ext.aoeRadius > 0.01f && Map != null)
            {
                foreach (var t in GenRadial.RadialDistinctThingsAround(Position, Map, ext.aoeRadius, true))
                {
                    if (t == hitThing) continue;
                    var pp = t as Pawn;
                    if (pp == null || pp.Dead)
                        continue;

                    // 팩션 필터: affectAllies=false일 때, 시전자와 적대 관계가 아니면 스킵
                    if (!ext.affectAllies && casterPawn != null && !pp.HostileTo(casterPawn))
                        continue;

                    ApplyHediffToTarget(pp, ext);
                }
            }

            // 마지막에 원본 Impact 처리(사운드/이펙트/파괴)
            base.Impact(hitThing, blockedByShield);
        }

        /// <summary>확장 설정에 따라 대상에게 hediff 부여.</summary>
        private static void ApplyHediffToTarget(Pawn target, GiveHediffProjectileExtension_Shapeshift ext)
        {
            ShapeshiftCoreUtility.GiveShiftHediff(target, ext.hediffDef);
        }
    }
}
