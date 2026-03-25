// ShapeshifterFramework | Root | ShapeshiftDefOf.cs
// 목적 : 모드에서 사용하는 Def를 DefOf로 캐싱하여 런타임 DefDatabase 조회를 제거.
// 용도 : RimWorld DefOf 시스템을 통해 XML 정의를 C#에서 직접 참조.
// 주의 : [MayRequireIdeology] 필드는 Ideology DLC 미설치 시 null.

using RimWorld;
using Verse;

namespace ShapeshifterFramework
{
    /// <summary>ShapeshifterFramework 모드에서 사용하는 모든 DefOf 데이터.</summary>
    [DefOf]
    public static class ShapeshiftDefOf
    {
        static ShapeshiftDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ShapeshiftDefOf));
        }

        // --- HistoryEventDefs (Ideology) ---
        [MayRequireIdeology]
        public static HistoryEventDef SSF_Shapeshifted;

        // --- PreceptDefs (Ideology) ---
        [MayRequireIdeology]
        public static PreceptDef SSF_Shapeshifting_Abhorrent;
    }
}
