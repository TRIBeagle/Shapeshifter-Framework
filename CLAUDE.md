# Shapeshifter Framework (SSF)

## What
RimWorld 1.6 모드 프레임워크. C# (.NET 4.8) + Harmony + XML Def.
핵심 클래스: `HediffComp_ShapeshiftCore` (partial class 4개, `Hediffs/` 폴더).
소스 루트: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/`

## Why
모더가 `ShapeshiftFormDef` XML만 작성하면 변신 폼을 추가할 수 있는 범용 프레임워크.

## Build
```bash
cd "Source/ShapeshifterFramework v1.6/ShapeshifterFramework"
dotnet build ShapeshifterFramework.csproj
```

## 코드 규칙
- 주석·파일 헤더 전부 한국어. 코드 내 영어 용어(Pawn, Verb, Hediff 등)는 그대로.
- .cs 파일 추가/삭제 시 csproj `<Compile Include>` 동기화.

## 변경 시 동기화
| 변경 유형 | 갱신 파일 |
|-----------|-----------|
| API·XML 필드·동작 | `FORMDEF_GUIDE_EN.md`, `FORMDEF_GUIDE_KO.md` |
| 호환성·개요 | `README.md` |
| 새 기능·버그 수정 | `TestMod_SSF/TEST_CHECKLIST.md` |

## 세부 규칙 (필요 시 참조)

작업 내용에 따라 아래 문서를 읽고 따를 것:

- `agent_docs/code_conventions.md` — 파일 헤더 형식, Harmony 패치 규칙, null 안전성
- `agent_docs/performance.md` — Tick 최적화, LINQ 금지, 캐싱 패턴

## 참고 문서 (필요 시 읽을 것)
- `FORMDEF_GUIDE_*.md` — FormDef XML 전체 필드 레퍼런스
- `DESIGN_NOTES.md` — 의도적 비표준 패턴 설계 근거
