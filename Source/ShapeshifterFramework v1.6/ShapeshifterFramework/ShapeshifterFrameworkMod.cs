// ShapeshifterFrameworkMod.cs
// 목적: 모드 엔트리. Settings 인스턴스 보관 및 설정창 연결.

using UnityEngine;
using Verse;

namespace ShapeshifterFramework
{
    public class ShapeshifterFrameworkMod : Mod
    {
        // 어디서나 접근 쉽게(static)
        public static ShapeshifterFrameworkSettings Settings;

        public ShapeshifterFrameworkMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ShapeshifterFrameworkSettings>(); // 저장값 로드
        }

        public override string SettingsCategory() => "Shapeshifter Framework";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings != null)
                Settings.DoSettingsWindowContents(inRect);
        }
    }
}
