# Shapeshifter Framework — 테스트 체크리스트

> TestMod_SSF 기준 | `[A]` = Auto-Verify (디버그 액션) | `[M]` = 인게임 수동 확인

---

## 폼 요약

| # | FormDef | 부모 | 트리거 | 핵심 테스트 |
|---|---------|------|--------|------------|
| 1 | SSFTest_BearForm | Animal | 아군 어빌리티, 약물 | 전체 텍스처 교체, 수영, 도구, 헤디프, 해제 드랍, FX |
| 2 | SSFTest_BearWarriorForm | Armored | 적 어빌리티 | 사운드, 이펙터, 아이콘, 장비 유지 |
| 3 | SSFTest_SheepForm | Animal | AoE, 투사체 | 성별 텍스처, 작업 태그 비활성, 강제 변신(해제 불가) |
| 4 | SSFTest_DarkKnightForm | Armored | 자기 어빌리티 (무기 부여) | 스폰 장비, 장비 잠금, 해제 헤디프 |
| 5 | SSFTest_BeastkinForm | Humanoid | 자기 어빌리티 (유전자) | 렌더 노드, 보이스, 5개 버브, 기즈모, 헤어 색상 |
| 6 | SSFTest_FullBeastForm | Animal | 자기 어빌리티 (2단계) | allowedFromForms, 듀얼 도구 |
| 7 | SSFTest_GuardianForm | Humanoid | 자기 어빌리티 | 초상화 스케일, 오프셋, 그림자, 유지 조건, 앰비언트 VFX |
| 8 | SSFTest_PhantomForm | Humanoid | 자기 어빌리티 (의류 부여) | 투명 셰이더, 체형, 피부 색상, FX 딜레이 |
| 9 | SSFTest_RaceLockedForm | Armored | 자기 어빌리티 (헤디프) | 종족 필터, 비대칭 장비 잠금, 머리형 |

---

## 1. BearForm (곰) — Animal 완전 변신

### [A] Auto-Verify
- [ ] 변신 진입 시 body 텍스처가 `SSFTest/Pawn/Bear`로 교체
- [ ] head, hair, beard, tattoo가 Hidden
- [ ] 의류/무기가 인벤토리로 이동
- [ ] 도구 `fangs` (Bite 18), `claws` (Scratch 12) 활성
- [ ] `replaceNativeTools=true` — 바닐라 도구 비활성
- [ ] addHediffs: `SSFTest_BeastArm` 부여 확인
- [ ] 해제 시 addHediffs 제거, 이전 장비 복귀
- [ ] revertDrops: `WoolMuffalo 10` 드랍

### [M] 수동 확인
- [ ] 수영 시 텍스처가 `SSFTest/Pawn/Bear_Swimming`으로 전환
- [ ] shadowVolume `(0.5, 0, 0.6)` 적용 (그림자 크기 확인)
- [ ] 남성/여성 텍스처 분기 (male/female 텍스처 경로 확인)
- [ ] transformEnterFleck 재생 (FleckStatic_PsychicPulse × 3, 스케일 1.5)
- [ ] bloodDef, bloodSmearDef 오버라이드 확인 (피격 시 혈흔 확인)
- [ ] 기즈모 아이콘 표시 (Enter/Revert)
- [ ] 해제 드랍 아이템이 폰 위치에 생성

---

## 2. BearWarriorForm (곰전사) — Armored

### [A] Auto-Verify
- [ ] body/head/hair 유지 (Default)
- [ ] 기존 장비 유지 (Keep)
- [ ] apparelEquipLock=Locked, weaponEquipLock=Unlocked
- [ ] 스탯 오프셋 적용 확인

### [M] 수동 확인
- [ ] transformEnterSound 재생
- [ ] transformEnterEffecter 재생
- [ ] 커스텀 기즈모 아이콘 (gizmoIconPathEnter/Revert)
- [ ] 의류 장착 시도 시 차단 메시지 표시
- [ ] 무기는 자유롭게 교체 가능

---

## 3. SheepForm (양) — Animal, 강제 변신

### [A] Auto-Verify
- [ ] `canRevertVoluntarily=false` — 해제 기즈모 없음
- [ ] 작업 태그 비활성: `disabledWorkTagsOnTransform` 적용
- [ ] 전체 그래픽 숨김 (All 필터)

### [M] 수동 확인
- [ ] 성별 텍스처: 남성/여성 다른 body 텍스처 적용
- [ ] AoE 어빌리티로 적대 폰만 변신 (아군 무시)
- [ ] 투사체(CursedArrow) 명중 시 변신 적용
- [ ] duration 만료 시 자동 해제
- [ ] 작업 스케줄에서 비활성 작업 표시

---

## 4. DarkKnightForm (암흑기사) — Armored, 스폰 장비

### [A] Auto-Verify
- [ ] spawnApparel: PlateArmor (Steel) 생성 및 착용
- [ ] spawnWeapon: LongSword (Plasteel) 생성 및 장비
- [ ] 충돌 기존 장비 → 인벤토리 이동 (conflictingGearHandling)
- [ ] equipLock: 의류=Locked, 무기=Locked
- [ ] 해제 시 스폰 장비 제거, 기존 장비 복구
- [ ] revertAddHediffs 적용 확인

### [M] 수동 확인
- [ ] 무기 부여(CompGiveAbility_Shapeshift) 장비 시 어빌리티 획득
- [ ] 무기 해제 시 어빌리티 제거
- [ ] 스폰된 장비의 재질(stuff) 올바른지 확인
- [ ] 해제 후 이전 장비가 올바르게 재착용

---

## 5. BeastkinForm (수인) — Humanoid, 렌더 노드

### [A] Auto-Verify
- [ ] body/head/hair 유지
- [ ] renderNode: FloppyEar, FurryTail 추가 확인
- [ ] 5개 커스텀 버브 활성
- [ ] verbGizmoOptions 각 버브별 라벨 확인
- [ ] hairColor 오버라이드 적용
- [ ] addAbilities 확인

### [M] 수동 확인
- [ ] 유전자(Gene_BeastkinShift)로 어빌리티 획득
- [ ] 렌더 노드(귀, 꼬리) 4방향 회전 확인
- [ ] 버브별 자동공격 토글 기즈모
- [ ] 보이스 오버라이드 (call, wounded, death, angry)
- [ ] 작업 제한 (disabledWorkTypesOnTransform) 확인
- [ ] Overhead 레이어만 숨겨지는지 확인

---

## 6. FullBeastForm (완전수) — Animal, 2단계 체인

### [A] Auto-Verify
- [ ] allowedFromForms: BeastkinForm에서만 시전 가능
- [ ] 미변신 상태에서 기즈모 비활성 (disabled, not hidden)
- [ ] 듀얼 도구 (fangs + claws) 활성
- [ ] 이전 폼(Beastkin) 해제 후 FullBeast 적용

### [M] 수동 확인
- [ ] Beastkin → FullBeast 연속 변신
- [ ] FullBeast에서 Beastkin 복귀 불가 (allowedFromForms 미포함)
- [ ] 해제 시 원래 상태로 완전 복귀 (2단계 모두 해제)

---

## 7. GuardianForm (수호자) — Humanoid, 유지 조건

### [A] Auto-Verify
- [ ] portraitDrawScale 적용 확인
- [ ] bodyOffset 적용 확인
- [ ] shadowVolume/shadowOffset 오버라이드
- [ ] sustainHediffs: GuardianMark 보유 시 유지
- [ ] sustainMode=Any
- [ ] GuardianMark 제거 → 자동 해제

### [M] 수동 확인
- [ ] ambientFleck 주기적 스폰 (interval, scale 확인)
- [ ] 초상화에서 스케일 변경 반영
- [ ] 유지 조건 위반 시 메시지 표시

---

## 8. PhantomForm (환영) — Humanoid, 셰이더

### [A] Auto-Verify
- [ ] head: mode=Replace + shaderTypeDefName=Transparent
- [ ] bodyType=Thin 적용
- [ ] skinColor 오버라이드 적용
- [ ] durationTicks 만료 시 자동 해제

### [M] 수동 확인
- [ ] 의류(PhantomCloak) 착용으로 어빌리티 획득
- [ ] 의류 해제 시 어빌리티 제거
- [ ] 투명 셰이더 시각 확인
- [ ] transformEnterFxDelayTicks 지연 후 FX 재생
- [ ] transformExitFxDelayTicks 지연 후 FX 재생

---

## 9. RaceLockedForm (종족 제한) — Armored

### [A] Auto-Verify
- [ ] formAllowedRaces: Human만 사용 가능
- [ ] 비인간 종족에서 어빌리티 숨김
- [ ] 비대칭 잠금: apparelEquipLock=Locked, weaponEquipLock=Unlocked
- [ ] headType 오버라이드 적용

### [M] 수동 확인
- [ ] 헤디프(Hediff_RacialAwakening)로 어빌리티 획득
- [ ] 인간 폰에서 정상 사용
- [ ] 비인간 폰에서 기즈모 숨김 확인

---

## 10. 트리거 소스 교차 테스트

### [A] Auto-Verify
- [ ] 어빌리티 자기 시전: 변신 진입 + 해제
- [ ] 어빌리티 타인 시전: 대상 변신
- [ ] AoE 어빌리티: 반경 내 적대만 변신 (affectHostileOnly)
- [ ] 약물(BearElixir): 복용 후 변신
- [ ] 스크롤(자기): 사용 후 변신 + 아이템 소멸
- [ ] 스크롤(대상): CompTargetable → 대상 변신
- [ ] 투사체(CursedArrow): 명중 시 변신

### [M] 수동 확인
- [ ] 장비 부여(Weapon_DarkBlade): 장비 시 어빌리티 → 해제 시 제거
- [ ] 의류 부여(Apparel_PhantomCloak): 착용 시 어빌리티 → 탈착 시 제거
- [ ] 유전자(Gene_BeastkinShift): 유전자 보유 시 어빌리티
- [ ] 헤디프(Hediff_ShiftBlessing): 헤디프 보유 시 어빌리티

---

## 11. AutoShift 테스트

### [A] Auto-Verify
- [ ] healthThreshold: HP 30% 미만에서 자동 변신
- [ ] severityThreshold: hediff severity가 기준값 이상에서 자동 변신
- [ ] triggerSunGlowBelow: 밤(SunGlow < 0.5) 자동 변신
- [ ] triggerInCombat: 징집 + 적 근접 시 자동 변신
- [ ] triggerOnce=true: 1회 발동 후 hediff 자체 제거
- [ ] triggerOnce=false: 반복 발동 (해제 후 재조건 충족 시)

### [M] 수동 확인
- [ ] checkIntervalTicks 간격 확인 (빠른/느린)
- [ ] 이미 변신 중일 때 AutoShift 미발동
- [ ] 조건 미충족 시 변신 안 함

---

## 12. 세이브/로드

### [M] 수동 확인
- [ ] 변신 중 세이브 → 로드 후 폼 유지
- [ ] 변신 중 duration 남은 시간 보존
- [ ] 변신 중 장비 상태 보존 (인벤토리, 스폰 장비)
- [ ] 변신 중 verbAutoToggle 상태 보존
- [ ] 해제 후 세이브 → 로드 후 정상 상태
- [ ] PostLoadInit: 레지스트리 재등록, 캐시 재빌드

---

## 13. 엣지 케이스

### [A] Auto-Verify
- [ ] 같은 폼 재시전 차단 (ShouldHideGizmo)
- [ ] 사망 Pawn에 변신 시도 → 실패
- [ ] 쓰러진 Pawn + revertOnDowned=true → 자동 해제
- [ ] 다른 변신 중 새 변신 → 이전 폼 해제 후 새 폼 적용

### [M] 수동 확인
- [ ] 카라반 참여 중 변신/해제
- [ ] 수면 중 duration 만료 → 해제
- [ ] 정신 이상 중 변신/해제
- [ ] 변신 중 사망 → 시체 원래 외형 복귀
- [ ] 변신 중 체포/구속 → 장비 처리 확인

---

## 14. 호환성

### [M] 수동 확인
- [ ] HAR 종족에서 변신 (body addon 스케일링)
- [ ] Facial Animation 활성 시 변신/해제 (얼굴 백업/복구)
- [ ] Simple Sidearms 활성 시 변신/해제 (무기 메모리 백업/복구)
- [ ] 이데올로기 노출 감정 억제 (`suppressIdeologyUncoveredThoughts`)
- [ ] 변신 규율: 금기 단계 → 변신 시 기분 -10, 의견 -20, "금기를 어김" 기억 감정 -10 (5일)
- [ ] 변신 규율: 중립 단계 → 변신 시 감정 없음
- [ ] 변신 규율: 명예로움 단계 → 변신 시 기분 +10, 의견 +20
- [ ] 성스러운 동물 폼 (`linkedSacredAnimalDef`) → 숭배 동물 일치 시 기분 +5

---

## 15. 성능 & 안정성

### [M] 수동 확인
- [ ] 다수 폰 동시 변신 (5+) — 프레임 저하 없음
- [ ] 장시간 변신 유지 — 메모리 누수 없음
- [ ] 빈번한 변신/해제 반복 — 크래시 없음
- [ ] 맵 전환 시 캐시 정리 (ClearAll) 확인
