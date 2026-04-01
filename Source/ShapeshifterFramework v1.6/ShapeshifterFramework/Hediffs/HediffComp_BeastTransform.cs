// ShapeshifterFramework | Hediffs | HediffComp_BeastTransform.cs
// 목적 : hediff severity가 임계값 도달 시 폰을 동물로 영구 전환 (짐승화).
// 용도 : 변신 중독/저주 오염 등의 후유증으로 폰이 완전히 동물이 되는 최종 단계.
//        severity 1.0 도달 시 폰을 제거하고 해당 PawnKindDef 동물을 스폰.
//        콜로니스트 → 길들여진 상태, 기타 → 야생 동물.
// 주의 : Thing 전환이므로 원본 폰의 모든 데이터(스킬/기억/관계)가 소실됨. 되돌릴 수 없음.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    public class HediffCompProperties_BeastTransform : HediffCompProperties
    {
        /// <summary>전환할 동물 PawnKindDef. null이면 전환하지 않음.</summary>
        public PawnKindDef animalKind;

        /// <summary>전환 발동 severity 임계값. 기본 1.0.</summary>
        public float severityThreshold = 1f;

        /// <summary>전환 시 레터 발송 여부.</summary>
        public bool sendLetter = true;

        /// <summary>레터 타이틀 번역 키.</summary>
        public string letterTitleKey = "SSF_BeastTransform_LetterTitle";

        /// <summary>레터 본문 번역 키. {0} = 폰 이름, {1} = 동물 이름.</summary>
        public string letterTextKey = "SSF_BeastTransform_LetterText";

        public HediffCompProperties_BeastTransform()
        {
            compClass = typeof(HediffComp_BeastTransform);
        }
    }

    /// <summary>severity 임계값 도달 시 폰 → 동물 영구 전환.</summary>
    public class HediffComp_BeastTransform : HediffComp
    {
        public HediffCompProperties_BeastTransform Props => (HediffCompProperties_BeastTransform)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Props.animalKind == null) return;
            if (parent.Severity < Props.severityThreshold) return;

            var pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed) return;

            // 60틱 간격으로 검사 (매 틱 불필요)
            if (!pawn.IsHashIntervalTick(60)) return;

            ExecuteTransform(pawn);
        }

        /// <summary>폰을 동물로 전환.</summary>
        private void ExecuteTransform(Pawn pawn)
        {
            var map = pawn.MapHeld;
            var pos = pawn.PositionHeld;
            var faction = pawn.Faction;
            bool wasColonist = pawn.IsColonist;
            string pawnName = pawn.LabelShortCap;

            // 변신 중이면 먼저 해제
            if (Utilities.ShapeshiftCoreUtility.TryGetCore(pawn, out var core))
            {
                try { core.RemoveForm(); }
                catch (System.Exception ex) { Log.Warning($"[SSF] BeastTransform: RemoveForm failed: {ex.Message}"); }
            }

            // 동물 스폰
            Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                Props.animalKind,
                faction: wasColonist ? faction : null, // 콜로니스트 → 길들여진 상태
                context: PawnGenerationContext.NonPlayer,
                tile: map?.Tile ?? -1,
                forceGenerateNewPawn: true
            ));

            if (map != null && pos.IsValid)
            {
                GenSpawn.Spawn(animal, pos, map);
            }

            // 레터 발송
            if (Props.sendLetter && map != null)
            {
                string title = Props.letterTitleKey.Translate();
                string text = Props.letterTextKey.Translate(pawnName, animal.LabelShortCap);
                Find.LetterStack.ReceiveLetter(title, text,
                    wasColonist ? LetterDefOf.NegativeEvent : LetterDefOf.NeutralEvent,
                    new TargetInfo(pos, map));
            }

            // 원본 폰 제거
            if (!pawn.Dead)
            {
                pawn.Kill(null);
            }
            if (!pawn.Destroyed && pawn.Corpse != null)
            {
                pawn.Corpse.Destroy();
            }
        }
    }
}
