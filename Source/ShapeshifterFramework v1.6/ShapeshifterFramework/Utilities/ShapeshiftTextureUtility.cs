// ShapeshifterFramework | Utilities | ShapeshiftTextureUtility.cs
// 목적 : 폼(FormDef)에 지정된 커스텀 UI 지즈모(Gizmo) 아이콘의 로드 및 캐싱을 담당.
// 용도 : ContentFinder를 통해 아이콘을 로드하되, 매 틱 호출되는 지즈모 렌더링의 부담을 없애기 위해 1회 찾은 텍스처는 Dictionary에 영구 보관하며, 파일이 없을 경우 안전하게 기본 아이콘(Fallback)을 반환함.

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    [StaticConstructorOnStartup]
    public static class ShapeshiftTextureUtility
    {
        // 1. 기본 아이콘 고정 캐시 (게임 시작 시 미리 메모리에 올려둠)
        public static readonly Texture2D DefaultEnterIcon = ContentFinder<Texture2D>.Get("UI/Commands/SSF_Shift_Enter", true);
        public static readonly Texture2D DefaultRevertIcon = ContentFinder<Texture2D>.Get("UI/Commands/SSF_Shift_Revert", true);

        // 2. 폼(Def)별 커스텀 아이콘을 기억해둘 메모리 장부(Dictionary)
        private static Dictionary<ShapeshiftFormDef, Texture2D> enterIconCache = new Dictionary<ShapeshiftFormDef, Texture2D>();
        private static Dictionary<ShapeshiftFormDef, Texture2D> revertIconCache = new Dictionary<ShapeshiftFormDef, Texture2D>();

        // 변신 아이콘 호출기
        public static Texture2D GetEnterIcon(ShapeshiftFormDef form)
        {
            if (form == null) return DefaultEnterIcon;

            // 장부에 이미 찾아둔 아이콘이 있으면 0.0001초 만에 바로 반환
            if (enterIconCache.TryGetValue(form, out Texture2D tex))
                return tex;

            // 장부에 없으면 (최초 1회만 여기로 들어옴)
            if (!string.IsNullOrEmpty(form.gizmoIconPathEnter))
            {
                // XML에 적힌 경로로 찾되, 만약 해당 경로에 이미지가 없으면 기본 아이콘으로 안전하게 폴백
                tex = ContentFinder<Texture2D>.Get(form.gizmoIconPathEnter, false) ?? DefaultEnterIcon;
            }
            else
            {
                tex = DefaultEnterIcon;
            }

            // 찾은 아이콘을 장부에 기록하고 반환
            enterIconCache[form] = tex;
            return tex;
        }

        // 해제 아이콘 호출기
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