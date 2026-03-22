# Shapeshifter Framework — 테스트 체크리스트

> TestMod_SSF 기준 | `[A]` = Auto-Verify (디버그 액션) | `[M]` = 인게임 수동 확인
> 넘버링: `{섹션}-A{번호}` = Auto-Verify | `{섹션}-M{번호}` = Manual

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
- [ ] `1-A01` 변신 진입 시 body 텍스처가 `Things/Pawn/Animal/Bear/Bear`로 교체
- [ ] `1-A02` head, hair, beard, tattoo가 Hidden
- [ ] `1-A03` 의류/무기가 드랍 (apparelOnTransform=Drop, weaponsOnTransform=Drop)
- [ ] `1-A04` 도구 `teeth` (Bite 12) 활성
- [ ] `1-A05` `replaceNativeTools=true` — 바닐라 도구 비활성
- [ ] `1-A06` addHediffs: `SSFTest_BeastArm` 부여 확인
- [ ] `1-A07` 해제 시 addHediffs 제거, 이전 장비 복귀
- [ ] `1-A08` revertDrops: `WoodLog 2` 드랍

### [M] 수동 확인
- [ ] `1-M01` 수영 시 텍스처가 `Things/Pawn/Animal/Bear/SwimmingBear`로 전환
- [ ] `1-M02` transformEnterFleck 재생 (PsycastSkipFlashEntry × 1, 스케일 1.8)
- [ ] `1-M03` bloodDef, bloodSmearDef 오버라이드 확인 (피격 시 혈흔 확인)
- [ ] `1-M04` 기즈모 아이콘 표시 (Enter/Revert)
- [ ] `1-M05` 해제 드랍 아이템이 폰 위치에 생성

---

## 2. BearWarriorForm (곰전사) — Armored

### [A] Auto-Verify
- [ ] `2-A01` body/head/hair 유지 (Default)
- [ ] `2-A02` 기존 장비 유지 (Keep)
- [ ] `2-A03` apparelEquipLock=Locked, weaponEquipLock=Unlocked
- [ ] `2-A04` 스탯 오프셋 적용 확인

### [M] 수동 확인
- [ ] `2-M01` transformEnterEffecter (Vaporize_Heatwave) 재생
- [ ] `2-M02` transformExitEffecter (ImpactSmallDustCloud) 재생
- [ ] `2-M03` soundAngry, soundMelee* 사운드 오버라이드 확인
- [ ] `2-M04` 커스텀 기즈모 아이콘 (gizmoIconPathEnter/Revert)
- [ ] `2-M05` 의류 장착 시도 시 차단 메시지 표시
- [ ] `2-M06` 무기는 자유롭게 교체 가능

---

## 3. SheepForm (양) — Animal, 강제 변신

### [A] Auto-Verify
- [ ] `3-A01` `canRevertVoluntarily=false` — 해제 기즈모 없음
- [ ] `3-A02` 작업 태그 비활성: `disabledWorkTagsOnTransform` 적용
- [ ] `3-A03` 전체 그래픽 숨김 (All 필터)

### [M] 수동 확인
- [ ] `3-M01` 성별 텍스처: 남성/여성 다른 body 텍스처 적용
- [ ] `3-M02` AoE 어빌리티로 적대 폰만 변신 (아군 무시)
- [ ] `3-M03` 투사체(CursedArrow) 명중 시 변신 적용
- [ ] `3-M04` duration 만료 시 자동 해제
- [ ] `3-M05` 작업 스케줄에서 비활성 작업 표시

---

## 4. DarkKnightForm (암흑기사) — Armored, 스폰 장비

### [A] Auto-Verify
- [ ] `4-A01` spawnApparel: PlateArmor (Plasteel) 생성 및 착용
- [ ] `4-A02` spawnWeapon: LongSword (Plasteel) 생성 및 장비
- [ ] `4-A03` 충돌 기존 장비 → 인벤토리 이동 (conflictingGearHandling)
- [ ] `4-A04` equipLock: 의류=Locked, 무기=Locked
- [ ] `4-A05` 해제 시 스폰 장비 제거, 기존 장비 복구
- [ ] `4-A06` revertAddHediffs 적용 확인

### [M] 수동 확인
- [ ] `4-M01` 무기 부여(CompGiveAbility_Shapeshift) 장비 시 어빌리티 획득
- [ ] `4-M02` 무기 해제 시 어빌리티 제거
- [ ] `4-M03` 스폰된 장비의 재질(stuff) 올바른지 확인
- [ ] `4-M04` 해제 후 이전 장비가 올바르게 재착용

---

## 5. BeastkinForm (수인) — Humanoid, 렌더 노드

### [A] Auto-Verify
- [ ] `5-A01` body/head/hair 유지
- [ ] `5-A02` renderNode: FloppyEar, FurryTail 추가 확인
- [ ] `5-A03` 5개 커스텀 버브 활성
- [ ] `5-A04` verbGizmoOptions 각 버브별 라벨 확인
- [ ] `5-A05` hairColor 오버라이드 적용
- [ ] `5-A06` addAbilities 확인

### [M] 수동 확인
- [ ] `5-M01` 유전자(Gene_BeastkinShift)로 어빌리티 획득
- [ ] `5-M02` 렌더 노드(귀, 꼬리) 4방향 회전 확인
- [ ] `5-M03` 버브별 자동공격 토글 기즈모
- [ ] `5-M04` 보이스 오버라이드 (call, wounded, death, angry)
- [ ] `5-M05` 작업 태그 제한 (disabledWorkTagsOnTransform: Crafting, Cooking) 확인
- [ ] `5-M06` Overhead 레이어만 숨겨지는지 확인

---

## 6. FullBeastForm (완전수) — Animal, 2단계 체인

### [A] Auto-Verify
- [ ] `6-A01` allowedFromForms: BeastkinForm에서만 시전 가능
- [ ] `6-A02` 미변신 상태에서 기즈모 비활성 (disabled, not hidden)
- [ ] `6-A03` 듀얼 도구 (fangs + claws) 활성
- [ ] `6-A04` 이전 폼(Beastkin) 해제 후 FullBeast 적용

### [M] 수동 확인
- [ ] `6-M01` Beastkin → FullBeast 연속 변신
- [ ] `6-M02` FullBeast에서 Beastkin 복귀 불가 (allowedFromForms 미포함)
- [ ] `6-M03` 해제 시 원래 상태로 완전 복귀 (2단계 모두 해제)

---

## 7. GuardianForm (수호자) — Humanoid, 유지 조건

### [A] Auto-Verify
- [ ] `7-A01` portraitDrawScale 적용 확인
- [ ] `7-A02` bodyOffset 적용 확인
- [ ] `7-A03` shadowVolume/shadowOffset 오버라이드
- [ ] `7-A04` sustainHediffs: GuardianMark 보유 시 유지
- [ ] `7-A05` sustainMode=Any
- [ ] `7-A06` GuardianMark 제거 → 자동 해제

### [M] 수동 확인
- [ ] `7-M01` ambientFleck 주기적 스폰 (interval, scale 확인)
- [ ] `7-M02` 초상화에서 스케일 변경 반영
- [ ] `7-M03` 유지 조건 위반 시 메시지 표시

---

## 8. PhantomForm (환영) — Humanoid, 셰이더

### [A] Auto-Verify
- [ ] `8-A01` head: mode=Replace + shaderTypeDefName=Transparent
- [ ] `8-A02` bodyType=Thin 적용
- [ ] `8-A03` skinColor 오버라이드 적용
- [ ] `8-A04` durationTicks 만료 시 자동 해제

### [M] 수동 확인
- [ ] `8-M01` 의류(PhantomCloak) 착용으로 어빌리티 획득
- [ ] `8-M02` 의류 해제 시 어빌리티 제거
- [ ] `8-M03` 투명 셰이더 시각 확인
- [ ] `8-M04` transformEnterFxDelayTicks 지연 후 FX 재생
- [ ] `8-M05` transformExitFxDelayTicks 지연 후 FX 재생

---

## 9. RaceLockedForm (종족 제한) — Armored

### [A] Auto-Verify
- [ ] `9-A01` formAllowedRaces: Human만 사용 가능
- [ ] `9-A02` 비인간 종족에서 어빌리티 숨김
- [ ] `9-A03` 비대칭 잠금: apparelEquipLock=Locked, weaponEquipLock=Unlocked
- [ ] `9-A04` headType 오버라이드 적용

### [M] 수동 확인
- [ ] `9-M01` 헤디프(Hediff_RacialAwakening)로 어빌리티 획득
- [ ] `9-M02` 인간 폰에서 정상 사용
- [ ] `9-M03` 비인간 폰에서 기즈모 숨김 확인

---

## 10. 트리거 소스 교차 테스트

### [A] Auto-Verify
- [ ] `10-A01` 어빌리티 자기 시전: 변신 진입 + 해제
- [ ] `10-A02` 어빌리티 타인 시전: 대상 변신
- [ ] `10-A03` AoE 어빌리티: 반경 내 적대만 변신 (affectHostileOnly)
- [ ] `10-A04` 약물(BearElixir): 복용 후 변신
- [ ] `10-A05` 스크롤(자기): 사용 후 변신 + 아이템 소멸
- [ ] `10-A06` 스크롤(대상): CompTargetable → 대상 변신
- [ ] `10-A07` 투사체(CursedArrow): 명중 시 변신

### [M] 수동 확인
- [ ] `10-M01` 장비 부여(Weapon_DarkBlade): 장비 시 어빌리티 → 해제 시 제거
- [ ] `10-M02` 의류 부여(Apparel_PhantomCloak): 착용 시 어빌리티 → 탈착 시 제거
- [ ] `10-M03` 유전자(Gene_BeastkinShift): 유전자 보유 시 어빌리티
- [ ] `10-M04` 헤디프(Hediff_ShiftBlessing): 헤디프 보유 시 어빌리티

---

## 11. AutoShift 테스트

### [A] Auto-Verify
- [ ] `11-A01` healthThreshold: HP 30% 미만에서 자동 변신
- [ ] `11-A02` severityThreshold: hediff severity가 기준값 이상에서 자동 변신
- [ ] `11-A03` triggerSunGlowBelow: 밤(SunGlow < 0.5) 자동 변신
- [ ] `11-A04` triggerInCombat: 징집 + 적 근접 시 자동 변신
- [ ] `11-A05` triggerOnce=true: 1회 발동 후 hediff 자체 제거
- [ ] `11-A06` triggerOnce=false: 반복 발동 (해제 후 재조건 충족 시)

### [M] 수동 확인
- [ ] `11-M01` checkIntervalTicks 간격 확인 (빠른/느린)
- [ ] `11-M02` 이미 변신 중일 때 AutoShift 미발동
- [ ] `11-M03` 조건 미충족 시 변신 안 함

---

## 12. 세이브/로드

### [M] 수동 확인
- [ ] `12-M01` 변신 중 세이브 → 로드 후 폼 유지
- [ ] `12-M02` 변신 중 duration 남은 시간 보존
- [ ] `12-M03` 변신 중 장비 상태 보존 (인벤토리, 스폰 장비)
- [ ] `12-M04` 변신 중 verbAutoToggle 상태 보존
- [ ] `12-M05` 해제 후 세이브 → 로드 후 정상 상태
- [ ] `12-M06` PostLoadInit: 레지스트리 재등록, 캐시 재빌드
- [ ] `12-M07` CompGiveAbility_Shapeshift: 장비 착용 중 세이브/로드 후 boundPawn 유지 — 어빌리티 회수 정상 동작
- [ ] `12-M08` needsInit 윈도우 중 세이브 → 로드 후 ApplyForm 정상 실행 (좀비 hediff 방지)

---

## 13. 엣지 케이스

### [A] Auto-Verify
- [ ] `13-A01` 같은 폼 재시전 차단 (ShouldHideGizmo)
- [ ] `13-A02` 사망 Pawn에 변신 시도 → 실패
- [ ] `13-A03` 쓰러진 Pawn + revertOnDowned=true → 자동 해제
- [ ] `13-A04` 다른 변신 중 새 변신 → 이전 폼 해제 후 새 폼 적용
- [ ] `13-A05` 파괴된(Destroyed) 폰에 FX 재생 시도 → 크래시 없이 스킵

### [M] 수동 확인
- [ ] `13-M01` 카라반 참여 중 변신/해제
- [ ] `13-M02` 수면 중 duration 만료 → 해제
- [ ] `13-M03` 정신 이상 중 변신/해제
- [ ] `13-M04` 변신 중 사망 → 시체 원래 외형 복귀
- [ ] `13-M05` 변신 중 체포/구속 → 장비 처리 확인
- [ ] `13-M06` Part가 null인 hediff 정리 시 크래시 없음 (CleanupNullPartHediffs 2-패스)

---

## 14. 호환성

### [M] 수동 확인
- [ ] `14-M01` HAR 종족에서 변신 (body addon 스케일링)
- [ ] `14-M02` Facial Animation 활성 시 변신/해제 (얼굴 백업/복구)
- [ ] `14-M03` Simple Sidearms 활성 시 변신/해제 (무기 메모리 백업/복구)
- [ ] `14-M04` 이데올로기 노출 감정 억제 (`suppressIdeologyUncoveredThoughts`)
- [ ] `14-M05` 변신 규율: 섭리에 대한 모독 → **변신 금지** (기즈모 비활성, CanTransformBasic false), 강제 변신 시 기분 -10, 의견 -20, 기억 감정 -10 (5일), **목격 기억 감정 -8 (5일)**
- [ ] `14-M06` 변신 규율: 부자연스러운 힘 → 변신 시 기분 -5, 의견 -10, 기억 감정 -5 (3일), **목격 기억 감정 -4 (3일)**
- [ ] `14-M06a` 변신 규율: 신경쓰지 않음 → 아무 효과 없음 (중립 단계)
- [ ] `14-M07` 변신 규율: 특별한 재능 → 변신 시 기분 +5, 의견 +10, **목격 기억 감정 +4 (3일)**
- [ ] `14-M08` 변신 규율: 신이 내린 축복 → 변신 시 기분 +10, 의견 +20, **목격 기억 감정 +8 (5일)**
- [ ] `14-M09` 성스러운 동물 폼 (`linkedSacredAnimalDef`) → 숭배 동물 일치 시 기분 +5

---

## 15. 성능 & 안정성

### [M] 수동 확인
- [ ] `15-M01` 다수 폰 동시 변신 (5+) — 프레임 저하 없음
- [ ] `15-M02` 장시간 변신 유지 — 메모리 누수 없음
- [ ] `15-M03` 빈번한 변신/해제 반복 — 크래시 없음
- [ ] `15-M04` 맵 전환 시 캐시 정리 (ClearAll) 확인
- [ ] `15-M05` 대형 폼(bodyDrawScale > 1) 줌아웃 렌더링 — 스레드 안전 (ThreadStatic _invokeArgs)
- [ ] `15-M06` 스냅샷 순회 중 Register/Unregister 안전성 확인 (ReleaseSnapshot)

---

## 16. ConfigErrors 검증

### [A] Auto-Verify
- [ ] `16-A01` sustainGenes에 존재하지 않는 GeneDef → ConfigError 출력
- [ ] `16-A02` formAllowedRaces에 null ThingDef → ConfigError 출력
- [ ] `16-A03` formDisallowedRaces에 null ThingDef → ConfigError 출력
- [ ] `16-A04` formAllowedMutants에 null MutantDef → ConfigError 출력
- [ ] `16-A05` formDisallowedMutants에 null MutantDef → ConfigError 출력
- [ ] `16-A06` spawnApparelOnTransform에 비의류 ThingDef → ConfigError 출력
- [ ] `16-A07` spawnWeaponOnTransform에 비무기 ThingDef → ConfigError 출력

---

## 17. 장비 잠금 & 드랍 안전성

### [M] 수동 확인
- [ ] `17-M01` 변신 중 무기 장착 시도 → 거부 메시지 + 장착 차단
- [ ] `17-M02` 변신 중 의류 착용 시도 → 거부 메시지 + 착용 차단
- [ ] `17-M03` suppressEquipLock=true 상태에서 내부 장비 복구 → 정상 장착/착용 허용
- [ ] `17-M04` 변신 해제 후 장비/의류 자동 복구 → suppressEquipLock 경유 정상 동작
- [ ] `17-M05` 폼 전용 생성 무기 드랍 시도 → 거부 메시지 (살아있는 플레이어 폰)
- [ ] `17-M06` 폼 전용 생성 무기 시스템 드랍 → "holdingOwner still set" 에러 없이 소멸
- [ ] `17-M07` 변신 중 비활성 작업(disabledWorkTypes) → 변신 해제 후 원래 작업 목록 복원 (캐시 오염 없음)
