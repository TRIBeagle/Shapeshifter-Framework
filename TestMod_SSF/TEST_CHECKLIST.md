# Shapeshifter Framework — 전체 기능 테스트 체크리스트

> TestMod_SSF 기준, 폼별 전체 기능 검증용

---

## 1. SSFTest_BearForm (곰 폼) — 동물형, abilityMode=None

### 트리거
- [ ] #001 `SSFTest_ShiftScroll_Self` 사용 → 곰 폼 변신
- [ ] #002 `SSFTest_ShiftScroll_Target`으로 다른 폰 지정 → 대상 곰 변신
- [ ] #003 `SSFTest_BearElixir` 복용 → 85% 확률 곰 변신
- [ ] #004 어빌리티 바에 변신 버튼 없음 (abilityMode=None)

### 체형/텍스처
- [ ] #005 곰 텍스처(`Things/Pawn/Animal/Bear/Bear`)로 교체됨
- [ ] #006 `bodyDrawScale` 2.5배 적용 (폰이 확실히 커짐)
- [ ] #007 수영 텍스처(`SwimmingBear`) + 수영 색상 `(255,255,255)` 적용
- [ ] #008 기본 색상 `(112,82,65)` 적용

### 스탯/능력치
- [ ] #009 이동속도 +1.5 (정보탭 확인)
- [ ] #010 근접명중 x1.20, 근접회피 x1.15
- [ ] #011 capMod: Manipulation setMax 0.2 (거의 못 씀)
- [ ] #012 capMod: Talking postFactor 0.1 (거의 못 말함)
- [ ] #013 capMod: Moving postFactor 1.30
- [ ] #014 capMod: Sight postFactor 1.10

### Hediff
- [ ] #015 `FibrousMechanites` 심각도 0.5로 부여됨
- [ ] #016 `SSFTest_BeastArm` 양 팔(Arm) 부위에 ForceAdd됨
- [ ] #017 건강 탭에서 hediff 확인 가능

### 어빌리티 부여
- [ ] #018 Royalty 활성 시 `Berserk` 어빌리티 추가됨
- [ ] #019 해제 시 Berserk 어빌리티 제거됨

### 장비 처리
- [ ] #020 의류 전부 Drop (바닥에 떨어짐)
- [ ] #021 무기 전부 Drop

### 근접 도구
- [ ] #022 네이티브 도구 교체됨 (`replaceNativeTools=true`)
- [ ] #023 `teeth` (Bite, power 12, cooldown 1.5) 만 사용
- [ ] #024 징집 후 근접 공격 시 물기 공격 확인

### 혈액/살점
- [ ] #025 피격 시 `Filth_BloodInsect` 혈흔
- [ ] #026 크롤링 시 `SSFTest_Filth_BloodInsectSmear` 혈흔
- [ ] #027 fleshType이 Insectoid로 변경 (독 저항 등 확인)

### 변신 이펙트
- [ ] #028 진입 시 `PsycastSkipFlashEntry` Fleck 1개, 스케일 1.8
- [ ] #029 진입 시 `PsychicPulseGlobal` 사운드
- [ ] #030 해제 시 `PsycastSkipFlashExit` Fleck + `PsychicSootheGlobal` 사운드
- [ ] #031 FX 쿨다운 30틱 (연속 변신 시 이펙트 스킵 확인)

### 지속/해제
- [ ] #032 15000틱 후 자동 해제
- [ ] #033 Revert 기즈모로 수동 해제 가능 (`canRevertVoluntarily=true`)
- [ ] #034 해제 후 hediff 전부 제거
- [ ] #035 해제 후 텍스처/체형 원상복귀
- [ ] #036 해제 후 스탯/capMod 원상복귀

### 폼 체이닝 (스크롤/약물 경유로 테스트)
- [ ] #037 인간 상태에서 스크롤/약물 사용 시 진입 가능 (`allowedFromForms`에 `None`)
- [ ] #038 `SSFTest_BeastkinForm` 상태에서 스크롤/약물 사용 시에도 진입 가능
- [ ] #039 (참고: abilityMode=None이라 어빌리티 바 버튼은 없음. 스크롤/약물로만 테스트)

---

## 2. SSFTest_BearWarriorForm (곰 전사) — 아머드형, Custom 어빌리티

### 트리거
- [ ] #040 `SSFTest_Ability_BuffAlly`로 아군 타겟 → 대상 곰 전사 변신
- [ ] #041 자기 자신 타겟도 가능 (`canTargetSelf=true`)
- [ ] #042 쿨다운 1500틱 적용

### 스탯
- [ ] #043 이동속도 +0.5
- [ ] #044 근접회피 +10
- [ ] #045 피해배율 x0.8 (데미지 감소)

### 사운드 오버라이드
- [ ] #046 분노 소리: `Pawn_Bear_Angry`
- [ ] #047 근접 히트(폰): `Pawn_Melee_BigBash_HitPawn`
- [ ] #048 근접 히트(건물): `Pawn_Melee_BigBash_HitBuilding`
- [ ] #049 근접 미스: `Pawn_Melee_BigBash_Miss`

### 이펙트
- [ ] #050 진입 이펙터: `Vaporize_Heatwave`
- [ ] #051 해제 이펙터: `ImpactSmallDustCloud`

### 기즈모 아이콘
- [ ] #052 진입 아이콘: `SSF_Shift_Entertest` (커스텀 테스트 아이콘)
- [ ] #053 해제 아이콘: `SSF_Shift_Reverttest`

### 장비
- [ ] #054 아머드형 기본 → 장비 유지 (Keep)

### 지속/해제
- [ ] #055 15000틱 후 자동 해제
- [ ] #056 수동 해제 가능

---

## 3. SSFTest_SheepForm (양 폼) — 동물형, 디버프/적대

### 트리거
- [ ] #057 `SSFTest_Ability_DebuffEnemy` (hostile=true)로 적 타겟 → 75% 확률 양 변신
- [ ] #058 `SSFTest_Ability_MassPolymorph`로 범위 5칸 → 60% 확률 일괄 변신
- [ ] #059 비폭력 폰은 DebuffEnemy/MassPolymorph 사용 불가

### 체형/텍스처
- [ ] #060 수컷: `SheepMale` 텍스처
- [ ] #061 암컷: `SheepFemale` 텍스처 (성별 분기!)
- [ ] #062 `bodyDrawScale` 0.6 (작아짐)

### 스탯/제한
- [ ] #063 이동속도 -1.0
- [ ] #064 `disabledWorkTags: Violent` → 폭력 행위 불가

### 해제
- [ ] #065 `canRevertVoluntarily=false` → Revert 기즈모 없음!
- [ ] #066 10000틱 경과 후에만 자동 해제

---

## 4. SSFTest_DarkKnightForm (암흑 기사) — 장비 소환형

### 트리거
- [ ] #067 `SSFTest_Ability_DarkKnight` 자기변신 (range=0)
- [ ] #068 쿨다운 3000틱

### 장비 소환
- [ ] #069 기존 의류 → 인벤토리로 이동 (`apparelOnTransform=Inventory`)
- [ ] #070 기존 무기 → 인벤토리로 이동 (`weaponsOnTransform=Inventory`)
- [ ] #071 플라스틸 판금갑옷 (`Apparel_PlateArmor`) 소환 착용
- [ ] #072 플라스틸 장검 (`MeleeWeapon_LongSword`) 소환 장비
- [ ] #073 소환 장비 재질이 Plasteel인지 확인

### 장비 잠금
- [ ] #074 `apparelEquipLock=Locked` → 변신 중 의류 탈착 불가
- [ ] #075 `weaponEquipLock=Locked` → 변신 중 무기 교체 불가
- [ ] #076 장비 교체 시도 시 차단 메시지

### 스탯
- [ ] #077 이동속도 -0.3
- [ ] #078 근접회피 +15
- [ ] #079 피해배율 x0.6
- [ ] #080 근접쿨다운배율 x0.85

### 해제
- [ ] #081 해제 시 소환 장비 자동 파괴 (소멸)
- [ ] #082 인벤토리의 기존 장비 재장착
- [ ] #083 18000틱 자동 해제

---

## 5a. SSFTest_BeastkinForm (수인 폼) — 휴머노이드형, 3단 변신 1단계

### 트리거
- [ ] #084 `SSFTest_Ability_Beastkin` 자기변신
- [ ] #085 쿨다운 1200틱

### 렌더 노드 (귀/꼬리)
- [ ] #086 머리에 `FloppyEars` 그래픽 표시
- [ ] #087 귀 색상이 스킨 컬러 (`colorType=Skin`)
- [ ] #088 몸에 `FurryTail` 그래픽 표시
- [ ] #089 꼬리 색상이 헤어 컬러 (`colorType=Hair`)
- [ ] #090 꼬리 방향별 오프셋: 북(뒤), 남(앞), 동/서(옆) 위치 정확
- [ ] #091 꼬리 `scaleOffsetByBodySize=true` 적용

### 그래픽 옵션
- [ ] #092 `bodyDrawScale` 1.20, `headDrawScale` 1.05
- [ ] #093 `headOffset` (0.0, 0.03) 적용
- [ ] #094 오버헤드 의류 숨김이 기본이지만 `Cape`, `Tuque`는 표시

### 보이스 오버라이드
- [ ] #095 호출: `Pawn_Furskin_Call`
- [ ] #096 사망: `Pawn_Furskin_Death`
- [ ] #097 부상: `Pawn_Furskin_Wounded`
- [ ] #098 분노: `Pawn_Bear_Angry`
- [ ] #099 식사: `PredatorLarge_Eat`
- [ ] #100 근접 히트/미스: BigBash 시리즈

### Verb (5종 원거리 무기)
- [ ] #101 돌격소총 (Verb_Shoot): 3점사, 사거리 28, 아이콘 확인
- [ ] #102 파편수류탄 (Verb_LaunchProjectile): 사거리 13, 지면 타겟 가능
- [ ] #103 화염방사 (Verb_ArcSprayProjectile): 사거리 16
- [ ] #104 화염 분사 (Verb_SpewFire): 수동캐스트 전용, 사거리 10
- [ ] #105 광선 빔 (Verb_ShootBeam): 빔 이펙트, 발화, 사거리 24.9

### Verb 기즈모 옵션
- [ ] #106 돌격소총 자동공격 기본 ON (`autoAttackDefault=true`)
- [ ] #107 나머지 4종 자동공격 기본 OFF
- [ ] #108 각 verb별 커스텀 아이콘 표시
- [ ] #109 각 verb별 한글 라벨 표시
- [ ] #110 토글 ON/OFF 시 자동공격 동작 변경

### 근접 도구
- [ ] #111 `claws` (Scratch, power 10) 추가됨
- [ ] #112 `replaceNativeTools=false` → 기존 도구도 유지
- [ ] #113 `replaceNativeVerbs=false` → 기존 verb도 유지

### 장비/Hediff
- [ ] #114 의류 → 인벤토리
- [ ] #115 무기 → 유지 (Keep)
- [ ] #116 `FibrousMechanites` hediff 부여
- [ ] #117 Royalty 시 `Waterskip` 어빌리티 추가

### 작업 제한
- [ ] #118 Crafting 작업 불가
- [ ] #119 Cooking 작업 불가
- [ ] #120 작업 탭에서 해당 항목 체크 해제 & 잠금

### 혈액/이펙트
- [ ] #121 Insectoid 피/혈흔
- [ ] #122 진입/해제 Fleck + 사운드

---

## 5b. SSFTest_FullBeastForm (야수 폼) — 3단 변신 2단계, abilityMode=Auto

### 트리거/폼 체이닝
- [ ] #123 인간 상태에서 진입 **불가** (`allowedFromForms`에 `None` 없음)
- [ ] #124 수인(BeastkinForm) 상태에서만 진입 **가능**
- [ ] #125 Auto 생성 어빌리티 `SSF_AutoAbility_SSFTest_FullBeastForm` 존재 확인
- [ ] #126 로그에 `[SSF] Generated 1 additional ability def(s)` 출력

### 스탯/도구
- [ ] #127 이동속도 +2.0
- [ ] #128 피해배율 x0.7
- [ ] #129 `fangs` (Bite, power 18) + `claws` (Scratch, power 14)
- [ ] #130 `replaceNativeTools=true` → 네이티브 도구 교체

### 해제/체이닝 복귀
- [ ] #131 12000틱 자동 해제
- [ ] #132 야수 폼 해제 후 → 수인 폼으로 복귀? 아니면 인간? (동작 확인)

---

## 6. SSFTest_GuardianForm (수호자) — 조건부 변신

### 변신 조건 (requirementsMode=Any)
- [ ] #133 `SSFTest_MagicStone` 인벤토리 보유 시 → 변신 가능
- [ ] #134 `FibrousMechanites` hediff 보유 시 → 변신 가능
- [ ] #135 둘 다 없으면 → 변신 불가 (어빌리티 비활성 or 실패)
- [ ] #136 둘 다 있어도 OK (Any 모드이므로)

### 비주얼
- [ ] #137 `bodyDrawScale` 1.4 (크게)
- [ ] #138 `portraitDrawScale` 1.3 → 캐릭터 포트레잇(좌측 초상화) 확대
- [ ] #139 `bodyOffset` (0, -0.1) → 약간 아래로

### 그림자 오버라이드
- [ ] #140 `shadowVolume` (0.6, 1.0, 0.6) → 큰 그림자
- [ ] #141 `shadowOffset` (0, 0, -0.05)

### 스탯/해제
- [ ] #142 이동속도 +0.3, 피해배율 x0.85
- [ ] #143 12000틱 자동 해제
- [ ] #144 마력의 돌 드롭 시 → 변신 유지? 즉시 해제? (동작 확인)

---

## 7. SSFTest_PhantomForm (유령) — 비주얼 오버라이드

### 비주얼
- [ ] #145 머리 텍스처 `Male_AverageNormal`로 교체
- [ ] #146 셰이더 `Transparent` → 반투명 렌더링
- [ ] #147 머리 색상 `(0.7, 0.8, 1.0, 0.5)` → 푸른빛 반투명
- [ ] #148 머리카락 숨김 (`hair: Hidden`)
- [ ] #149 `bodyType` → Thin (마른 체형으로 변경)

### FX 지연
- [ ] #150 진입 FX 30틱 딜레이 후 재생 (`transformEnterFxDelayTicks=30`)
- [ ] #151 해제 FX 15틱 딜레이 후 재생 (`transformExitFxDelayTicks=15`)

### 작업 제한
- [ ] #152 소방(Firefighter) 작업 불가 (`disabledWorkTypesOnTransform`)
- [ ] #153 WorkTag이 아닌 WorkTypeDef 직접 차단 정상 작동

### 해제
- [ ] #154 10000틱 자동 해제
- [ ] #155 해제 후 머리/체형/셰이더 원상복귀
- [ ] #156 해제 후 머리카락 다시 표시

---

## 8. SSFTest_RaceLockedForm (종족 제한) — 인간 전용

### 종족 제한
- [ ] #157 인간(Human) 폰 → 변신 성공
- [ ] #158 비인간 종족 폰 → 변신 불가 (차단 메시지)

### 머리타입
- [ ] #159 `headType=Male_AverageNormal`로 고정
- [ ] #160 여성 폰도 Male 머리로 강제 변경되는지 확인

### 장비 잠금
- [ ] #161 `apparelEquipLock=Locked` → 의류 탈착 불가
- [ ] #162 `weaponEquipLock=Unlocked` → 무기 자유 교체 가능

### 스탯/해제
- [ ] #163 이동속도 +0.5, 근접회피 +10, 피해배율 x0.75
- [ ] #164 12000틱 자동 해제

---

## 9. AoE 투사체 (SSFTest_Ability_MassPolymorph)

- [ ] #165 사거리 25칸 투사체 발사
- [ ] #166 warmupTime 2.5초
- [ ] #167 착탄 지점 반경 5칸 내 모든 폰 대상
- [ ] #168 각 폰 60% 확률로 양 변신
- [ ] #169 아군도 영향받는지 확인
- [ ] #170 지면 타겟 가능 (`canTargetLocations=true`)

---

## 10. 아이템/약물 경로

- [ ] #171 `SSFTest_ShiftScroll_Self`: 사용 후 아이템 파괴
- [ ] #172 `SSFTest_ShiftScroll_Self`: stackLimit 5 동작
- [ ] #173 `SSFTest_ShiftScroll_Target`: 타겟 선택 UI 표시
- [ ] #174 `SSFTest_ShiftScroll_Target`: 대상 근처로 이동 후 사용
- [ ] #175 `SSFTest_BearElixir`: DrugLab 제작 가능 (허브약 1개)
- [ ] #176 `SSFTest_BearElixir`: drugCategory=Medical, 복용 120틱
- [ ] #177 `SSFTest_MagicStone`: 인벤토리 소지 아이템 (소비 안 됨)

---

## 11. 공통 / 크로스커팅 테스트

- [ ] #178 세이브/로드 후 변신 상태 유지
- [ ] #179 세이브/로드 후 스탯/hediff/텍스처 정상
- [ ] #180 변신 중 사망 → 시체 텍스처/혈액 정상
- [ ] #181 변신 중 징집/비징집 전환
- [ ] #182 이미 변신 중 같은 폼 재사용 시도 → 차단 or 갱신
- [ ] #183 이미 변신 중 다른 폼 사용 → 기존 폼 해제 → 새 폼 적용
- [ ] #184 카라반 이동 중 변신 타이머 만료
- [ ] #185 빨간 에러 로그 없음 (전 테스트 과정)

---

> **총 185개 항목** | 8개 폼 + AoE + 아이템 + 공통 테스트
