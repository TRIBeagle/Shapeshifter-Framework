// ShapeshifterFramework | Comps | IngestionOutcomeDoer_Shapeshift.cs
// 목적 : 약물(Drug) 섭취 시 폰을 변신시키는 IngestionOutcomeDoer 확장.
// 용도 : 바닐라의 IngestionOutcomeDoer를 상속하여, 약물 XML의 <outcomeDoers>에서 사용.
//        섭취 완료 시 hediffDef 기반 GiveShiftHediff 호출.
// XML 사용 예:
//   <outcomeDoers>
//     <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_Shapeshift">
//       <hediffDef>Hediff_Polymorph_Sheep</hediffDef>
//     </li>
//   </outcomeDoers>

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    public class IngestionOutcomeDoer_Shapeshift : IngestionOutcomeDoer
    {
        /// <summary>변신 적용에 사용할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</summary>
        public HediffDef hediffDef;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (pawn == null || pawn.Dead) return;

            if (hediffDef == null)
            {
                Log.Error("[SSF] IngestionOutcomeDoer_Shapeshift: hediffDef가 지정되지 않았습니다.");
                return;
            }
            ShapeshiftCoreUtility.GiveShiftHediff(pawn, hediffDef);
        }
    }
}
