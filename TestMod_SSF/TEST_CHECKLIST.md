# Shapeshifter Framework — 테스트 체크리스트

> TestMod_SSF 기준 | `[A]` = Auto-Verify (디버그 액션) | `[M]` = 인게임 수동 확인

---

## 준비

1. TestMod_SSF + ShapeshifterFramework 활성화, 개발자 모드 ON
2. 새 게임 (자유지대, 식민자 3명+)
3. `SSF: Dump Form Info`로 9개 폼 로드 확인, `[SSF]` 에러 없음

---

## 폼 요약

| 폼 | 부모 | 트리거 | 핵심 기능 |
|----|------|--------|-----------|
| BearForm | Animal | 약물/스크롤 | body Replace, 수영텍스처, tools, addHediffs, revertDrops, FX |
| BearWarriorForm | Armored | 어빌리티(대상) | 사운드, Effecter, gear Keep |
| SheepForm | Animal | 어빌리티(적대)/AoE | 성별텍스처, canRevertVoluntarily=false, workTags |
| DarkKnightForm | Armored | 어빌리티(자기) | spawnApparel/Weapon, equipLock, revertAddHediffs |
| BeastkinForm | Humanoid | 어빌리티(자기) | renderNodes, 5종 verb+기즈모, addAbilities 체인 |
| FullBeastForm | Animal | addAbilities 체인 | allowedFromForms, 2단 변신 |
| GuardianForm | Humanoid | 어빌리티(소지아이템) | sustain, portrait/shadow, ambientFleck, revertOnDowned |
| PhantomForm | Humanoid | 어빌리티(유전자/의류) | head Replace, Transparent 셰이더, FX delay |
| RaceLockedForm | Armored | 어빌리티(Hediff) | formAllowedRaces, headType, 비대칭 equipLock |

---

## 1. BearForm — 동물형 기본

### 트리거
- [ ] `[M]` 1-1. ShiftScroll_Self / ShiftScroll_Target / BearElixir(85%) 각각 변신 성공

### 비주얼
- [ ] `[M]` 1-2. 곰 텍스처 + bodyDrawScale 2.5 + 기본색 (112,82,65)
- [ ] `[M]` 1-3. 수영 시 SwimmingBear 텍스처 + 수영색 (255,255,255)

### Auto-Verify
> `[A]` linkedHediff 스탯(이동+1.5, 근접명중x1.20, 근접회피x1.15, capMods 4종), addHediff(FibrousMechanites sev0.5, BeastArm 양팔), addAbility(Berserk), gear(apparel=Drop, weapon=Drop), verbTracker(replaceNativeTools), bloodCache, timer(15000틱)

### 수동 확인
- [ ] `[M]` 1-4. 물기(teeth) 근접공격만 사용
- [ ] `[M]` 1-5. 혈흔 Filth_BloodInsect + 크롤링 smear + fleshType Insectoid
- [ ] `[M]` 1-6. 진입/해제 Fleck + Sound + FX 쿨다운 30틱
- [ ] `[M]` 1-7. revertDrops: 해제 시 아이템 드랍 확인
- [ ] `[M]` 1-8. Revert 기즈모 수동 해제 가능
- [ ] `[M]` 1-9. 해제 후 텍스처/체형/스탯/hediff 전부 원복
- [ ] `[M]` 1-10. 다른 폼 상태에서 스크롤/약물로 덮어쓰기 가능

---

## 2. BearWarriorForm — 아머드형, 사운드/이펙터

### 트리거
- [ ] `[M]` 2-1. Ability_BuffAlly 아군/자기 타겟 변신 + 쿨다운 1500틱

### Auto-Verify
> `[A]` linkedHediff 스탯(이동+0.5, 근접회피+10, 피해배율x0.8), soundCache(Pawn_Bear_Angry), timer(15000틱)

### 수동 확인
- [ ] `[M]` 2-2. 근접 사운드: 히트(폰)=Shot_Charge_Blaster, 히트(건물)=Explosion_EMP, 미스=Pawn_Mech_Scyther_Call
- [ ] `[M]` 2-3. 진입 Effecter Vaporize_Heatwave / 해제 ImpactSmallDustCloud
- [ ] `[M]` 2-4. 커스텀 기즈모 아이콘
- [ ] `[M]` 2-5. 장비 유지 (Keep)

---

## 3. SheepForm — 적대 디버프, 강제 변신

### 트리거
- [ ] `[M]` 3-1. Ability_DebuffEnemy 적 타겟 75% + Ability_MassPolymorph AoE 60%
- [ ] `[M]` 3-2. 비폭력 폰 캐스트 불가

### 비주얼
- [ ] `[M]` 3-3. 성별 텍스처 분기 (SheepMale / SheepFemale) + bodyDrawScale 0.6

### Auto-Verify
> `[A]` linkedHediff 스탯(이동-1.0), workTags(Violent), timer(10000틱)

### 수동 확인
- [ ] `[M]` 3-4. Revert 기즈모 없음 (canRevertVoluntarily=false)

---

## 4. DarkKnightForm — 장비 소환

### 트리거
- [ ] `[M]` 4-1. Ability_DarkKnight 자기변신 + 쿨다운 3000틱

### Auto-Verify
> `[A]` gear(apparel=Inventory, weapon=Inventory), spawnApparel(PlateArmor), spawnWeapon(LongSword), stuff(Plasteel), equipLock(apparel=Locked, weapon=Locked), timer(18000틱)

### 수동 확인
- [ ] `[M]` 4-2. 변신 중 장비 교체 시도 → 차단 메시지
- [ ] `[M]` 4-3. revertAddHediffs: 해제 시 hediff 부여 확인 (바닐라 수명, 프레임워크 비추적)
- [ ] `[M]` 4-4. 해제 시 소환 장비 소멸 + 기존 장비 인벤토리에서 재장착

---

## 5. BeastkinForm — 휴머노이드, 2단 변신 1단계

### 트리거
- [ ] `[M]` 5-1. Ability_Beastkin 자기변신 + 쿨다운 1200틱

### 비주얼
- [ ] `[M]` 5-2. 렌더노드: 머리에 FloppyEars(Skin색), 몸에 FurryTail(Hair색)
- [ ] `[M]` 5-3. bodyDrawScale 1.20 + headDrawScale 1.05 + headOffset (0, 0.03)
- [ ] `[M]` 5-4. 오버헤드 의류 숨김, Cape/Tuque만 표시

### Auto-Verify
> `[A]` hairColor(0.85,0.85,0.95), verbTracker(돌격소총/수류탄/화염방사/화염분사/광선빔 + claws), gear(apparel=Inventory), addHediff(FibrousMechanites), addAbility(Waterskip), workTags(Crafting+Cooking)

### 수동 확인
- [ ] `[M]` 5-5. 보이스: call/death/wounded/angry/eating/melee 커스텀 사운드
- [ ] `[M]` 5-6. Verb 기즈모: 돌격소총 자동공격 ON / 나머지 4종 OFF + 커스텀 아이콘/라벨
- [ ] `[M]` 5-7. replaceNativeTools=false → 기존 도구 유지 + claws 추가
- [ ] `[M]` 5-8. Insectoid 혈흔 + 진입/해제 Fleck

---

## 6. FullBeastForm — 2단 변신 체인

### 체이닝
- [ ] `[M]` 6-1. 인간 상태에서 진입 불가 (어빌리티 없음)
- [ ] `[M]` 6-2. 수인(BeastkinForm) 상태에서만 Ability_FullBeast 표시 + 진입 가능
- [ ] `[M]` 6-3. 수인 해제 시 FullBeast 어빌리티 제거

### Auto-Verify
> `[A]` linkedHediff 스탯(이동+2.0, 피해배율x0.7), verbTracker(fangs+claws, replaceNativeTools), timer(12000틱)

### 수동 확인
- [ ] `[M]` 6-4. 야수 해제 후 복귀 동작 확인

---

## 7. GuardianForm — 조건부 유지, 앰비언트 VFX

### 유지 조건 (sustainMode=Any)
- [ ] `[M]` 7-1. MagicStone 인벤토리 소지 → 변신 가능
- [ ] `[M]` 7-2. Hediff_GuardianMark 보유 → 변신 가능
- [ ] `[M]` 7-3. 둘 다 없으면 변신 불가 / 둘 다 있어도 OK

### 비주얼
- [ ] `[M]` 7-4. bodyDrawScale 1.4 + portraitDrawScale 1.3 + bodyOffset (0, -0.1)
- [ ] `[M]` 7-5. shadowVolume (0.6,1.0,0.6) + shadowOffset (0,0,-0.05)

### 앰비언트 VFX
- [ ] `[M]` 7-6. ambientFleck: 지정 간격마다 Fleck 스폰 확인
- [ ] `[M]` 7-7. 맵 밖(캐러밴) → VFX 미재생 + 에러 없음
- [ ] `[M]` 7-8. 세이브/로드 후 앰비언트 자동 재생성

### Auto-Verify
> `[A]` linkedHediff 스탯(이동+0.3, 피해배율x0.85), timer(12000틱)

### 수동 확인
- [ ] `[M]` 7-9. revertOnDowned=true → 의식 상실 시 자동 해제
- [ ] `[M]` 7-10. MagicStone 드롭 시 변신 유지 여부 확인

---

## 8. PhantomForm — 비주얼 오버라이드, FX 딜레이

### 비주얼
- [ ] `[M]` 8-1. 머리 텍스처 Male_AverageNormal + 셰이더 Transparent(반투명)
- [ ] `[M]` 8-2. 머리색 (0.7,0.8,1.0,0.5) 푸른빛 + 머리카락 숨김

### Auto-Verify
> `[A]` bodyType(Thin), skinColor(0.7,0.8,1.0), workTypes(Firefighter), timer(10000틱)

### 수동 확인
- [ ] `[M]` 8-3. FX 딜레이: 진입 30틱 / 해제 15틱
- [ ] `[M]` 8-4. 해제 후 머리/체형/셰이더/피부색/머리카락 전부 원복

---

## 9. RaceLockedForm — 종족 제한, 비대칭 잠금

### Auto-Verify
> `[A]` raceFilter(Human 통과, 비인간 차단), headType(Male_AverageNormal), equipLock(apparel=Locked, weapon=Unlocked), linkedHediff 스탯, timer(12000틱)

### 뮤턴트 필터 (Anomaly)
- [ ] `[M]` 9-1. formAllowedMutants → 해당 뮤턴트만 변신 가능
- [ ] `[M]` 9-2. formDisallowedMutants → 해당 뮤턴트 차단
- [ ] `[M]` 9-3. 비-어빌리티 경로에서도 뮤턴트 필터 작동
- [ ] `[M]` 9-4. Anomaly DLC 미설치 시 에러 없음 (MayRequire)

---

## 10. 트리거 소스별 검증

### 어빌리티 경로
- [ ] `[M]` 10-1. AoE 투사체: 사거리 25 + 반경 5칸 + 60% 양 변신
- [ ] `[M]` 10-2. CursedArrow 투사체: 40% 양 변신

### 아이템/약물
- [ ] `[M]` 10-3. ShiftScroll_Self: 사용 후 파괴, stackLimit 5
- [ ] `[M]` 10-4. ShiftScroll_Target: 타겟 선택 UI + 이동 후 사용
- [ ] `[M]` 10-5. BearElixir: DrugLab 제작, 복용 120틱
- [ ] `[M]` 10-6. MagicStone: 인벤토리 소지 (소비 안 됨)

### 유전자 (Biotech)
- [ ] `[M]` 10-7. Gene_BeastkinShift / Gene_PhantomShift → 어빌리티 부여 + 변신 작동
- [ ] `[M]` 10-8. 유전자 제거 → 어빌리티 즉시 제거
- [ ] `[M]` 10-9. Biotech 미설치 시 에러 없음 (MayRequire)

### 장착 아이템
- [ ] `[M]` 10-10. Weapon_DarkBlade 장착 → DarkKnight 어빌리티 / 해제 시 제거
- [ ] `[M]` 10-11. Apparel_PhantomCloak 착용 → Phantom 어빌리티 / 탈의 시 제거
- [ ] `[M]` 10-12. 인벤토리만 보유 → 어빌리티 미부여

### 소지 아이템
- [ ] `[M]` 10-13. MagicStone 인벤토리 → Guardian 어빌리티 / 드롭 시 제거 / 재소지 시 재부여

### Hediff 기반
- [ ] `[M]` 10-14. Hediff_ShiftBlessing → BuffAlly 어빌리티 / 제거 시 회수
- [ ] `[M]` 10-15. Hediff_RacialAwakening → RaceLocked 어빌리티

### 크로스 경로
- [ ] `[M]` 10-16. 같은 어빌리티 복수 경로 → 중복 없이 1개, 한 경로 제거해도 유지
- [ ] `[M]` 10-17. 각 경로별 세이브/로드 후 어빌리티 + 변신 상태 유지

---

## 11. 조건부 자동 변신 (HediffComp_AutoShift)

- [ ] `[M]` 11-1. healthThreshold: 체력 30% 미만 → 자동 변신
- [ ] `[M]` 11-2. triggerMentalStates: Berserk 진입 → 트리거
- [ ] `[M]` 11-3. triggerSunGlowBelow: 밝기 0.5 미만(밤) → 트리거
- [ ] `[M]` 11-4. triggerInCombat: 징집/피격 + 적 근처 → 트리거 (NPC 포함)
- [ ] `[M]` 11-5. 이미 변신 중 → 재트리거 건너뜀
- [ ] `[M]` 11-6. successChance < 1.0 → 확률적 발동
- [ ] `[M]` 11-7. triggerOnce=true → 발동 후 hediff 제거
- [ ] `[M]` 11-8. triggerOnce=false → 해제 후 재트리거 가능
- [ ] `[M]` 11-9. checkIntervalTicks 간격 정상 (60 vs 120 vs 240)
- [ ] `[M]` 11-10. 세이브/로드 후 hasTriggered 플래그 유지

---

## 12. 공통 테스트

### 세이브/로드
- [ ] `[M]` 12-1. 변신 상태 세이브 → 로드 → 스탯/hediff/텍스처/컬러 정상 + 해제 복원

### 엣지 케이스
- [ ] `[M]` 12-2. 변신 중 사망 → 시체/사운드/혈흔 정상
- [ ] `[M]` 12-3. 변신 중 징집/비징집 전환
- [ ] `[M]` 12-4. 같은 폼 재사용 → 기즈모 숨김 / 다른 폼 → 비활성 + 툴팁
- [ ] `[M]` 12-5. allowedFromForms 허용 폼 → 변신 중에도 활성
- [ ] `[M]` 12-6. 카라반/동면관/수송포드 중 변신 유지
- [ ] `[M]` 12-7. 인공팔/결손팔에 BeastArm ForceAdd 정상

### 해제 부산물
- [ ] `[M]` 12-8. revertDrops: despawned 폰 → 드랍 건너뜀 + 에러 없음
- [ ] `[M]` 12-9. revertAddHediffs: 사망 폰 → 부여 건너뜀 + 에러 없음
- [ ] `[M]` 12-10. addHediffs(추적)와 revertAddHediffs(비추적) 동시 사용 시 독립 작동

### 앰비언트 VFX
- [ ] `[M]` 12-11. ambientEffecter 설정 폼 → 변신 중 Effecter 지속 재생
- [ ] `[M]` 12-12. ambientEffecter 해제 시 Cleanup → 잔상 없음
- [ ] `[M]` 12-13. 세이브/로드 후 ambientEffecter 자동 재생성

### 이념 (Ideology)
- [ ] `[M]` 12-14. 동물형 변신 시 알몸 무드 페널티 억제

### Simple Sidearms
- [ ] `[M]` 12-15. SS 활성: 변신 시 메모리 클리어 → 해제 시 원복 → 세이브/로드 정상
- [ ] `[M]` 12-16. SS 비활성: 호환 패치 미적용, 에러 없음

---

## 13. 디버그 액션

- [ ] `[M]` 13-1. Inspect Active Form: linkedHediff 기준 스탯/캐퍼 표시
- [ ] `[M]` 13-2. Dump Pawn State: Stat Offsets/Factors/Capacity Mods 출력
- [ ] `[M]` 13-3. AddedPart/Hediff 실패 시 `[SSF]` 경고 로그
- [ ] `[M]` 13-4. 장시간 플레이 후 메모리 누적 없음

---

## 요약

| 구분 | 항목 수 |
|------|---------|
| **[A] Auto-Verify** | ~60개 (폼별 Auto-Verify 블록) |
| **[M] 수동 확인** | ~80개 |

> Auto-Verify 디버그 액션 실행 시 각 ✓/✗ 항목이 카테고리별로 자동 검증됩니다.
