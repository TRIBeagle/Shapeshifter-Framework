// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.Gizmos.cs
// 목적 : 변신 해제 명령 및 폼 verb 기즈모 생성.
// 용도 : hediff의 GetGizmos()에서 호출되어 상태 바/해제 버튼과 ranged verb 명령/자동공격 토글을 UI에 표시.

using RimWorld;
using ShapeshifterFramework.Gizmos;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    public partial class HediffComp_ShapeshiftCore
    {
        /// <summary>해제 및 verb 기즈모 생성. hediff의 GetGizmos()에서 호출.</summary>
        public IEnumerable<Gizmo> GetGizmosExtra()
        {
            var pawn = Pawn;
            if (pawn == null) yield break;

            if (!pawn.IsColonistPlayerControlled)
                yield break;

            if (isTransformed && currentForm != null)
            {
                bool showBar = ShapeshifterFrameworkMod.Settings?.showShapeshiftBar ?? true;

                if (showBar)
                {
                    // 프로그레스 바 기즈모
                    yield return new Gizmo_ShapeshiftStatus { core = this };
                }

                // 해제 버튼 — 바 표시 여부와 무관하게 항상 단독 표시
                if (ResolvedCanRevertVoluntarily)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSF_Command_RevertLabel".Translate(),
                        defaultDesc = "SSF_Command_RevertDesc".Translate(),
                        action = delegate { RemoveForm(); },
                        icon = ShapeshiftTextureUtility.GetRevertIcon(currentForm)
                    };
                }
            }

            if (!pawn.Drafted) yield break;

            var vt = ShapeshiftVerbTracker;
            if (vt == null) yield break;

            bool canViolent = !pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool showToggle = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;
            bool multiSelected = Find.Selector != null && Find.Selector.NumSelected > 1;
            _tmpSeenVerbs.Clear();
            var seen = _tmpSeenVerbs;

            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue;

                if (v.caster == null) v.caster = pawn;
                if (!seen.Add(v)) continue;

                int idx = i;

                // FindGizmoOption 1회 조회 후 label/desc/icon에 재사용
                var gizOpt = FindGizmoOption(idx, v);

                bool projectileOk = !(v is Verb_LaunchProjectile) || v.verbProps.defaultProjectile != null;

                var cmd = new Command_VerbTarget
                {
                    defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: false, gizOpt),
                    defaultDesc = GetVerbDesc(idx, v, forToggle: false, gizOpt),
                    icon = GetVerbIcon(idx, v, gizOpt) ?? v.UIIcon,
                    verb = v,
                };
                if (!projectileOk)
                    cmd.Disable("SSF_Message_NoProjectile".Translate());
                if (!canViolent)
                    cmd.Disable("IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn));
                else if (!v.Available())
                    cmd.Disable("CommandCannotFire".Translate());

                // 신경열 비용 체크: tracker가 있지만 오버플로우 시 비활성
                // tracker == null (DLC 없음/메카노이드) → 신경열 비용 자체 무시, 자유 사용
                float entropyGizmo = gizOpt != null ? gizOpt.entropyCost : 0f;
                if (entropyGizmo > 0f)
                {
                    var entropy = pawn.psychicEntropy;
                    if (entropy != null && entropy.WouldOverflowEntropy(entropyGizmo))
                        cmd.Disable("SSF_Message_EntropyOverflow".Translate());
                }

                yield return cmd;

                if (multiSelected) continue;

                if (showToggle)
                {
                    var tgl = new Command_Toggle
                    {
                        defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: true, gizOpt),
                        defaultDesc = GetVerbDesc(idx, v, forToggle: true, gizOpt),
                        icon = GetVerbIcon(idx, v, gizOpt) ?? v.UIIcon,
                        isActive = () => IsAutoAttackEnabled(idx, v),
                        toggleAction = () => ToggleAutoAttack(idx, v),
                        groupable = false,
                    };
                    if (!canViolent)
                        tgl.Disable("IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn));
                    yield return tgl;
                }
            }
        }
    }
}
