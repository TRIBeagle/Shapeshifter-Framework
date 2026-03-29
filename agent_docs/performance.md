# 성능 규칙

RimWorld은 싱글스레드. Tick마다 수백 폰을 처리하므로 핫패스 최적화 필수.

## Tick 최적화

- `CompPostTick()` 내 무거운 로직은 `IsHashIntervalTick(60)` 이상 간격으로.
- 상태 체크(아이템 소실, 사망, 쓰러짐)만 주기적 실행. 매 틱 금지.

## LINQ 금지 (핫패스)

- Tick, Gizmo, Patch에서 LINQ 사용 금지 — `for` 루프 사용.
- 초기화/1회성 코드에서는 LINQ 허용.

## 캐싱 패턴

- **ShapeshiftRegistry**: Dictionary O(1) 룩업. 폰별 hediff 리스트 스캔 대신 사용.
- **리플렉션 캐싱**: `AccessTools` 결과를 `static` 필드에 1회 저장. 실패 시 재탐색 없음.
- **Compiled delegate**: 핫패스 리플렉션 호출은 `Delegate.CreateDelegate()`로 컴파일.
- **DefDatabase 캐싱**: `ManeuversByCapacity` 등 Def 목록은 Dictionary로 lazy init 후 재사용.

## 컬렉션 순회

- 순회 중 제거 필요 시 역순 `for` 루프 (`for (int i = count - 1; i >= 0; i--)`).
- `ShapeshiftRegistry.GetSnapshot()` — 순회 중 변경 방지용 스냅샷 패턴.
