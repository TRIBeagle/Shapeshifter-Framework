// ShapeshifterFramework | Hediffs | HediffComp_PermanentTransform.cs
// 목적 : hediff severity가 임계값 도달 시 폰을 동물 또는 Thing으로 영구 전환.
// 용도 : 변신 중독/저주 등의 최종 단계. 되돌릴 수 없음.
//        animalKind 지정 시 → 해당 동물 폰 스폰 (콜로니스트 → 길들여진, 기타 → 야생)
//        thingDef 지정 시 → 해당 Thing 스폰 (치즈/조각상 등 폰→비폰 전환)
//        둘 다 지정 시 animalKind 우선.
// 주의 : keepName/keepRelations 미사용 시 원본 폰의 모든 데이터(스킬/기억/관계)가 소실됨.

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

        /// <summary>원본 폰의 이름을 전환된 동물에 이전. thingDef 전환 시 무시.</summary>
        public bool keepName = true;

        /// <summary>원본 폰의 사회적 관계를 전환된 동물에 이전. thingDef 전환 시 무시.</summary>
        public bool keepRelations = true;

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
    }

    /// <summary>severity 임계값 도달 시 폰 → 동물/Thing 영구 전환.</summary>
    public class HediffComp_PermanentTransform : HediffComp
    {
        public HediffCompProperties_PermanentTransform Props => (HediffCompProperties_PermanentTransform)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Props.animalKind == null && Props.thingDef == null) return;
            if (parent.Severity < Props.severityThreshold) return;

            var pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed) return;

            // 60틱 간격으로 검사
            if (!pawn.IsHashIntervalTick(60)) return;

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

            // 원본 폰 제거
            if (!pawn.Dead)
                pawn.Kill(null);
            if (!pawn.Destroyed && pawn.Corpse != null)
                pawn.Corpse.Destroy();
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

            // 이름 이전
            if (Props.keepName && original.Name != null)
                animal.Name = original.Name;

            // 관계 이전
            if (Props.keepRelations && original.relations != null)
                TransferRelations(original, animal);

            if (map != null && pos.IsValid)
                GenSpawn.Spawn(animal, pos, map);

            return animal.LabelShortCap;
        }

        /// <summary>Thing 스폰 (비폰 전환).</summary>
        private string SpawnThing(Map map, IntVec3 pos)
        {
            Thing thing = ThingMaker.MakeThing(Props.thingDef);
            thing.stackCount = Props.thingCount;
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
