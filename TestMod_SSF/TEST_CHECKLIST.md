# Shapeshifter Framework — 테스트 체크리스트

> TestMod_SSF 기준 | `[A]` = Auto-Verify 자동검증 | `[M]` = 인게임 수동확인

---

## 1. SSFTest_BearForm (곰) — 동물형, 스크롤/약물

### 트리거
- [ ] `[M]` ShiftScroll_Self / ShiftScroll_Target / BearElixir(85%) 각각 변신 성공

### 비주얼
- [ ] `[M]` 곰 텍스처 + bodyDrawScale 2.5배 + 기본색 (112,82,65)
- [ ] `[M]` 수영 시 SwimmingBear 텍스처 + 수영색 (255,255,255)

### Auto-Verify (13항목)
> statHediff(이동+1.5, 근접명중x1.20, 근접회피x1.15, capMods 4종), addHediff(FibrousMechanites sev0.5, BeastArm 양팔), addAbility(Berserk), gearApparel(Drop), gearWeapon(Drop), verbTracker(replaceNativeTools), bloodCache, timer(15000틱)

### 수동확인
- [ ] `[M]` 물기(teeth) 근접공격만 사용됨
- [ ] `[M]` 혈흔 Filth_BloodInsect + 크롤링 smear + fleshType Insectoid
- [ ] `[M]` 진입/해제 Fleck + Sound + FX 쿨다운 30틱
- [ ] `[M]` Revert 기즈모 수동 해제 가능
- [ ] `[M]` 해제 후 텍스처/체형/스탯/hediff 전부 원복
- [ ] `[M]` 다른 폼(BeastkinForm) 상태에서 스크롤/약물로 덮어쓰기 가능

---

## 2. SSFTest_BearWarriorForm (곰 전사) — 아머드형

### 트리거
- [ ] `[M]` Ability_BuffAlly 아군/자기 타겟 변신 + 쿨다운 1500틱

### Auto-Verify (4항목)
> statHediff(이동+0.5, 근접회피+10, 피해배율x0.8), soundCache(Pawn_Bear_Angry), timer(15000틱)

### 수동확인
- [ ] `[M]` 근접 사운드: 히트(폰)=Shot_Charge_Blaster, 히트(건물)=Explosion_EMP, 미스=Pawn_Mech_Scyther_Call
- [ ] `[M]` 진입 Effecter Vaporize_Heatwave / 해제 ImpactSmallDustCloud
- [ ] `[M]` 커스텀 기즈모 아이콘 (Enter/Revert)
- [ ] `[M]` 장비 유지 (Keep)

---

## 3. SSFTest_SheepForm (양) — 적대 디버프

### 트리거
- [ ] `[M]` Ability_DebuffEnemy 적 타겟 75% + Ability_MassPolymorph AoE 60%
- [ ] `[M]` 비폭력 폰 캐스트 불가

### 비주얼
- [ ] `[M]` 성별 텍스처 분기 (SheepMale / SheepFemale) + bodyDrawScale 0.6

### Auto-Verify (3항목)
> statHediff(이동-1.0), workTags(Violent), timer(10000틱)

### 수동확인
- [ ] `[M]` Revert 기즈모 없음 (canRevertVoluntarily=false)

---

## 4. SSFTest_DarkKnightForm (암흑 기사) — 장비 소환

### 트리거
- [ ] `[M]` Ability_DarkKnight 자기변신 + 쿨다운 3000틱

### Auto-Verify (9항목)
> gearApparel(Inventory), gearWeapon(Inventory), spawnApparel(PlateArmor), spawnWeapon(LongSword), stuffApparel(Plasteel), stuffWeapon(Plasteel), equipLockApparel(Locked), equipLockWeapon(Locked), timer(18000틱)

### 수동확인
- [ ] `[M]` 변신 중 장비 교체 시도 → 차단 메시지
- [ ] `[M]` 해제 시 소환 장비 소멸 + 기존 장비 인벤토리에서 재장착

---

## 5a. SSFTest_BeastkinForm (수인) — 휴머노이드, 2단 변신 1단계

### 트리거
- [ ] `[M]` Ability_Beastkin 자기변신 + 쿨다운 1200틱

### 비주얼
- [ ] `[M]` 렌더노드: 머리에 FloppyEars(Skin색), 몸에 FurryTail(Hair색, 방향별 오프셋)
- [ ] `[M]` bodyDrawScale 1.20 + headDrawScale 1.05 + headOffset (0, 0.03)
- [ ] `[M]` 오버헤드 의류 숨김, Cape/Tuque만 표시

### Auto-Verify (12항목)
> hairColor(0.85,0.85,0.95), verbTracker(돌격소총/수류탄/화염방사/화염분사/광선빔 + claws), gearApparel(Inventory), addHediff(FibrousMechanites), addAbility(Waterskip), workTags(Crafting+Cooking)

### 보이스
- [ ] `[M]` call/death/wounded/angry/eating/melee 전부 커스텀 사운드

### Verb 기즈모
- [ ] `[M]` 돌격소총 자동공격 ON / 나머지 4종 OFF + 커스텀 아이콘/라벨 + 토글

### 수동확인
- [ ] `[M]` replaceNativeTools=false → 기존 도구 유지 + claws 추가
- [ ] `[M]` replaceNativeVerbs=false → 기존 verb 유지
- [ ] `[M]` Insectoid 혈흔 + 진입/해제 Fleck

---

## 5b. SSFTest_FullBeastForm (야수) — 2단 변신 체인

### 트리거/체이닝
- [ ] `[M]` 인간 상태에서 진입 불가 (어빌리티 자체 없음)
- [ ] `[M]` 수인(BeastkinForm) 상태에서만 Ability_FullBeast 표시 + 진입 가능
- [ ] `[M]` 수인 해제 시 FullBeast 어빌리티도 제거

### Auto-Verify (4항목)
> statHediff(이동+2.0, 피해배율x0.7), verbTracker(fangs+claws, replaceNativeTools), timer(12000틱)

### 수동확인
- [ ] `[M]` 야수 해제 후 복귀 동작 확인 (수인으로? 인간으로?)

---

## 6. SSFTest_GuardianForm (수호자) — 조건부 변신

### 트리거 (sustainMode=Any)
- [ ] `[M]` MagicStone 인벤토리 소지 → 변신 가능
- [ ] `[M]` Hediff_GuardianMark 보유 → 변신 가능
- [ ] `[M]` 둘 다 없으면 변신 불가 / 둘 다 있어도 OK

### 비주얼
- [ ] `[M]` bodyDrawScale 1.4 + portraitDrawScale 1.3 + bodyOffset (0, -0.1)
- [ ] `[M]` shadowVolume (0.6,1.0,0.6) + shadowOffset (0,0,-0.05)

### Auto-Verify (3항목)
> statHediff(이동+0.3, 피해배율x0.85), timer(12000틱)

### 수동확인
- [ ] `[M]` revertOnDowned=true → 의식 상실 시 자동 해제, 일반 피격은 유지
- [ ] `[M]` MagicStone 드롭 시 변신 유지 여부 확인

---

## 7. SSFTest_PhantomForm (유령) — 비주얼 오버라이드

### 비주얼
- [ ] `[M]` 머리 텍스처 Male_AverageNormal + 셰이더 Transparent(반투명)
- [ ] `[M]` 머리색 (0.7,0.8,1.0,0.5) 푸른빛 + 머리카락 숨김

### Auto-Verify (5항목)
> bodyType(Thin), skinColor(0.7,0.8,1.0), workTypes(Firefighter), timer(10000틱), R.bodyType+R.headType+R.skinColor

### 수동확인
- [ ] `[M]` FX 딜레이: 진입 30틱 / 해제 15틱
- [ ] `[M]` 해제 후 머리/체형/셰이더/피부색/머리카락 전부 원복

---

## 8. SSFTest_RaceLockedForm (종족 제한) — 인간 전용

### Auto-Verify (6항목)
> raceFilter(Human 통과, 비인간 차단), headType(Male_AverageNormal), equipLockApparel(Locked), equipLockWeapon(Unlocked), statHediff(이동+0.5, 근접회피+10, 피해배율x0.75), timer(12000틱)

---

## 9. AoE 투사체 (MassPolymorph)

- [ ] `[M]` 사거리 25 + warmup 2.5초 + 반경 5칸 + 60% 확률 양 변신
- [ ] `[M]` 아군 영향 + 지면 타겟 가능

---

## 10. 아이템/약물

- [ ] `[M]` ShiftScroll_Self: 사용 후 파괴, stackLimit 5
- [ ] `[M]` ShiftScroll_Target: 타겟 선택 UI + 이동 후 사용
- [ ] `[M]` BearElixir: DrugLab 제작, drugCategory=Medical, 복용 120틱
- [ ] `[M]` MagicStone: 인벤토리 소지 아이템 (소비 안 됨)

---

## 11. 어빌리티 획득 경로

### 유전자 (Biotech)
- [ ] `[M]` Gene_BeastkinShift / Gene_PhantomShift → 어빌리티 부여 + 변신 작동
- [ ] `[M]` 유전자 제거 → 어빌리티 즉시 제거 / 두 유전자 동시 보유 OK
- [ ] `[M]` Biotech 미설치 시 에러 없음 (MayRequire)

### 장착 아이템 (CompGiveAbility_SSF, requireEquipped=true)
- [ ] `[M]` Weapon_DarkBlade 장착 → DarkKnight 어빌리티 / 해제 시 제거
- [ ] `[M]` Apparel_PhantomCloak 착용 → Phantom 어빌리티 / 탈의 시 제거
- [ ] `[M]` 인벤토리만 보유 → 어빌리티 미부여

### 소지 아이템 (requireEquipped=false)
- [ ] `[M]` MagicStone 인벤토리 → Guardian 어빌리티 / 드롭 시 제거 / 재소지 시 재부여

### Hediff 기반 (바닐라 HediffComp_GiveAbility)
- [ ] `[M]` Hediff_ShiftBlessing → BuffAlly 어빌리티 / 제거 시 회수
- [ ] `[M]` Hediff_RacialAwakening → RaceLocked 어빌리티 (인간만 변신, 비인간 차단)

### 크로스 경로
- [ ] `[M]` 같은 어빌리티 복수 경로 → 중복 없이 1개, 한 경로 제거해도 유지
- [ ] `[M]` 각 경로별 세이브/로드 후 어빌리티 + 변신 상태 유지

---

## 12. 공통 테스트

### 세이브/로드
- [ ] `[M]` 변신 상태 세이브 → 로드 → 스탯/hediff/텍스처/컬러 정상 + 해제 복원

### 엣지 케이스
- [ ] `[M]` 변신 중 사망 → 시체/사운드/혈흔 정상
- [ ] `[M]` 변신 중 징집/비징집 전환
- [ ] `[M]` 같은 폼 재사용 → 기즈모 숨김 / 다른 폼 → 비활성 + 툴팁
- [ ] `[M]` allowedFromForms 허용 폼 → 변신 중에도 활성
- [ ] `[M]` 카라반/동면관/수송포드 이동 중 변신 유지
- [ ] `[M]` 인공팔/결손팔에 BeastArm ForceAdd 정상

### 이념 (Ideology)
- [ ] `[M]` 동물형 변신 시 알몸 무드 페널티 억제

### Simple Sidearms 호환
- [ ] `[M]` SS 활성: 변신 시 메모리 클리어 → 해제 시 원복 → 세이브/로드 정상
- [ ] `[M]` SS 비활성: 호환 패치 미적용, 에러 없음

---

## 13. 디버그 액션

- [ ] `[M]` Inspect Active Form: linkedHediff 기준 스탯/캐퍼 표시
- [ ] `[M]` Dump Pawn State: Stat Offsets/Factors/Capacity Mods 출력
- [ ] `[M]` AddedPart/Hediff/RemoveHediff 실패 시 `[SSF]` 경고 로그
- [ ] `[M]` HAR alignWithHead boolean 판정 + CompGiveAbility_SSF PostDeSpawn 1.6 시그니처
- [ ] `[M]` 장시간 플레이 후 로그 딕셔너리 메모리 누적 없음

---

## 요약

| 구분 | 항목 수 |
|------|---------|
| **[A] Auto-Verify** | **~60개** (폼별 Auto-Verify 블록) |
| **[M] 수동확인** | **~65개** |

> Auto-Verify 디버그 액션 실행 시 각 ✓/✗ 항목이 카테고리별로 자동 검증됩니다.
