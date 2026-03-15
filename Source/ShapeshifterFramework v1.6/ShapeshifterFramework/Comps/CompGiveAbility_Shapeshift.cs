// ShapeshifterFramework | Comps | CompGiveAbility_Shapeshift.cs
// 목적 : 아이템(ThingDef)에 부착하여, 소지/장비 시 폰에 AbilityDef를 부여하는 범용 ThingComp.
// 용도 : requireEquipped=false → 인벤토리 소지만으로 어빌리티 부여. requireEquipped=true → 착용/장착 시에만 부여.
//        아이템이 드롭/해제되면 어빌리티 자동 회수.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 소지/장비 시 어빌리티를 부여하는 ThingComp.</summary>
    public class CompGiveAbility_Shapeshift : ThingComp
    {
        public CompProperties_GiveAbility_Shapeshift Props => (CompProperties_GiveAbility_Shapeshift)props;

        private Pawn boundPawn;

        public override void CompTick()
        {
            base.CompTick();
            // 매 60틱마다 체크 (성능 최적화)
            if (parent.IsHashIntervalTick(60))
                UpdateAbilityGrant();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            UpdateAbilityGrant();
        }

        /// <summary>현재 소유자 Pawn을 확인하고 어빌리티 부여/회수.</summary>
        private void UpdateAbilityGrant()
        {
            if (Props.ability == null) return;

            Pawn currentOwner = GetCurrentOwner();

            if (currentOwner != boundPawn)
            {
                // 이전 소유자에서 회수
                TryRevokeAbility(boundPawn);
                boundPawn = null;

                // 새 소유자에게 부여
                if (currentOwner != null)
                {
                    TryGrantAbility(currentOwner);
                    boundPawn = currentOwner;
                }
            }
        }

        /// <summary>현재 이 아이템을 소유(또는 장비)한 Pawn 반환.</summary>
        private Pawn GetCurrentOwner()
        {
            if (parent == null || parent.Destroyed) return null;

            // 장비로 착용/장착 중인 경우
            if (parent.ParentHolder is Pawn_ApparelTracker apparelTracker)
                return apparelTracker.pawn;
            if (parent.ParentHolder is Pawn_EquipmentTracker equipTracker)
                return equipTracker.pawn;

            // requireEquipped=true면 장비 착용 시에만 부여
            if (Props.requireEquipped)
                return null;

            // 인벤토리에 있는 경우
            if (parent.ParentHolder is Pawn_InventoryTracker invTracker)
                return invTracker.pawn;

            return null;
        }

        private void TryGrantAbility(Pawn pawn)
        {
            if (pawn?.abilities == null || Props.ability == null) return;
            if (pawn.abilities.GetAbility(Props.ability) != null) return; // 이미 있음
            pawn.abilities.GainAbility(Props.ability);
        }

        private void TryRevokeAbility(Pawn pawn)
        {
            if (pawn?.abilities == null || Props.ability == null) return;
            if (pawn.abilities.GetAbility(Props.ability) == null) return; // 없음
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
