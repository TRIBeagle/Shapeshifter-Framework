// ShapeshifterFramework | Utilities | ShapeshiftTextureUtility.cs
// 목적 : 폼(FormDef)에 지정된 커스텀 UI 기즈모(Gizmo) 아이콘의 로드 및 캐싱을 담당.
// 용도 : ContentFinder를 통해 아이콘을 로드하되, 매 틱 호출되는 기즈모 렌더링의 부담을 없애기 위해
//        1회 찾은 텍스처는 Dictionary에 영구 보관하며, 파일이 없을 경우 안전하게 기본 아이콘(Fallback)을 반환함.
// 주의 : gizmoIconPathEnter는 어빌리티 기즈모가 대체하므로 삭제됨. Revert 아이콘만 관리.

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    [StaticConstructorOnStartup]
    public static class ShapeshiftTextureUtility
    {
        // 기본 아이콘 고정 캐시
        public static readonly Texture2D DefaultRevertIcon = ContentFinder<Texture2D>.Get("UI/Commands/SSF_Shift_Revert", true);

        // 폼별 커스텀 아이콘 캐시
        private static Dictionary<ShapeshiftFormDef, Texture2D> revertIconCache = new Dictionary<ShapeshiftFormDef, Texture2D>();

        // 해제 아이콘 조회
        public static Texture2D GetRevertIcon(ShapeshiftFormDef form)
        {
            if (form == null) return DefaultRevertIcon;

            if (revertIconCache.TryGetValue(form, out Texture2D tex))
                return tex;

            if (!string.IsNullOrEmpty(form.gizmoIconPathRevert))
                tex = ContentFinder<Texture2D>.Get(form.gizmoIconPathRevert, false) ?? DefaultRevertIcon;
            else
                tex = DefaultRevertIcon;

            revertIconCache[form] = tex;
            return tex;
        }
    }
}
