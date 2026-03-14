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
- `TestMod_SSF/TEST_CHECKLIST.md` — 해당 카테고리에 테스트 항목 추가/수정
- `TestMod_SSF/TEST_PLAN.md` — 테스트 실행 계획 갱신
- 필요 시 `TestMod_SSF/1.6/Defs/` 아래 테스트용 XML Def 추가

## 빌드
```bash
cd "Source/ShapeshifterFramework v1.6/ShapeshifterFramework"
dotnet build ShapeshifterFramework.csproj
```
