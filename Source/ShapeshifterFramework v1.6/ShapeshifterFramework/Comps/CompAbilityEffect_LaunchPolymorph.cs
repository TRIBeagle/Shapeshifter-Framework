// ShapeshifterFramework | Comps | CompAbilityEffect_LaunchPolymorph.cs
// 목적 : Ability 시전 시 폴리모프 투사체를 직접 생성·발사하는 효과 컴포넌트.
// 용도 : Verb_CastAbility는 Verb_LaunchProjectile이 아니라 defaultProjectile을 발사하지 않음.
//        이 컴포넌트가 Apply() 시점에 투사체를 직접 스폰하여 타겟으로 발사함.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>Ability로 폴리모프 투사체를 발사.</summary>
    public class CompAbilityEffect_LaunchPolymorph : CompAbilityEffect
    {
        public new CompProperties_AbilityLaunchPolymorph Props
            => (CompProperties_AbilityLaunchPolymorph)props;

        private ThingDef _cachedProjectileDef;
        private bool _resolved;

        private ThingDef ProjectileDef
        {
            get
            {
                if (!_resolved)
                {
                    _cachedProjectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.projectileDefName);
                    _resolved = true;
                }
                return _cachedProjectileDef;
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && ProjectileDef != null;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var caster = parent.pawn;
            if (caster == null || caster.Map == null) return;

            var projDef = ProjectileDef;
            if (projDef == null)
            {
                Log.Warning("[SSF] CompAbilityEffect_LaunchPolymorph: missing projectile def: " + Props.projectileDefName);
                return;
            }

            var proj = (Projectile)GenSpawn.Spawn(projDef, caster.Position, caster.Map);
            proj.Launch(caster, caster.DrawPos, target, target, ProjectileHitFlags.All);
        }
    }
}
