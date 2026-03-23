// ShapeshifterFramework | Comps | IngestionOutcomeDoer_ExtendShapeshift.cs
// 목적 : 약물 섭취 시 현재 변신의 지속 시간을 연장하는 IngestionOutcomeDoer.
// 용도 : 변신 중인 폰이 복용하면 ExtendDuration 호출. 변신 중이 아니면 메시지 표시 후 무효.
//        targetFormDef를 지정하면 해당 폼일 때만 연장, 미지정이면 어떤 폼이든 연장.
// XML 사용 예:
//   <outcomeDoers>
//     <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_ExtendShapeshift">
//       <extendTicks>30000</extendTicks>
//       <targetFormDef>SSFTest_BearForm</targetFormDef>          <!-- 선택: 특정 폼 한정 -->
//       <allowExtendBeyondMax>false</allowExtendBeyondMax>       <!-- 선택: 최대 시간 초과 허용 -->
//     </li>
//   </outcomeDoers>

using RimWorld;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>약물 섭취 시 변신 지속 시간을 연장하는 IngestionOutcomeDoer.</summary>
    public class IngestionOutcomeDoer_ExtendShapeshift : IngestionOutcomeDoer
    {
        /// <summary>연장할 틱 수 (양수: 연장, 음수: 단축).</summary>
        public int extendTicks;

        /// <summary>특정 폼에서만 연장 허용. null이면 현재 폼 종류 무관.</summary>
        public string targetFormDef;

        /// <summary>true면 원래 폼 최대 시간을 초과하여 연장 가능. 기본 false.</summary>
        public bool allowExtendBeyondMax;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (pawn == null || pawn.Dead) return;

            if (extendTicks == 0)
            {
                Log.Error("[SSF] IngestionOutcomeDoer_ExtendShapeshift: extendTicks가 0입니다.");
                return;
            }

            // 변신 중이 아니면 차단
            if (!ShapeshiftCoreUtility.TryGetCore(pawn, out var core) || !core.isTransformed)
            {
                Messages.Message("SSF_Message_NotTransformed".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // targetFormDef 지정 시 현재 폼과 일치하는지 확인
            if (!targetFormDef.NullOrEmpty() && core.currentForm?.defName != targetFormDef)
            {
                Messages.Message("SSF_Message_WrongFormForExtend".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            core.ExtendDuration(extendTicks, allowExtendBeyondMax);
            Messages.Message("SSF_Message_DurationExtended".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
