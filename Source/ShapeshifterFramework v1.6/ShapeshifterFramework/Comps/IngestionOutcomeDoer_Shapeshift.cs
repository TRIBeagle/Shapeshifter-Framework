// ShapeshifterFramework | Comps | IngestionOutcomeDoer_Shapeshift.cs
// 목적 : 약물(Drug) 섭취 시 폰을 변신시키는 IngestionOutcomeDoer 확장.
// 용도 : 바닐라의 IngestionOutcomeDoer를 상속하여, 약물 XML의 <outcomeDoers>에서 사용.
//        섭취 완료 시 hediffDef 기반 GiveShiftHediff 호출.
// 주의 : 이미 다른 폼 변신 중이면 방어적으로 변신 무효화. 이데올로기 차단은 FloatMenu 패치에서 담당 (수술 투여 허용을 위해 여기서는 미체크).
// XML 사용 예:
//   <outcomeDoers>
//     <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_Shapeshift">
//       <hediffDef>Hediff_Polymorph_Sheep</hediffDef>
//     </li>
//   </outcomeDoers>

using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Comps
{
    public class IngestionOutcomeDoer_Shapeshift : IngestionOutcomeDoer
    {
        /// <summary>변신 적용에 사용할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</summary>
        public HediffDef hediffDef;

        /// <summary>이미 변신 중이더라도 전환을 허용할 소스 폼 defName 목록.
        /// null/빈 목록이면 변신 중 섭취 불가 (기본 동작).</summary>
        public List<string> allowedFromForms;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (pawn == null || pawn.Dead) return;

            if (hediffDef == null)
            {
                Log.Error("[SSF] IngestionOutcomeDoer_Shapeshift: hediffDef가 지정되지 않았습니다.");
                return;
            }

            // 이미 변신 중이면 차단 (동일 폼 포함, allowedFromForms 예외)
            // 이데올로기 차단은 여기서 하지 않음 — 수술(Recipe_AdministerIngestible) 투여 시
            // pawn이 환자(수신자)이므로, 타인에 의한 강제 변신까지 차단하게 됨.
            // 자가 섭취 차단은 FloatMenu 패치(Patch_IngestIdeologyBlock)에서 담당.
            if (ShapeshiftEligibility.IsAlreadyTransformed(pawn)
                && !ShapeshiftEligibility.IsFormTransitionAllowed(pawn, allowedFromForms))
            {
                Messages.Message("SSF_Message_AlreadyTransformed".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            ShapeshiftCoreUtility.GiveShiftHediff(pawn, hediffDef);
        }
    }
}
