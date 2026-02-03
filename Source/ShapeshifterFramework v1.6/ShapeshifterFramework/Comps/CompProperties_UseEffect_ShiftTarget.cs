// ShapeshifterFramework | Comps | CompProperties_UseEffect_ShiftTarget.cs
// 목적   : 아이템 사용 효과로 Pawn을 변신시키는 속성 정의 컨테이너.
// 용도   : <see cref="CompUseEffect_ShiftTarget"/> 와 연결되어 대상 Pawn을 Props에 지정된 폼으로 변신시킨다.
// 변경   : 2025-09-22 v1.0 — 프로젝트 주석 규칙에 맞춰 정리 (주석만 수정, 로직 변경 없음).

using RimWorld;

namespace ShapeshifterFramework.Comps
{
    /// <summary>
    /// 아이템 사용 시 Pawn을 변신시키는 속성 정의.
    /// - <see cref="formDefName"/> : 변신시킬 대상 폼(defName)
    /// - <see cref="successChance"/> : 성공 확률 (0~1, 기본 1.0)
    /// </summary>
    public class CompProperties_UseEffect_ShiftTarget : CompProperties_UseEffect
    {
        /// <summary>
        /// 변신시킬 대상 폼(defName).
        /// </summary>
        public string formDefName;

        /// <summary>
        /// 변신 성공 확률 (0~1). 기본값은 1.0.
        /// </summary>
        public float successChance = 1.0f;

        /// <summary>
        /// 생성자: 본 속성과 연결된 실행 클래스 <see cref="CompUseEffect_ShiftTarget"/> 지정.
        /// </summary>
        public CompProperties_UseEffect_ShiftTarget()
        {
            compClass = typeof(CompUseEffect_ShiftTarget);
        }
    }
}
