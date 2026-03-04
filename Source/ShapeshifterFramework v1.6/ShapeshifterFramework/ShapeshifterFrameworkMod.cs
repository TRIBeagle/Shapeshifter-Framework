// ShapeshifterFramework | Root | ShapeshifterFrameworkMod.cs
// 목적 : 모드의 메인 진입점(Entry Point) 및 환경 설정 창(Mod Settings) UI 렌더링.
// 용도 : 림월드 엔진 로드 시 가장 먼저 인스턴스화되어 설정 데이터(Settings)를 전역 공간에 할당하며, 유저가 인게임 '모드 설정' 메뉴를 열었을 때 DoSettingsWindowContents를 통해 UI를 그림.

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
