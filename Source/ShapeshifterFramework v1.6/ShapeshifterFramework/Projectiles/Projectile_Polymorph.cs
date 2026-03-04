// ShapeshifterFramework | Projectiles | Projectile_Polymorph.cs
// 목적 : PolymorphProjectileExtension의 설정을 읽어, 피격 대상을 실제로 변신시키는 커스텀 투사체 로직.
// 용도 : 투사체 명중(Impact) 시 방패(Shield)에 막혔는지 1차 검증 후, 직격 대상 및 aoeRadius 반경 내의 모든 살아있는 폰(Pawn)을 탐색(GenRadial)하여 ShapeshiftTargetUtility.TryShiftPawn을 호출함.
// 주의 : 투사체의 기본 이펙트나 파괴 로직을 정상 수행하기 위해, 변신 처리가 끝난 후 마지막에 반드시 base.Impact()를 호출하도록 설계됨.

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
                    if (t != hitThing && t is Pawn pp && !pp.Dead)
                        ShapeshiftTargetUtility.TryShiftPawn(pp, ext.formDefName, ext.successChance);
                }
            }

            // 마지막에 원본 Impact 처리(사운드/이펙트/파괴)
            base.Impact(hitThing, blockedByShield);
        }
    }
}
