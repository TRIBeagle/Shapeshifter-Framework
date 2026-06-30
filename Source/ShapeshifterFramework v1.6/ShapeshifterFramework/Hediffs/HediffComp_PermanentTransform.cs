// ShapeshifterFramework | Hediffs | HediffComp_PermanentTransform.cs
// 목적 : hediff severity가 임계값 도달 시 폰을 동물 또는 Thing으로 영구 전환.
// 용도 : 변신 중독/저주 등의 최종 단계. 되돌릴 수 없음.
//        animalKind 지정 시 → 해당 동물 폰 스폰 (콜로니스트 → 길들여진, 기타 → 야생)
//        thingDef 지정 시 → 해당 Thing 스폰 (치즈/조각상 등 폰→비폰 전환)
//        둘 다 지정 시 animalKind 우선.
// 주의 : 동물 전환 시 이름/관계 자동 이전. Thing 전환 시 모든 데이터 소실.

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    public class HediffCompProperties_PermanentTransform : HediffCompProperties
    {
        /// <summary>전환할 동물 PawnKindDef. null이면 thingDef 사용.</summary>
        public PawnKindDef animalKind;

        /// <summary>전환할 ThingDef (비폰). animalKind가 null일 때 사용.</summary>
        public ThingDef thingDef;

        /// <summary>thingDef 스폰 시 수량. 기본 1.</summary>
        public int thingCount = 1;

        /// <summary>전환 발동 severity 임계값. 기본 1.0.</summary>
        public float severityThreshold = 1f;

        /// <summary>전환 시 레터 발송 여부.</summary>
        public bool sendLetter = true;

        /// <summary>레터 타이틀 번역 키.</summary>
        public string letterTitleKey = "SSF_PermanentTransform_LetterTitle";

        /// <summary>레터 본문 번역 키. {0} = 폰 이름, {1} = 결과물 이름.</summary>
        public string letterTextKey = "SSF_PermanentTransform_LetterText";

        public HediffCompProperties_PermanentTransform()
        {
            compClass = typeof(HediffComp_PermanentTransform);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (var e in base.ConfigErrors(parentDef)) yield return e;
            if (animalKind == null && thingDef == null)
                yield return $"{parentDef.defName}: HediffCompProperties_PermanentTransform — animalKind와 thingDef가 모두 null (전환 대상 없음)";
            if (thingCount <= 0)
                yield return $"{parentDef.defName}: HediffCompProperties_PermanentTransform.thingCount must be > 0 (got {thingCount})";
            if (severityThreshold <= 0f)
                yield return $"{parentDef.defName}: HediffCompProperties_PermanentTransform.severityThreshold should be > 0 (got {severityThreshold})";
        }
    }

    /// <summary>severity 임계값 도달 시 폰 → 동물/Thing 영구 전환.</summary>
    public class HediffComp_PermanentTransform : HediffComp
    {
        public HediffCompProperties_PermanentTransform Props => (HediffCompProperties_PermanentTransform)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // 가장 싼 상수 검사부터: 설정 없으면 즉시 반환
            if (Props.animalKind == null && Props.thingDef == null) return;

            var pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed) return;

            // 60틱 간격 게이트를 severity 검사보다 먼저 — 매 틱 부담 최소화 (동작 동일)
            if (!pawn.IsHashIntervalTick(60)) return;
            if (parent.Severity < Props.severityThreshold) return;

            ExecuteTransform(pawn);
        }

        /// <summary>폰을 동물 또는 Thing으로 전환.</summary>
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
                catch (System.Exception ex) { Log.Warning($"[SSF] PermanentTransform: RemoveForm failed: {ex.Message}"); }
            }

            string resultLabel;

            if (Props.animalKind != null)
            {
                resultLabel = SpawnAnimal(pawn, map, pos, faction, wasColonist);
            }
            else
            {
                resultLabel = SpawnThing(map, pos);
            }

            // 레터 발송
            if (Props.sendLetter && map != null)
            {
                string title = Props.letterTitleKey.Translate();
                string text = Props.letterTextKey.Translate(pawnName, resultLabel);
                Find.LetterStack.ReceiveLetter(title, text,
                    wasColonist ? LetterDefOf.NegativeEvent : LetterDefOf.NeutralEvent,
                    new TargetInfo(pos, map));
            }

            // 원본 폰 제거 — Kill()이 시체 생성/파괴를 내부 처리
            if (!pawn.Dead)
                pawn.Kill(null);
        }

        /// <summary>동물 폰 생성 + 이름/관계 이전.</summary>
        private string SpawnAnimal(Pawn original, Map map, IntVec3 pos, Faction faction, bool wasColonist)
        {
            Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                Props.animalKind,
                faction: wasColonist ? faction : null,
                context: PawnGenerationContext.NonPlayer,
                tile: map?.Tile ?? -1,
                forceGenerateNewPawn: true
            ));

            // 동물 전환: 이름/관계 자동 이전 (동물도 폰이므로 가능)
            if (original.Name != null)
                animal.Name = original.Name;
            if (original.relations != null)
                TransferRelations(original, animal);

            if (map != null && pos.IsValid)
                GenSpawn.Spawn(animal, pos, map);

            return animal.LabelShortCap;
        }

        /// <summary>Thing 스폰 (비폰 전환).</summary>
        private string SpawnThing(Map map, IntVec3 pos)
        {
            var def = Props.thingDef;
            // MadeFromStuff ThingDef(무기/일부 가구)는 stuff 없이 MakeThing 시 예외 → 기본 재료 전달
            Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
            if (thing == null) return def.label ?? def.defName;
            thing.stackCount = System.Math.Min(System.Math.Max(Props.thingCount, 1), def.stackLimit);
            if (map != null && pos.IsValid)
                GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
            return thing.LabelShortCap;
        }

        /// <summary>원본 폰의 직접 관계를 동물에 복사.</summary>
        private static void TransferRelations(Pawn from, Pawn to)
        {
            if (from.relations?.DirectRelations == null) return;

            // 순회 중 수정 방지를 위해 스냅샷
            var snapshot = new List<DirectPawnRelation>(from.relations.DirectRelations);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var rel = snapshot[i];
                if (rel.otherPawn == null || rel.otherPawn.Dead) continue;

                try
                {
                    to.relations.AddDirectRelation(rel.def, rel.otherPawn);
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[SSF] PermanentTransform: relation transfer failed ({rel.def?.defName}): {ex.Message}");
                }
            }
        }
    }
}
