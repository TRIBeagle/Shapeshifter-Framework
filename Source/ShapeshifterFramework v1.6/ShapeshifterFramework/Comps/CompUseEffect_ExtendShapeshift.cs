// ShapeshifterFramework | Comps | CompUseEffect_ExtendShapeshift.cs
// 목적 : 아이템(Use) 사용 시 현재 변신의 지속 시간을 연장하는 CompUseEffect.
// 용도 : 변신 중인 폰이 사용하면 ExtendDuration 호출. 변신 중이 아니면 사용 불가.
//        targetFormDef를 지정하면 해당 폼일 때만 연장, 미지정이면 어떤 폼이든 연장.

using RimWorld;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 사용 시 변신 지속 시간을 연장하는 효과.</summary>
    public class CompUseEffect_ExtendShapeshift : CompUseEffect
    {
        public CompProperties_UseEffect_ExtendShapeshift Props
            => (CompProperties_UseEffect_ExtendShapeshift)props;

        /// <summary>사용 가능 여부 판정. 변신 중이 아니거나 폼이 다르면 차단.</summary>
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            if (!ShapeshiftCoreUtility.TryGetCore(pawn, out var core) || !core.isTransformed)
                return "SSF_Message_NotTransformed".Translate(pawn.LabelShort);

            if (!Props.targetFormDef.NullOrEmpty() && core.currentForm?.defName != Props.targetFormDef)
                return "SSF_Message_WrongFormForExtend".Translate(pawn.LabelShort);

            return base.CanBeUsedBy(pawn);
        }

        /// <summary>아이템 사용 시 연장 효과 실행.</summary>
        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);

            if (Props.extendTicks == 0)
            {
                Log.Error("[SSF] CompUseEffect_ExtendShapeshift: extendTicks is 0.");
                return;
            }

            if (!ShapeshiftCoreUtility.TryGetCore(user, out var core) || !core.isTransformed)
            {
                Messages.Message("SSF_Message_NotTransformed".Translate(user.LabelShort), user, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!Props.targetFormDef.NullOrEmpty() && core.currentForm?.defName != Props.targetFormDef)
            {
                Messages.Message("SSF_Message_WrongFormForExtend".Translate(user.LabelShort), user, MessageTypeDefOf.RejectInput, false);
                return;
            }

            core.ExtendDuration(Props.extendTicks, Props.allowExtendBeyondMax);
            Messages.Message("SSF_Message_DurationExtended".Translate(user.LabelShort), user, MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
