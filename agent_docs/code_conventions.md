# C# 코딩 컨벤션

## 파일 헤더 (모든 .cs 파일 필수)

```csharp
// ShapeshifterFramework | [폴더명] | [파일명].cs
// 목적 : [핵심 역할 1-2문장]
// 용도 : [구체적 사용 맥락]
// 주의 : [주의사항] (해당 시에만)
```

폴더명 예시: Root, Patches, Hediffs, Utilities, Comps, Compat, Gizmos, Extensions, Projectiles, Ideology, Debugs

## Harmony 패치

- `Patches/` 폴더에 파일 1개 = 패치 1개. 파일명: `Patch_클래스명_메서드명.cs`
- `[HarmonyPatch]` 어트리뷰트 사용. `HarmonyInit.cs`에서 `PatchAll()` 자동 등록.
- 클래스명: `Patch_클래스명_메서드명` (internal static class)
- Prefix는 `true` 리턴 = 바닐라 계속, `false` = 바닐라 스킵.
- 모든 패치는 try-catch로 감싸고 `Log.Warning("[SSF] ...")` 폴백.
- 파라미터: `__instance`(this), `___필드명`(private 필드), `__result`(리턴값)

## Null 안전성

- `pawn.health?.hediffSet?.hediffs` — null-conditional 체이닝.
- `as` 캐스팅 후 반드시 null 체크.
- `AccessTools.Field()` / `AccessTools.Method()` 결과 null 체크 필수.
- 패치 Prefix/Postfix 첫 줄에서 null 가드 → `return true`.

## 기타

- 주석·XML doc 전부 한국어. RimWorld API 용어(Pawn, Verb, Hediff 등)는 영어 그대로.
- Compat 패치는 `Compat/` 폴더에서 `CompatManager.cs` 통해 조건부 등록.
