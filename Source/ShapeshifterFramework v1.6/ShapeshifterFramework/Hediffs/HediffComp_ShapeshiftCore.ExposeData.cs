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

            Color tmpHairColor = originalHairColor ?? default;
            bool hasHairColor = originalHairColor.HasValue;
            Scribe_Values.Look(ref hasHairColor, "hasOriginalHairColor", false);
            Scribe_Values.Look(ref tmpHairColor, "originalHairColor");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                originalHairColor = hasHairColor ? tmpHairColor : (Color?)null;

            Color tmpSkinColor = originalSkinColor ?? default;
            bool hasSkinColor = originalSkinColor.HasValue;
            Scribe_Values.Look(ref hasSkinColor, "hasOriginalSkinColor", false);
            Scribe_Values.Look(ref tmpSkinColor, "originalSkinColor");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                originalSkinColor = hasSkinColor ? tmpSkinColor : (Color?)null;

            // Def 리스트

            List<AbilityDef> tmpAbilities = null;
            if (Scribe.mode == LoadSaveMode.Saving) tmpAbilities = tempAddedAbilities;
            Scribe_Collections.Look(ref tmpAbilities, "tempAddedAbilities", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedAbilities.Clear();
                if (tmpAbilities != null) tempAddedAbilities.AddRange(tmpAbilities);
            }

            List<HediffDef> tmpHediffDefs = null;
            if (Scribe.mode == LoadSaveMode.Saving) tmpHediffDefs = tempAddedHediffsDefCache;
            Scribe_Collections.Look(ref tmpHediffDefs, "tempAddedHediffsDefCache", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedHediffsDefCache.Clear();
                if (tmpHediffDefs != null) tempAddedHediffsDefCache.AddRange(tmpHediffDefs);
            }

            // Reference 리스트 - hediff

            List<Hediff> tmpHediffs = null;
            if (Scribe.mode == LoadSaveMode.Saving) tmpHediffs = tempAddedHediffs;
            Scribe_Collections.Look(ref tmpHediffs, "tempAddedHediffs", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedHediffs.Clear();
                tmpHediffsLoad = tmpHediffs;
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
                tmpPrevApIds = apIds != null ? new HashSet<string>(apIds) : null;
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
                tmpPrevWpIds = wpIds != null ? new HashSet<string>(wpIds) : null;
            }

            // Deep 리스트

            List<ShapeshiftPartRestoreRecord> tmpRestore = null;
            if (Scribe.mode == LoadSaveMode.Saving) tmpRestore = tempPartRestoreRecords;
            Scribe_Collections.Look(ref tmpRestore, "tempPartRestoreRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempPartRestoreRecords.Clear();
                if (tmpRestore != null) tempPartRestoreRecords.AddRange(tmpRestore);
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

            if (tmpHediffsLoad != null)
            {
                int lostCount = 0;
                for (int i = 0; i < tmpHediffsLoad.Count; i++)
                {
                    if (tmpHediffsLoad[i] != null)
                        tempAddedHediffs.Add(tmpHediffsLoad[i]);
                    else
                        lostCount++;
                }
                if (lostCount > 0)
                    Log.Warning($"[SSF] {lostCount} hediff reference(s) lost during load for pawn {Pawn?.Name}. Revert may leave orphaned hediffs.");
                tmpHediffsLoad = null;
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
            CleanupTransformArtifacts(pawn);
        }

        /// <summary>변신 잔여물(파츠/hediff/능력/생성 장비/외형) 강제 정리.
        /// 로드 고아 정리(CleanupOrphanedTransformData)와 ApplyForm 실패 롤백이 공유.</summary>
        internal void CleanupTransformArtifacts(Pawn pawn)
        {
            // 파츠 원복 — 기록이 있으면 원래 부위 상태로 (죽은 폰은 내부에서 스킵)
            try { RestoreBodyParts(pawn); }
            catch (Exception ex) { Log.Warning($"[SSF] CleanupTransformArtifacts RestoreBodyParts error: {ex}"); }

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
            RestoreAppearance(pawn);

            tempPartRestoreRecords.Clear();
            shapeshiftVerbTracker = null;
            _verbKeyCache = null;

            try { RefreshPawn(pawn, this); } catch (Exception ex) { Log.Warning($"[SSF] Orphan cleanup RefreshPawn error: {ex}"); }
        }

        /// <summary>로드 후 ThingID로 저장된 장비 참조를 실제 Thing으로 복원. CompPostTick에서 호출.</summary>
        internal void ResolveGearFromIds(Pawn pawn)
        {
            needsGearResolve = false;
            if (tmpPrevApIds == null && tmpPrevWpIds == null) return;

            // 0차: 착용/장착 중 장비 — 기본값 GearHandling.Keep 폼은 '이전 장비'가 여전히 착용 상태.
            // 이걸 안 보면 로드마다 "could not be resolved" 거짓 경고 + 추적 리스트 소실.
            if (tmpPrevApIds != null && tmpPrevApIds.Count > 0 && pawn.apparel != null)
            {
                var worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    var a = worn[i];
                    if (a != null && tmpPrevApIds.Contains(a.ThingID))
                    {
                        prevApparels.Add(a);
                        tmpPrevApIds.Remove(a.ThingID);
                    }
                }
            }
            if (tmpPrevWpIds != null && tmpPrevWpIds.Count > 0 && pawn.equipment != null)
            {
                var eqs = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < eqs.Count; i++)
                {
                    var e = eqs[i];
                    if (e != null && tmpPrevWpIds.Contains(e.ThingID))
                    {
                        prevWeapons.Add(e);
                        tmpPrevWpIds.Remove(e.ThingID);
                    }
                }
            }

            // 1차: 인벤토리 탐색
            if (pawn.inventory?.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    var t = pawn.inventory.innerContainer[i];
                    if (tmpPrevApIds != null && tmpPrevApIds.Contains(t.ThingID) && t is Apparel ap)
                    {
                        prevApparels.Add(ap);
                        tmpPrevApIds.Remove(t.ThingID);
                    }
                    else if (tmpPrevWpIds != null && tmpPrevWpIds.Contains(t.ThingID) && t is ThingWithComps twc)
                    {
                        prevWeapons.Add(twc);
                        tmpPrevWpIds.Remove(t.ThingID);
                    }
                }
            }

            // 2차 스캔 가드: 인벤토리에서 모두 해석됐으면 맵/캐러밴 전수 순회를 스킵 (대형 맵 분할상환)
            bool anyUnresolved = (tmpPrevApIds != null && tmpPrevApIds.Count > 0)
                                 || (tmpPrevWpIds != null && tmpPrevWpIds.Count > 0);
            if (anyUnresolved)
            {
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
            }

            if (tmpPrevApIds != null && tmpPrevApIds.Count > 0)
                Log.Warning($"[SSF] {tmpPrevApIds.Count} prev apparel(s) could not be resolved for {pawn?.Name}. Items may be lost on revert.");
            if (tmpPrevWpIds != null && tmpPrevWpIds.Count > 0)
                Log.Warning($"[SSF] {tmpPrevWpIds.Count} prev weapon(s) could not be resolved for {pawn?.Name}. Items may be lost on revert.");
            tmpPrevApIds = null;
            tmpPrevWpIds = null;
        }

        /// <summary>Thing 리스트에서 ThingID 매칭으로 장비 참조 복원.</summary>
        private void ResolveGearFromThingList(IList<Thing> things)
        {
            if (things == null) return;
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t == null) continue;
                if (tmpPrevApIds != null && tmpPrevApIds.Count > 0 && tmpPrevApIds.Contains(t.ThingID) && t is Apparel ap)
                {
                    prevApparels.Add(ap);
                    tmpPrevApIds.Remove(t.ThingID);
                }
                else if (tmpPrevWpIds != null && tmpPrevWpIds.Count > 0 && tmpPrevWpIds.Contains(t.ThingID) && t is ThingWithComps twc)
                {
                    prevWeapons.Add(twc);
                    tmpPrevWpIds.Remove(t.ThingID);
                }
            }
        }
    }
}
