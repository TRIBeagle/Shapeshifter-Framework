// ShapeshifterFramework | Utilities | FloatMenuPatchHelper.cs
// 목적 : FloatMenuMakerMap.GetOptions Postfix 패치들이 공통으로 사용하는 헬퍼.
// 용도 : Pawn 추출, 비활성 FloatMenuOption 생성, Thing 추출 등 중복 로직을 한곳에 모음.

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class FloatMenuPatchHelper
    {
        /// <summary>FloatMenuMakerMap.GetOptions의 파라미터에서 Humanlike Pawn을 추출. 없으면 null.</summary>
        public static Pawn GetHumanlikePawn(List<Pawn> selectedPawns, FloatMenuContext context)
        {
            Pawn pawn = null;
            if (context != null && context.FirstSelectedPawn != null) pawn = context.FirstSelectedPawn;
            else if (selectedPawns != null && selectedPawns.Count > 0) pawn = selectedPawns[0];
            if (pawn == null || !pawn.RaceProps.Humanlike) return null;
            return pawn;
        }

        /// <summary>FloatMenuOption에서 대상 Thing 추출. iconThing 우선, 없으면 revalidateClickTarget 폴백.</summary>
        public static Thing GetTargetThing(FloatMenuOption opt)
        {
            if (opt == null) return null;
            Thing target = opt.iconThing;
            if (target == null)
            {
                var ct = opt.revalidateClickTarget as Thing;
                if (ct != null) target = ct;
            }
            return target;
        }

        /// <summary>기존 FloatMenuOption을 비활성 옵션으로 교체. 라벨 끝에 사유를 덧붙임.</summary>
        public static FloatMenuOption MakeDisabled(FloatMenuOption original, string reasonSuffix)
        {
            return new FloatMenuOption(original.Label + reasonSuffix, null)
            {
                Disabled = true,
                iconThing = original.iconThing,
                tutorTag = original.tutorTag,
                autoTakeable = false
            };
        }
    }
}
