// ShapeshifterFramework | GameComponent_DrugPolicySync.cs
// 목적 : 기존 세이브에 모드 약물이 추가되었을 때 DrugPolicy 내부 엔트리 배열에 누락된 약물을 보충.
// 용도 : 게임 로드 시 모든 DrugPolicy의 entriesInt를 현재 DefDatabase<ThingDef>의 약물 목록과 동기화.
//        DrugPolicy.get_Item(ThingDef)에서 누락 약물 조회 시 발생하는 ArgumentException을 원천 방지.
// 주의 : DrugPolicy.entriesInt는 비공개 필드이므로 리플렉션으로 접근. RimWorld 업데이트 시 필드명 변경 가능.

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework
{
    /// <summary>게임 로드 시 DrugPolicy에 누락된 약물 엔트리를 보충하는 GameComponent.</summary>
    internal sealed class GameComponent_DrugPolicySync : GameComponent
    {
        // DrugPolicy.entriesInt 필드 캐시 (한 번만 조회)
        private static readonly FieldInfo EntriesField =
            AccessTools.Field(typeof(DrugPolicy), "entriesInt");

        public GameComponent_DrugPolicySync(Game game) : base(game) { }

        /// <summary>기존 세이브 로드 시 동기화 실행.</summary>
        public override void LoadedGame()
        {
            SyncAllPolicies();
        }

        /// <summary>모든 DrugPolicy의 엔트리를 현재 약물 목록과 동기화.</summary>
        private static void SyncAllPolicies()
        {
            if (EntriesField == null)
            {
                Log.Warning("[ShapeshifterFramework] DrugPolicy.entriesInt 필드를 찾을 수 없음 — 약물 정책 동기화 건너뜀.");
                return;
            }

            var db = Current.Game?.drugPolicyDatabase;
            if (db == null) return;

            // 현재 DefDatabase에 등록된 모든 약물 수집
            var allDrugs = new List<ThingDef>();
            var defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i].IsDrug)
                    allDrugs.Add(defs[i]);
            }
            if (allDrugs.Count == 0) return;

            int totalAdded = 0;

            foreach (var policy in db.AllPolicies)
            {
                var entries = EntriesField.GetValue(policy) as List<DrugPolicyEntry>;
                if (entries == null) continue;

                // 기존 엔트리에 있는 약물 ThingDef 집합
                var existing = new HashSet<ThingDef>();
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i]?.drug != null)
                        existing.Add(entries[i].drug);
                }

                // 누락된 약물 추가
                for (int i = 0; i < allDrugs.Count; i++)
                {
                    if (!existing.Contains(allDrugs[i]))
                    {
                        entries.Add(new DrugPolicyEntry { drug = allDrugs[i] });
                        totalAdded++;
                    }
                }
            }

            if (totalAdded > 0)
                Log.Message($"[ShapeshifterFramework] DrugPolicy 동기화 완료: {totalAdded}개 누락 약물 엔트리 추가.");
        }
    }
}
