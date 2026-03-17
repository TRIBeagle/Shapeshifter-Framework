// ShapeshifterFramework | Debugs | Shapeshifter_DebugActions.cs
// 목적 : 림월드 개발자 모드(Dev Mode)에서 폼(Form) 설정값이 인게임에 정상 적용되는지 실시간으로 검증하기 위한 디버그 도구 모음.
// 용도 : - Inspect Active Form : 현재 폰에 적용된 폼의 핵심 효과(그래픽, 파츠, 스탯 보정치 등)를 플로트 메뉴로 요약 표시.
//        - Play Form Sounds : 폼에 정의된 각종 사운드(보이스, 피격, 사망, 섭취음 등)를 즉시 테스트 재생.
//        - Dump Pawn State to Log : 폰의 런타임 상태(변신 여부 무관)와 폼의 모든 세부 데이터를 콘솔 로그로 전문 덤프(Dump).
// 주의 : 바닐라 DebugAction 패턴을 준수하며, 인게임 실제 플레이 로직에는 관여하지 않고 오직 모더의 FormDef 세팅 검증 및 디버깅 편의를 위해서만 작동함.

using LudeonTK;
using RimWorld;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShapeshifterFramework.Debugs
{
    /// <summary>Debug 탭 "Shapeshifter Framework" 디버그 액션 모음.</summary>
    public static class Shapeshifter_DebugActions
    {
        #region 1) 활성 폼 요약 확인(Inspect)

        [DebugAction(
            category = "Shapeshifter Framework",
            name = "Inspect Active Form...",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        private static void InspectActiveForm(Pawn pawn)
        {
            // Dev 모드/유효 Pawn만 허용
            if (!Prefs.DevMode || pawn == null || pawn.DestroyedOrNull()) return;

            // HediffComp_ShapeshiftCore 기반 조회
            ShapeshiftCoreUtility.TryGetCore(pawn, out var comp);
            var form = comp != null ? comp.currentForm : null;
            var opts = new List<FloatMenuOption>();

            if (form == null)
            {
                opts.Add(new FloatMenuOption("No active form", null));
                Find.WindowStack.Add(new FloatMenu(opts));
                return;
            }

            // 상단 요약
            string head = $"Form: {form.defName}  (labelKey: {form.label ?? "null"})";
            opts.Add(new FloatMenuOption(head, null));

            // 그래픽/파츠 제어(요약)
            opts.Add(new FloatMenuOption(SummarizeParts(form), null));

            // 장비/무기 숨김/표시
            opts.Add(new FloatMenuOption(SummarizeRenderFilters(form), null));

            // Verb/Tool
            opts.Add(new FloatMenuOption(SummarizeVerbsTools(form), null));

            // 스탯/캐퍼
            opts.Add(new FloatMenuOption(SummarizeStatsCaps(form), null));

            // 사운드/혈흔
            opts.Add(new FloatMenuOption(SummarizeSounds(form), null));
            opts.Add(new FloatMenuOption(SummarizeBlood(form), null));

            // 전문 덤프 메뉴
            opts.Add(new FloatMenuOption("Dump to log", () =>
            {
                var sb = new StringBuilder(2048);
                BuildFullDump(pawn, form, sb);
                Log.Message(sb.ToString());
                Messages.Message("Shapeshifter: dumped to log.", MessageTypeDefOf.TaskCompletion, false);
            }));

            Find.WindowStack.Add(new FloatMenu(opts));
        }

        #endregion

        #region 2) 폼 사운드 즉시 재생(Play)

        [DebugAction(
            category = "Shapeshifter Framework",
            name = "Play Form Sounds...",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        private static void PlayFormSounds(Pawn pawn)
        {
            // Dev 모드/유효 Pawn만 허용
            if (!Prefs.DevMode || pawn == null || pawn.DestroyedOrNull()) return;

            // HediffComp_ShapeshiftCore 기반 조회
            ShapeshiftCoreUtility.TryGetCore(pawn, out var comp);
            var form = comp != null ? comp.currentForm : null;
            var opts = new List<FloatMenuOption>();

            if (form == null)
            {
                opts.Add(new FloatMenuOption("No active form", null));
                Find.WindowStack.Add(new FloatMenu(opts));
                return;
            }

            // 폼에 정의된 각 사운드를 재생 옵션으로 노출
            TryAddPlayOption(opts, pawn, form.soundCall, "soundCall");
            TryAddPlayOption(opts, pawn, form.soundAngry, "soundAngry");
            TryAddPlayOption(opts, pawn, form.soundWounded, "soundWounded");
            TryAddPlayOption(opts, pawn, form.soundDeath, "soundDeath");
            TryAddPlayOption(opts, pawn, form.soundEating, "soundEating");

            if (opts.Count == 0)
                opts.Add(new FloatMenuOption("No sounds defined on this form", null));

            Find.WindowStack.Add(new FloatMenu(opts));
        }

        #endregion

        #region 3) 폰 상태 전문 로그 덤프(Dump)

        [DebugAction(
            category = "Shapeshifter Framework",
            name = "Dump Pawn State to Log",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        private static void DumpActiveFormToLog(Pawn pawn)
        {
            // Dev 모드/유효 Pawn만 허용
            if (!Prefs.DevMode || pawn == null || pawn.DestroyedOrNull()) return;

            // HediffComp_ShapeshiftCore 기반 조회
            ShapeshiftCoreUtility.TryGetCore(pawn, out var comp);
            var form = comp != null ? comp.currentForm : null;

            var sb = new StringBuilder(2048);
            BuildFullDump(pawn, form, sb);
            Log.Message(sb.ToString());
            Messages.Message("Shapeshifter: dumped to log.", MessageTypeDefOf.TaskCompletion, false);
        }

        #endregion

        #region 4) 전체 폼 자동 검증(Auto-Verify)

        [DebugAction(
            category = "Shapeshifter Framework",
            name = "Auto-Verify All Forms...",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        private static void AutoVerifyAllForms(Pawn pawn)
        {
            if (!Prefs.DevMode || pawn == null || pawn.DestroyedOrNull()) return;

            // HediffComp_ShapeshiftCore 기반 조회 — 없으면 ShapeshiftCore comp를 가진 HediffDef로 부트스트랩
            if (!ShapeshiftCoreUtility.TryGetCore(pawn, out var comp))
            {
                // HediffCompProperties_ShapeshiftCore를 가진 HediffDef 검색
                HediffDef bootstrapDef = null;
                var allHediffDefs = DefDatabase<HediffDef>.AllDefsListForReading;
                for (int i = 0; i < allHediffDefs.Count; i++)
                {
                    var hd = allHediffDefs[i];
                    if (hd?.comps == null) continue;
                    for (int c = 0; c < hd.comps.Count; c++)
                    {
                        if (hd.comps[c] is HediffCompProperties_ShapeshiftCore)
                        { bootstrapDef = hd; break; }
                    }
                    if (bootstrapDef != null) break;
                }
                if (bootstrapDef == null)
                {
                    Log.Warning("[SSF-Test] No HediffDef with HediffCompProperties_ShapeshiftCore found.");
                    return;
                }
                ShapeshiftCoreUtility.GiveShiftHediff(pawn, bootstrapDef);
                if (!ShapeshiftCoreUtility.TryGetCore(pawn, out comp))
                {
                    Log.Warning("[SSF-Test] Failed to bootstrap HediffComp_ShapeshiftCore.");
                    return;
                }
            }

            // 기존 변신 해제
            if (comp.isTransformed) comp.RemoveForm();

            var allForms = DefDatabase<ShapeshiftFormDef>.AllDefsListForReading;
            var sb = new StringBuilder(4096);
            sb.AppendLine($"[SSF-Test] ═══ Auto-Verify All Forms for {pawn.LabelCap} ({pawn.ThingID}) ═══");
            sb.AppendLine($"  Total forms registered: {allForms.Count}");
            sb.AppendLine();

            int totalPass = 0, totalFail = 0, totalSkip = 0;

            // 원본 상태 스냅샷
            BodyTypeDef origBody = pawn.story?.bodyType;
            HeadTypeDef origHead = pawn.story?.headType;
            Color? origHair = pawn.story != null ? (Color?)pawn.story.HairColor : null;
            Color? origSkin = pawn.story?.skinColorOverride;

            for (int f = 0; f < allForms.Count; f++)
            {
                var form = allForms[f];
                if (form == null) continue;

                sb.AppendLine($"── [{f+1}/{allForms.Count}] {form.defName} ──");

                // 변신 전 장비 스냅샷 (Drop/Inventory 검증용)
                var prevApparelSnapshot = new List<Thing>();
                var prevWeaponSnapshot = new List<Thing>();
                if (pawn.apparel != null)
                {
                    var worn = pawn.apparel.WornApparel;
                    for (int a = 0; a < worn.Count; a++)
                        if (worn[a] != null) prevApparelSnapshot.Add(worn[a]);
                }
                if (pawn.equipment?.Primary != null)
                    prevWeaponSnapshot.Add(pawn.equipment.Primary);

                // 변신 전 addHediff 기존 존재 여부 스냅샷
                var preExistingAddHediffs = new HashSet<HediffDef>();
                if (form.addHediffs != null && pawn.health?.hediffSet != null)
                {
                    for (int h = 0; h < form.addHediffs.Count; h++)
                    {
                        var entry = form.addHediffs[h];
                        if (entry?.hediff == null || entry.hediff.addedPartProps != null) continue;
                        if (pawn.health.hediffSet.GetFirstHediffOfDef(entry.hediff) != null)
                            preExistingAddHediffs.Add(entry.hediff);
                    }
                }

                // 변신 시도
                try
                {
                    comp.ApplyForm(form, "None");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  ✗ CRASH on ApplyForm: {ex.Message}");
                    totalFail++;
                    sb.AppendLine();
                    continue;
                }

                if (!comp.isTransformed || comp.currentForm != form)
                {
                    sb.AppendLine($"  - Skipped (eligibility filter or apply failed)");
                    totalSkip++;
                    sb.AppendLine();
                    continue;
                }

                // ── 변신 상태 검증 ──
                int checks = 0, passed = 0;

                string fn = form.defName; // 체크리스트 매핑용 폼 이름

                // 1. 변신 hediff 존재 (스탯은 HediffDef stages에서 제공)
                {
                    checks++;
                    string cl = CL(fn, "statHediff");
                    var hediffDef = comp.parent?.def;
                    if (hediffDef != null && pawn.health?.hediffSet?.GetFirstHediffOfDef(hediffDef) != null)
                    { passed++; sb.AppendLine($"  ✓ Shapeshift hediff OK: {hediffDef.defName}{cl}"); }
                    else sb.AppendLine($"  ✗ Shapeshift hediff missing: {hediffDef?.defName ?? "null"}{cl}");
                }

                // 2. 체형 변경
                if (form.bodyType != null && pawn.story != null)
                {
                    checks++;
                    string cl = CL(fn, "bodyType");
                    if (pawn.story.bodyType == form.bodyType)
                    { passed++; sb.AppendLine($"  ✓ BodyType OK: {form.bodyType.defName}{cl}"); }
                    else sb.AppendLine($"  ✗ BodyType expected={form.bodyType.defName} actual={pawn.story.bodyType?.defName}{cl}");
                }

                // 3. 머리형 변경
                if (form.headType != null && pawn.story != null)
                {
                    checks++;
                    string cl = CL(fn, "headType");
                    if (pawn.story.headType == form.headType)
                    { passed++; sb.AppendLine($"  ✓ HeadType OK: {form.headType.defName}{cl}"); }
                    else sb.AppendLine($"  ✗ HeadType expected={form.headType.defName} actual={pawn.story.headType?.defName}{cl}");
                }

                // 4. 머리색 변경
                if (form.hairColor.HasValue && pawn.story != null)
                {
                    checks++;
                    string cl = CL(fn, "hairColor");
                    if (ColorsClose(pawn.story.HairColor, form.hairColor.Value))
                    { passed++; sb.AppendLine($"  ✓ HairColor OK{cl}"); }
                    else sb.AppendLine($"  ✗ HairColor expected={form.hairColor.Value} actual={pawn.story.HairColor}{cl}");
                }

                // 5. 피부색 변경
                if (form.skinColor.HasValue && pawn.story != null)
                {
                    checks++;
                    string cl = CL(fn, "skinColor");
                    Color? actual = pawn.story.skinColorOverride;
                    if (actual.HasValue && ColorsClose(actual.Value, form.skinColor.Value))
                    { passed++; sb.AppendLine($"  ✓ SkinColor OK{cl}"); }
                    else sb.AppendLine($"  ✗ SkinColor expected={form.skinColor.Value} actual={actual}{cl}");
                }

                // 6. 추가 hediff
                if (form.addHediffs != null)
                {
                    for (int h = 0; h < form.addHediffs.Count; h++)
                    {
                        var entry = form.addHediffs[h];
                        if (entry?.hediff == null) continue;
                        checks++;
                        string cl = CL(fn, "addHediff", entry.hediff.defName);
                        if (pawn.health?.hediffSet?.GetFirstHediffOfDef(entry.hediff) != null)
                        { passed++; sb.AppendLine($"  ✓ addHediff OK: {entry.hediff.defName}{cl}"); }
                        else sb.AppendLine($"  ✗ addHediff missing: {entry.hediff.defName}{cl}");
                    }
                }

                // 7. 추가 어빌리티
                if (form.addAbilities != null)
                {
                    for (int a = 0; a < form.addAbilities.Count; a++)
                    {
                        var aDef = form.addAbilities[a];
                        if (aDef == null) continue;
                        checks++;
                        string cl = CL(fn, "addAbility", aDef.defName);
                        if (pawn.abilities?.GetAbility(aDef) != null)
                        { passed++; sb.AppendLine($"  ✓ addAbility OK: {aDef.defName}{cl}"); }
                        else sb.AppendLine($"  ✗ addAbility missing: {aDef.defName}{cl}");
                    }
                }

                // 8. 소환 장비
                if (form.spawnApparelOnTransform != null)
                {
                    for (int s = 0; s < form.spawnApparelOnTransform.Count; s++)
                    {
                        var apDef = form.spawnApparelOnTransform[s];
                        if (apDef == null) continue;
                        checks++;
                        string cl = CL(fn, "spawnApparel");
                        bool found = false;
                        if (pawn.apparel != null)
                        {
                            var worn = pawn.apparel.WornApparel;
                            for (int w = 0; w < worn.Count; w++)
                                if (worn[w].def == apDef) { found = true; break; }
                        }
                        if (found) { passed++; sb.AppendLine($"  ✓ Spawn apparel OK: {apDef.defName}{cl}"); }
                        else sb.AppendLine($"  ✗ Spawn apparel missing: {apDef.defName}{cl}");
                    }
                }

                if (form.spawnWeaponOnTransform != null)
                {
                    for (int s = 0; s < form.spawnWeaponOnTransform.Count; s++)
                    {
                        var wpDef = form.spawnWeaponOnTransform[s];
                        if (wpDef == null) continue;
                        checks++;
                        string cl = CL(fn, "spawnWeapon");
                        bool found = pawn.equipment?.Primary?.def == wpDef;
                        if (found) { passed++; sb.AppendLine($"  ✓ Spawn weapon OK: {wpDef.defName}{cl}"); }
                        else sb.AppendLine($"  ✗ Spawn weapon missing: {wpDef.defName}{cl}");
                    }
                }

                // 9. VerbTracker
                bool hasVerbs = form.verbs != null && form.verbs.Count > 0;
                bool hasTools = form.tools != null && form.tools.Count > 0;
                if (hasVerbs || hasTools)
                {
                    checks++;
                    string cl = CL(fn, "verbTracker");
                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt != null && vt.AllVerbs != null && vt.AllVerbs.Count > 0)
                    { passed++; sb.AppendLine($"  ✓ VerbTracker OK (count={vt.AllVerbs.Count}){cl}"); }
                    else sb.AppendLine($"  ✗ VerbTracker null/empty (verbs={form.verbs?.Count ?? 0}, tools={form.tools?.Count ?? 0}){cl}");
                }

                // 10. 런타임 캐시
                if (form.soundCall != null)
                {
                    checks++;
                    string cl = CL(fn, "soundCache");
                    SoundDef cached;
                    if (ShapeshiftRuntimeCaches.CallByPawn.TryGetValue(pawn, out cached) && cached == form.soundCall)
                    { passed++; sb.AppendLine($"  ✓ soundCall cache OK{cl}"); }
                    else sb.AppendLine($"  ✗ soundCall cache mismatch{cl}");
                }
                if (form.bloodDef != null)
                {
                    checks++;
                    string cl = CL(fn, "bloodCache");
                    ThingDef cached;
                    if (ShapeshiftRuntimeCaches.BloodByPawn.TryGetValue(pawn, out cached) && cached == form.bloodDef)
                    { passed++; sb.AppendLine($"  ✓ bloodDef cache OK{cl}"); }
                    else sb.AppendLine($"  ✗ bloodDef cache mismatch{cl}");
                }

                // 11. 타이머
                if (form.durationTicks.HasValue && form.durationTicks.Value > 0)
                {
                    checks++;
                    string cl = CL(fn, "timer");
                    if (comp.RemainingShapeshiftTicks > 0)
                    { passed++; sb.AppendLine($"  ✓ Timer OK (~{form.durationTicks.Value} ticks){cl}"); }
                    else sb.AppendLine($"  ✗ Timer not set (expected ~{form.durationTicks.Value}){cl}");
                }

                // 12. 소환 장비 재질(stuff) 검증
                if (form.spawnApparelOnTransform != null && form.spawnApparelStuff != null && pawn.apparel != null)
                {
                    var worn = pawn.apparel.WornApparel;
                    for (int s = 0; s < form.spawnApparelOnTransform.Count; s++)
                    {
                        var apDef = form.spawnApparelOnTransform[s];
                        if (apDef == null) continue;
                        checks++;
                        string cl = CL(fn, "stuffApparel");
                        bool stuffOk = false;
                        for (int w = 0; w < worn.Count; w++)
                        {
                            if (worn[w].def == apDef && worn[w].Stuff == form.spawnApparelStuff)
                            { stuffOk = true; break; }
                        }
                        if (stuffOk) { passed++; sb.AppendLine($"  ✓ Apparel stuff OK: {apDef.defName} ({form.spawnApparelStuff.defName}){cl}"); }
                        else sb.AppendLine($"  ✗ Apparel stuff wrong: {apDef.defName} expected={form.spawnApparelStuff.defName}{cl}");
                    }
                }
                if (form.spawnWeaponOnTransform != null && form.spawnWeaponStuff != null)
                {
                    for (int s = 0; s < form.spawnWeaponOnTransform.Count; s++)
                    {
                        var wpDef = form.spawnWeaponOnTransform[s];
                        if (wpDef == null) continue;
                        checks++;
                        string cl = CL(fn, "stuffWeapon");
                        bool stuffOk = pawn.equipment?.Primary?.def == wpDef && pawn.equipment.Primary.Stuff == form.spawnWeaponStuff;
                        if (stuffOk) { passed++; sb.AppendLine($"  ✓ Weapon stuff OK: {wpDef.defName} ({form.spawnWeaponStuff.defName}){cl}"); }
                        else sb.AppendLine($"  ✗ Weapon stuff wrong: {wpDef.defName} expected={form.spawnWeaponStuff.defName}{cl}");
                    }
                }

                // 13. 장비 처리 모드 검증 (Inventory/Drop)
                if (form.apparelOnTransform == GearHandling.Inventory && prevApparelSnapshot.Count > 0)
                {
                    checks++;
                    string cl = CL(fn, "gearApparel");
                    bool allInInv = true;
                    for (int pa = 0; pa < prevApparelSnapshot.Count; pa++)
                    {
                        if (prevApparelSnapshot[pa] == null || prevApparelSnapshot[pa].Destroyed) continue;
                        if (!pawn.inventory.innerContainer.Contains(prevApparelSnapshot[pa]))
                        { allInInv = false; break; }
                    }
                    if (allInInv) { passed++; sb.AppendLine($"  ✓ Apparel→Inventory OK{cl}"); }
                    else sb.AppendLine($"  ✗ Apparel→Inventory FAIL: prev apparel not in inventory{cl}");
                }
                if (form.weaponsOnTransform == GearHandling.Inventory && prevWeaponSnapshot.Count > 0)
                {
                    checks++;
                    string cl = CL(fn, "gearWeapon");
                    bool allInInv = true;
                    for (int pw = 0; pw < prevWeaponSnapshot.Count; pw++)
                    {
                        if (prevWeaponSnapshot[pw] == null || prevWeaponSnapshot[pw].Destroyed) continue;
                        if (!pawn.inventory.innerContainer.Contains(prevWeaponSnapshot[pw]))
                        { allInInv = false; break; }
                    }
                    if (allInInv) { passed++; sb.AppendLine($"  ✓ Weapon→Inventory OK{cl}"); }
                    else sb.AppendLine($"  ✗ Weapon→Inventory FAIL: prev weapons not in inventory{cl}");
                }
                if (form.apparelOnTransform == GearHandling.Drop && prevApparelSnapshot.Count > 0)
                {
                    checks++;
                    string cl = CL(fn, "gearApparel");
                    bool anyOnGround = false;
                    for (int pa = 0; pa < prevApparelSnapshot.Count; pa++)
                    {
                        if (prevApparelSnapshot[pa] != null && !prevApparelSnapshot[pa].Destroyed && prevApparelSnapshot[pa].Spawned)
                        { anyOnGround = true; break; }
                    }
                    if (anyOnGround) { passed++; sb.AppendLine($"  ✓ Apparel→Drop OK{cl}"); }
                    else sb.AppendLine($"  ✗ Apparel→Drop FAIL: prev apparel not on ground{cl}");
                }
                if (form.weaponsOnTransform == GearHandling.Drop && prevWeaponSnapshot.Count > 0)
                {
                    checks++;
                    string cl = CL(fn, "gearWeapon");
                    bool anyOnGround = false;
                    for (int pw = 0; pw < prevWeaponSnapshot.Count; pw++)
                    {
                        if (prevWeaponSnapshot[pw] != null && !prevWeaponSnapshot[pw].Destroyed && prevWeaponSnapshot[pw].Spawned)
                        { anyOnGround = true; break; }
                    }
                    if (anyOnGround) { passed++; sb.AppendLine($"  ✓ Weapon→Drop OK{cl}"); }
                    else sb.AppendLine($"  ✗ Weapon→Drop FAIL: prev weapons not on ground{cl}");
                }

                // 14. 장비 잠금(EquipLock) 검증
                {
                    bool expectApparelLock = ShapeshiftEquipRules.LockApparel(comp);
                    bool expectWeaponLock = ShapeshiftEquipRules.LockWeapons(comp);
                    if (form.apparelEquipLock != EquipLockMode.Auto || form.apparelOnTransform != GearHandling.Keep)
                    {
                        checks++;
                        string cl = CL(fn, "equipLockApparel");
                        bool expectedLock = form.apparelEquipLock == EquipLockMode.Locked ||
                            (form.apparelEquipLock == EquipLockMode.Auto && form.apparelOnTransform != GearHandling.Keep);
                        if (expectApparelLock == expectedLock)
                        { passed++; sb.AppendLine($"  ✓ Apparel EquipLock OK (locked={expectedLock}){cl}"); }
                        else sb.AppendLine($"  ✗ Apparel EquipLock: expected={expectedLock} actual={expectApparelLock}{cl}");
                    }
                    if (form.weaponEquipLock != EquipLockMode.Auto || form.weaponsOnTransform != GearHandling.Keep)
                    {
                        checks++;
                        string cl = CL(fn, "equipLockWeapon");
                        bool expectedLock = form.weaponEquipLock == EquipLockMode.Locked ||
                            (form.weaponEquipLock == EquipLockMode.Auto && form.weaponsOnTransform != GearHandling.Keep);
                        if (expectWeaponLock == expectedLock)
                        { passed++; sb.AppendLine($"  ✓ Weapon EquipLock OK (locked={expectedLock}){cl}"); }
                        else sb.AppendLine($"  ✗ Weapon EquipLock: expected={expectedLock} actual={expectWeaponLock}{cl}");
                    }
                }

                // 15. 작업 제한 검증 (disabledWorkTags / disabledWorkTypes)
                if (form.disabledWorkTagsOnTransform != WorkTags.None)
                {
                    checks++;
                    string cl = CL(fn, "workTags");
                    var disabled = pawn.GetDisabledWorkTypes(permanentOnly: false);
                    var allWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
                    bool tagOk = true;
                    for (int wt = 0; wt < allWorkTypes.Count; wt++)
                    {
                        var wd = allWorkTypes[wt];
                        if (wd == null) continue;
                        if ((wd.workTags & form.disabledWorkTagsOnTransform) != WorkTags.None)
                        {
                            if (!disabled.Contains(wd))
                            { tagOk = false; sb.AppendLine($"  ✗ WorkTag block missing: {wd.defName}{cl}"); break; }
                        }
                    }
                    if (tagOk) { passed++; sb.AppendLine($"  ✓ WorkTag restrictions OK{cl}"); }
                }
                if (form.disabledWorkTypesOnTransform != null && form.disabledWorkTypesOnTransform.Count > 0)
                {
                    checks++;
                    string cl = CL(fn, "workTypes");
                    var disabled = pawn.GetDisabledWorkTypes(permanentOnly: false);
                    bool typeOk = true;
                    for (int wt = 0; wt < form.disabledWorkTypesOnTransform.Count; wt++)
                    {
                        var wd = form.disabledWorkTypesOnTransform[wt];
                        if (wd == null) continue;
                        if (!disabled.Contains(wd))
                        { typeOk = false; sb.AppendLine($"  ✗ WorkType block missing: {wd.defName}{cl}"); break; }
                    }
                    if (typeOk) { passed++; sb.AppendLine($"  ✓ WorkType restrictions OK{cl}"); }
                }

                sb.AppendLine($"  → Apply: {passed}/{checks} passed");
                bool applyOk = (passed == checks);

                // ── 해제 검증 ──
                int rc = 0, rp = 0;
                try { comp.RemoveForm(); }
                catch (Exception ex)
                {
                    sb.AppendLine($"  ✗ CRASH on RemoveForm: {ex.Message}");
                    totalFail++;
                    sb.AppendLine();
                    continue;
                }

                // isTransformed 해제
                rc++;
                if (!comp.isTransformed) { rp++; sb.AppendLine($"  ✓ [Revert] isTransformed=false"); }
                else sb.AppendLine($"  ✗ [Revert] Still transformed after RemoveForm");

                // 체형/머리형 원복
                if (origBody != null && pawn.story != null)
                {
                    rc++;
                    string cl = CL(fn, "R.bodyType");
                    if (pawn.story.bodyType == origBody) { rp++; sb.AppendLine($"  ✓ [Revert] BodyType restored{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] BodyType not restored: {origBody.defName} vs {pawn.story.bodyType?.defName}{cl}");
                }
                if (origHead != null && pawn.story != null)
                {
                    rc++;
                    string cl = CL(fn, "R.headType");
                    if (pawn.story.headType == origHead) { rp++; sb.AppendLine($"  ✓ [Revert] HeadType restored{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] HeadType not restored: {origHead.defName} vs {pawn.story.headType?.defName}{cl}");
                }

                // 컬러 원복
                if (origHair.HasValue && pawn.story != null)
                {
                    rc++;
                    string cl = CL(fn, "R.hairColor");
                    if (ColorsClose(pawn.story.HairColor, origHair.Value)) { rp++; sb.AppendLine($"  ✓ [Revert] HairColor restored{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] HairColor not restored{cl}");
                }
                if (pawn.story != null)
                {
                    rc++;
                    string cl = CL(fn, "R.skinColor");
                    if (pawn.story.skinColorOverride == origSkin) { rp++; sb.AppendLine($"  ✓ [Revert] SkinColor restored{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] SkinColor not restored{cl}");
                }

                // 변신 폼 해제 확인
                {
                    rc++;
                    string cl = CL(fn, "R.statHediff");
                    if (comp.currentForm == null)
                    { rp++; sb.AppendLine($"  ✓ [Revert] currentForm cleared{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] currentForm not cleared: {comp.currentForm.defName}{cl}");
                }

                // 추가 hediff 제거 (addedPart 제외, 변신 전 기존 존재분 스킵)
                if (form.addHediffs != null)
                {
                    for (int h = 0; h < form.addHediffs.Count; h++)
                    {
                        var entry = form.addHediffs[h];
                        if (entry?.hediff == null || entry.hediff.addedPartProps != null) continue;
                        // 변신 전부터 존재했던 hediff는 폼이 생성하지 않았으므로 검증 스킵
                        if (preExistingAddHediffs.Contains(entry.hediff))
                        {
                            sb.AppendLine($"  ⊘ [Revert] addHediff pre-existing (skip): {entry.hediff.defName}");
                            continue;
                        }
                        rc++;
                        string cl = CL(fn, "R.addHediff", entry.hediff.defName);
                        if (pawn.health?.hediffSet?.GetFirstHediffOfDef(entry.hediff) == null)
                        { rp++; sb.AppendLine($"  ✓ [Revert] addHediff removed: {entry.hediff.defName}{cl}"); }
                        else sb.AppendLine($"  ✗ [Revert] addHediff not removed: {entry.hediff.defName}{cl}");
                    }
                }

                // 추가 어빌리티 제거
                if (form.addAbilities != null)
                {
                    for (int a = 0; a < form.addAbilities.Count; a++)
                    {
                        var aDef = form.addAbilities[a];
                        if (aDef == null) continue;
                        rc++;
                        string cl = CL(fn, "R.addAbility", aDef.defName);
                        if (pawn.abilities?.GetAbility(aDef) == null)
                        { rp++; sb.AppendLine($"  ✓ [Revert] addAbility removed: {aDef.defName}{cl}"); }
                        else sb.AppendLine($"  ✗ [Revert] addAbility not removed: {aDef.defName}{cl}");
                    }
                }

                // 캐시 정리
                rc++;
                SoundDef __c;
                if (!ShapeshiftRuntimeCaches.CallByPawn.TryGetValue(pawn, out __c))
                { rp++; sb.AppendLine($"  ✓ [Revert] Runtime cache cleared"); }
                else sb.AppendLine($"  ✗ [Revert] Runtime cache not cleared");

                // 소환 장비 파괴 확인
                if (form.spawnApparelOnTransform != null && form.spawnApparelOnTransform.Count > 0)
                {
                    rc++;
                    string cl = CL(fn, "R.spawnApparel");
                    bool anySpawnedRemains = false;
                    if (pawn.apparel != null)
                    {
                        var worn = pawn.apparel.WornApparel;
                        for (int s = 0; s < form.spawnApparelOnTransform.Count; s++)
                        {
                            var apDef = form.spawnApparelOnTransform[s];
                            if (apDef == null) continue;
                            for (int w = 0; w < worn.Count; w++)
                                if (worn[w].def == apDef) { anySpawnedRemains = true; break; }
                            if (anySpawnedRemains) break;
                        }
                    }
                    if (!anySpawnedRemains) { rp++; sb.AppendLine($"  ✓ [Revert] Spawned apparel destroyed{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] Spawned apparel not destroyed{cl}");
                }
                if (form.spawnWeaponOnTransform != null && form.spawnWeaponOnTransform.Count > 0)
                {
                    rc++;
                    string cl = CL(fn, "R.spawnWeapon");
                    bool weaponRemains = false;
                    if (pawn.equipment?.Primary != null)
                    {
                        for (int s = 0; s < form.spawnWeaponOnTransform.Count; s++)
                        {
                            if (pawn.equipment.Primary.def == form.spawnWeaponOnTransform[s])
                            { weaponRemains = true; break; }
                        }
                    }
                    if (!weaponRemains) { rp++; sb.AppendLine($"  ✓ [Revert] Spawned weapon destroyed{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] Spawned weapon not destroyed{cl}");
                }

                // 작업 제한 해제 확인
                if (form.disabledWorkTagsOnTransform != WorkTags.None || (form.disabledWorkTypesOnTransform != null && form.disabledWorkTypesOnTransform.Count > 0))
                {
                    rc++;
                    if (!comp.isTransformed) { rp++; sb.AppendLine($"  ✓ [Revert] Work restrictions cleared"); }
                    else sb.AppendLine($"  ✗ [Revert] Work restrictions not cleared");
                }

                // 장비 잠금 해제 확인
                {
                    rc++;
                    string cl = CL(fn, "R.equipLock");
                    bool lockCleared = !ShapeshiftEquipRules.LockApparel(comp) && !ShapeshiftEquipRules.LockWeapons(comp);
                    if (lockCleared) { rp++; sb.AppendLine($"  ✓ [Revert] EquipLock cleared{cl}"); }
                    else sb.AppendLine($"  ✗ [Revert] EquipLock still active{cl}");
                }

                sb.AppendLine($"  → Revert: {rp}/{rc} passed");
                bool revertOk = (rp == rc);

                if (applyOk && revertOk) totalPass++;
                else totalFail++;

                sb.AppendLine();
            }

            sb.AppendLine("═══ Summary ═══");
            sb.AppendLine($"  Forms: {allForms.Count}  |  Pass: {totalPass}  |  Fail: {totalFail}  |  Skip: {totalSkip}");
            if (totalFail == 0) sb.AppendLine("  ★ ALL CHECKS PASSED ★");
            else sb.AppendLine($"  ⚠ {totalFail} form(s) had failures");

            Log.Message(sb.ToString());
            Messages.Message($"SSF Auto-Verify: {totalPass} pass, {totalFail} fail, {totalSkip} skip — see log", MessageTypeDefOf.TaskCompletion, false);
        }

        #region 체크리스트 매핑 (TEST_CHECKLIST.md 항목 번호 연동)

        // key = "formDefName|checkCategory" 또는 "formDefName|checkCategory|subKey"
        // value = 체크리스트 항목 번호 (예: "#009~#014")
        private static readonly Dictionary<string, string> CMap = new Dictionary<string, string>
        {
            // ── §1 BearForm ──
            {"SSFTest_BearForm|statHediff",       "1-AV1"},
            {"SSFTest_BearForm|addHediff|FibrousMechanites", "1-AV2"},
            {"SSFTest_BearForm|addHediff|SSFTest_BeastArm",  "1-AV2"},
            {"SSFTest_BearForm|addAbility|Berserk",          "1-AV3"},
            {"SSFTest_BearForm|gearApparel",      "1-AV4"},
            {"SSFTest_BearForm|gearWeapon",       "1-AV4"},
            {"SSFTest_BearForm|verbTracker",      "1-AV5"},
            {"SSFTest_BearForm|bloodCache",       "1-AV6"},
            {"SSFTest_BearForm|timer",            "1-AV7"},
            {"SSFTest_BearForm|R.statHediff",     "1-AV8"},
            {"SSFTest_BearForm|R.addAbility|Berserk", "1-AV8"},
            {"SSFTest_BearForm|R.bodyType",       "1-AV8"},
            {"SSFTest_BearForm|R.addHediff|FibrousMechanites", "1-AV8"},

            // ── §2 BearWarriorForm ──
            {"SSFTest_BearWarriorForm|statHediff",  "2-AV1"},
            {"SSFTest_BearWarriorForm|soundCache",  "2-AV2"},
            {"SSFTest_BearWarriorForm|timer",       "2-AV3"},

            // ── §3 SheepForm ──
            {"SSFTest_SheepForm|statHediff",      "3-AV1"},
            {"SSFTest_SheepForm|workTags",        "3-AV2"},
            {"SSFTest_SheepForm|timer",           "3-AV3"},

            // ── §4 DarkKnightForm ──
            {"SSFTest_DarkKnightForm|statHediff",       "4-AV1"},
            {"SSFTest_DarkKnightForm|gearApparel",      "4-AV2"},
            {"SSFTest_DarkKnightForm|gearWeapon",       "4-AV2"},
            {"SSFTest_DarkKnightForm|spawnApparel",     "4-AV3"},
            {"SSFTest_DarkKnightForm|spawnWeapon",      "4-AV3"},
            {"SSFTest_DarkKnightForm|stuffApparel",     "4-AV4"},
            {"SSFTest_DarkKnightForm|stuffWeapon",      "4-AV4"},
            {"SSFTest_DarkKnightForm|equipLockApparel", "4-AV5"},
            {"SSFTest_DarkKnightForm|equipLockWeapon",  "4-AV5"},
            {"SSFTest_DarkKnightForm|timer",            "4-AV6"},
            {"SSFTest_DarkKnightForm|R.spawnApparel",   "4-AV7"},
            {"SSFTest_DarkKnightForm|R.spawnWeapon",    "4-AV7"},
            {"SSFTest_DarkKnightForm|R.equipLock",      "4-AV7"},

            // ── §5 BeastkinForm ──
            {"SSFTest_BeastkinForm|hairColor",    "5-AV1"},
            {"SSFTest_BeastkinForm|verbTracker",  "5-AV2"},
            {"SSFTest_BeastkinForm|gearApparel",  "5-AV3"},
            {"SSFTest_BeastkinForm|addHediff|FibrousMechanites", "5-AV4"},
            {"SSFTest_BeastkinForm|addAbility|Waterskip", "5-AV5"},
            {"SSFTest_BeastkinForm|workTags",     "5-AV6"},
            {"SSFTest_BeastkinForm|R.hairColor",  "5-AV7"},
            {"SSFTest_BeastkinForm|R.addAbility|Waterskip", "5-AV7"},

            // ── §6 FullBeastForm ──
            {"SSFTest_FullBeastForm|statHediff",  "6-AV1"},
            {"SSFTest_FullBeastForm|verbTracker", "6-AV2"},
            {"SSFTest_FullBeastForm|timer",       "6-AV3"},

            // ── §7 GuardianForm ──
            {"SSFTest_GuardianForm|statHediff",   "7-AV1"},
            {"SSFTest_GuardianForm|timer",        "7-AV2"},

            // ── §8 PhantomForm ──
            {"SSFTest_PhantomForm|bodyType",      "8-AV1"},
            {"SSFTest_PhantomForm|skinColor",     "8-AV2"},
            {"SSFTest_PhantomForm|workTypes",     "8-AV3"},
            {"SSFTest_PhantomForm|timer",         "8-AV4"},
            {"SSFTest_PhantomForm|R.bodyType",    "8-AV5"},
            {"SSFTest_PhantomForm|R.skinColor",   "8-AV5"},
            {"SSFTest_PhantomForm|R.headType",    "8-AV5"},

            // ── §9 RaceLockedForm ──
            {"SSFTest_RaceLockedForm|raceFilter",        "9-AV1"},
            {"SSFTest_RaceLockedForm|headType",          "9-AV2"},
            {"SSFTest_RaceLockedForm|equipLockApparel",  "9-AV3"},
            {"SSFTest_RaceLockedForm|equipLockWeapon",   "9-AV3"},
            {"SSFTest_RaceLockedForm|statHediff",        "9-AV4"},
            {"SSFTest_RaceLockedForm|timer",             "9-AV5"},
        };

        /// <summary>체크리스트 참조 문자열 반환. 매핑 없으면 빈 문자열.</summary>
        private static string CL(string formDef, string category, string sub = null)
        {
            string key = sub != null ? $"{formDef}|{category}|{sub}" : $"{formDef}|{category}";
            string items;
            return CMap.TryGetValue(key, out items) ? $" [{items}]" : "";
        }

        #endregion

        private static bool ColorsClose(Color a, Color b, float tolerance = 0.02f)
        {
            return Mathf.Abs(a.r - b.r) < tolerance
                && Mathf.Abs(a.g - b.g) < tolerance
                && Mathf.Abs(a.b - b.b) < tolerance;
        }

        #endregion

        #region 헬퍼(플로트 메뉴·요약 문자열·덤프 빌더)

        /// <summary>SoundDef 재생용 플로트 메뉴 항목 추가.</summary>
        private static void TryAddPlayOption(List<FloatMenuOption> opts, Pawn pawn, SoundDef def, string label)
        {
            if (def == null) return;
            opts.Add(new FloatMenuOption($"Play {label}", () =>
            {
                try
                {
                    var target = new TargetInfo(pawn.Position, pawn.Map);
                    if (def.sustain)
                    {
                        // Sustainer(지속음)은 짧게 스폰 후 즉시 종료
                        var sustainer = def.TrySpawnSustainer(SoundInfo.InMap(target, MaintenanceType.None));
                        sustainer?.End();
                    }
                    else
                    {
                        def.PlayOneShot(target);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[SSF] Failed to play {label}: {e}");
                }
            }));
        }

        /// <summary>파츠 모드 요약.</summary>
        private static string SummarizeParts(ShapeshiftFormDef f)
        {
            // body/head/hair/beard/tattoo* 의 PartControlMode 요약 (성별 분기는 간단 표기)
            return $"Parts: body={ModeOf(f.body)} head={ModeOf(f.head)} hair={ModeOf(f.hair)} beard={ModeOf(f.beard)} tBody={ModeOf(f.tattooBody)} tHead={ModeOf(f.tattooHead)}";
        }

        private static string ModeOf(PartOverrideOption opt)
        {
            if (opt == null) return "null";
            // 공용 모드 우선 노출 (성별 노드는 상세 덤프에서 확인)
            return opt.mode.ToString();
        }

        /// <summary>렌더 필터 요약.</summary>
        private static string SummarizeRenderFilters(ShapeshiftFormDef f)
        {
            return $"Render filters: apparel(Hide={CountOrAll(f.renderHideApparelLayers, f.renderHideApparelDefNames)}, Show={CountOrAll(f.renderShowApparelLayers, f.renderShowApparelDefNames)}) " +
                   $"weapon(Hide={CountOrAll(f.renderHideWeaponTags, f.renderHideWeaponDefNames)}, Show={CountOrAll(f.renderShowWeaponTags, f.renderShowWeaponDefNames)}) " +
                   $"genes(Hide={CountOrAll(f.renderHideGeneExclusionTags, f.renderHideGeneDefNames)}, Show={CountOrAll(f.renderShowGeneExclusionTags, f.renderShowGeneDefNames)})";
        }

        private static string CountOrAll(List<string> a, List<string> b)
        {
            bool allA = ContainsAll(a);
            bool allB = ContainsAll(b);
            if (allA || allB) return "All";
            int ca = a != null ? a.Count : 0;
            int cb = b != null ? b.Count : 0;
            int sum = ca + cb;
            return sum.ToString();
        }

        private static bool ContainsAll(List<string> list)
        {
            if (list == null || list.Count == 0) return false;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == "All") return true;
            return false;
        }

        /// <summary>Verb/Tool 요약.</summary>
        private static string SummarizeVerbsTools(ShapeshiftFormDef f)
        {
            int v = f.verbs != null ? f.verbs.Count : 0;
            int t = f.tools != null ? f.tools.Count : 0;
            string rv = f.replaceNativeVerbs.HasValue ? (f.replaceNativeVerbs.Value ? "replace" : "keep") : "n/a";
            string rt = f.replaceNativeTools.HasValue ? (f.replaceNativeTools.Value ? "replace" : "keep") : "n/a";
            return $"Verbs/Tools: verbs={v}({rv}), tools={t}({rt})";
        }

        /// <summary>스탯/캐퍼 요약 (연결된 HediffDef의 첫 번째 stage에서 참조).</summary>
        private static string SummarizeStatsCaps(ShapeshiftFormDef f)
        {
            HediffDef hediffDef = null;
            if (ShapeshiftFormIndex.FormToHediffDefs.TryGetValue(f, out var hediffList) && hediffList.Count > 0)
                hediffDef = hediffList[0];
            var stage = hediffDef?.stages != null && hediffDef.stages.Count > 0 ? hediffDef.stages[0] : null;
            int so = stage?.statOffsets != null ? stage.statOffsets.Count : 0;
            int sf = stage?.statFactors != null ? stage.statFactors.Count : 0;
            int cm = stage?.capMods != null ? stage.capMods.Count : 0;
            string src = hediffDef?.defName ?? "none";
            return $"Stats ({src}): offsets={so}, factors={sf}, caps={cm}";
        }

        /// <summary>사운드 요약 (defName 표시).</summary>
        private static string SummarizeSounds(ShapeshiftFormDef f)
        {
            return $"Sounds: call={DefNameOrNull(f.soundCall)} angry={DefNameOrNull(f.soundAngry)} wounded={DefNameOrNull(f.soundWounded)} death={DefNameOrNull(f.soundDeath)} eat={DefNameOrNull(f.soundEating)} meleeHit={DefNameOrNull(f.soundMeleeHitPawn)} meleeMiss={DefNameOrNull(f.soundMeleeMiss)}";
        }

        /// <summary>혈흔/살점 요약 (defName 표시).</summary>
        private static string SummarizeBlood(ShapeshiftFormDef f)
        {
            return $"Blood/Flesh: blood={DefNameOrNull(f.bloodDef)} smear={DefNameOrNull(f.bloodSmearDef)} flesh={DefNameOrNull(f.fleshType)}";
        }

        private static string DefNameOrNull(Def d) => d != null ? d.defName : "null";

        private static string TryGetCachedName<T>(System.Runtime.CompilerServices.ConditionalWeakTable<Pawn, T> table, Pawn pawn) where T : class
        {
            if (pawn == null) return "null";
            T val;
            if (table.TryGetValue(pawn, out val) && val is Def def)
                return def.defName;
            return "null";
        }

        /// <summary>폰 상태 전체 덤프 빌드. form이 null이면 비변신 상태 기본 정보만 출력.</summary>
        private static void BuildFullDump(Pawn pawn, ShapeshiftFormDef f, StringBuilder sb)
        {
            sb.AppendLine($"[Shapeshifter] Dump for {pawn?.LabelCap} ({pawn?.ThingID})");
            sb.AppendLine($"  Race: {pawn?.def?.defName ?? "null"}  BodyType: {pawn?.story?.bodyType?.defName ?? "null"}  HeadType: {pawn?.story?.headType?.defName ?? "null"}");
            sb.AppendLine($"  Form: {(f != null ? f.defName : "(none — not transformed)")}");
            if (f != null)
                sb.AppendLine($"  LabelKey: {f.label ?? "null"}  DescKey: {f.description ?? "null"}");
            sb.AppendLine();

            // ──────────────── 항상 표시: Runtime Verb State ────────────────
            sb.AppendLine("== Runtime Verb State ==");
            try
            {
                // 1) Pawn native verbTracker
                var nativeVerbs = pawn.verbTracker?.AllVerbs;
                sb.AppendLine($"  pawn.verbTracker.AllVerbs: {(nativeVerbs != null ? nativeVerbs.Count.ToString() : "null")}");
                if (nativeVerbs != null)
                {
                    for (int i = 0; i < nativeVerbs.Count; i++)
                    {
                        var v = nativeVerbs[i];
                        if (v == null) continue;
                        var vma = v as Verb_MeleeAttack;
                        string toolInfo = vma?.tool != null ? $"tool={vma.tool.label}(power={vma.tool.power:0.#})" : "tool=null";
                        string manInfo = vma?.maneuver != null ? $"maneuver={vma.maneuver.defName}" : "";
                        sb.AppendLine($"    [{i}] {v.GetType().Name} melee={v.verbProps?.IsMeleeAttack} {toolInfo} {manInfo}");
                    }
                }

                // 2) Shapeshift verbTracker
                // HediffComp_ShapeshiftCore 기반 조회
                ShapeshiftCoreUtility.TryGetCore(pawn, out var ssfComp);
                var ssfVt = ssfComp?.ShapeshiftVerbTracker;
                var ssfVerbs = ssfVt?.AllVerbs;
                sb.AppendLine($"  shapeshiftVerbTracker.AllVerbs: {(ssfVerbs != null ? ssfVerbs.Count.ToString() : "null")}");
                if (ssfVerbs != null)
                {
                    for (int i = 0; i < ssfVerbs.Count; i++)
                    {
                        var v = ssfVerbs[i];
                        if (v == null) continue;
                        var vma = v as Verb_MeleeAttack;
                        string toolInfo = vma?.tool != null ? $"tool={vma.tool.label}(power={vma.tool.power:0.#})" : "tool=null";
                        string manInfo = vma?.maneuver != null ? $"maneuver={vma.maneuver.defName}" : "";
                        sb.AppendLine($"    [{i}] {v.GetType().Name} melee={v.verbProps?.IsMeleeAttack} ranged={v.verbProps?.Ranged} {toolInfo} {manInfo}");
                    }
                }

                // 3) Race native tools (ThingDef.tools)
                var raceTools = pawn?.def?.tools;
                sb.AppendLine($"  race.tools (ThingDef): {(raceTools != null ? raceTools.Count.ToString() : "null")}");
                if (raceTools != null)
                {
                    for (int i = 0; i < raceTools.Count; i++)
                    {
                        var t = raceTools[i];
                        if (t == null) continue;
                        sb.AppendLine($"    [{i}] {t.label ?? "?"} power={t.power:0.##} cooldown={t.cooldownTime:0.##} caps=[{CapList(t)}]");
                    }
                }
            }
            catch (Exception ex) { sb.AppendLine($"  [Error dumping runtime verbs: {ex.Message}]"); }
            sb.AppendLine();

            // ──────────────── 항상 표시: Blood / Flesh ────────────────
            sb.AppendLine("== Blood / Flesh ==");
            var raceProps = pawn?.def?.race;
            // 런타임 캐시(패치가 실제 참조하는 값)를 우선 표시
            ThingDef cachedBlood = null, cachedSmear = null;
            FleshTypeDef cachedFlesh = null;
            ShapeshiftRuntimeCaches.BloodByPawn.TryGetValue(pawn, out cachedBlood);
            ShapeshiftRuntimeCaches.SmearByPawn.TryGetValue(pawn, out cachedSmear);
            ShapeshiftRuntimeCaches.FleshTypeByPawn.TryGetValue(pawn, out cachedFlesh);
            string activeBlood = (cachedBlood ?? raceProps?.BloodDef)?.defName ?? "null";
            string activeSmear = cachedSmear?.defName ?? "(race default)";
            string activeFlesh = (cachedFlesh ?? raceProps?.FleshType)?.defName ?? "null";
            sb.AppendLine($"  [Active]  bloodDef={activeBlood}  bloodSmearDef={activeSmear}  fleshType={activeFlesh}");
            sb.AppendLine($"  [Race]    bloodDef={raceProps?.BloodDef?.defName ?? "null"}  fleshType={raceProps?.FleshType?.defName ?? "null"}");
            if (f != null)
                sb.AppendLine($"  [Form]   bloodDef={f.bloodDef?.defName ?? "null"}  bloodSmearDef={f.bloodSmearDef?.defName ?? "null"}  fleshType={f.fleshType?.defName ?? "null"}");
            else
                sb.AppendLine("  (no form override)");
            sb.AppendLine();

            // ──────────────── 항상 표시: Sounds (실제 defName) ────────────────
            sb.AppendLine("== Sounds ==");
            try
            {
                // 런타임 캐시(패치가 실제 참조하는 값)를 Active로 표시
                sb.AppendLine("  [Active]  (런타임 캐시 — 패치가 실제 사용하는 값)");
                sb.AppendLine($"    call     = {TryGetCachedName(ShapeshiftRuntimeCaches.CallByPawn, pawn)}");
                sb.AppendLine($"    wounded  = {TryGetCachedName(ShapeshiftRuntimeCaches.WoundedByPawn, pawn)}");
                sb.AppendLine($"    death    = {TryGetCachedName(ShapeshiftRuntimeCaches.DeathByPawn, pawn)}");
                sb.AppendLine($"    angry    = {TryGetCachedName(ShapeshiftRuntimeCaches.AngryByPawn, pawn)}");
            }
            catch { sb.AppendLine("    [Error reading runtime cache]"); }
            if (f != null)
            {
                sb.AppendLine("  [Form]    (폼 Def에 정의된 값)");
                DumpSound(sb, "    call    ", f.soundCall);
                DumpSound(sb, "    angry   ", f.soundAngry);
                DumpSound(sb, "    wounded ", f.soundWounded);
                DumpSound(sb, "    death   ", f.soundDeath);
                DumpSound(sb, "    eating  ", f.soundEating);
                DumpSound(sb, "    meleeHit", f.soundMeleeHitPawn);
                DumpSound(sb, "    meleeBld", f.soundMeleeHitBuilding);
                DumpSound(sb, "    meleeMis", f.soundMeleeMiss);
            }
            sb.AppendLine();

            // ──────────────── 폼 전용 섹션: 변신 중일 때만 ────────────────
            if (f == null)
            {
                sb.AppendLine("(Not transformed — form-specific sections skipped)");
                return;
            }

            // 그리기 보정(스케일/오프셋)
            sb.AppendLine("== Draw adjustments ==");
            sb.AppendLine($"  bodyDrawScale={Val(f.bodyDrawScale)} headDrawScale={Val(f.headDrawScale)} portraitDrawScale={Val(f.portraitDrawScale)}");
            sb.AppendLine($"  bodyOffset={Val(f.bodyOffset)} headOffset={Val(f.headOffset)}");
            sb.AppendLine();

            // 파츠 제어
            sb.AppendLine("== Parts ==");
            DumpPart(sb, "body", f.body);
            DumpPart(sb, "head", f.head);
            DumpPart(sb, "hair", f.hair);
            DumpPart(sb, "beard", f.beard);
            DumpPart(sb, "tattooBody", f.tattooBody);
            DumpPart(sb, "tattooHead", f.tattooHead);
            sb.AppendLine();

            // 렌더 필터
            sb.AppendLine("== Render filters ==");
            DumpFilter(sb, "apparel.HideLayers", f.renderHideApparelLayers);
            DumpFilter(sb, "apparel.HideDefs", f.renderHideApparelDefNames);
            DumpFilter(sb, "apparel.ShowLayers", f.renderShowApparelLayers);
            DumpFilter(sb, "apparel.ShowDefs", f.renderShowApparelDefNames);
            DumpFilter(sb, "weapon.HideTags", f.renderHideWeaponTags);
            DumpFilter(sb, "weapon.HideDefs", f.renderHideWeaponDefNames);
            DumpFilter(sb, "weapon.ShowTags", f.renderShowWeaponTags);
            DumpFilter(sb, "weapon.ShowDefs", f.renderShowWeaponDefNames);
            DumpFilter(sb, "gene.HideExcl", f.renderHideGeneExclusionTags);
            DumpFilter(sb, "gene.HideDefs", f.renderHideGeneDefNames);
            DumpFilter(sb, "gene.ShowExcl", f.renderShowGeneExclusionTags);
            DumpFilter(sb, "gene.ShowDefs", f.renderShowGeneDefNames);
            sb.AppendLine();

            // 장비 처리/락
            sb.AppendLine("== Gear handling ==");
            sb.AppendLine($"  apparelOnTransform={f.apparelOnTransform} weaponOnTransform={f.weaponsOnTransform}");
            sb.AppendLine($"  apparelEquipLock={f.apparelEquipLock} weaponEquipLock={f.weaponEquipLock}");
            sb.AppendLine();

            // Verbs / Tools (Form Definition)
            sb.AppendLine("== Verbs/Tools (Form Def) ==");
            sb.AppendLine($"  replaceNativeVerbs={BoolVal(f.replaceNativeVerbs)} replaceNativeTools={BoolVal(f.replaceNativeTools)}");
            DumpVerbs(sb, f.verbs);
            DumpTools(sb, f.tools);
            sb.AppendLine();

            // 스탯/캐퍼 (연결된 HediffDef의 첫 번째 stage에서 참조)
            HediffDef statHediffDef = null;
            if (ShapeshiftFormIndex.FormToHediffDefs.TryGetValue(f, out var statHediffList) && statHediffList.Count > 0)
                statHediffDef = statHediffList[0];
            var stage = statHediffDef?.stages != null && statHediffDef.stages.Count > 0 ? statHediffDef.stages[0] : null;
            sb.AppendLine($"== Stats/Caps (source: {statHediffDef?.defName ?? "null"}) ==");
            sb.AppendLine("  ── Stat Offsets ──");
            DumpStatMods(sb, stage?.statOffsets);
            sb.AppendLine("  ── Stat Factors ──");
            DumpStatMods(sb, stage?.statFactors);
            sb.AppendLine("  ── Capacity Mods ──");
            DumpCapMods(sb, stage?.capMods);
            sb.AppendLine();

            // 작업/이데올로기
            sb.AppendLine("== Work / Ideology ==");
            sb.AppendLine($"  disabledWorkTags={f.disabledWorkTagsOnTransform}");
            DumpWorkTypes(sb, f.disabledWorkTypesOnTransform);
            sb.AppendLine($"  suppressIdeologyUncoveredThoughts={f.suppressIdeologyUncoveredThoughts}");
            sb.AppendLine($"  linkedSacredAnimalDef={f.linkedSacredAnimalDef?.defName ?? "null"}");
        }

        private static string CapList(Tool t)
        {
            if (t?.capacities == null || t.capacities.Count == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < t.capacities.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(t.capacities[i]?.defName ?? "null");
            }
            return sb.ToString();
        }

        // 값 포맷 도우미
        private static string Val(float? f) => f.HasValue ? f.Value.ToString("0.###") : "null";
        private static string Val(Vector2? v) => v.HasValue ? v.Value.ToString() : "null";
        private static string BoolVal(bool? b) => b.HasValue ? (b.Value ? "true" : "false") : "null";

        private static void DumpPart(StringBuilder sb, string name, PartOverrideOption p)
        {
            if (p == null) { sb.AppendLine($"  {name}=null"); return; }
            sb.AppendLine($"  {name}: mode={p.mode} path={p.replacementTexPath ?? "null"} color={(p.color.HasValue ? p.color.Value.ToString() : "null")}");
        }

        private static void DumpFilter(StringBuilder sb, string name, List<string> list)
        {
            if (list == null) { sb.AppendLine($"  {name}=null"); return; }
            sb.Append("  ").Append(name).Append("=[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(list[i]);
            }
            sb.AppendLine("]");
        }

        private static void DumpVerbs(StringBuilder sb, List<VerbProperties> verbs)
        {
            if (verbs == null) { sb.AppendLine("  verbs=null"); return; }
            sb.AppendLine($"  verbs.Count={verbs.Count}");
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null) continue;
                sb.AppendLine($"    [{i}] {v.label ?? v.ToString()}  (category={v.category}, warmup={v.warmupTime:0.##}, melee={v.IsMeleeAttack})");
            }
        }

        private static void DumpTools(StringBuilder sb, List<Tool> tools)
        {
            if (tools == null) { sb.AppendLine("  tools=null"); return; }
            sb.AppendLine($"  tools.Count={tools.Count}");
            for (int i = 0; i < tools.Count; i++)
            {
                var t = tools[i];
                if (t == null) continue;
                sb.AppendLine($"    [{i}] {t.label ?? t.ToString()}  (power={t.power:0.##}, chance={t.chanceFactor:0.##}, cooldown={t.cooldownTime:0.##}, caps=[{CapList(t)}])");
            }
        }

        private static void DumpStatMods(StringBuilder sb, List<StatModifier> mods)
        {
            if (mods == null || mods.Count == 0) { sb.AppendLine("    (none)"); return; }
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m == null || m.stat == null) continue;
                sb.AppendLine($"    {m.stat.defName} = {m.value:+0.###;-0.###;0}");
            }
        }

        private static void DumpCapMods(StringBuilder sb, List<PawnCapacityModifier> caps)
        {
            if (caps == null || caps.Count == 0) { sb.AppendLine("    (none)"); return; }
            for (int i = 0; i < caps.Count; i++)
            {
                var c = caps[i];
                if (c == null || c.capacity == null) continue;
                var parts = new List<string>();
                if (c.offset != 0f) parts.Add($"offset={c.offset:+0.###;-0.###;0}");
                if (c.setMax < 999f) parts.Add($"setMax={c.setMax:0.###}");
                if (c.postFactor != 1f) parts.Add($"postFactor={c.postFactor:0.###}");
                sb.AppendLine($"    {c.capacity.defName}: {string.Join(", ", parts)}");
            }
        }

        private static void DumpSound(StringBuilder sb, string name, SoundDef def)
        {
            sb.AppendLine($"  {name}={(def != null ? def.defName : "null")}");
        }

        private static void DumpWorkTypes(StringBuilder sb, List<WorkTypeDef> list)
        {
            if (list == null) { sb.AppendLine("  disabledWorkTypes=(none)"); return; }
            for (int i = 0; i < list.Count; i++)
            {
                var wt = list[i];
                if (wt != null) sb.AppendLine($"  - {wt.defName}");
            }
        }

        #endregion
    }
}
