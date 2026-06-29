# Shapeshifter Framework — 테스트 체크리스트

최신 기준 커밋: `be173ec` (2026-04-11)
이전 기준 커밋: `e72e60c` (2026-04-02)

> TestMod_SSF 기준 | `[A]` = Auto-Verify (디버그 액션) | `[M]` = 인게임 수동 확인
> 넘버링: `{섹션}-A{번호}` = Auto-Verify | `{섹션}-M{번호}` = Manual

---

## 폼 요약

| # | FormDef | 부모 | 트리거 | 핵심 테스트 |
|---|---------|------|--------|------------|
| 1 | SSFTest_BearForm | Animal | 약물, 스크롤, 리프레시 약물/스크롤/범용스크롤 | 전체 텍스처 교체, 수영, 도구, 헤디프, 해제 드랍, FX, 성스러운 동물, **allowedFromForms 약물/아이템**, **범용 연장** |
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

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 약물 | 개발자 콘솔 → `SSFTest_BearElixir` 스폰 → 폰에게 복용 지시 |
| 스크롤(자기) | 개발자 콘솔 → `SSFTest_ShiftScroll_Self` 스폰 → 폰이 사용 |
| 스크롤(대상) | 개발자 콘솔 → `SSFTest_ShiftScroll_Target` 스폰 → 대상 폰 선택 사용 |
| 리프레시 약물 | 개발자 콘솔 → `SSFTest_BearRefreshElixir` 스폰 → **곰 변신 중** 폰이 복용 (allowedFromForms 테스트) |
| 리프레시 스크롤 | 개발자 콘솔 → `SSFTest_ShiftScroll_BearRefresh` 스폰 → **곰 변신 중** 폰이 사용 (allowedFromForms 테스트) |
| 범용 리프레시 스크롤 | 개발자 콘솔 → `SSFTest_ShiftScroll_UniversalRefresh` 스폰 → **아무 폼 변신 중** 폰이 사용 (범용 연장, allowExtendBeyondMax=true) |
| 아군 어빌리티 | BearWarrior 어빌리티(`SSFTest_Ability_BuffAlly`)와 다름 — BearForm 직접 어빌리티 없음, 위 아이템 경로로만 진입 |

### [A] Auto-Verify
- [ ] `1-A01` 변신 진입 시 body 텍스처가 `Things/Pawn/Animal/Bear/Bear`로 교체
- [ ] `1-A02` head, hair, beard, tattoo가 Hidden
- [ ] `1-A03` 의류/무기가 드랍 (apparelOnTransform=Drop, weaponsOnTransform=Drop)
- [ ] `1-A04` 도구 `teeth` (Bite 12) 활성
- [ ] `1-A05` `replaceNativeTools=true` — 바닐라 도구 비활성
- [ ] `1-A06` addHediffs: `SSFTest_BeastArm` 부여 확인
- [ ] `1-A07` 해제 시 addHediffs 제거, 이전 장비 복귀
- [ ] `1-A08` revertDrops: `WoodLog 2` 드랍
- [ ] `1-A09` beard=Hidden, tattooBody=Hidden, tattooHead=Hidden 적용
- [ ] `1-A10` 변신 중 근접 공격 시 상처 라벨에 폼 이름("bear form") 자동 표시
- [ ] `1-A11` suppressIdeologyUncoveredThoughts=true → 노출 감정 미발생
- [ ] `1-A12` formAllowedMutants: Anomaly DLC 설치 시 뮤턴트 필터 동작 확인 (별도 테스트 필요)

### [M] 수동 확인
- [ ] `1-M01` 수영 시 텍스처가 `Things/Pawn/Animal/Bear/SwimmingBear`로 전환
- [ ] `1-M02` transformEnterFleck 재생 (PsycastSkipFlashEntry × 1, 스케일 1.8)
- [ ] `1-M03` bloodDef, bloodSmearDef 오버라이드 확인 (피격 시 혈흔 확인)
- [ ] `1-M04` 기즈모 아이콘 표시 (Enter/Revert)
- [ ] `1-M05` 해제 드랍 아이템이 폰 위치에 생성

---

## 2. BearWarriorForm (곰전사) — Armored

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 아군 어빌리티 | 개발자 콘솔 → 시전자에게 `SSFTest_Ability_BuffAlly` 부여 → 아군 폰 대상 시전 |
| 헤디프 경유 | 개발자 콘솔 → 폰에게 `SSFTest_Hediff_ShiftBlessing` 부여 → 자동으로 BuffAlly 어빌리티 획득 → 아군 시전 |
| AutoShift(전투) | 개발자 콘솔 → 폰에게 `SSFTest_Hediff_CombatAutoShift` 부여 → 징집 + 적 근접 시 자동 변신 (1회성) |

### [A] Auto-Verify
- [ ] `2-A01` body/head/hair 유지 (Default)
- [ ] `2-A02` 기존 장비 유지 (Keep)
- [ ] `2-A03` apparelEquipLock=Locked, weaponEquipLock=Unlocked
- [ ] `2-A04` 스탯 오프셋 적용 확인

### [M] 수동 확인
- [ ] `2-M01` transformEnterEffecter (Vaporize_Heatwave) 재생
- [ ] `2-M02` transformExitEffecter (ImpactSmallDustCloud) 재생
- [ ] `2-M03` soundAngry, soundMelee* 사운드 오버라이드 확인
- [ ] `2-M04` 커스텀 기즈모 아이콘 (gizmoIconPathRevert)
- [ ] `2-M05` 의류 장착 시도 시 차단 메시지 표시
- [ ] `2-M06` 무기는 자유롭게 교체 가능
- [ ] `2-M07` ambientFleck (PsycastSkipInnerExit) 변신 중 주기적 스폰 (60틱 간격)

---

## 3. SheepForm (양) — Animal, 강제 변신

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 적 어빌리티 | 개발자 콘솔 → 시전자에게 `SSFTest_Ability_DebuffEnemy` 부여 → 적대 폰 대상 시전 |
| AoE 어빌리티 | 개발자 콘솔 → 시전자에게 `SSFTest_Ability_MassPolymorph` 부여 → 적대 폰 근처 지점 시전 (반경 5) |
| 투사체(저주 활) | 개발자 콘솔 → `SSFTest_Weapon_CursedBow` 스폰 → 폰 장비 → 적 사격, 명중 시 변신 |

### [A] Auto-Verify
- [ ] `3-A01` `canRevertVoluntarily=false` — 해제 기즈모 없음
- [ ] `3-A02` 작업 태그 비활성: `disabledWorkTagsOnTransform` 적용
- [ ] `3-A03` 전체 그래픽 숨김 (All 필터)
- [ ] `3-A04` renderHideApparelLayers=All → 모든 의류 그래픽 숨김
- [ ] `3-A05` renderHideWeaponTags=All → 모든 무기 그래픽 숨김
- [ ] `3-A06` renderHideHediffDefNames=All → 모든 헤디프 그래픽 숨김

### [M] 수동 확인
- [ ] `3-M01` 성별 텍스처: 남성/여성 다른 body 텍스처 적용
- [ ] `3-M02` AoE 어빌리티로 적대 폰만 변신 (아군 무시)
- [ ] `3-M03` 투사체(CursedArrow) 명중 시 변신 적용
- [ ] `3-M04` duration 만료 시 자동 해제
- [ ] `3-M05` 작업 스케줄에서 비활성 작업 표시

---

## 4. DarkKnightForm (암흑기사) — Armored, 스폰 장비

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 자기 어빌리티 | 개발자 콘솔 → 폰에게 `SSFTest_Ability_DarkKnight` 부여 → 자기 시전 |
| 무기 부여 | 개발자 콘솔 → `SSFTest_Weapon_DarkBlade` 스폰 → 폰 장비 → 자동으로 어빌리티 획득 → 시전 |

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

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 자기 어빌리티 | 개발자 콘솔 → 폰에게 `SSFTest_Ability_Beastkin` 부여 → 자기 시전 |
| 유전자 (Biotech) | 개발자 콘솔 → 폰에게 `SSFTest_Gene_BeastkinShift` 유전자 추가 → 자동으로 어빌리티 획득 → 시전 |

### [A] Auto-Verify
- [ ] `5-A01` body/head/hair 유지
- [ ] `5-A02` renderNode: FloppyEar, FurryTail 추가 확인
- [ ] `5-A03` 5개 커스텀 버브 활성
- [ ] `5-A04` verbGizmoOptions 각 버브별 라벨 확인
- [ ] `5-A05` hairColor 오버라이드 적용
- [ ] `5-A06` addAbilities 확인
- [ ] `5-A07` renderShowApparelLayers: Overhead 레이어 의류만 표시
- [ ] `5-A08` verbGizmoOptions desc/toggleLabel/toggleDesc 표시 확인 (AssaultRifle verb)

### [M] 수동 확인
- [ ] `5-M01` 유전자(Gene_BeastkinShift)로 어빌리티 획득
- [ ] `5-M02` 렌더 노드(귀, 꼬리) 4방향 회전 확인
- [ ] `5-M03` 버브별 자동공격 토글 기즈모
- [ ] `5-M04` 보이스 오버라이드 (call, wounded, death, angry)
- [ ] `5-M05` 작업 태그 제한 (disabledWorkTagsOnTransform: Crafting, Cooking) 확인
- [ ] `5-M06` Overhead 레이어만 숨겨지는지 확인
- [ ] `5-M07` FragGrenade/ShootBeam 사용 시 변신 시간 차감 확인 (durationCostTicks)
- [ ] `5-M08` 차감 verb 기즈모 설명에 "변신 시간 소모" 텍스트 표시 확인
- [ ] `5-M09` PrimalRoar 어빌리티: 수인 변신 시에만 표시, 사용 시 변신 시간 7500틱 차감 확인
- [ ] `5-M10` PrimalRoar 어빌리티: 변신 해제 시 어빌리티 제거 확인
- [ ] `5-M11` PrimalRoar 어빌리티: 툴팁에 "변신 시간 소모" 표시 확인
- [ ] `5-M12` entropyCost 설정 verb: psylink 있는 폰에서 사용 시 신경열 증가 확인
- [ ] `5-M13` entropyCost 설정 verb: psylink 없는 폰 (tracker=null) → 신경열 비용 무시, 자유 사용 확인
- [ ] `5-M14` entropyCost 설정 verb: psylink 있지만 신경열 꽉 찼을 때 기즈모 비활성 확인
- [ ] `5-M15` entropyCost 설정 verb: 로열티 DLC 비활성 시 신경열 비용 무시, 자유 사용 확인
- [ ] `5-M16` entropyCost 기즈모 설명에 "신경열: N" 텍스트 표시 확인
- [ ] `5-M17` Dev Mode 켠 상태에서 변신 → 기즈모 라벨/설명이 PseudoTranslated로 깨지지 않음 (verbProps.label·verbGizmoOptions label/desc는 표시 리터럴, .Translate() 미적용)

---

## 6. FullBeastForm (완전수) — Animal, 2단계 체인

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 2단계 어빌리티 | **먼저 BeastkinForm 진입 필수** → BeastkinForm의 `addAbilities`로 `SSFTest_Ability_FullBeast` 자동 부여 → 시전 |
| 전제 조건 | BeastkinForm 상태가 아니면 기즈모 비활성 (`allowedFromForms: BeastkinForm`) |

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

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 자기 어빌리티 | 개발자 콘솔 → 폰에게 `SSFTest_Ability_Guardian` 부여 → 자기 시전 |
| 유지 조건 준비 | 변신 유지를 테스트하려면 사전에 `SSFTest_Hediff_GuardianMark` 헤디프 부여 필요 (sustainHediffs 조건) |

### [A] Auto-Verify
- [ ] `7-A01` portraitDrawScale 적용 확인
- [ ] `7-A02` bodyOffset 적용 확인
- [ ] `7-A03` shadowVolume/shadowOffset 오버라이드
- [ ] `7-A04` sustainHediffs: GuardianMark 보유 시 유지
- [ ] `7-A05` sustainMode=Any
- [ ] `7-A06` GuardianMark 제거 → 자동 해제 (sustainApparels 조건도 미충족 시)
- [ ] `7-A07` sustainApparels: PlateArmor 착용 시 GuardianMark 없이도 유지 (Any 모드)

### [M] 수동 확인
- [ ] `7-M01` ambientFleck 주기적 스폰 (interval, scale 확인)
- [ ] `7-M02` 초상화에서 스케일 변경 반영
- [ ] `7-M03` 유지 조건 위반 시 메시지 표시

---

## 8. PhantomForm (환영) — Humanoid, 셰이더

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 자기 어빌리티 | 개발자 콘솔 → 폰에게 `SSFTest_Ability_Phantom` 부여 → 자기 시전 |
| 의류 부여 | 개발자 콘솔 → `SSFTest_Apparel_PhantomCloak` 스폰 → 폰 착용 → 자동으로 어빌리티 획득 → 시전 |
| 유전자 (Biotech) | 개발자 콘솔 → 폰에게 `SSFTest_Gene_PhantomShift` 유전자 추가 → 자동으로 어빌리티 획득 → 시전 |

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

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 자기 어빌리티 | 개발자 콘솔 → 인간 폰에게 `SSFTest_Ability_RaceLocked` 부여 → 자기 시전 |
| 헤디프 경유 | 개발자 콘솔 → 폰에게 `SSFTest_Hediff_RacialAwakening` 부여 → 자동으로 어빌리티 획득 → 시전 |
| 종족 제한 테스트 | 비인간 폰에게 동일하게 부여 → 기즈모 숨김 확인 |

### [A] Auto-Verify
- [ ] `9-A01` formAllowedRaces: Human만 사용 가능
- [ ] `9-A02` 비인간 종족에서 어빌리티 숨김
- [ ] `9-A03` 비대칭 잠금: apparelEquipLock=Locked, weaponEquipLock=Unlocked
- [ ] `9-A04` headType 오버라이드 적용
- [ ] `9-A05` formDisallowedRaces: Waster 종족 변신 차단

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
- [ ] `10-A06a` 스크롤(대상): **사용자가 변신 중**이어도 타인 대상 스크롤 사용 가능
- [ ] `10-A06b` 스크롤(대상): 타인 대상 스크롤을 **이미 변신 중인 대상**에게 사용 시 차단
- [ ] `10-A07` 투사체(CursedArrow): 명중 시 변신
- [ ] `10-A08` 약물(BearRefreshElixir, allowedFromForms): 곰 변신 중 복용 → 변신 갱신 허용
- [ ] `10-A09` 스크롤(ShiftScroll_BearRefresh, allowedFromForms): 곰 변신 중 사용 → 변신 갱신 허용
- [ ] `10-A10` 약물(BearRefreshElixir): **비곰 폼** 변신 중 복용 시도 → 차단
- [ ] `10-A11` 스크롤(ShiftScroll_UniversalRefresh, targetFormDef 미설정): **아무 폼** 변신 중 사용 → 20000틱 연장 허용
- [ ] `10-A12` 스크롤(ShiftScroll_UniversalRefresh): 비변신 상태 사용 시도 → 차단 ("변신 중이 아닙니다")

### [M] 수동 확인
- [ ] `10-M01` 장비 부여(Weapon_DarkBlade): 장비 시 어빌리티 → 해제 시 제거
- [ ] `10-M02` 의류 부여(Apparel_PhantomCloak): 착용 시 어빌리티 → 탈착 시 제거
- [ ] `10-M03` 유전자(Gene_BeastkinShift): 유전자 보유 시 어빌리티
- [ ] `10-M04` 헤디프(Hediff_ShiftBlessing): 헤디프 보유 시 어빌리티
- [ ] `10-M05` 동일 AbilityDef를 부여하는 아이템 2개 동시 장비 → 하나만 해제/드롭/파괴 시 어빌리티 유지 (다른 소스가 남아있으면 회수 안 함)

---

## 11. AutoShift 테스트

### 폼 획득 방법
| 경로 | 방법 |
|------|------|
| 야간/체력 자동변신 | 개발자 콘솔 → 폰에게 `SSFTest_Hediff_AutoShiftCurse` 부여 → 밤(SunGlow<0.5) 또는 HP 30% 미만 시 자동 곰 변신 |
| 전투 자동변신 (1회) | 개발자 콘솔 → 폰에게 `SSFTest_Hediff_CombatAutoShift` 부여 → 징집 + 적 근접 시 곰전사 자동 변신, 1회 발동 후 헤디프 제거 |

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
- [ ] `13-A01` 같은 폼 재시전 → 차단 (변신 유지, 갱신 없음. allowedFromForms 설정 시에만 허용)
- [ ] `13-A02` 사망 Pawn에 변신 시도 → 실패
- [ ] `13-A03` 쓰러진 Pawn + revertOnDowned=true → 자동 해제
- [ ] `13-A04` 다른 변신 중 새 변신 → 차단 (allowedFromForms 설정 시에만 전환 허용)
- [ ] `13-A04a` 약물 ExtendShapeshift: `SSFTest_BearRefreshElixir`(targetFormDef=BearForm) → 곰 변신 중 섭취 시 시간 연장, 비변신 시 FloatMenu 비활성 + 약물 소모 없음
- [ ] `13-A04b` 아이템 ExtendShapeshift: `SSFTest_ShiftScroll_BearRefresh`(targetFormDef=BearForm) → 곰 변신 중 사용 시 시간 연장, 비변신 시 사용 불가
- [ ] `13-A04c` allowedFromForms 미설정: `SSFTest_BearElixir`/`SSFTest_ShiftScroll_Self`는 변신 중 차단 (기본 동작 유지)
- [ ] `13-A04d` ExtendShapeshift 약물: 다른 폼(늑대 등) 변신 중 `SSFTest_BearRefreshElixir` 섭취 → FloatMenu 비활성 ("필요한 폼이 아닙니다") + 약물 소모 없음
- [ ] `13-A04e` ExtendShapeshift: allowExtendBeyondMax=false → 연장 후 남은 시간이 원래 최대 시간을 초과하지 않음 (`SSFTest_ShiftScroll_BearRefresh`)
- [ ] `13-A04f` ExtendShapeshift: allowExtendBeyondMax=true → 연장 후 남은 시간이 원래 최대 시간 초과 가능 (`SSFTest_ShiftScroll_UniversalRefresh`)
- [ ] `13-A04g` ExtendShapeshift: targetFormDef 미설정(범용) → 곰/양/다크나이트 등 어떤 폼에서든 연장 성공 (`SSFTest_ShiftScroll_UniversalRefresh`)
- [ ] `13-A05` 파괴된(Destroyed) 폰에 FX 재생 시도 → 크래시 없이 스킵
- [ ] `13-A06` sourceItem(변신 트리거 장비) 장착 중 → 변신 유지 (Spawned 상태여도 해제 안 됨)
- [ ] `13-A07` sourceItem 파괴 → 즉시 변신 해제
- [ ] `13-A08` sourceItem 드랍(바닥에 놓음) → 즉시 변신 해제

### [M] 수동 확인
- [ ] `13-M01` 카라반 참여 중 변신/해제
- [ ] `13-M02` 수면 중 duration 만료 → 해제
- [ ] `13-M03` 정신 이상 중 변신/해제
- [ ] `13-M04` 변신 중 사망 → 시체 원래 외형 복귀
- [ ] `13-M05` 변신 중 체포/구속 → 장비 처리 확인
- [ ] `13-M06` Part가 null인 hediff 정리 시 크래시 없음 (CleanupNullPartHediffs 2-패스)
- [ ] `13-M07` ExtendDuration 양수 → 시간제 변신의 남은 시간 증가 확인 (allowBeyondMax=true)
- [ ] `13-M08` ExtendDuration 음수(남은 시간 초과) → 타이머 0 도달, 다음 틱 자동 해제
- [ ] `13-M09` ExtendDuration on 영구 변신 → 무시 (변화 없음)
- [ ] `13-M10` ExtendDuration allowBeyondMax=false → 원래 최대 시간 이내로 제한 확인
- [ ] `13-M11` `SSFTest_BearRefreshElixir` 곰 변신 중 복용 → 남은 시간 30000틱 증가 확인 (인스펙터 확인)
- [ ] `13-M12` `SSFTest_ShiftScroll_BearRefresh` 곰 변신 중 사용 → 남은 시간 30000틱 증가 확인
- [ ] `13-M13` `SSFTest_ShiftScroll_UniversalRefresh` 아무 폼 변신 중 사용 → 남은 시간 20000틱 증가 확인 (최대 시간 초과 허용)
- [ ] `13-M14` 연장 약물(`BearRefreshElixir`) 비변신 시 우클릭 → FloatMenu 비활성 + 약물 소모 없음
- [ ] `13-M15` 연장 약물(`BearRefreshElixir`) 비곰 폼 변신 중 우클릭 → FloatMenu 비활성 ("필요한 폼이 아닙니다")
- [ ] `13-M16` 변신 중 자기 전용 어빌리티(`targetRequired=false`) → 기즈모에서 숨겨짐
- [ ] `13-M17` 변신 중 타인 대상 어빌리티(`targetRequired=true`) → 기즈모 표시 유지
- [ ] `13-M18` 어빌리티 호버 툴팁 → 폼 이름/지속시간(또는 "Permanent") 표시 확인
- [ ] `13-M19` 기즈모 바 — 긴 폼 이름 → Tiny 폰트 축소 + 말줄임 + 호버 전체 이름

---

## 14. 호환성

### [M] 수동 확인
- [ ] `14-M01` HAR 종족에서 변신 (body addon 스케일링)
- [ ] `14-M02` Facial Animation 활성 시 변신/해제 (얼굴 백업/복구)
- [ ] `14-M02a` FA 활성 시 변신 진입 → FA 상태(FaceTypeDef, 눈 색상 등) 백업 확인 (로그에 `OverridesHook` Patched 메시지)
- [ ] `14-M02b` FA 활성 시 변신 해제 → FA 상태 원본 복구 확인
- [ ] `14-M02c` FA 비활성 시 → `OverridesHook` 패치 미적용 확인 (Prepare false)
- [ ] `14-M03` Simple Sidearms 활성 시 변신/해제 (무기 메모리 백업/복구)
- [ ] `14-M03a` Pocket Sand 활성 + weaponEquipLock=Locked 폼(DarkKnight) → 무기 선택 기즈모 숨김
- [ ] `14-M03b` Pocket Sand 활성 + weaponEquipLock=Unlocked 폼(RaceLocked) → 무기 선택 기즈모 정상 표시
- [ ] `14-M04` 이데올로기 노출 감정 억제 (`suppressIdeologyUncoveredThoughts`)
- [ ] `14-M05` 변신 규율: 섭리에 대한 모독 → **자기 주도 변신 금지** (기즈모 비활성 "사상이 금지함", 약물 섭취 불가, 주문서 사용 불가), **타인에 의한 강제 변신은 허용** (원래 시간 유지), 강제 변신 시 기분 -10, 의견 -20, 기억 감정 -10 (5일), **목격 기억 감정 -8 (5일)**
- [ ] `14-M05a` 변신 규율: 섭리에 대한 모독 — 약물 우클릭 시 "(사상이 금지함)" 비활성 옵션 확인
- [ ] `14-M05b` 변신 규율: 섭리에 대한 모독 — 타인 어빌리티/투사체로 강제 변신 성공 확인
- [ ] `14-M06` 변신 규율: 부자연스러운 힘 → 변신 시 기분 -5, 의견 -10, 기억 감정 -5 (3일), **목격 기억 감정 -4 (3일)**
- [ ] `14-M06a` 변신 규율: 신경쓰지 않음 → 아무 효과 없음 (중립 단계)
- [ ] `14-M07` 변신 규율: 특별한 재능 → 변신 시 기분 +5, 의견 +10, **목격 기억 감정 +4 (3일)**
- [ ] `14-M08` 변신 규율: 신이 내린 축복 → 변신 시 기분 +10, 의견 +20, **목격 기억 감정 +8 (5일)**
- [ ] `14-M09` 성스러운 동물 폼 (`linkedSacredAnimalDef`) → 규율 단계별 기분: 혐오(-8) / 못마땅(-3) / 무관심(+2) / 존중(+5) / 숭고(+8)
  - 테스트 대상: `SSFTest_BearForm` (linkedSacredAnimalDef=`Bear_Grizzly`)
  - 방법: 이데올로기에서 성스러운 동물을 회색곰(Bear_Grizzly)으로 설정 → 변신 규율 단계 변경 → BearForm 변신 시 감정 확인
- [ ] `14-M10` CE 환경에서 BearForm teeth 공격 → ToolCE 관통값(sharp=4, blunt=8) 적용 확인 (MayRequire XML 패치)
- [ ] `14-M11` CE 환경에서 BeastkinForm claws 공격 → ToolCE 관통값(sharp=3, blunt=6) 적용 확인 (MayRequire XML 패치)
- [ ] `14-M12` CE 환경에서 BeastkinForm AssaultRifle verb → VerbPropertiesCE(Verb_ShootCE) 동작 확인 (MayRequire XML 패치)
- [ ] `14-M13` CE 없이 로드 시 → CE 패치 미적용, 바닐라 Tool/VerbProperties 정상 동작 확인
- [ ] `14-M14` SS 활성 + 폼 A→B 직접 전환(allowedFromForms) → B 변신 중 원본 사이드암 메모리 부활 안 함, 최종 해제 시 원본 정상 복원 (내부 RemoveForm 복원 보류 플래그)
- [ ] `14-M15` FA 활성 + 변신 폰 렌더 중 스케일 예외 발생 시 → `[SSF]` 경고가 매 프레임 반복되지 않고 1회만 기록 (HasFailed 가드)

---

## 15. 폼 전환 차단 & 좀비 hediff 방지

### [M] 수동 확인
- [ ] `15-M01` 변신 중 자기가 다른 폼 약물 섭취 → "이미 다른 형태로 변신 중" FloatMenu 비활성
- [ ] `15-M02` 변신 중 자기가 다른 폼 주문서 사용 → "이미 다른 형태로 변신 중" 사용 불가
- [ ] `15-M03` 변신 중 같은 폼 재변신 → 차단 (동일 폼 포함 모든 변신 차단, allowedFromForms 예외)
- [ ] `15-M04` 변신 중 타인 어빌리티/투사체로 다른 폼 강제 → 차단 (변신 유지)
- [ ] `15-M05` allowedFromForms 설정된 어빌리티 → 기존대로 전환 허용
- [ ] `15-M05a` allowedFromForms 설정된 약물(`SSFTest_BearRefreshElixir`) → 해당 폼에서 섭취 허용, 다른 폼에서 차단
- [ ] `15-M05b` allowedFromForms 설정된 아이템(`SSFTest_ShiftScroll_BearRefresh`) → 해당 폼에서 사용 허용, 다른 폼에서 차단
- [ ] `15-M05c` targetFormDef 미설정 범용 아이템(`SSFTest_ShiftScroll_UniversalRefresh`) → 어떤 폼에서든 사용 허용
- [ ] `15-M06` 종족 불일치 등 변신 실패 → 메시지 + hediff 자동 제거 (좀비 hediff 방지)
- [ ] `15-M07` Abhorrent 폰에게 수술(Recipe_AdministerIngestible)로 변신 약물 투여 → 변신 성공 (자기 주도가 아니므로 허용)

---

## 16. 성능 & 안정성

### [M] 수동 확인
- [ ] `16-M01` 다수 폰 동시 변신 (5+) — 프레임 저하 없음
- [ ] `16-M02` 장시간 변신 유지 — 메모리 누수 없음
- [ ] `16-M03` 빈번한 변신/해제 반복 — 크래시 없음
- [ ] `16-M04` 맵 전환 시 캐시 정리 (ClearAll) 확인
- [ ] `16-M05` 대형 폼(bodyDrawScale > 1) 줌아웃 렌더링 — 스레드 안전 (ThreadStatic _invokeArgs)
- [ ] `16-M06` 스냅샷 순회 중 Register/Unregister 안전성 확인 (ReleaseSnapshot)
- [ ] `16-M07` spawnApparelOnTransform 장비가 Wear 실패(슬롯 충돌 등) 시에도 해제 시 정상 정리 확인 (generatedApparel 선등록)
- [ ] `16-M08` spawnWeaponOnTransform 장비가 AddEquipment 실패 시에도 해제 시 정상 정리 확인 (generatedWeapons 선등록)
- [ ] `16-M09` 앰비언트 VFX 활성 중 변신 해제 → Effecter.Cleanup 예외 없이 정리 확인

---

## 17. ConfigErrors 검증

### [A] Auto-Verify
- [ ] `17-A01` sustainGenes에 존재하지 않는 GeneDef → ConfigError 출력
- [ ] `17-A02` formAllowedRaces에 null ThingDef → ConfigError 출력
- [ ] `17-A03` formDisallowedRaces에 null ThingDef → ConfigError 출력
- [ ] `17-A04` formAllowedMutants에 null MutantDef → ConfigError 출력
- [ ] `17-A05` formDisallowedMutants에 null MutantDef → ConfigError 출력
- [ ] `17-A06` spawnApparelOnTransform에 비의류 ThingDef → ConfigError 출력
- [ ] `17-A07` spawnWeaponOnTransform에 비무기 ThingDef → ConfigError 출력
- [ ] `17-A08` sustainMode 명시 + sustain* 리스트 4개 모두 비어있음 → ConfigError 경고 (모드 효과 없음 안내)
- [ ] `17-A09` HediffCompProperties_Harvestable.resourceDef = null → ConfigError 경고
- [ ] `17-A10` HediffCompProperties_Harvestable.intervalTicks ≤ 0 → ConfigError 경고
- [ ] `17-A11` HediffCompProperties_Harvestable.resourceAmount ≤ 0 → ConfigError 경고

---

## 18. 장비 잠금 & 드랍 안전성

### [M] 수동 확인
- [ ] `18-M01` 변신 중 무기 장착 시도 → 거부 메시지 + 장착 차단
- [ ] `18-M02` 변신 중 의류 착용 시도 → 거부 메시지 + 착용 차단
- [ ] `18-M03` suppressEquipLock=true 상태에서 내부 장비 복구 → 정상 장착/착용 허용
- [ ] `18-M04` 변신 해제 후 장비/의류 자동 복구 → suppressEquipLock 경유 정상 동작
- [ ] `18-M05` 폼 전용 생성 무기 드랍 시도 → 거부 메시지 (살아있는 플레이어 폰)
- [ ] `18-M06` 폼 전용 생성 무기 시스템 드랍 → "holdingOwner still set" 에러 없이 소멸
- [ ] `18-M07` 변신 중 비활성 작업(disabledWorkTypes) → 변신 해제 후 원래 작업 목록 복원 (캐시 오염 없음)

---

## 19. 동물 텍스처 교체 (PawnRenderNode_AnimalPart)

### [M] 수동 확인
- [ ] `19-M01` 인간→양 변신: body 텍스처 교체 확인
- [ ] `19-M02` 동물(고양이 등)→양 변신 (AoE): body 텍스처 교체 확인
- [ ] `19-M03` 동물 변신 해제 시 원래 텍스처 복원

---

## 20. 상처 라벨 (폼 이름 자동 표시)

### [M] 수동 확인
- [ ] `20-M01` 변신 중 근접 공격 → 상처 라벨에 폼 이름 표시 (예: "bear form teeth")
- [ ] `20-M02` 변신 중 장비 공격 → 상처 라벨에 장비 이름 유지 (폼 이름 아님)
- [ ] `20-M03` 비변신 상태 근접 공격 → 바닐라 라벨 유지

---

## 21. 영구 전환 (HediffComp_PermanentTransform)

### [M] 수동 확인
- [ ] `21-M01` severity 1.0 도달 → 폰 제거 + 지정 동물 스폰
- [ ] `21-M02` 동물 전환: 이름/관계 이전 확인
- [ ] `21-M03` Thing 전환 (thingDef 지정): 해당 Thing 스폰, 이름/관계 없음
- [ ] `21-M04` 전환 시 레터 발송 확인
- [ ] `21-M05` 콜로니스트 → 길들여진 동물, 비콜로니스트 → 야생 동물
- [ ] `21-M06` 변신 중 severity 도달 → RemoveForm 먼저 실행 후 전환

---

## 22. 자원 수확 (HediffComp_Harvestable)

### [M] 수동 확인
- [ ] `22-M01` hediff 부여 → fullness 성장 시작 (건강 탭에 % 표시)
- [ ] `22-M02` fullness 100% → WorkGiver가 수확 Job 할당 (autoSpawn=false)
- [ ] `22-M03` autoSpawn=true → fullness 100% 시 바닥에 자동 스폰
- [ ] `22-M04` requiredGender=Female → 수컷 폰에서 비활성
- [ ] `22-M05` hediff 제거 → fullness 리셋, 수확 불가
- [ ] `22-M06` 수확 시 AnimalGatherYield 스탯 반영 (낮으면 "Product wasted")
- [ ] `22-M07` 세이브/로드 후 fullness 보존

---

## 23. 카테고리 필터 (allowHumanlike/allowAnimals/allowMechanoids/allowMutants)

### [M] 수동 확인
- [ ] `23-M01` 기본값 (Humanlike+Animal 허용): 인간 폰 변신 성공
- [ ] `23-M02` 기본값: 동물 폰 AoE 변신 성공
- [ ] `23-M03` 기본값: 메카노이드 투사체 변신 차단 확인
- [ ] `23-M04` allowMechanoids=true: 메카노이드 투사체 변신 성공
- [ ] `23-M05` allowAnimals=false: 동물 AoE 변신 차단, 인간만 변신
- [ ] `23-M06` allowHumanlike=false + allowAnimals=true: 동물만 변신 가능, 인간 차단
- [ ] `23-M07` 투사체 사전 차단: CanTransformBasic 실패 시 hediff 부여 안 됨
- [ ] `23-M08` 어빌리티 대상 사전 차단: CanTransformBasic 실패 시 시전 불가

---

## 24. 변신 금지 조건 (forbiddenHediffs / forbiddenMentalStates)

### [M] 수동 확인
- [ ] `24-M01` forbiddenHediffs에 특정 질병 지정 → 해당 질병 보유 시 변신 차단
- [ ] `24-M02` forbiddenMentalStates에 Berserk 지정 → 버서크 중 변신 차단
- [ ] `24-M03` forbiddenHediffs 미설정(null) → 제한 없이 변신 가능

---

## 25. AutoShift AND/OR 로직 (requireAllConditions)

### [M] 수동 확인
- [ ] `25-M01` requireAllConditions=false (기본) → 조건 하나만 충족해도 변신 발동 (OR)
- [ ] `25-M02` requireAllConditions=true → 모든 조건 동시 충족 시에만 변신 발동 (AND)
- [ ] `25-M03` requireAllConditions=true, 조건 일부만 충족 → 변신 미발동

---

## 26. sustain 해제 상세 메시지

### [M] 수동 확인
- [ ] `26-M01` sustain 의류 해제 → 메시지에 "sustainApparels" 구체적 원인 표시
- [ ] `26-M02` sustain 무기 해제 → 메시지에 "sustainWeapons" 표시
- [ ] `26-M03` Any 모드 전체 미충족 → "all sustain categories" 표시
