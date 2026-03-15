# ShapeshifterFramework — 테스트 실행 플랜

## 아키텍처 참고

- 스탯/능력치 보정: `linkedHediff`의 HediffDef stages (바닐라 패턴)
- 캐스트 조건(종족/폼 제한): `CompProperties_AbilityShiftTarget`의 `allowedRaces`, `allowedFromForms`
- 변신 유지 조건: `sustainHediffs` / `sustainMode` 필드
- 어빌리티 부여: 유전자 / 장착아이템(CompGiveAbility_SSF) / Hediff / 폼 addAbilities

## 테스트 폼 커버리지

| 폼 | 타입 | 트리거 | 핵심 검증 |
|----|------|--------|-----------|
| BearForm | 동물형 | 약물/스크롤 | body Replace, 수영텍스처, linkedHediff 스탯, addHediffs(AddedPart), tools, 혈흔/살점, FX |
| BearWarriorForm | 아머드형 | 어빌리티(대상) | 사운드 오버라이드, Effecter FX, gear Keep |
| SheepForm | 동물형 | 어빌리티(적대)/AoE | 성별텍스처, canRevertVoluntarily=false, disabledWorkTags, successChance |
| DarkKnightForm | 장비소환 | 어빌리티(자기) | spawnApparel/Weapon+stuff, equipLock, Inventory 처리 |
| BeastkinForm | 휴머노이드 | 어빌리티(자기) | renderNodes, 5종 verb+기즈모, hairColor, replaceNative=false |
| FullBeastForm | 체인 2단계 | addAbilities 체인 | allowedFromForms, 2단 변신 경로 |
| GuardianForm | 조건부 | 어빌리티(소지아이템) | sustainHediffs/sustainMode=Any, portrait/shadow, revertOnDowned |
| PhantomForm | 비주얼 | 어빌리티(유전자/의류) | head Replace, Transparent 셰이더, bodyType/skinColor, FX delay |
| RaceLockedForm | 종족제한 | 어빌리티(Hediff) | allowedRaces=Human, headType, 비대칭 equipLock |

## 테스트 실행 순서

### 준비
1. TestMod_SSF + ShapeshifterFramework 활성화, 개발자 모드 ON
2. 새 게임 (자유지대, 식민자 3명+)
3. `SSF: Dump Form Info`로 9개 폼 로드 확인, `[SSF]` 에러 없음

### 순서

| 단계 | 대상 | 검증 내용 |
|------|------|-----------|
| 1 | BearForm | 약물/스크롤 변신 → 비주얼/스탯/hediff/FX → 수동해제 → 자동해제 |
| 2 | BearWarrior/Sheep | 어빌리티(대상/적대) → 사운드/Effecter → 작업제한 |
| 3 | DarkKnight | 장비소환 → equipLock → 해제 시 소멸+복원 |
| 4 | Beastkin→FullBeast | 2단 체인: renderNode/verb/기즈모 → addAbilities → FullBeast 진입/해제 |
| 5 | Guardian | sustainHediffs/sustainMode → portrait/shadow → revertOnDowned |
| 6 | Phantom | head Replace/셰이더/bodyType/skinColor → FX delay → WorkType 차단 |
| 7 | RaceLocked | allowedRaces → headType → 비대칭 equipLock |
| 8 | 어빌리티 획득 | 유전자/장착무기/장착의류/소지아이템/Hediff 각 경로 |
| 9 | 세이브/로드 | 변신 상태 저장 → 로드 → 해제 복원 |
| 10 | 엣지/호환 | 사망/카라반/SS호환/디버그액션 |

### addAbilities 체인 검증

```
BeastkinForm 적용
  → addAbilities: [Waterskip, SSFTest_Ability_FullBeast]
  → 수인 상태에서 Ability_FullBeast 사용 → FullBeastForm 진입
  → BeastkinForm 해제 → Ability_FullBeast 제거
```
