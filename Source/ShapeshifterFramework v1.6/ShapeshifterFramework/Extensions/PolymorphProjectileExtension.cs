using Verse;

namespace ShapeshifterFramework.Extensions
{
    public class PolymorphProjectileExtension : DefModExtension
    {
        // 필수: 적용할 폼(defName)
        public string formDefName;

        // 성공 확률(0~1), 기본 1.0
        public float successChance = 1f;

        // 선택: AoE 반경(타일). 0 또는 이하 → 단일 타겟
        public float aoeRadius = 0f;

        // (확장 포인트) 이후 필요시 추가:
        // public bool excludeMechanoids = true;
        // public List<string> requiredTags;
    }
}
