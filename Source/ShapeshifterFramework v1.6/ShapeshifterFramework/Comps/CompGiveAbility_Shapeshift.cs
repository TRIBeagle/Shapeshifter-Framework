// ShapeshifterFramework | Comps | CompGiveAbility_Shapeshift.cs
// 목적 : 아이템(ThingDef)에 부착하여, 장비(착용/장착) 시 폰에 AbilityDef를 부여하는 ThingComp.
// 용도 : 착용/장착 시 어빌리티 부여, 해제/드롭 시 자동 회수.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 장비 시 어빌리티를 부여하는 ThingComp.</summary>
    public class CompGiveAbility_Shapeshift : ThingComp
    {
        public CompProperties_GiveAbility_Shapeshift Props => (CompProperties_GiveAbility_Shapeshift)props;

        private Pawn boundPawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref boundPawn, "boundPawn");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(60))
                UpdateAbilityGrant();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            UpdateAbilityGrant();
        }

        /// <summary>현재 장비 소유자 확인 후 어빌리티 부여/회수.</summary>
        private void UpdateAbilityGrant()
        {
            if (Props.ability == null) return;

            Pawn currentOwner = GetEquippedOwner();

            if (currentOwner != boundPawn)
            {
                TryRevokeAbility(boundPawn);
                boundPawn = null;

                if (currentOwner != null)
                {
                    TryGrantAbility(currentOwner);
                    boundPawn = currentOwner;
                }
            }
        }

        /// <summary>이 아이템을 장비(착용/장착)한 Pawn 반환. 인벤토리는 무시.</summary>
        private Pawn GetEquippedOwner()
        {
            if (parent == null || parent.Destroyed) return null;

            if (parent.ParentHolder is Pawn_ApparelTracker apparelTracker)
                return apparelTracker.pawn;
            if (parent.ParentHolder is Pawn_EquipmentTracker equipTracker)
                return equipTracker.pawn;

            return null;
        }

        private void TryGrantAbility(Pawn pawn)
        {
            if (pawn?.abilities == null || Props.ability == null) return;
            if (pawn.abilities.GetAbility(Props.ability) != null) return;
            pawn.abilities.GainAbility(Props.ability);
        }

        private void TryRevokeAbility(Pawn pawn)
        {
            if (pawn?.abilities == null || Props.ability == null) return;
            if (pawn.abilities.GetAbility(Props.ability) == null) return;
            pawn.abilities.RemoveAbility(Props.ability);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            TryRevokeAbility(boundPawn);
            boundPawn = null;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            TryRevokeAbility(boundPawn);
            boundPawn = null;
        }
    }
}
