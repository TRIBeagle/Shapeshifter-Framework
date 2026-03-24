# Shapeshifter Framework — Claude Code 프로젝트 규칙

## 프로젝트 개요
RimWorld 1.6 모드 프레임워크. C# (.NET Framework 4.8) + Harmony 패치.
소스 경로: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/`

## 코드 작성 규칙

### 주석 스타일
- **파일 헤더** (필수, 모든 .cs 파일):
  ```
  // ShapeshifterFramework | [폴더명] | [파일명].cs
  // 목적 : [한국어로 이 파일의 핵심 역할 1-2문장]
  // 용도 : [한국어로 구체적 사용 맥락/동작 설명]
  // 주의 : [한국어로 주의사항] (해당 시에만)
  ```
- **XML doc comments**: 한국어 `/// <summary>설명</summary>`
- **인라인 주석**: 한국어 `// 설명`
- **영어 사용 금지**: 주석은 전부 한국어. 코드 내 영어 용어(Pawn, Verb, Hediff 등)는 그대로 사용.

### csproj 동기화
파일 추가/삭제 시 반드시 `ShapeshifterFramework.csproj`의 `<Compile Include="...">` 항목을 갱신할 것.
기존 항목의 폴더/알파벳 정렬 순서를 따를 것.

### 문서 갱신
API 변경, 새 XML 필드 추가/삭제, 동작 변경 시 아래 문서를 동기화:
- `FORMDEF_GUIDE_EN.md` (영어 작성 가이드)
- `FORMDEF_GUIDE_KO.md` (한국어 작성 가이드)
- `README.md` (프로젝트 개요)

### 테스트모드 반영
새 기능이나 동작 변경 시:
- `TestMod_SSF/TEST_CHECKLIST.md` — 테스트 체크리스트 및 실행 계획 (단일 파일)
- 필요 시 `TestMod_SSF/1.6/Defs/` 아래 테스트용 XML Def 추가

### 세이브 호환성 (최우선 원칙)
- `ExposeData()`의 Scribe 키 문자열은 **절대 변경/삭제 금지**.
- 새 필드 추가 시 반드시 기존 세이브에서 안전한 기본값(default) 설정.
- 직렬화 대상 클래스(Hediff, ThingComp, Apparel 등)의 **네임스페이스와 클래스명 변경 금지**.
- 변경 전 세이브 영향 분석 필수.

### 성능 원칙
- **Tick() / TickInterval()** 내 코드는 최소한으로. 무거운 로직은 TickRare(250틱) 이상 간격으로.
- **리플렉션**: 1회 탐색 후 static 캐시. 실패 시 재탐색 없음.
- **LINQ 지양**: 핫패스(Tick, Gizmo, ThoughtWorker)에서는 for 루프 사용.
- **컬렉션 순회 중 수정**: `AllPawnsSpawned` 등 순회 중 폰 사망 가능 시 스냅샷(`.ToList()` 또는 별도 리스트) 필수.
- 유틸 메서드 반복 호출 금지. 로컬 변수에 캐시.

### Null 안전성
- `pawn.jobs?.curDriver?.asleep` 패턴 사용 (jobs/curDriver는 null 가능).
- `pawn.mindState?.mentalStateHandler` null 체크 후 접근.
- `AccessTools.Field()` 결과는 반드시 null 체크.
- `as` 캐스팅 후 null 체크 없이 멤버 접근 금지.

### Harmony 패치
- Patch 클래스는 `Patches/` 폴더에 파일 1개 = 패치 1개.
- `[HarmonyPatch]` 어트리뷰트 사용. `PatchAll()` 자동 등록.
- ThoughtWorker에서 게임 상태 변경(부작용) 금지. CompTickRare 등 안전한 위치에서 수행.

## 빌드
```bash
cd "Source/ShapeshifterFramework v1.6/ShapeshifterFramework"
dotnet build ShapeshifterFramework.csproj
```
