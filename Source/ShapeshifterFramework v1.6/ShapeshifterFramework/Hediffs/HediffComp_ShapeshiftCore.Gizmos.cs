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

            foreach (var g in YieldVerbGizmos(pawn))
                yield return g;
        }

        /// <summary>원거리 verb 명령/자동공격 토글 기즈모 생성.</summary>
        private IEnumerable<Gizmo> YieldVerbGizmos(Pawn pawn)
        {
            var vt = ShapeshiftVerbTracker;
            if (vt == null) yield break;

            bool canViolent = !pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool showToggle = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;
            bool multiSelected = Find.Selector != null && Find.Selector.NumSelected > 1;
            _tmpSeenVerbs.Clear();

            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null || !v.verbProps.Ranged) continue;
                if (v.caster == null) v.caster = pawn;
                if (!_tmpSeenVerbs.Add(v)) continue;

                int idx = i;
                var gizOpt = FindGizmoOption(idx, v);

                var cmd = BuildVerbCommand(pawn, idx, v, gizOpt, canViolent);
                yield return cmd;

                if (multiSelected || !showToggle) continue;

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

        /// <summary>단일 verb 명령 기즈모 생성 + 비활성 조건 적용.</summary>
        private Command_VerbTarget BuildVerbCommand(Pawn pawn, int idx, Verb v, VerbGizmoOption gizOpt, bool canViolent)
        {
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

            // 신경열 비용 체크
            float entropyGizmo = gizOpt != null ? gizOpt.entropyCost : 0f;
            if (entropyGizmo > 0f)
            {
                var entropy = pawn.psychicEntropy;
                if (entropy != null && entropy.WouldOverflowEntropy(entropyGizmo))
                    cmd.Disable("SSF_Message_EntropyOverflow".Translate());
            }

            return cmd;
        }
    }
}
