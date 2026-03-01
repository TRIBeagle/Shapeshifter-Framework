// .NET 4.8 / C# 7.3
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 변신 시작 시점의 원래 상태를 기록해두었다가,
    /// 변신 해제 시 이 기록을 기반으로 정확히 복원하기 위한 레코드.
    /// (IExposable 구현으로 세이브/로드 지원)
    /// </summary>
    public class ShapeshiftPartRestoreRecord : IExposable
    {
        public BodyPartRecord Part;                           // 대상 파츠
        public bool WasMissingBefore;                         // 변신 전 결손 여부
        public List<PreExistingAddedEntry> PreExistingAdded;  // 변신 전 파츠에 있던 AddedPart들

        public void ExposeData()
        {
            Scribe_BodyParts.Look(ref Part, "part");
            Scribe_Values.Look(ref WasMissingBefore, "wasMissingBefore", false);
            Scribe_Collections.Look(ref PreExistingAdded, "preExistingAdded", LookMode.Deep);
        }

        // 내부 엔트리: 변신 전 해당 파츠에 깔려 있던 AddedPart 하나
        public class PreExistingAddedEntry : IExposable
        {
            public HediffDef Def;
            public float? Severity;
            public BodyPartDef PartDef;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref Def, "def");
                Scribe_Defs.Look(ref PartDef, "partDef");

                // Nullable float(float?) 처리 트릭
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    bool hasSeverity = Severity.HasValue;
                    Scribe_Values.Look(ref hasSeverity, "hasSeverity", false);
                    if (hasSeverity)
                    {
                        float val = Severity.Value;
                        Scribe_Values.Look(ref val, "severity", 0f);
                    }
                }
                else if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    bool hasSeverity = false;
                    Scribe_Values.Look(ref hasSeverity, "hasSeverity", false);
                    if (hasSeverity)
                    {
                        float val = 0f;
                        Scribe_Values.Look(ref val, "severity", 0f);
                        Severity = val;
                    }
                    else
                    {
                        Severity = null;
                    }
                }
            }
        }
    }
}