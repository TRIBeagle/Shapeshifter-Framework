# Shapeshifter Framework — 전체 기능 테스트 체크리스트

> TestMod_SSF 기준, 폼별 전체 기능 검증용
> `[AUTO]` = Auto-Verify 디버그 액션으로 자동검증 | `[MANUAL]` = 인게임 직접 확인 필요

---

## 1. SSFTest_BearForm (곰 폼) — 동물형, 스크롤/약물 트리거

### 트리거
- [ ] #001 `[MANUAL]` `SSFTest_ShiftScroll_Self` 사용 → 곰 폼 변신
- [ ] #002 `[MANUAL]` `SSFTest_ShiftScroll_Target`으로 다른 폰 지정 → 대상 곰 변신
- [ ] #003 `[MANUAL]` `SSFTest_BearElixir` 복용 → 85% 확률 곰 변신
- [ ] #004 `[MANUAL]` 어빌리티 바에 곰 변신 버튼 없음 (스크롤/약물로만 트리거)

### 체형/텍스처
- [ ] #005 `[MANUAL]` 곰 텍스처(`Things/Pawn/Animal/Bear/Bear`)로 교체됨
- [ ] #006 `[MANUAL]` `bodyDrawScale` 2.5배 적용 (폰이 확실히 커짐)
- [ ] #007 `[MANUAL]` 수영 텍스처(`SwimmingBear`) + 수영 색상 `(255,255,255)` 적용
- [ ] #008 `[MANUAL]` 기본 색상 `(112,82,65)` 적용

### 스탯/능력치
- [ ] #009 `[AUTO]` 이동속도 +1.5 (정보탭 확인) → `statHediff`
- [ ] #010 `[AUTO]` 근접명중 x1.20, 근접회피 x1.15 → `statHediff`
- [ ] #011 `[AUTO]` capMod: Manipulation setMax 0.2 → `statHediff`
- [ ] #012 `[AUTO]` capMod: Talking postFactor 0.1 → `statHediff`
- [ ] #013 `[AUTO]` capMod: Moving postFactor 1.30 → `statHediff`
- [ ] #014 `[AUTO]` capMod: Sight postFactor 1.10 → `statHediff`

### Hediff
- [ ] #015 `[AUTO]` `FibrousMechanites` 심각도 0.5로 부여됨 → `addHediff`
- [ ] #016 `[AUTO]` `SSFTest_BeastArm` 양 팔(Arm) 부위에 ForceAdd됨 → `addHediff`
- [ ] #017 `[AUTO]` 건강 탭에서 hediff 확인 가능 → `statHediff`

### 어빌리티 부여
- [ ] #018 `[AUTO]` Royalty 활성 시 `Berserk` 어빌리티 추가됨 → `addAbility`
- [ ] #019 `[AUTO]` 해제 시 Berserk 어빌리티 제거됨 → `R.addAbility`

### 장비 처리
- [ ] #020 `[AUTO]` 의류 전부 Drop (바닥에 떨어짐) → `gearApparel`
- [ ] #021 `[AUTO]` 무기 전부 Drop → `gearWeapon`

### 근접 도구
- [ ] #022 `[AUTO]` 네이티브 도구 교체됨 (`replaceNativeTools=true`) → `verbTracker`
- [ ] #023 `[MANUAL]` `teeth` (Bite, power 12, cooldown 1.5) 만 사용
- [ ] #024 `[MANUAL]` 징집 후 근접 공격 시 물기 공격 확인

### 혈액/살점
- [ ] #025 `[AUTO]` 피격 시 `Filth_BloodInsect` 혈흔 → `bloodCache` (캐시 등록 검증)
- [ ] #026 `[MANUAL]` 크롤링 시 `SSFTest_Filth_BloodInsectSmear` 혈흔
- [ ] #027 `[MANUAL]` fleshType이 Insectoid로 변경 (독 저항 등 확인)

### 변신 이펙트
- [ ] #028 `[MANUAL]` 진입 시 `PsycastSkipFlashEntry` Fleck 1개, 스케일 1.8
- [ ] #029 `[MANUAL]` 진입 시 `PsychicPulseGlobal` 사운드
- [ ] #030 `[MANUAL]` 해제 시 `PsycastSkipFlashExit` Fleck + `PsychicSootheGlobal` 사운드
- [ ] #031 `[MANUAL]` FX 쿨다운 30틱 (연속 변신 시 이펙트 스킵 확인)

### 지속/해제
- [ ] #032 `[AUTO]` 15000틱 후 자동 해제 → `timer` (타이머 설정 확인)
- [ ] #033 `[MANUAL]` Revert 기즈모로 수동 해제 가능 (`canRevertVoluntarily=true`)
- [ ] #034 `[AUTO]` 해제 후 hediff 전부 제거 → `R.statHediff`, `R.addHediff`
- [ ] #035 `[AUTO]` 해제 후 텍스처/체형 원상복귀 → `R.bodyType`
- [ ] #036 `[AUTO]` 해제 후 스탯/capMod 원상복귀 → `R.statHediff`

### 폼 체이닝 (스크롤/약물 경유로 테스트)
- [ ] #037 `[MANUAL]` 인간 상태에서 스크롤/약물 사용 시 진입 가능
- [ ] #038 `[MANUAL]` `SSFTest_BeastkinForm` 상태에서 스크롤/약물 사용 시에도 진입 가능 (기존 폼 해제 → 새 폼 적용)
- [ ] #039 `[MANUAL]` (참고: 곰 폼 전용 어빌리티 없음. 스크롤/약물로만 테스트)

---

## 2. SSFTest_BearWarriorForm (곰 전사) — 아머드형

### 트리거
> 획득: `SSFTest_Ability_BuffAlly`는 Dev mode로 직접 부여 (어빌리티 바 → Add ability)
- [ ] #040 `[MANUAL]` `SSFTest_Ability_BuffAlly`로 아군 타겟 → 대상 곰 전사 변신
- [ ] #041 `[MANUAL]` 자기 자신 타겟도 가능 (`canTargetSelf=true`)
- [ ] #042 `[MANUAL]` 쿨다운 1500틱 적용

### 스탯
- [ ] #043 `[AUTO]` 이동속도 +0.5 → `statHediff`
- [ ] #044 `[AUTO]` 근접회피 +10 → `statHediff`
- [ ] #045 `[AUTO]` 피해배율 x0.8 (데미지 감소) → `statHediff`

### 사운드 오버라이드
- [ ] #046 `[AUTO]` 분노 소리: `Pawn_Bear_Angry` → `soundCache` (캐시 등록)
- [ ] #047 `[MANUAL]` 근접 히트(폰): `Shot_Charge_Blaster` (차지 블라스터 발사음 — 근접인데 총소리!)
- [ ] #048 `[MANUAL]` 근접 히트(건물): `Explosion_EMP` (EMP 폭발음)
- [ ] #049 `[MANUAL]` 근접 미스: `Pawn_Mech_Scyther_Call` (사이더 기계음)

### 이펙트
- [ ] #050 `[MANUAL]` 진입 이펙터: `Vaporize_Heatwave`
- [ ] #051 `[MANUAL]` 해제 이펙터: `ImpactSmallDustCloud`

### 기즈모 아이콘
- [ ] #052 `[MANUAL]` 진입 아이콘: `SSF_Shift_Entertest` (커스텀 테스트 아이콘)
- [ ] #053 `[MANUAL]` 해제 아이콘: `SSF_Shift_Reverttest`

### 장비
- [ ] #054 `[MANUAL]` 아머드형 기본 → 장비 유지 (Keep)

### 지속/해제
- [ ] #055 `[AUTO]` 15000틱 후 자동 해제 → `timer`
- [ ] #056 `[MANUAL]` 수동 해제 가능

---

## 3. SSFTest_SheepForm (양 폼) — 동물형, 디버프/적대

### 트리거
> 획득: `SSFTest_Ability_DebuffEnemy`, `SSFTest_Ability_MassPolymorph` 모두 Dev mode로 직접 부여
- [ ] #057 `[MANUAL]` `SSFTest_Ability_DebuffEnemy` (hostile=true)로 적 타겟 → 75% 확률 양 변신
- [ ] #058 `[MANUAL]` `SSFTest_Ability_MassPolymorph`로 범위 5칸 → 60% 확률 일괄 변신
- [ ] #059 `[MANUAL]` 비폭력 폰은 DebuffEnemy/MassPolymorph 사용 불가

### 체형/텍스처
- [ ] #060 `[MANUAL]` 수컷: `SheepMale` 텍스처
- [ ] #061 `[MANUAL]` 암컷: `SheepFemale` 텍스처 (성별 분기!)
- [ ] #062 `[MANUAL]` `bodyDrawScale` 0.6 (작아짐)

### 스탯/제한
- [ ] #063 `[AUTO]` 이동속도 -1.0 → `statHediff`
- [ ] #064 `[AUTO]` `disabledWorkTags: Violent` → 폭력 행위 불가 → `workTags`

### 해제
- [ ] #065 `[MANUAL]` `canRevertVoluntarily=false` → Revert 기즈모 없음!
- [ ] #066 `[AUTO]` 10000틱 경과 후에만 자동 해제 → `timer`

---

## 4. SSFTest_DarkKnightForm (암흑 기사) — 장비 소환형

### 트리거
> 획득: `SSFTest_Ability_DarkKnight`는 Dev mode로 직접 부여
- [ ] #067 `[MANUAL]` `SSFTest_Ability_DarkKnight` 자기변신 (range=0)
- [ ] #068 `[MANUAL]` 쿨다운 3000틱

### 장비 소환
- [ ] #069 `[AUTO]` 기존 의류 → 인벤토리로 이동 → `gearApparel`
- [ ] #070 `[AUTO]` 기존 무기 → 인벤토리로 이동 → `gearWeapon`
- [ ] #071 `[AUTO]` 플라스틸 판금갑옷 (`Apparel_PlateArmor`) 소환 착용 → `spawnApparel`
- [ ] #072 `[AUTO]` 플라스틸 장검 (`MeleeWeapon_LongSword`) 소환 장비 → `spawnWeapon`
- [ ] #073 `[AUTO]` 소환 장비 재질이 Plasteel인지 확인 → `stuffApparel`, `stuffWeapon`

### 장비 잠금
- [ ] #074 `[AUTO]` `apparelEquipLock=Locked` → 변신 중 의류 탈착 불가 → `equipLockApparel`
- [ ] #075 `[AUTO]` `weaponEquipLock=Locked` → 변신 중 무기 교체 불가 → `equipLockWeapon`
- [ ] #076 `[MANUAL]` 장비 교체 시도 시 차단 메시지

### 스탯
- [ ] #077 `[AUTO]` 이동속도 -0.3 → `statHediff`
- [ ] #078 `[AUTO]` 근접회피 +15 → `statHediff`
- [ ] #079 `[AUTO]` 피해배율 x0.6 → `statHediff`
- [ ] #080 `[AUTO]` 근접쿨다운배율 x0.85 → `statHediff`

### 해제
- [ ] #081 `[AUTO]` 해제 시 소환 장비 자동 파괴 (소멸) → `R.spawnApparel`, `R.spawnWeapon`
- [ ] #082 `[AUTO]` 인벤토리의 기존 장비 재장착 → `R.equipLock`
- [ ] #083 `[AUTO]` 18000틱 자동 해제 → `timer`

---

## 5a. SSFTest_BeastkinForm (수인 폼) — 휴머노이드형, 3단 변신 1단계

### 트리거
> 획득: `SSFTest_Ability_Beastkin`은 Dev mode로 직접 부여
- [ ] #084 `[MANUAL]` `SSFTest_Ability_Beastkin` 자기변신
- [ ] #085 `[MANUAL]` 쿨다운 1200틱

### 렌더 노드 (귀/꼬리)
- [ ] #086 `[MANUAL]` 머리에 `FloppyEars` 그래픽 표시
- [ ] #087 `[MANUAL]` 귀 색상이 스킨 컬러 (`colorType=Skin`)
- [ ] #088 `[MANUAL]` 몸에 `FurryTail` 그래픽 표시
- [ ] #089 `[MANUAL]` 꼬리 색상이 헤어 컬러 (`colorType=Hair`)
- [ ] #090 `[MANUAL]` 꼬리 방향별 오프셋: 북(뒤), 남(앞), 동/서(옆) 위치 정확
- [ ] #091 `[MANUAL]` 꼬리 `scaleOffsetByBodySize=true` 적용

### 그래픽 옵션
- [ ] #092 `[MANUAL]` `bodyDrawScale` 1.20, `headDrawScale` 1.05
- [ ] #093 `[MANUAL]` `headOffset` (0.0, 0.03) 적용
- [ ] #094 `[MANUAL]` 오버헤드 의류 숨김이 기본이지만 `Cape`, `Tuque`는 표시

### 보이스 오버라이드
- [ ] #095 `[MANUAL]` 호출: `Pawn_Furskin_Call`
- [ ] #096 `[MANUAL]` 사망: `Pawn_Furskin_Death`
- [ ] #097 `[MANUAL]` 부상: `Pawn_Furskin_Wounded`
- [ ] #098 `[MANUAL]` 분노: `Pawn_Bear_Angry`
- [ ] #099 `[MANUAL]` 식사: `PredatorLarge_Eat`
- [ ] #100 `[MANUAL]` 근접 히트/미스: BigBash 시리즈

### Verb (5종 원거리 무기)
- [ ] #101 `[AUTO]` 돌격소총 (Verb_Shoot): 3점사, 사거리 28 → `verbTracker` (등록 확인)
- [ ] #102 `[AUTO]` 파편수류탄 (Verb_LaunchProjectile): 사거리 13 → `verbTracker`
- [ ] #103 `[AUTO]` 화염방사 (Verb_ArcSprayProjectile): 사거리 16 → `verbTracker`
- [ ] #104 `[AUTO]` 화염 분사 (Verb_SpewFire): 수동캐스트 전용, 사거리 10 → `verbTracker`
- [ ] #105 `[AUTO]` 광선 빔 (Verb_ShootBeam): 빔 이펙트, 사거리 24.9 → `verbTracker`

### Verb 기즈모 옵션
- [ ] #106 `[MANUAL]` 돌격소총 자동공격 기본 ON (`autoAttackDefault=true`)
- [ ] #107 `[MANUAL]` 나머지 4종 자동공격 기본 OFF
- [ ] #108 `[MANUAL]` 각 verb별 커스텀 아이콘 표시
- [ ] #109 `[MANUAL]` 각 verb별 한글 라벨 표시
- [ ] #110 `[MANUAL]` 토글 ON/OFF 시 자동공격 동작 변경
- [ ] #111 `[MANUAL]` 기즈모 순서: 각 verb마다 [공격 커맨드] → [자동공격 토글] 순서로 표시

### 기본 컬러 오버라이드
- [ ] #112 `[AUTO]` 머리카락 색상 `(0.85, 0.85, 0.95)` (은빛)으로 변경됨 → `hairColor`
- [ ] #113 `[AUTO]` 해제 후 원래 머리카락 색상으로 복원 → `R.hairColor`

### 근접 도구
- [ ] #114 `[AUTO]` `claws` (Scratch, power 10) 추가됨 → `verbTracker`
- [ ] #115 `[MANUAL]` `replaceNativeTools=false` → 기존 도구도 유지
- [ ] #116 `[MANUAL]` `replaceNativeVerbs=false` → 기존 verb도 유지

### 장비/Hediff
- [ ] #117 `[AUTO]` 의류 → 인벤토리 → `gearApparel`
- [ ] #118 `[MANUAL]` 무기 → 유지 (Keep)
- [ ] #119 `[AUTO]` `FibrousMechanites` hediff 부여 → `addHediff`
- [ ] #120 `[AUTO]` Royalty 시 `Waterskip` 어빌리티 추가 → `addAbility`

### 작업 제한
- [ ] #121 `[AUTO]` Crafting 작업 불가 → `workTags`
- [ ] #122 `[AUTO]` Cooking 작업 불가 → `workTags`
- [ ] #123 `[MANUAL]` 작업 탭에서 해당 항목 체크 해제 & 잠금

### 혈액/이펙트
- [ ] #124 `[MANUAL]` Insectoid 피/혈흔
- [ ] #125 `[MANUAL]` 진입/해제 Fleck + 사운드

---

## 5b. SSFTest_FullBeastForm (야수 폼) — 3단 변신 2단계, addAbilities 체인

### 트리거/폼 체이닝
> 획득: `SSFTest_Ability_FullBeast`는 BeastkinForm의 `addAbilities`로 자동 부여 (수인 변신 시에만 존재)
- [ ] #126 `[MANUAL]` 인간 상태에서 진입 **불가** (FullBeast 어빌리티 자체가 없음)
- [ ] #127 `[MANUAL]` 수인(BeastkinForm) 상태에서만 진입 **가능**
- [ ] #128 `[MANUAL]` 수인 폼의 `addAbilities`로 부여된 `SSFTest_Ability_FullBeast` 어빌리티 확인
- [ ] #129 `[MANUAL]` 수인 폼 해제 시 `SSFTest_Ability_FullBeast` 어빌리티도 함께 제거

### 스탯/도구
- [ ] #130 `[AUTO]` 이동속도 +2.0 → `statHediff`
- [ ] #131 `[AUTO]` 피해배율 x0.7 → `statHediff`
- [ ] #132 `[AUTO]` `fangs` (Bite, power 18) + `claws` (Scratch, power 14) → `verbTracker`
- [ ] #133 `[MANUAL]` `replaceNativeTools=true` → 네이티브 도구 교체

### 해제/체이닝 복귀
- [ ] #134 `[AUTO]` 12000틱 자동 해제 → `timer`
- [ ] #135 `[MANUAL]` 야수 폼 해제 후 → 수인 폼으로 복귀? 아니면 인간? (동작 확인)

---

## 6. SSFTest_GuardianForm (수호자) — 조건부 변신

### 변신 조건 (sustainMode=Any)
> 획득: `SSFTest_Ability_Guardian`은 `SSFTest_MagicStone` 소지 시 자동 부여 (`CompGiveAbility_SSF`, `requireEquipped=false`)
- [ ] #136 `[MANUAL]` `SSFTest_MagicStone` 인벤토리 보유 시 → 변신 가능
- [ ] #137 `[MANUAL]` `SSFTest_Hediff_GuardianMark` hediff 보유 시 → 변신 가능
- [ ] #138 `[MANUAL]` 둘 다 없으면 → 변신 불가 (어빌리티 비활성 or 실패)
- [ ] #139 `[MANUAL]` 둘 다 있어도 OK (Any 모드이므로)

### 비주얼
- [ ] #140 `[MANUAL]` `bodyDrawScale` 1.4 (크게)
- [ ] #141 `[MANUAL]` `portraitDrawScale` 1.3 → 캐릭터 포트레잇(좌측 초상화) 확대
- [ ] #142 `[MANUAL]` `bodyOffset` (0, -0.1) → 약간 아래로

### 그림자 오버라이드
- [ ] #143 `[MANUAL]` `shadowVolume` (0.6, 1.0, 0.6) → 큰 그림자
- [ ] #144 `[MANUAL]` `shadowOffset` (0, 0, -0.05)

### 스탯/해제
- [ ] #145 `[AUTO]` 이동속도 +0.3, 피해배율 x0.85 → `statHediff`
- [ ] #146 `[AUTO]` 12000틱 자동 해제 → `timer`
- [ ] #147 `[MANUAL]` 마력의 돌 드롭 시 → 변신 유지? 즉시 해제? (동작 확인)

---

## 7. SSFTest_PhantomForm (유령) — 비주얼 오버라이드

> 획득: `SSFTest_Ability_Phantom`은 Dev mode로 직접 부여

### 비주얼
- [ ] #148 `[MANUAL]` 머리 텍스처 `Male_AverageNormal`로 교체
- [ ] #149 `[MANUAL]` 셰이더 `Transparent` → 반투명 렌더링
- [ ] #150 `[MANUAL]` 머리 색상 `(0.7, 0.8, 1.0, 0.5)` → 푸른빛 반투명
- [ ] #151 `[MANUAL]` 머리카락 숨김 (`hair: Hidden`)
- [ ] #152 `[AUTO]` `bodyType` → Thin (마른 체형으로 변경) → `bodyType`

### 기본 스킨컬러 오버라이드
- [ ] #153 `[AUTO]` 피부색 `(0.7, 0.8, 1.0)` (창백한 푸른빛)으로 변경됨 → `skinColor`
- [ ] #154 `[AUTO]` 해제 후 원래 피부색으로 복원 → `R.skinColor`

### FX 지연
- [ ] #155 `[MANUAL]` 진입 FX 30틱 딜레이 후 재생 (`transformEnterFxDelayTicks=30`)
- [ ] #156 `[MANUAL]` 해제 FX 15틱 딜레이 후 재생 (`transformExitFxDelayTicks=15`)

### 작업 제한
- [ ] #157 `[AUTO]` 소방(Firefighter) 작업 불가 → `workTypes`
- [ ] #158 `[MANUAL]` WorkTag이 아닌 WorkTypeDef 직접 차단 정상 작동

### 해제
- [ ] #159 `[AUTO]` 10000틱 자동 해제 → `timer`
- [ ] #160 `[AUTO]` 해제 후 머리/체형/셰이더/피부색 원상복귀 → `R.bodyType`, `R.headType`, `R.skinColor`
- [ ] #161 `[MANUAL]` 해제 후 머리카락 다시 표시

---

## 8. SSFTest_RaceLockedForm (종족 제한) — 인간 전용

> 획득: `SSFTest_Ability_RaceLocked`는 Dev mode로 직접 부여 (`allowedRaces: Human` — 비인간은 기즈모 자체가 숨겨짐)

### 종족 제한
- [ ] #162 `[AUTO]` 인간(Human) 폰 → 변신 성공 → `raceFilter`
- [ ] #163 `[AUTO]` 비인간 종족 폰 → 변신 불가 (차단 메시지) → `raceFilter`

### 머리타입
- [ ] #164 `[AUTO]` `headType=Male_AverageNormal`로 고정 → `headType`
- [ ] #165 `[AUTO]` 여성 폰도 Male 머리로 강제 변경되는지 확인 → `headType`

### 장비 잠금
- [ ] #166 `[AUTO]` `apparelEquipLock=Locked` → 의류 탈착 불가 → `equipLockApparel`
- [ ] #167 `[AUTO]` `weaponEquipLock=Unlocked` → 무기 자유 교체 가능 → `equipLockWeapon`

### 스탯/해제
- [ ] #168 `[AUTO]` 이동속도 +0.5, 근접회피 +10, 피해배율 x0.75 → `statHediff`
- [ ] #169 `[AUTO]` 12000틱 자동 해제 → `timer`

---

## 9. AoE 투사체 (SSFTest_Ability_MassPolymorph)

> 획득: Dev mode로 직접 부여

- [ ] #170 `[MANUAL]` 사거리 25칸 투사체 발사
- [ ] #171 `[MANUAL]` warmupTime 2.5초
- [ ] #172 `[MANUAL]` 착탄 지점 반경 5칸 내 모든 폰 대상
- [ ] #173 `[MANUAL]` 각 폰 60% 확률로 양 변신
- [ ] #174 `[MANUAL]` 아군도 영향받는지 확인
- [ ] #175 `[MANUAL]` 지면 타겟 가능 (`canTargetLocations=true`)

---

## 10. 아이템/약물 경로

- [ ] #176 `[MANUAL]` `SSFTest_ShiftScroll_Self`: 사용 후 아이템 파괴
- [ ] #177 `[MANUAL]` `SSFTest_ShiftScroll_Self`: stackLimit 5 동작
- [ ] #178 `[MANUAL]` `SSFTest_ShiftScroll_Target`: 타겟 선택 UI 표시
- [ ] #179 `[MANUAL]` `SSFTest_ShiftScroll_Target`: 대상 근처로 이동 후 사용
- [ ] #180 `[MANUAL]` `SSFTest_BearElixir`: DrugLab 제작 가능 (허브약 1개)
- [ ] #181 `[MANUAL]` `SSFTest_BearElixir`: drugCategory=Medical, 복용 120틱
- [ ] #182 `[MANUAL]` `SSFTest_MagicStone`: 인벤토리 소지 아이템 (소비 안 됨)

---

## 11. 공통 / 크로스커팅 테스트

- [ ] #183 `[MANUAL]` 세이브/로드 후 변신 상태 유지
- [ ] #184 `[MANUAL]` 세이브/로드 후 스탯/hediff/텍스처/컬러 정상
- [ ] #185 `[MANUAL]` 변신 중 사망 → 시체 텍스처/혈액 정상
- [ ] #186 `[MANUAL]` 변신 중 징집/비징집 전환
- [ ] #187 `[MANUAL]` 이미 변신 중 같은 폼 재사용 시도 → 차단 or 갱신
- [ ] #188 `[MANUAL]` 이미 변신 중 다른 폼 사용 → 기존 폼 해제 → 새 폼 적용
- [ ] #189 `[MANUAL]` 카라반 이동 중 변신 타이머 만료
- [ ] #190 `[MANUAL]` 빨간 에러 로그 없음 (전 테스트 과정)

---

## 12. 디버그 액션 & 내부 로깅 검증

- [ ] #191 `[MANUAL]` `SSF: Inspect Active Form` → 스탯/캐퍼 요약이 mainHediff 기준으로 정상 표시
- [ ] #192 `[MANUAL]` `SSF: Dump Pawn State to Log` → Stat Offsets/Factors/Capacity Mods 섹션에 mainHediff.stages[0] 데이터 출력
- [ ] #193 `[MANUAL]` AddedPart(ForceAdd) 적용 실패 시 `[SSF] RestorePart failed` 경고 로그 출력 (의도적 실패 유도)
- [ ] #194 `[MANUAL]` Hediff severity 설정 실패 시 `[SSF] Set severity failed` 경고 로그 출력
- [ ] #195 `[MANUAL]` RemoveHediff 실패 시 `[SSF] RemoveHediff failed` 경고 로그 출력
- [ ] #196 `[MANUAL]` HAR 모드 활성 시 헤드 애드온 판정에서 `alignWithHead` boolean 검사 정상 작동
- [ ] #197 `[MANUAL]` CompGiveAbility_SSF의 아이템 디스폰 시 어빌리티 정상 회수 (PostDeSpawn 1.6 시그니처)
- [ ] #198 `[MANUAL]` 장시간 플레이(1일+) 후 로그 딕셔너리 메모리 누적 없음 (Dev 모드 프로파일러)

---

## 13. 어빌리티 부여/차단 조건 검증

### 캐스트 조건 (CompProperties_AbilityShiftTarget)
- [ ] #199 `[MANUAL]` `SSFTest_Ability_RaceLocked`: Human 폰 → 기즈모 표시, 캐스트 성공 (`allowedRaces: Human`)
- [ ] #200 `[MANUAL]` `SSFTest_Ability_RaceLocked`: 비인간 종족 폰 → 기즈모 숨김 (`ShouldHideGizmo=true`)
- [ ] #201 `[MANUAL]` `SSFTest_Ability_DebuffEnemy`: `successChance=0.75` → 반복 시전 시 약 25% 실패 확인
- [ ] #202 `[MANUAL]` 이미 같은 폼 변신 중인 폰에게 재시전 → 기즈모 숨김 + `CanApplyOn` 차단

### 아이템 기반 어빌리티 부여 (CompGiveAbility_SSF)
- [ ] #203 `[MANUAL]` `SSFTest_MagicStone` 인벤토리 소지 → `SSFTest_Ability_Guardian` 어빌리티 바에 표시 (`requireEquipped=false`)
- [ ] #204 `[MANUAL]` `SSFTest_MagicStone` 바닥에 드롭 → Guardian 어빌리티 즉시 제거
- [ ] #205 `[MANUAL]` `SSFTest_MagicStone` 재소지 → 어빌리티 재부여
- [ ] #206 `[MANUAL]` 아이템 소유자 변경(다른 폰에게 전달) → 기존 소유자 어빌리티 제거, 새 소유자에게 부여

### 폼 addAbilities (변신 중 부여)
- [ ] #207 `[MANUAL]` BearForm 변신 → Royalty 활성 시 `Berserk` 어빌리티 부여 (`MayRequire` 조건부)
- [ ] #208 `[MANUAL]` BearForm 변신 → Royalty 비활성 시 `Berserk` 어빌리티 미부여 (에러 없음)
- [ ] #209 `[MANUAL]` BeastkinForm 변신 → `SSFTest_Ability_FullBeast` 어빌리티 바에 즉시 표시
- [ ] #210 `[MANUAL]` BeastkinForm 변신 → Royalty 활성 시 `Waterskip`도 함께 부여 (`MayRequire` 조건부)
- [ ] #211 `[MANUAL]` BeastkinForm 해제 → `SSFTest_Ability_FullBeast` + `Waterskip` 모두 제거

### addAbilities 체인 (2단 변신 경로)
- [ ] #212 `[MANUAL]` 인간 상태 → `SSFTest_Ability_FullBeast` 어빌리티 바에 없음 (폼 부여가 아니므로)
- [ ] #213 `[MANUAL]` BeastkinForm 진입 → `SSFTest_Ability_FullBeast` 표시 → 사용 시 FullBeastForm 진입 성공
- [ ] #214 `[MANUAL]` FullBeastForm 해제 → BeastkinForm 상태라면 다시 FullBeast 어빌리티 존재 여부 확인
- [ ] #215 `[MANUAL]` BeastkinForm 해제 → FullBeast 어빌리티 소멸 → 인간 상태에서 FullBeast 사용 불가

### 폼 유지 조건 (sustainHediffs/sustainMode)
- [ ] #216 `[MANUAL]` GuardianForm: `sustainHediffs=SSFTest_Hediff_GuardianMark`, `sustainMode=Any` → hediff 유지 시 변신 유지
- [ ] #217 `[MANUAL]` GuardianForm: `sustainHediffs` 충족 + `SSFTest_MagicStone` 소지 → 둘 다 만족 OK (Any 모드)
- [ ] #218 `[MANUAL]` GuardianForm: 변신 중 `SSFTest_Hediff_GuardianMark` 치료(제거) 시 → 유지 조건 변화 확인

---

## 14. 어빌리티 획득 수단 다양화 테스트 (ShiftSources)

> AbilityDef 체제 전환 후 각 경로별 어빌리티 부여/회수가 정상 동작하는지 검증

### 유전자 (GeneDef) — Biotech DLC
- [ ] #219 `[MANUAL]` `SSFTest_Gene_BeastkinShift` 유전자 보유 폰 → `SSFTest_Ability_Beastkin` 어빌리티 바에 표시
- [ ] #220 `[MANUAL]` 유전자 보유 폰 → 수인 변신 정상 작동 (어빌리티 → 폼 진입 → 효과 적용)
- [ ] #221 `[MANUAL]` 유전자 제거(Dev mode) → 수인 변신 어빌리티 즉시 제거
- [ ] #222 `[MANUAL]` `SSFTest_Gene_PhantomShift` 유전자 보유 폰 → `SSFTest_Ability_Phantom` 어빌리티 바에 표시
- [ ] #223 `[MANUAL]` 유전자 보유 폰 → 유령 변신 정상 작동
- [ ] #224 `[MANUAL]` 두 유전자 동시 보유 → 수인 + 유령 어빌리티 모두 표시
- [ ] #225 `[MANUAL]` 유전자 어빌리티로 변신 중 → 유전자 UI에서 억제 유전자 디밍 표시 정상
- [ ] #226 `[MANUAL]` Biotech DLC 미설치 시 유전자 정의 무시 (에러 없음, `MayRequire` 작동)

### 장착 무기 (CompGiveAbility_SSF, requireEquipped=true)
- [ ] #227 `[MANUAL]` `SSFTest_Weapon_DarkBlade` 장비 슬롯 장착 → `SSFTest_Ability_DarkKnight` 어빌리티 바에 표시
- [ ] #228 `[MANUAL]` 무기 장착 상태 → 암흑 기사 변신 정상 작동
- [ ] #229 `[MANUAL]` 무기 해제(다른 무기 장착 or 드롭) → 암흑 기사 어빌리티 즉시 제거
- [ ] #230 `[MANUAL]` 변신 중 무기 해제 시도 → 변신 해제 후 어빌리티 회수 확인
- [ ] #231 `[MANUAL]` 인벤토리에만 보유(장비 슬롯 아님) → 어빌리티 미부여 (`requireEquipped=true`)
- [ ] #232 `[MANUAL]` 무기를 다른 폰에게 전달 → 기존 소유자 어빌리티 제거, 새 장착자에게 부여

### 장착 의류 (CompGiveAbility_SSF, requireEquipped=true)
- [ ] #233 `[MANUAL]` `SSFTest_Apparel_PhantomCloak` 착용 → `SSFTest_Ability_Phantom` 어빌리티 바에 표시
- [ ] #234 `[MANUAL]` 망토 착용 상태 → 유령 변신 정상 작동
- [ ] #235 `[MANUAL]` 망토 탈의 → 유령 어빌리티 즉시 제거
- [ ] #236 `[MANUAL]` 인벤토리에만 보유(착용 아님) → 어빌리티 미부여 (`requireEquipped=true`)
- [ ] #237 `[MANUAL]` 유령 변신 중 망토 상태 확인 (apparelOnTransform 동작과 겹침 확인)

### Hediff 기반 어빌리티 (HediffComp_GiveAbility — 바닐라 패턴)
- [ ] #238 `[MANUAL]` `SSFTest_Hediff_ShiftBlessing` 부여(Dev mode) → `SSFTest_Ability_BuffAlly` 어빌리티 바에 표시
- [ ] #239 `[MANUAL]` 축복 헤디프 보유 → 곰 전사 버프 어빌리티 정상 작동 (아군 대상 변신)
- [ ] #240 `[MANUAL]` 축복 헤디프 제거 → 곰 전사 어빌리티 즉시 제거
- [ ] #241 `[MANUAL]` `SSFTest_Hediff_RacialAwakening` 부여(Dev mode) → `SSFTest_Ability_RaceLocked` 어빌리티 바에 표시
- [ ] #242 `[MANUAL]` 종족 각성 + 인간 폰 → 종족 제한 변신 성공
- [ ] #243 `[MANUAL]` 종족 각성 + 비인간 폰 → 종족 제한 변신 차단 (allowedRaces 필터 정상)
- [ ] #244 `[MANUAL]` 종족 각성 헤디프 제거 → 종족 제한 어빌리티 제거

### 크로스 경로 검증 (복합 테스트)
- [ ] #245 `[MANUAL]` 같은 어빌리티를 여러 경로로 동시 부여 시 중복 확인 (유전자 + 아이템 동시 → 어빌리티 1개만)
- [ ] #246 `[MANUAL]` 한 경로 제거 → 다른 경로가 살아있으면 어빌리티 유지 확인
- [ ] #247 `[MANUAL]` 유전자 어빌리티로 변신 → 세이브/로드 후 유전자 경유 어빌리티 & 변신 상태 유지
- [ ] #248 `[MANUAL]` 장착 아이템 어빌리티로 변신 → 세이브/로드 후 장착 상태 & 어빌리티 유지
- [ ] #249 `[MANUAL]` Hediff 어빌리티로 변신 → 세이브/로드 후 헤디프 & 어빌리티 유지
- [ ] #250 `[MANUAL]` 모든 경로(유전자/장착무기/장착의류/헤디프/소지아이템/약물/스크롤) 빨간 에러 없이 작동

---

## 요약

| 구분 | 항목 수 | 비율 |
|------|---------|------|
| **[AUTO] 자동검증** | **70개** | **27%** |
| **[MANUAL] 수동검증** | **191개** | **73%** |

### Auto-Verify 체크 카테고리 → 체크리스트 매핑

| 카테고리 | 설명 | 관련 항목 |
|----------|------|----------|
| `statHediff` | 스탯 HediffDef 생성/제거 | #009~#017, #034, #036, #043~#045, #063, #077~#080, #130~#131, #145, #168 |
| `bodyType` | 체형 변경/원복 | #152, #160 |
| `headType` | 머리형 변경/원복 | #160, #164~#165 |
| `hairColor` | 머리색 변경/원복 | #112~#113 |
| `skinColor` | 피부색 변경/원복 | #153~#154 |
| `addHediff` | 추가 hediff 부여/제거 | #015~#016, #034, #119 |
| `addAbility` | 추가 어빌리티 부여/제거 | #018~#019, #120 |
| `spawnApparel` | 소환 의류 착용 | #071 |
| `spawnWeapon` | 소환 무기 장비 | #072 |
| `stuffApparel/Weapon` | 소환 장비 재질 | #073 |
| `verbTracker` | Verb/Tool 등록 | #022, #101~#105, #114, #132 |
| `soundCache` | 사운드 런타임 캐시 | #025, #046 |
| `bloodCache` | 혈흔 런타임 캐시 | #025 |
| `timer` | 지속시간 타이머 | #032, #055, #066, #083, #134, #146, #159, #169 |
| `gearApparel` | 의류 Drop/Inventory 처리 | #020, #069, #117 |
| `gearWeapon` | 무기 Drop/Inventory 처리 | #021, #070 |
| `equipLockApparel` | 의류 착용 잠금 | #074, #166 |
| `equipLockWeapon` | 무기 착용 잠금 | #075, #167 |
| `workTags` | WorkTag 기반 작업 차단 | #064, #121~#122 |
| `workTypes` | WorkTypeDef 직접 차단 | #157 |
| `raceFilter` | 종족 제한 필터 | #162~#163 |
| `R.spawnApparel/Weapon` | 해제 시 소환 장비 파괴 | #081 |
| `R.equipLock` | 해제 시 장비 잠금 해제 | #082 |

> **총 261개 항목** | 8개 폼 + AoE + 아이템 + 공통 + 디버그/로깅 + 어빌리티 조건 + 획득 수단 다양화 + 제노타입 제한 테스트
> Auto-Verify 로그에서 각 ✓/✗ 줄 끝에 `[#nnn]` 형태로 체크리스트 번호가 표시됩니다.
