// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.ExposeData.cs
// 목적 : 변신 상태의 저장/로드(Scribe) 처리 및 로드 후 초기화(PostLoadInit).
// 용도 : CompExposeData에서 Saving/LoadingVars/PostLoadInit 모드별 직렬화를 수행하고,
//        로드 후 장비 참조 복원(needsGearResolve) 및 고아 데이터 정리를 담당.
// 주의 : prevApparels/prevWeapons는 ThingID 문자열로 저장 후 PostLoadInit에서 재탐색.
//        캐러밴/포드 내 장비도 탐색하여 오프맵 장비 유실을 방지.

using RimWorld;
using RimWorld.Planet;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    public partial class HediffComp_ShapeshiftCore
    {
        /// <summary>저장/로드 처리.</summary>
        public override void CompExposeData()
        {
            base.CompExposeData();

            Scribe_Defs.Look(ref currentForm, "currentForm");
            Scribe_Values.Look(ref needsInit, "needsInit", false);
            Scribe_Values.Look(ref transformTimer, "transformTimer", 0, true);
            Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
            Scribe_Defs.Look(ref originalHeadType, "originalHeadType");

            Scribe_Values.Look(ref hasSavedColors, "hasSavedColors", false);

            Color __tmpHairColor = originalHairColor ?? default;
            bool __hasHairColor = originalHairColor.HasValue;
            Scribe_Values.Look(ref __hasHairColor, "hasOriginalHairColor", false);
            Scribe_Values.Look(ref __tmpHairColor, "originalHairColor");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                originalHairColor = __hasHairColor ? __tmpHairColor : (Color?)null;

            Color __tmpSkinColor = originalSkinColor ?? default;
            bool __hasSkinColor = originalSkinColor.HasValue;
            Scribe_Values.Look(ref __hasSkinColor, "hasOriginalSkinColor", false);
            Scribe_Values.Look(ref __tmpSkinColor, "originalSkinColor");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                originalSkinColor = __hasSkinColor ? __tmpSkinColor : (Color?)null;

            // Def 리스트

            List<AbilityDef> __tmpAbilities = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpAbilities = tempAddedAbilities;
            Scribe_Collections.Look(ref __tmpAbilities, "tempAddedAbilities", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedAbilities.Clear();
                if (__tmpAbilities != null) tempAddedAbilities.AddRange(__tmpAbilities);
            }

            List<HediffDef> __tmpHediffDefs = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpHediffDefs = tempAddedHediffsDefCache;
            Scribe_Collections.Look(ref __tmpHediffDefs, "tempAddedHediffsDefCache", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedHediffsDefCache.Clear();
                if (__tmpHediffDefs != null) tempAddedHediffsDefCache.AddRange(__tmpHediffDefs);
            }

            // Reference 리스트 - hediff

            List<Hediff> __tmpHediffs = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpHediffs = tempAddedHediffs;
            Scribe_Collections.Look(ref __tmpHediffs, "tempAddedHediffs", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedHediffs.Clear();
                __tmpHediffsLoad = __tmpHediffs;
            }

            // prevApparels - ThingID 문자열 저장

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<string> apIds = new List<string>(prevApparels.Count);
                for (int i = 0; i < prevApparels.Count; i++)
                {
                    if (prevApparels[i] != null) apIds.Add(prevApparels[i].ThingID);
                }
                Scribe_Collections.Look(ref apIds, "prevApparelIds", LookMode.Value);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<string> apIds = null;
                Scribe_Collections.Look(ref apIds, "prevApparelIds", LookMode.Value);
                prevApparels.Clear();
                __tmpPrevApIds = apIds != null ? new HashSet<string>(apIds) : null;
            }

            // prevWeapons - ThingID 문자열 저장

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<string> wpIds = new List<string>(prevWeapons.Count);
                for (int i = 0; i < prevWeapons.Count; i++)
                {
                    if (prevWeapons[i] != null) wpIds.Add(prevWeapons[i].ThingID);
                }
                Scribe_Collections.Look(ref wpIds, "prevWeaponIds", LookMode.Value);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<string> wpIds = null;
                Scribe_Collections.Look(ref wpIds, "prevWeaponIds", LookMode.Value);
                prevWeapons.Clear();
                __tmpPrevWpIds = wpIds != null ? new HashSet<string>(wpIds) : null;
            }

            // Deep 리스트

            List<ShapeshiftPartRestoreRecord> __tmpRestore = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpRestore = tempPartRestoreRecords;
            Scribe_Collections.Look(ref __tmpRestore, "tempPartRestoreRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempPartRestoreRecords.Clear();
                if (__tmpRestore != null) tempPartRestoreRecords.AddRange(__tmpRestore);
            }

            // verbAutoToggle 딕셔너리
            Scribe_Collections.Look(ref verbAutoToggle, "ssfVerbToggle",
                LookMode.Value, LookMode.Value, ref tmpVerbToggleKeys, ref tmpVerbToggleVals);
            if (verbAutoToggle == null)
                verbAutoToggle = new Dictionary<string, bool>();
            Scribe_Collections.Look(ref sourceItems, "sourceItems", LookMode.Reference);
            Scribe_Collections.Look(ref generatedApparel, "generatedApparel", LookMode.Reference);
            Scribe_Collections.Look(ref generatedWeapons, "generatedWeapons", LookMode.Reference);

            // Scribe_Collections.Look은 노드 미존재 시 null을 반환 — PostLoadInit 전 접근 방어
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (sourceItems == null) sourceItems = new List<Thing>();
                if (generatedApparel == null) generatedApparel = new List<Apparel>();
                if (generatedWeapons == null) generatedWeapons = new List<ThingWithComps>();
            }

            // PostLoadInit
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                HandlePostLoadInit();
        }

        /// <summary>로드 완료 후 초기화 — 참조 복원, 고아 데이터 정리, 상태 재등록.</summary>
        private void HandlePostLoadInit()
        {
            if (sourceItems != null) sourceItems.RemoveAll(x => x == null);
            else sourceItems = new List<Thing>();

            if (generatedApparel != null) generatedApparel.RemoveAll(x => x == null);
            else generatedApparel = new List<Apparel>();

            if (generatedWeapons != null) generatedWeapons.RemoveAll(x => x == null);
            else generatedWeapons = new List<ThingWithComps>();

            if (__tmpHediffsLoad != null)
            {
                int lostCount = 0;
                for (int i = 0; i < __tmpHediffsLoad.Count; i++)
                {
                    if (__tmpHediffsLoad[i] != null)
                        tempAddedHediffs.Add(__tmpHediffsLoad[i]);
                    else
                        lostCount++;
                }
                if (lostCount > 0)
                    Log.Warning($"[SSF] {lostCount} hediff reference(s) lost during load for pawn {Pawn?.Name}. Revert may leave orphaned hediffs.");
                __tmpHediffsLoad = null;
            }
            else
            {
                for (int i = tempAddedHediffs.Count - 1; i >= 0; i--)
                {
                    if (tempAddedHediffs[i] == null)
                        tempAddedHediffs.RemoveAt(i);
                }
            }

            needsGearResolve = true;

            var pawn = Pawn;
            if (pawn != null && pawn.Dead && isTransformed)
            {
                RemoveForm();
            }
            else if (isTransformed && currentForm != null && pawn != null)
            {
                ApplyRuntimeCaches(pawn, currentForm);
                ShapeshiftRegistry.Register(pawn, this);
            }
            else if (pawn != null && !pawn.Dead && currentForm == null
                && (tempAddedHediffs.Count > 0 || tempAddedAbilities.Count > 0
                    || generatedApparel.Count > 0 || generatedWeapons.Count > 0))
            {
                CleanupOrphanedTransformData(pawn);
            }
        }

        /// <summary>FormDef 삭제 등으로 currentForm이 null이지만 변신 잔여 데이터가 있을 때 강제 정리.</summary>
        private void CleanupOrphanedTransformData(Pawn pawn)
        {
            Log.Warning($"[SSF] Pawn {pawn.Name}: orphaned transform data found (FormDef removed?). Forcing cleanup.");

            // hediff 잔여 제거
            if (pawn.health != null)
            {
                for (int i = 0; i < tempAddedHediffs.Count; i++)
                {
                    var h = tempAddedHediffs[i];
                    if (h != null && pawn.health.hediffSet.hediffs.Contains(h))
                        pawn.health.RemoveHediff(h);
                }
            }
            tempAddedHediffs.Clear();
            tempAddedHediffsDefCache.Clear();

            // 능력 잔여 제거
            if (pawn.abilities != null)
            {
                for (int i = 0; i < tempAddedAbilities.Count; i++)
                {
                    if (tempAddedAbilities[i] != null)
                        pawn.abilities.RemoveAbility(tempAddedAbilities[i]);
                }
            }
            tempAddedAbilities.Clear();

            // 생성 장비 파괴
            for (int i = generatedApparel.Count - 1; i >= 0; i--)
            {
                if (generatedApparel[i] != null && !generatedApparel[i].Destroyed)
                    generatedApparel[i].Destroy(DestroyMode.Vanish);
            }
            generatedApparel.Clear();
            for (int i = generatedWeapons.Count - 1; i >= 0; i--)
            {
                if (generatedWeapons[i] != null && !generatedWeapons[i].Destroyed)
                    generatedWeapons[i].Destroy(DestroyMode.Vanish);
            }
            generatedWeapons.Clear();

            // 체형 원복
            if (pawn.story != null)
            {
                if (originalBodyType != null) pawn.story.bodyType = originalBodyType;
                if (originalHeadType != null) pawn.story.headType = originalHeadType;
                if (hasSavedColors)
                {
                    if (originalHairColor.HasValue) pawn.story.HairColor = originalHairColor.Value;
                    pawn.story.skinColorOverride = originalSkinColor;
                    hasSavedColors = false;
                }
            }

            tempPartRestoreRecords.Clear();
            shapeshiftVerbTracker = null;
            _verbKeyCache = null;

            try { RefreshPawn(pawn, this); } catch (Exception ex) { Log.Warning($"[SSF] Orphan cleanup RefreshPawn error: {ex}"); }
        }

        /// <summary>로드 후 ThingID로 저장된 장비 참조를 실제 Thing으로 복원. CompPostTick에서 호출.</summary>
        internal void ResolveGearFromIds(Pawn pawn)
        {
            needsGearResolve = false;
            if (__tmpPrevApIds == null && __tmpPrevWpIds == null) return;

            // 1차: 인벤토리 탐색
            if (pawn.inventory?.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    var t = pawn.inventory.innerContainer[i];
                    if (__tmpPrevApIds != null && __tmpPrevApIds.Contains(t.ThingID) && t is Apparel ap)
                    {
                        prevApparels.Add(ap);
                        __tmpPrevApIds.Remove(t.ThingID);
                    }
                    else if (__tmpPrevWpIds != null && __tmpPrevWpIds.Contains(t.ThingID) && t is ThingWithComps twc)
                    {
                        prevWeapons.Add(twc);
                        __tmpPrevWpIds.Remove(t.ThingID);
                    }
                }
            }

            // 2차: 맵 내 장비 탐색
            if (pawn.Map != null)
            {
                // AllThings 대신 HaulableEverOrMinifiable로 범위 축소 — 대형 맵 성능 개선
                var haulables = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEverOrMinifiable);
                ResolveGearFromThingList(haulables);
            }
            else
            {
                // 캐러밴/포드 내 장비 탐색
                var caravan = pawn.GetCaravan();
                if (caravan != null)
                {
                    var things = CaravanInventoryUtility.AllInventoryItems(caravan);
                    ResolveGearFromThingList(things);
                }
            }

            if (__tmpPrevApIds != null && __tmpPrevApIds.Count > 0)
                Log.Warning($"[SSF] {__tmpPrevApIds.Count} prev apparel(s) could not be resolved for {pawn?.Name}. Items may be lost on revert.");
            if (__tmpPrevWpIds != null && __tmpPrevWpIds.Count > 0)
                Log.Warning($"[SSF] {__tmpPrevWpIds.Count} prev weapon(s) could not be resolved for {pawn?.Name}. Items may be lost on revert.");
            __tmpPrevApIds = null;
            __tmpPrevWpIds = null;
        }

        /// <summary>Thing 리스트에서 ThingID 매칭으로 장비 참조 복원.</summary>
        private void ResolveGearFromThingList(IList<Thing> things)
        {
            if (things == null) return;
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t == null) continue;
                if (__tmpPrevApIds != null && __tmpPrevApIds.Count > 0 && __tmpPrevApIds.Contains(t.ThingID) && t is Apparel ap)
                {
                    prevApparels.Add(ap);
                    __tmpPrevApIds.Remove(t.ThingID);
                }
                else if (__tmpPrevWpIds != null && __tmpPrevWpIds.Count > 0 && __tmpPrevWpIds.Contains(t.ThingID) && t is ThingWithComps twc)
                {
                    prevWeapons.Add(twc);
                    __tmpPrevWpIds.Remove(t.ThingID);
                }
            }
        }
    }
}
