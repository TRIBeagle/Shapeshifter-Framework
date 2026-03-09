// ShapeshifterFramework | Comps | CompProperties_AbilityLaunchPolymorph.cs
// 목적 : Ability 시전 시 폴리모프 투사체를 직접 발사하는 효과 속성.
// 용도 : Verb_CastAbility는 defaultProjectile을 사용하지 않으므로, CompAbilityEffect에서 투사체를 직접 스폰·발사해야 함.

using RimWorld;

namespace ShapeshifterFramework.Comps
{
    /// <summary>Ability 폴리모프 투사체 발사 속성.</summary>
    public class CompProperties_AbilityLaunchPolymorph : CompProperties_AbilityEffect
    {
        // 발사할 투사체 ThingDef의 defName
        public string projectileDefName;

        public CompProperties_AbilityLaunchPolymorph()
        {
            compClass = typeof(CompAbilityEffect_LaunchPolymorph);
        }
    }
}
