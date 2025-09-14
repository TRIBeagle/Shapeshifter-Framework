// .NET 4.8 / C# 7.3
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 변신 시작 시점의 원래 상태를 기록해두었다가,
    /// 변신 해제 시 이 기록을 기반으로 정확히 복원하기 위한 레코드.
    /// </summary>
    public class ShapeshiftPartRestoreRecord
    {
        public BodyPartRecord Part;                           // 대상 파츠
        public bool WasMissingBefore;                         // 변신 전 결손 여부
        public List<PreExistingAddedEntry> PreExistingAdded;  // 변신 전 파츠에 있던 AddedPart들

        // 내부 엔트리: 변신 전 해당 파츠에 깔려 있던 AddedPart 하나
        public class PreExistingAddedEntry
        {
            public HediffDef Def;
            public float? Severity;
        }
    }
}
