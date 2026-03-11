// ShapeshifterFramework | Comps | CompProperties_GiveAbility_SSF.cs
// 목적 : 아이템(ThingDef)에 부착하여, 소지/장비 시 폰에 AbilityDef를 부여하는 범용 ThingComp 속성.
// 용도 : 바닐라에는 장비→어빌리티 부여 패턴이 없으므로, HediffComp_GiveAbility 패턴을 차용하여 직접 구현.
// _SSF 접미어: 림월드 에러로그/디버그인스펙터에서 네임스페이스가 잘리므로 프레임워크 소속을 명시.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 소지/장비 시 어빌리티를 부여하는 ThingComp 속성.</summary>
    public class CompProperties_GiveAbility_SSF : CompProperties
    {
        /// <summary>부여할 어빌리티.</summary>
        public AbilityDef ability;

        /// <summary>true: 장비(착용/장착) 시에만 부여. false: 인벤토리 소지만으로 부여.</summary>
        public bool requireEquipped = false;

        public CompProperties_GiveAbility_SSF()
        {
            compClass = typeof(CompGiveAbility_SSF);
        }
    }
}
