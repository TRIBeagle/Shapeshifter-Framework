// ShapeshifterFramework | Utilities | ShapeshiftPartRestoreRecord.cs
// 목적 : 변신 시작 전, 폰의 특정 신체 부위(Part)에 대한 상태를 기록하는 세이브/로드용 데이터 구조체.
// 용도 : 원래 부위가 결손(Missing) 상태였는지, 인공 장기(AddedPart)가 부착되어 있었는지, 그리고 그 심각도(Severity)는 어땠는지를 저장하여 변신 해제 시 원상 복원을 지원함.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>변신 전 파츠 상태 기록. 해제 시 복원용 (IExposable).</summary>
    public class ShapeshiftPartRestoreRecord : IExposable
    {
        public BodyPartRecord Part;
        public bool WasMissingBefore;
        public List<PreExistingAddedEntry> PreExistingAdded;

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