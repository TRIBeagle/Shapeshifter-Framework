# ShapeshifterFramework 전체 기능 테스트 플랜

## 테스트 폼 & 커버리지 매핑

### 기존 폼 (ShiftForms.xml)

| # | 폼 | 트리거 | 주요 테스트 항목 |
|---|-----|--------|-----------------|
| 1 | SSFTest_BearForm | 약물(BearElixir), 아이템(ShiftScroll) | body Replace+수영텍스처, 색상, statOffsets/Factors, capMods, addHediffs(AddedPart+일반), addAbilities, tools+replaceNativeTools, 혈흔/살점/fleshType, Fleck FX+Sound, allowedFromForms, duration, canRevertVoluntarily, apparelOnTransform=Drop, weaponsOnTransform=Drop, abilityMode=None |
| 2 | SSFTest_BearWarriorForm | 어빌리티(BuffAlly, 대상지정) | Armored 베이스, abilityMode=Custom, soundAngry/melee 오버라이드, Effecter FX, gear Keep |
| 3 | SSFTest_SheepForm | 어빌리티(DebuffEnemy, 적대), AoE투사체(MassPolymorph) | body Replace+성별 텍스처, canRevertVoluntarily=false, disabledWorkTags, 축소 스케일, hostile 어빌리티, successChance |
| 4 | SSFTest_DarkKnightForm | 어빌리티(DarkKnight, 자기변신) | spawnApparel/Weapon+stuff, conflictingGearHandling, equipLock(Locked/Locked), Inventory 처리 |
| 5a | SSFTest_BeastkinForm | 어빌리티(Beastkin, 자기변신) | Humanoid 베이스, renderNodeProperties(귀/꼬리), renderShowApparelDefNames, headDrawScale/Offset, 전체 보이스 오버라이드, verbs(5종)+verbGizmoOptions, tools, replaceNativeVerbs/Tools=false, disabledWorkTags(리스트), 혈흔/살점 |
| 5b | SSFTest_FullBeastForm | Auto 자동생성 어빌리티 | allowedFromForms 체인(수인→야수), abilityMode=Auto, 2단변신 |

### 신규 폼 (추가됨)

| # | 폼 | 트리거 | 미테스트 기능 커버 |
|---|-----|--------|-------------------|
| 6 | SSFTest_GuardianForm | 어빌리티(Guardian) | **requiredItems**, **requiredHediffs**, **requirementsMode=Any**, portraitDrawScale, bodyOffset, shadowVolume/Offset |
| 7 | SSFTest_PhantomForm | 어빌리티(Phantom) | **head Replace**, **shaderTypeDefName**(Transparent), **disabledWorkTypesOnTransform**, **bodyType** 오버라이드, hair Hidden 명시, FX delay ticks |
| 8 | SSFTest_RaceLockedForm | 어빌리티(RaceLocked) | **allowedRaces**, **headType** 오버라이드, weaponEquipLock=Unlocked+apparelEquipLock=Locked |

---

## 테스트 체크리스트

### A. 변신 트리거 (5종)
- [ ] **A1** 약물: BearElixir 복용 → BearForm 변신 (85% 확률)
- [ ] **A2** 아이템(자기): ShiftScroll_Self 사용 → BearForm 변신
- [ ] **A3** 아이템(대상): ShiftScroll_Target으로 다른 폰 선택 → BearForm 변신
- [ ] **A4** 어빌리티(대상): BuffAlly로 동료에게 BearWarriorForm 부여
- [ ] **A5** 어빌리티(적대): DebuffEnemy로 적에게 SheepForm 부여 (75% 확률)
- [ ] **A6** 어빌리티(자기): DarkKnight 자기 변신
- [ ] **A7** 어빌리티(자동생성): FullBeastForm의 Auto 어빌리티 확인
- [ ] **A8** AoE 투사체: MassPolymorph 발사 → 반경5칸 내 양 변신 (60% 확률)

### B. 외형 & 렌더링
- [ ] **B1** body Replace: 곰 텍스처로 교체 확인 (BearForm)
- [ ] **B2** body Replace 성별: 양 텍스처 남/여 구분 확인 (SheepForm)
- [ ] **B3** 수영 텍스처: BearForm으로 물에 들어가면 SwimmingBear 텍스처
- [ ] **B4** 수영 색상: swimmingColor 적용 확인
- [ ] **B5** bodyDrawScale: BearForm(1.6배), SheepForm(0.6배), BeastkinForm(1.2배)
- [ ] **B6** headDrawScale + headOffset: BeastkinForm에서 머리 크기/위치 확인
- [ ] **B7** renderNodeProperties: BeastkinForm 귀/꼬리 표시
- [ ] **B8** renderShowApparelDefNames: BeastkinForm에서 망토/투크 표시
- [ ] **B9** renderHideApparelLayers=All: BearForm에서 모든 의류 그래픽 숨김
- [ ] **B10** renderHideWeaponDefNames=All: BearForm에서 무기 그래픽 숨김
- [ ] **B11** head/hair/beard Hidden: Animal 베이스에서 머리/헤어/수염 숨김
- [ ] **B12** portraitDrawScale: GuardianForm 정보창에서 크기 확인 (신규)
- [ ] **B13** bodyOffset: GuardianForm 바디 위치 보정 확인 (신규)
- [ ] **B14** shadowVolume/Offset: GuardianForm 그림자 크기/위치 (신규)
- [ ] **B15** head Replace: PhantomForm 머리 텍스처 교체 (신규)
- [ ] **B16** shaderTypeDefName: PhantomForm 반투명 셰이더 (신규)
- [ ] **B17** bodyType 오버라이드: PhantomForm 체형 교체 (신규)
- [ ] **B18** headType 오버라이드: RaceLockedForm 머리타입 교체 (신규)

### C. 스탯 & 능력치
- [ ] **C1** statOffsets: 이동속도 변화 확인 (정보창에서 수치)
- [ ] **C2** statFactors: 근접명중률/회피율 배수 확인
- [ ] **C3** capMods: 조작/대화/이동/시력 능력치 보정 (BearForm — Manipulation setMax=0.2)
- [ ] **C4** 동적 HediffDef: 정보창 건강 탭에 SSF_FormStats 헤디프 표시

### D. Hediff & Ability 부여
- [ ] **D1** addHediffs 일반: FibrousMechanites 부여 확인 (BearForm)
- [ ] **D2** addHediffs AddedPart: BeastArm 양팔 부착 (ForceAdd 정책)
- [ ] **D3** addHediffs severity: FibrousMechanites severity 0.5 설정 확인
- [ ] **D4** addAbilities: Berserk 어빌리티 부여 (Royalty 필요)
- [ ] **D5** 해제 시 hediff/ability 제거 확인

### E. 장비 처리
- [ ] **E1** apparelOnTransform=Drop: BearForm 변신 시 의류 바닥에 드롭
- [ ] **E2** apparelOnTransform=Inventory: BeastkinForm 변신 시 의류 인벤토리
- [ ] **E3** apparelOnTransform=Keep: BearWarriorForm 변신 시 의류 유지
- [ ] **E4** weaponsOnTransform=Drop/Inventory/Keep: 각각 확인
- [ ] **E5** spawnApparel/Weapon: DarkKnightForm 변신 시 플라스틸 판금갑옷+장검 소환
- [ ] **E6** 해제 시 소환 장비 소멸 + 원래 장비 복원
- [ ] **E7** equipLock: DarkKnightForm 변신 중 장비 교체 불가
- [ ] **E8** conflictingGearHandling: 소환 장비와 겹치는 기존 장비 인벤토리 이동

### F. 전투 (Verb & Tool)
- [ ] **F1** tools + replaceNativeTools=true: BearForm 물기만 가능 (기존 근접 비활성)
- [ ] **F2** tools + replaceNativeTools=false: BeastkinForm 할퀴기 + 기존 근접 유지
- [ ] **F3** verbs (Verb_Shoot): BeastkinForm 돌격소총 발사
- [ ] **F4** verbs (Verb_LaunchProjectile): 수류탄 투척
- [ ] **F5** verbs (Verb_ArcSprayProjectile): 화염방사
- [ ] **F6** verbs (Verb_SpewFire): 화염 분사 (수동캐스트)
- [ ] **F7** verbs (Verb_ShootBeam): 광선 빔
- [ ] **F8** verbGizmoOptions: 각 verb별 아이콘/라벨/자동공격 토글 UI 확인
- [ ] **F9** 해제 시 verb/tool 원복

### G. 사운드
- [ ] **G1** soundCall: BeastkinForm call 소리
- [ ] **G2** soundWounded: 피격 시 폼 사운드
- [ ] **G3** soundDeath: 사망 시 폼 사운드
- [ ] **G4** soundAngry: 분노 시 곰 소리 (BearWarriorForm/BeastkinForm)
- [ ] **G5** soundEating: 식사 시 폼 사운드 (BeastkinForm)
- [ ] **G6** soundMelee (Hit/Miss): 근접 공격 시 폼 사운드

### H. VFX
- [ ] **H1** transformEnterFleck: 변신 시작 Fleck 파티클
- [ ] **H2** transformExitFleck: 변신 해제 Fleck 파티클
- [ ] **H3** transformEnterSound/ExitSound: 변신 시작/해제 사운드
- [ ] **H4** transformEnterEffecter/ExitEffecter: BearWarriorForm Effecter
- [ ] **H5** transformFxCooldownTicks: 연속 변신 시 FX 중복 방지

### I. 혈흔 & 살점
- [ ] **I1** bloodDef: BearForm/BeastkinForm 피격 시 곤충형 혈흔
- [ ] **I2** bloodSmearDef: 크롤링 시 커스텀 혈흔 스미어
- [ ] **I3** fleshType: Insectoid 살점 타입 (상처 텍스처 변경)

### J. 작업 제한
- [ ] **J1** disabledWorkTagsOnTransform: SheepForm Violent 태그 차단 (징집 불가)
- [ ] **J2** disabledWorkTagsOnTransform 복수: BeastkinForm Crafting+Cooking 차단
- [ ] **J3** 작업 탭 툴팁에 차단 사유 표시 확인
- [ ] **J4** disabledWorkTypesOnTransform: 특정 WorkTypeDef 차단 (신규 PhantomForm)
- [ ] **J5** 해제 시 작업 제한 해제 확인

### K. 변신 조건 & 제한
- [ ] **K1** allowedFromForms: FullBeastForm은 BeastkinForm 상태에서만 진입 가능
- [ ] **K2** allowedFromForms에 "None" 포함: BearForm은 비변신 상태에서도 진입 가능
- [ ] **K3** canRevertVoluntarily=false: SheepForm 해제 기즈모 비활성
- [ ] **K4** durationTicks: 시간 경과 후 자동 해제
- [ ] **K5** requiredItems: GuardianForm — 마력의 돌 인벤토리 소지 필요 (신규)
- [ ] **K6** requiredHediffs: GuardianForm — 특정 hediff 필요 (신규)
- [ ] **K7** requirementsMode=Any: 조건 중 하나만 충족하면 변신 가능 (신규)
- [ ] **K8** allowedRaces: RaceLockedForm — Human만 변신 가능 (신규)

### L. 이념 (Ideology DLC)
- [ ] **L1** suppressIdeologyUncoveredThoughts: 동물형 변신 시 알몸 무드 페널티 없음

### M. 세이브/로드
- [ ] **M1** 변신 상태에서 세이브 → 로드 후 폼 유지
- [ ] **M2** hediff/ability/장비 상태 복원
- [ ] **M3** 로드 후 해제 → 원래 상태 복원

### N. 엣지 케이스
- [ ] **N1** 변신 중 사망 → 시체 처리, 사운드, 혈흔
- [ ] **N2** 변신 중 징집 해제/재징집
- [ ] **N3** 2단 변신 중 1단 해제 (수인→야수 상태에서 야수 해제)
- [ ] **N4** 변신 중 캐러밴/월드맵 이동
- [ ] **N5** 이미 변신 중 같은 폼 재사용 시도
- [ ] **N6** 약물+어빌리티 동시에 같은 폼 트리거
- [ ] **N7** 인공 팔이 있는 폰에 BeastArm ForceAdd (기존 인공장기 제거 후 부착)
- [ ] **N8** 결손 팔이 있는 폰에 BeastArm ForceAdd (복원 후 부착)

### O. 디버그 액션
- [ ] **O1** SSF: Dump Form Info — 폼 정보 덤프 확인
- [ ] **O2** SSF: Play Sound — 사운드 재생 확인

---

## Auto 어빌리티 자동생성 검증

abilityMode=Auto인 폼(FullBeastForm)은 프레임워크가 런타임에 AbilityDef를 자동생성함.

```
FormDef (abilityMode=Auto)
  → ResolveReferences() → ShapeshiftAbilityGenerator.TryGenerateFor()
  → "SSF_AutoAbility_{defName}" 생성 → DefDatabase 등록
  → 90틱 주기 SyncFormAbilities()로 eligible 폰에 GainAbility()
```

- [ ] 게임 로드 시 `[SSF] Generated N auto-ability` 로그 확인
- [ ] FullBeastForm: 수인 폼 상태에서 어빌리티 바에 표시
- [ ] 비변신 상태에서 allowedFromForms 미충족 → 어빌리티 미표시
- [ ] 세이브/로드 후 자동생성 AbilityDef 재생성 확인

---

## 테스트 실행 순서

### 준비
1. TestMod_SSF + ShapeshifterFramework 활성화, 개발자 모드 ON
2. 새 게임 (자유지대, 식민자 3명+)
3. 로그에서 `[SSF]` 에러 체크 → `SSF: Dump Form Info`로 9개 폼 로드 확인

### 1단계: 기본 변신 (BearForm) → A1, B1, B5, B9-11, C1-4, D1-5, E1, H1-3
약물 복용 → 곰 텍스처/스탯/hediff/FX → 기즈모 해제 → duration 자동해제

### 2단계: 아이템 트리거 → A2-3
ShiftScroll Self/Target → 변신 + 아이템 소모

### 3단계: 어빌리티 (BearWarrior/Sheep) → A4-5, E3, G4-6, H4, J1, K3
BuffAlly(대상) → BearWarrior / DebuffEnemy(적대) → SheepForm

### 4단계: 장비 소환 (DarkKnight) → A6, E5-8
DarkKnight 변신 → 소환 장비 → equipLock → 해제 시 소멸

### 5단계: 3단 변신 + Auto 어빌리티 → A7, B6-8, E2, K1-2, N3
Beastkin → renderNode/head → FullBeast Auto 어빌리티 → 2단 해제

### 6단계: 전투 verb (BeastkinForm) → F1-9
5종 verb 발사 + verbGizmo + 근접 tool

### 7단계: 보이스 & 혈흔 → G1-3,5, I1-3
피격/사망/식사 사운드 + 혈흔/스미어/fleshType

### 8단계: 작업 제한 → J1-3, J5
Crafting+Cooking 차단, Violent 차단, 해제 후 복원

### 9단계: AoE 변신 → A8
MassPolymorph → 반경 5칸 양 변신 (60%)

### 10단계: 수영 텍스처 → B3-4
BearForm + 물 타일 → SwimmingBear 텍스처

### 11단계: 조건부 변신 (Guardian) → K5-7, B12-14
requiredItems/requiredHediffs (Any 모드) + portrait/offset/shadow

### 12단계: 비주얼 오버라이드 (Phantom) → B15-17, J4, H5
head Replace + Transparent 셰이더 + bodyType + FX delay + WorkType 차단

### 13단계: 종족 제한 (RaceLocked) → K8, B18, E7
allowedRaces=Human + headType + 의류잠금/무기자유

### 14단계: 세이브/로드 → M1-3
다중 변신 상태 세이브 → 로드 → 해제 복원

### 15단계: 엣지 케이스 → N1-8
사망/캐러밴/인공장기/결손/중복시도
