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
            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;

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

                        if (st != null && st.forbidDroppedItemsOnTransform && dropped != null && dropped.Spawned)
                        {
                            dropped.SetForbidden(true);
                        }
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

                        if (st != null && st.forbidDroppedItemsOnTransform && dropped != null && dropped.Spawned)
                        {
                            dropped.SetForbidden(true);
                        }
                    }
                }
            }
        }

        void SpawnAndEquipFormGear(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            using (new ShapeshiftEquipLockScope(this))
            {
                if (pawn.apparel != null && form.spawnApparelOnTransform != null && form.spawnApparelOnTransform.Count > 0)
                {
                    for (int i = 0; i < form.spawnApparelOnTransform.Count; i++)
                    {
                        ThingDef apparelDef = form.spawnApparelOnTransform[i];
                        if (apparelDef == null || !apparelDef.IsApparel) continue;

                        if (pawn.apparel != null)
                        {
                            List<Apparel> worn = pawn.apparel.WornApparel;
                            for (int j = worn.Count - 1; j >= 0; j--)
                            {
                                Apparel existingAp = worn[j];
                                if ((sourceItems != null && sourceItems.Contains(existingAp)) || generatedApparel.Contains(existingAp)) continue;

                                if (pawn.RaceProps?.body != null && !ApparelUtility.CanWearTogether(apparelDef, existingAp.def, pawn.RaceProps.body))
                                {
                                    pawn.apparel.Remove(existingAp);
                                    if (form.conflictingGearHandling == GearHandling.Drop)
                                    {
                                        TryDropThing(existingAp, pawn.PositionHeld, pawn.MapHeld);
                                    }
                                    else
                                    {
                                        if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(existingAp, false)) { }
                                        else TryDropThing(existingAp, pawn.PositionHeld, pawn.MapHeld);
                                    }

                                    if (existingAp.Spawned && ShapeshifterFrameworkMod.Settings != null && ShapeshifterFrameworkMod.Settings.forbidDroppedItemsOnTransform)
                                    {
                                        existingAp.SetForbidden(true);
                                    }

                                    if (!prevApparels.Contains(existingAp)) prevApparels.Add(existingAp);
                                }
                            }
                        }

                        ThingDef stuff = null;
                        if (apparelDef.MadeFromStuff)
                        {
                            stuff = form.spawnApparelStuff;
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(apparelDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(apparelDef);
                            }
                        }

                        Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuff);

                        if (pawn.apparel != null)
                        {
                            pawn.apparel.Wear(newApparel, dropReplacedApparel: false);
                            pawn.apparel.Lock(newApparel);
                            generatedApparel.Add(newApparel);
                        }
                    }
                }

                if (pawn.equipment != null && form.spawnWeaponOnTransform != null && form.spawnWeaponOnTransform.Count > 0)
                {
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        ThingWithComps existingWep = pawn.equipment.Primary;
                        if ((sourceItems == null || !sourceItems.Contains(existingWep)) && !generatedWeapons.Contains(existingWep))
                        {
                            pawn.equipment.Remove(existingWep);

                            if (form.conflictingGearHandling == GearHandling.Drop)
                            {
                                TryDropThing(existingWep, pawn.PositionHeld, pawn.MapHeld);
                            }
                            else
                            {
                                if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(existingWep, false)) { }
                                else TryDropThing(existingWep, pawn.PositionHeld, pawn.MapHeld);
                            }

                            if (existingWep.Spawned && ShapeshifterFrameworkMod.Settings != null && ShapeshifterFrameworkMod.Settings.forbidDroppedItemsOnTransform)
                            {
                                existingWep.SetForbidden(true);
                            }

                            if (!prevWeapons.Contains(existingWep)) prevWeapons.Add(existingWep);
                        }
                    }

                    for (int i = 0; i < form.spawnWeaponOnTransform.Count; i++)
                    {
                        ThingDef weaponDef = form.spawnWeaponOnTransform[i];
                        if (weaponDef == null || !weaponDef.IsWeapon) continue;

                        ThingDef stuff = null;
                        if (weaponDef.MadeFromStuff)
                        {
                            stuff = form.spawnWeaponStuff;
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(weaponDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(weaponDef);
                            }
                        }

                        ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef, stuff);

                        if (pawn.equipment != null)
                        {
                            pawn.equipment.AddEquipment(newWeapon);
                            generatedWeapons.Add(newWeapon);
                        }
                    }
                }
            }
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

        void TryReequipPreviousGear(Pawn pawn)
        {
            ShapeshiftDiagnostics.Info($"TryReequip: weapons={prevWeapons.Count}, apparels={prevApparels.Count}");
            if (pawn == null || pawn.Dead) return;

            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            bool allowInv = (st == null) ? true : st.autoReequipFromInventory;
            bool allowGround = (st == null) ? true : st.autoReequipFromGround;

            var toQueue = new List<Job>(prevWeapons.Count + prevApparels.Count);

            using (new ShapeshiftEquipLockScope(this))
            {
                if (prevWeapons.Count > 0)
                {
                    for (int i = 0; i < prevWeapons.Count; i++)
                    {
                        ThingWithComps w = prevWeapons[i];
                        if (w == null || w.Destroyed) continue;

                        if (w.Spawned)
                        {
                            if (!allowGround) continue;

                            if (w.Map == pawn.MapHeld && pawn.CanReach(w, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                if (w.IsForbidden(pawn)) w.SetForbidden(false);
                                Job job = JobMaker.MakeJob(JobDefOf.Equip, w);
                                job.playerForced = true;
                                toQueue.Add(job);
                            }
                            continue;
                        }

                        if (allowInv && pawn.inventory?.innerContainer?.Contains(w) == true)
                        {
                            ShapeshiftInventoryReequipUtility.SafeEquipFromInventory(pawn, w);
                        }
                    }
                }

                if (prevApparels.Count > 0)
                {
                    for (int i = 0; i < prevApparels.Count; i++)
                    {
                        Apparel ap = prevApparels[i];
                        if (ap == null || ap.Destroyed) continue;

                        if (ap.Spawned)
                        {
                            if (!allowGround) continue;

                            if (ap.Map == pawn.MapHeld && pawn.CanReach(ap, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                if (ap.IsForbidden(pawn)) ap.SetForbidden(false);
                                Job job = JobMaker.MakeJob(JobDefOf.Wear, ap);
                                job.playerForced = true;
                                toQueue.Add(job);
                            }
                            continue;
                        }

                        if (allowInv && pawn.inventory?.innerContainer?.Contains(ap) == true)
                        {
                            ShapeshiftInventoryReequipUtility.SafeWearFromInventory(pawn, ap, dropReplaced: true);
                        }
                    }
                }
            }

            if (toQueue.Count > 0 && pawn.jobs != null)
            {
                Job first = toQueue[0];
                pawn.jobs.TryTakeOrderedJob(first);
                for (int i = 1; i < toQueue.Count; i++)
                    pawn.jobs.jobQueue.EnqueueLast(toQueue[i]);
            }

            prevWeapons.Clear();
            prevApparels.Clear();
        }
    }
}
