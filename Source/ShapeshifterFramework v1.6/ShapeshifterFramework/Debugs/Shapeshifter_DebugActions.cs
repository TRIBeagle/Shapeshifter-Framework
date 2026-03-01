// ShapeshifterFramework | Debugs | Shapeshifter_DebugActions.cs
// 목적  : Debug(Action) 탭에 ‘Shapeshifter Framework’ 카테고리를 추가하여
//         - 현재 폼 주요 효과를 한눈에 확인(Inspect)
//         - 폼 사운드 즉시 재생(Play)
//         - 활성 폼 전체 요약을 로그로 덤프(Dump)
//         를 지원한다.
// 변경  : 2025-09-23 v1.0 — 프로젝트 주석 규칙 적용(주석·구분만 정리, 로직 무변경)

using LudeonTK;
using RimWorld;
using ShapeshifterFramework.Comps;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShapeshifterFramework.Debugs
{
    /// <summary>
    /// Debug(Action) 탭 > "Shapeshifter Framework"
    /// - Inspect Active Form (ToolMapForPawns): 현재 Pawn의 활성 폼 요약을 플로트 메뉴로 표시
    /// - Play Form Sounds (ToolMapForPawns): 폼에 정의된 보이스/먹는 소리를 즉시 재생
    /// - Dump Active Form to Log (ToolMapForPawns): 활성 폼 전체 정보를 로그로 출력
    ///
    /// 참고
    /// - 플로트 메뉴 구성 패턴은 바닐라 DebugAction 사례를 따름.
    /// - 표시 항목은 현 ShapeshiftFormDef 스키마를 기준으로 요약/상세를 구분함.
    /// </summary>
    public static class Shapeshifter_DebugActions
    {
        #region 상수/필드

        // (미사용) 라벨 컬러 예시 — 필요 시 UI 그리기에서 활용
        private static readonly Color LabelColor = new Color(0.9f, 0.95f, 1f);

        #endregion

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

            var comp = pawn.TryGetComp<CompShapeshifter>();
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

            var comp = pawn.TryGetComp<CompShapeshifter>();
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

        #region 3) 활성 폼 전문 로그 덤프(Dump)

        [DebugAction(
            category = "Shapeshifter Framework",
            name = "Dump Active Form to Log",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        private static void DumpActiveFormToLog(Pawn pawn)
        {
            // Dev 모드/유효 Pawn만 허용
            if (!Prefs.DevMode || pawn == null || pawn.DestroyedOrNull()) return;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            var form = comp != null ? comp.currentForm : null;
            if (form == null)
            {
                Messages.Message("No active form.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var sb = new StringBuilder(2048);
            BuildFullDump(pawn, form, sb);
            Log.Message(sb.ToString());
            Messages.Message("Shapeshifter: dumped to log.", MessageTypeDefOf.TaskCompletion, false);
        }

        #endregion

        #region 헬퍼(플로트 메뉴·요약 문자열·덤프 빌더)

        /// <summary>
        /// 특정 SoundDef를 재생하는 플로트 메뉴 항목을 추가한다.
        /// </summary>
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
                    Log.Warning($"[Shapeshifter] Failed to play {label}: {e}");
                }
            }));
        }

        /// <summary>PartOverrideOption(몸/머리/헤어/수염/문신)의 모드 요약.</summary>
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

        /// <summary>의복/무기/유전자 렌더 필터 요약(All/개수).</summary>
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

        /// <summary>verbs/tools 개수 및 대체 여부 요약.</summary>
        private static string SummarizeVerbsTools(ShapeshiftFormDef f)
        {
            int v = f.verbs != null ? f.verbs.Count : 0;
            int t = f.tools != null ? f.tools.Count : 0;
            string rv = f.replaceNativeVerbs.HasValue ? (f.replaceNativeVerbs.Value ? "replace" : "keep") : "n/a";
            string rt = f.replaceNativeTools.HasValue ? (f.replaceNativeTools.Value ? "replace" : "keep") : "n/a";
            return $"Verbs/Tools: verbs={v}({rv}), tools={t}({rt})";
        }

        /// <summary>스탯/캐퍼 수정 항목 요약(개수만).</summary>
        private static string SummarizeStatsCaps(ShapeshiftFormDef f)
        {
            int so = f.statOffsets != null ? f.statOffsets.Count : 0;
            int sf = f.statFactors != null ? f.statFactors.Count : 0;
            int cm = f.capMods != null ? f.capMods.Count : 0;
            return $"Stats: offsets={so}, factors={sf}, caps={cm}";
        }

        /// <summary>사운드 정의 유무 요약.</summary>
        private static string SummarizeSounds(ShapeshiftFormDef f)
        {
            return $"Sounds: call={(f.soundCall != null)} angry={(f.soundAngry != null)} wounded={(f.soundWounded != null)} death={(f.soundDeath != null)} eat={(f.soundEating != null)} melee(hitPawn={(f.soundMeleeHitPawn != null)}, hitBld={(f.soundMeleeHitBuilding != null)}, miss={(f.soundMeleeMiss != null)})";
        }

        /// <summary>혈흔/살점 타입 정의 유무 요약.</summary>
        private static string SummarizeBlood(ShapeshiftFormDef f)
        {
            return $"Blood/Flesh: blood={(f.bloodDef != null)} smear={(f.bloodSmearDef != null)} flesh={(f.fleshType != null)}";
        }

        /// <summary>활성 폼 전체 정보를 로그용 문자열로 구성.</summary>
        private static void BuildFullDump(Pawn pawn, ShapeshiftFormDef f, StringBuilder sb)
        {
            sb.AppendLine($"[Shapeshifter] Dump for {pawn?.LabelCap} ({pawn?.ThingID})");
            sb.AppendLine($"  Form: {f.defName}");
            sb.AppendLine($"  LabelKey: {f.label ?? "null"}  DescKey: {f.description ?? "null"}");
            sb.AppendLine();

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

            // Verbs / Tools
            sb.AppendLine("== Verbs/Tools ==");
            sb.AppendLine($"  replaceNativeVerbs={BoolVal(f.replaceNativeVerbs)} replaceNativeTools={BoolVal(f.replaceNativeTools)}");
            DumpVerbs(sb, f.verbs);
            DumpTools(sb, f.tools);
            sb.AppendLine();

            // 스탯/캐퍼
            sb.AppendLine("== Stat Offsets ==");
            DumpStatMods(sb, f.statOffsets);
            sb.AppendLine("== Stat Factors ==");
            DumpStatMods(sb, f.statFactors);
            sb.AppendLine("== Capacity Mods ==");
            DumpCapMods(sb, f.capMods);
            sb.AppendLine();

            // 사운드
            sb.AppendLine("== Sounds ==");
            DumpSound(sb, "call", f.soundCall);
            DumpSound(sb, "angry", f.soundAngry);
            DumpSound(sb, "wounded", f.soundWounded);
            DumpSound(sb, "death", f.soundDeath);
            DumpSound(sb, "eating", f.soundEating);
            DumpSound(sb, "meleeHitPawn", f.soundMeleeHitPawn);
            DumpSound(sb, "meleeHitBuilding", f.soundMeleeHitBuilding);
            DumpSound(sb, "meleeMiss", f.soundMeleeMiss);
            sb.AppendLine();

            // 혈흔/살점
            sb.AppendLine("== Blood / Flesh ==");
            sb.AppendLine($"  bloodDef={f.bloodDef?.defName ?? "null"}  bloodSmearDef={f.bloodSmearDef?.defName ?? "null"}  fleshType={f.fleshType?.defName ?? "null"}");
            sb.AppendLine();

            // 작업/이데올로지
            sb.AppendLine("== Work / Ideology ==");
            sb.AppendLine($"  disabledWorkTags={f.disabledWorkTagsOnTransform}");
            DumpWorkTypes(sb, f.disabledWorkTypesOnTransform);
            sb.AppendLine($"  suppressIdeologyUncoveredThoughts={f.suppressIdeologyUncoveredThoughts}");
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
                sb.AppendLine($"    [{i}] {t.label ?? t.ToString()}  (power={t.power:0.##}, chance={t.chanceFactor:0.##}, cooldown={t.cooldownTime:0.##})");
            }
        }

        private static void DumpStatMods(StringBuilder sb, List<StatModifier> mods)
        {
            if (mods == null) { sb.AppendLine("  (none)"); return; }
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m == null || m.stat == null) continue;
                sb.AppendLine($"  - {m.stat.defName} = {m.value:0.###}");
            }
        }

        private static void DumpCapMods(StringBuilder sb, List<PawnCapacityModifier> caps)
        {
            if (caps == null) { sb.AppendLine("  (none)"); return; }
            for (int i = 0; i < caps.Count; i++)
            {
                var c = caps[i];
                if (c == null || c.capacity == null) continue;
                sb.AppendLine($"  - {c.capacity.defName}: offset={c.offset:0.###} setMax={c.setMax:0.###} postFactor={c.postFactor:0.###}");
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