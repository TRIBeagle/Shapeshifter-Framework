// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.Gear.cs
// 목적 : 변신 전/후 장비(의류·무기) 스냅샷, 처리, 재착용, 드랍 유틸.
// 용도 : 변신 시 기존 장비를 인벤토리/지면으로 이동하고 폼 전용 장비를 소환·장착,
//        변신 해제 시 이전 장비를 자동 재착용하는 로직을 관리.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Hediffs
{
    public partial class HediffComp_ShapeshiftCore
    {
        void CaptureCurrentGear(Pawn pawn)
        {
            if (pawn == null) return;

            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    var a = worn[i];
                    if (a != null) prevApparels.Add(a);
                }
            }

            if (pawn.equipment != null)
            {
                List<ThingWithComps> eqs = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < eqs.Count; i++)
                {
                    var e = eqs[i];
                    if (e != null) prevWeapons.Add(e);
                }
            }
        }

        void HandleGearOnTransform(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            IntVec3 pos = pawn.PositionHeld;
            Map map = pawn.MapHeld;

            if (form.apparelOnTransform != GearHandling.Keep && pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;

                for (int i = worn.Count - 1; i >= 0; i--)
                {
                    Apparel ap = worn[i];
                    if (ap == null) continue;

                    if ((sourceItems != null && sourceItems.Contains(ap)) || generatedApparel.Contains(ap)) continue;

                    if (form.apparelOnTransform == GearHandling.Inventory)
                    {
                        pawn.apparel.Remove(ap);
                        if (pawn.inventory != null && pawn.inventory.innerContainer != null)
                        {
                            if (!pawn.inventory.innerContainer.TryAdd(ap, false))
                                TryDropThing(ap, pos, map);
                        }
                        else TryDropThing(ap, pos, map);
                    }
                    else
                    {
                        Apparel dropped = null;
                        if (!pawn.apparel.TryDrop(ap, out dropped, pos, forbid: false))
                        {
                            pawn.apparel.Remove(ap);
                            TryDropThing(ap, pos, map);
                            dropped = ap;
                        }
                        TryForbidDropped(dropped);
                    }
                }
            }

            if (form.weaponsOnTransform != GearHandling.Keep && pawn.equipment != null)
            {
                List<ThingWithComps> list = pawn.equipment.AllEquipmentListForReading;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    ThingWithComps eq = list[i];
                    if (eq == null) continue;

                    if ((sourceItems != null && sourceItems.Contains(eq)) || generatedWeapons.Contains(eq)) continue;

                    if (form.weaponsOnTransform == GearHandling.Inventory)
                    {
                        pawn.equipment.Remove(eq);
                        if (pawn.inventory != null && pawn.inventory.innerContainer != null)
                        {
                            if (!pawn.inventory.innerContainer.TryAdd(eq, false))
                                TryDropThing(eq, pos, map);
                        }
                        else TryDropThing(eq, pos, map);
                    }
                    else
                    {
                        ThingWithComps dropped = null;
                        if (!pawn.equipment.TryDropEquipment(eq, out dropped, pos, forbid: false))
                        {
                            pawn.equipment.Remove(eq);
                            TryDropThing(eq, pos, map);
                            dropped = eq;
                        }
                        TryForbidDropped(dropped);
                    }
                }
            }
        }

        /// <summary>폼 전용 의류/무기를 생성하고 장착.</summary>
        void SpawnAndEquipFormGear(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;
            using (new ShapeshiftEquipLockScope(this))
            {
                SpawnFormApparel(pawn, form);
                SpawnFormWeapons(pawn, form);
            }
        }

        /// <summary>폼 전용 의류 생성: 충돌 의류 처리 후 새 의류 착용.</summary>
        private void SpawnFormApparel(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn.apparel == null || form.spawnApparelOnTransform == null || form.spawnApparelOnTransform.Count == 0) return;

            for (int i = 0; i < form.spawnApparelOnTransform.Count; i++)
            {
                ThingDef apparelDef = form.spawnApparelOnTransform[i];
                if (apparelDef == null || !apparelDef.IsApparel) continue;

                // 충돌 의류 제거
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int j = worn.Count - 1; j >= 0; j--)
                {
                    Apparel existingAp = worn[j];
                    if ((sourceItems != null && sourceItems.Contains(existingAp)) || generatedApparel.Contains(existingAp)) continue;

                    if (pawn.RaceProps?.body != null && !ApparelUtility.CanWearTogether(apparelDef, existingAp.def, pawn.RaceProps.body))
                    {
                        pawn.apparel.Remove(existingAp);
                        HandleConflictingGear(existingAp, pawn, form.conflictingGearHandling);
                        if (!prevApparels.Contains(existingAp)) prevApparels.Add(existingAp);
                    }
                }

                // 종족 body에 이 의류를 착용 가능한지 사전 검증 (HAR 비인간 종족 등)
                if (pawn.RaceProps?.body != null && !ApparelUtility.HasPartsToWear(pawn, apparelDef))
                {
                    ShapeshiftDiagnostics.Info($"SpawnFormApparel: {apparelDef.defName} cannot be worn by {pawn.def.defName} (no matching body parts). Skipped.");
                    continue;
                }

                // 재질 결정 및 생성
                ThingDef stuff = ResolveStuff(apparelDef, form.spawnApparelStuff);
                Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuff);

                generatedApparel.Add(newApparel); // Wear 예외 시 고아 방지 — RemoveForm에서 반드시 파괴
                pawn.apparel.Wear(newApparel, dropReplacedApparel: false);
                pawn.apparel.Lock(newApparel);
            }
        }

        /// <summary>폼 전용 무기 생성: 기존 무기 처리 후 새 무기 장착.</summary>
        private void SpawnFormWeapons(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn.equipment == null || form.spawnWeaponOnTransform == null || form.spawnWeaponOnTransform.Count == 0) return;

            // 기존 무기 처리
            if (pawn.equipment.Primary != null)
            {
                ThingWithComps existingWep = pawn.equipment.Primary;
                if ((sourceItems == null || !sourceItems.Contains(existingWep)) && !generatedWeapons.Contains(existingWep))
                {
                    pawn.equipment.Remove(existingWep);
                    HandleConflictingGear(existingWep, pawn, form.conflictingGearHandling);
                    if (!prevWeapons.Contains(existingWep)) prevWeapons.Add(existingWep);
                }
            }

            for (int i = 0; i < form.spawnWeaponOnTransform.Count; i++)
            {
                ThingDef weaponDef = form.spawnWeaponOnTransform[i];
                if (weaponDef == null || !weaponDef.IsWeapon) continue;

                ThingDef stuff = ResolveStuff(weaponDef, form.spawnWeaponStuff);
                ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef, stuff);

                generatedWeapons.Add(newWeapon); // AddEquipment 예외 시 고아 방지 — RemoveForm에서 반드시 파괴
                pawn.equipment.AddEquipment(newWeapon);
            }
        }

        /// <summary>ThingDef에 맞는 재질 결정. 지정 재질이 유효하지 않으면 기본 재질 반환.</summary>
        private static ThingDef ResolveStuff(ThingDef thingDef, ThingDef preferredStuff)
        {
            if (!thingDef.MadeFromStuff) return null;
            if (preferredStuff != null && preferredStuff.stuffProps != null && preferredStuff.stuffProps.CanMake(thingDef))
                return preferredStuff;
            return GenStuff.DefaultStuffFor(thingDef);
        }

        /// <summary>설정에 따라 드랍된 아이템을 금지(Forbid) 처리.</summary>
        static void TryForbidDropped(Thing dropped)
        {
            if (dropped == null || !dropped.Spawned) return;
            var st = ShapeshifterFrameworkMod.Settings;
            if (st != null && st.forbidDroppedItemsOnTransform)
                dropped.SetForbidden(true);
        }

        /// <summary>충돌 장비 처리 — Drop 모드면 바닥에, 아니면 인벤토리에 시도 후 실패 시 바닥에.</summary>
        static void HandleConflictingGear(Thing item, Pawn pawn, GearHandling handling)
        {
            if (item == null || pawn == null) return;
            if (handling == GearHandling.Drop)
            {
                TryDropThing(item, pawn.PositionHeld, pawn.MapHeld);
            }
            else
            {
                if (pawn.inventory?.innerContainer == null || !pawn.inventory.innerContainer.TryAdd(item, false))
                    TryDropThing(item, pawn.PositionHeld, pawn.MapHeld);
            }
            TryForbidDropped(item);
        }

        static void TryDropThing(Thing t, IntVec3 pos, Map map)
        {
            if (t == null) return;
            try
            {
                if (map != null && pos.IsValid)
                {
                    GenPlace.TryPlaceThing(t, pos, map, ThingPlaceMode.Near);
                }
                else
                {
                    ThingOwner owner = t.holdingOwner;
                    if (owner != null) owner.Remove(t);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSF] TryDropThing failed for '{t.Label}': {ex}");
            }
        }

        /// <summary>변신 해제 후 이전 장비를 자동 재착용.</summary>
        void TryReequipPreviousGear(Pawn pawn)
        {
            ShapeshiftDiagnostics.Info($"TryReequip: weapons={prevWeapons.Count}, apparels={prevApparels.Count}");
            if (pawn == null || pawn.Dead) return;

            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            bool allowInv = st == null || st.autoReequipFromInventory;
            bool allowGround = st == null || st.autoReequipFromGround;

            var toQueue = new List<Job>(prevWeapons.Count + prevApparels.Count);

            using (new ShapeshiftEquipLockScope(this))
            {
                CollectReequipJobs(pawn, prevWeapons, allowInv, allowGround, toQueue, isWeapon: true);
                CollectReequipJobs(pawn, prevApparels, allowInv, allowGround, toQueue, isWeapon: false);
            }

            EnqueueJobs(pawn, toQueue);
            prevWeapons.Clear();
            prevApparels.Clear();
        }

        /// <summary>이전 장비 목록에서 재착용 가능한 아이템을 수집. 인벤토리는 즉시 착용, 바닥은 Job 큐잉.</summary>
        private void CollectReequipJobs<T>(Pawn pawn, List<T> items, bool allowInv, bool allowGround, List<Job> toQueue, bool isWeapon) where T : Thing
        {
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (item == null || item.Destroyed) continue;

                if (item.Spawned)
                {
                    if (!allowGround) continue;
                    if (item.Map == pawn.MapHeld && pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        if (item.IsForbidden(pawn)) item.SetForbidden(false);
                        Job job = JobMaker.MakeJob(isWeapon ? JobDefOf.Equip : JobDefOf.Wear, item);
                        job.playerForced = true;
                        toQueue.Add(job);
                    }
                    continue;
                }

                if (allowInv && pawn.inventory?.innerContainer?.Contains(item) == true)
                {
                    if (isWeapon)
                        ShapeshiftInventoryReequipUtility.SafeEquipFromInventory(pawn, item as ThingWithComps);
                    else
                        ShapeshiftInventoryReequipUtility.SafeWearFromInventory(pawn, item as Apparel, dropReplaced: true);
                }
            }
        }

        /// <summary>수집된 재착용 Job들을 첫 번째는 즉시, 나머지는 큐에 추가.</summary>
        private static void EnqueueJobs(Pawn pawn, List<Job> toQueue)
        {
            if (toQueue.Count == 0 || pawn.jobs == null) return;
            pawn.jobs.TryTakeOrderedJob(toQueue[0]);
            if (pawn.jobs.jobQueue != null)
            {
                for (int i = 1; i < toQueue.Count; i++)
                    pawn.jobs.jobQueue.EnqueueLast(toQueue[i]);
            }
        }
    }
}
