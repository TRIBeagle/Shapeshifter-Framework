// ShapeshifterFramework | Comps | IngestionOutcomeDoer_Shapeshift.cs
// 목적 : 약물(Drug) 섭취 시 폰을 변신시키는 IngestionOutcomeDoer 확장.
// 용도 : 바닐라의 IngestionOutcomeDoer를 상속하여, 약물 XML의 <outcomeDoers>에서 사용.
//        섭취 완료 시 hediffDef가 있으면 ShapeshiftCoreUtility.ApplyShift, 없으면 ShapeshiftTargetUtility.TryShiftPawn(formDefName) 폴백.
// XML 사용 예:
//   <outcomeDoers>
//     <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_Shapeshift">
//       <formDefName>Polymorph_Sheep</formDefName>
//       <successChance>0.85</successChance>
//     </li>
//   </outcomeDoers>

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    public class IngestionOutcomeDoer_Shapeshift : IngestionOutcomeDoer
    {
        public string formDefName;
        /// <summary>HediffDef 기반 변신 (Phase 2). hediffDef가 지정되면 formDefName보다 우선 사용.</summary>
        public HediffDef hediffDef;
        public float successChance = 1f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (pawn == null || pawn.Dead) return;

            // hediffDef 경로 우선
            if (hediffDef != null)
            {
                ShapeshiftCoreUtility.ApplyShift(pawn, hediffDef, successChance);
                return;
            }

            // formDefName 폴백
            if (string.IsNullOrEmpty(formDefName))
            {
                Log.Warning("[SSF] IngestionOutcomeDoer_Shapeshift: formDefName is null or empty.");
                return;
            }
            ShapeshiftTargetUtility.TryShiftPawn(pawn, formDefName, successChance);
        }
    }
}
