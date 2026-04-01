# Shapeshifter Framework (RimWorld 1.6)

> **WIP** — Not yet released on Steam Workshop. Features and API may change.

---

## Description (ENG)

A universal shapeshifting mod framework for RimWorld 1.6.
Modders can define custom transformation forms using **XML only** — no C# required.
The framework handles visuals, stats, equipment, VFX, sound, and all lifecycle management.

### How It Works

```
HediffDef (entry point — stats/severity)
  └─ HediffCompProperties_ShapeshiftCore
       └─ formDef ──▶ ShapeshiftFormDef (visual/gear/sound/VFX data)
```

- **HediffDef**: Stat modifiers (statOffsets/statFactors/capMods) and severity
- **ShapeshiftFormDef**: Read-only data sheet for visuals, equipment, sound, VFX
- **N:1 mapping**: Multiple HediffDefs can share one FormDef (same look, different stats)

### Features

**Transformation System**
- Timed or permanent forms with optional voluntary revert
- Revert on downed, sustain conditions (apparel/weapon/hediff/gene), auto-shift triggers
- Multi-stage form chains (`allowedFromForms`)
- Revert byproducts (item drops, hediff apply)

**Visuals**
- Body/head/hair/beard/tattoo per-part control (Default / Hidden / Replace)
- Animal pawn body texture replacement (via PawnRenderNode_AnimalPart)
- Gender-specific textures and swimming variants
- Scale, offset, portrait scale, shadow override
- BodyType / HeadType / hair color / skin color override
- Shader override per part (e.g., Transparent)
- PawnRenderNode support (ears, tails, wings, etc.)
- Apparel/weapon/gene/hediff layer visibility filtering (with wildcard `All` and `*` patterns)

**Stats & Combat**
- Stats via HediffDef stages (statOffsets, statFactors, capMods)
- Custom verbs & tools with per-verb auto-attack toggle gizmos
- Neural heat (entropy) cost per verb (Royalty DLC)
- Duration cost per verb (shift time deducted on use)
- `replaceNativeVerbs` / `replaceNativeTools` for full override
- Melee sound override (hit pawn / hit building / miss)
- Wound labels automatically show form name instead of race name
- Work type / work tag restrictions

**Equipment**
- Per-slot gear handling: Keep / Inventory / Drop
- Equipment lock: Auto / Locked / Unlocked (apparel/weapon independent)
- Spawn gear on transform (with stuff material)
- Previous gear auto-reequip on revert

**VFX & Sound**
- Enter/exit sound, effecter, fleck (with count, scale, delay, cooldown)
- Ambient effecter & fleck (sustained during transform)
- Voice override (call, wounded, death, angry, eating)
- Blood / blood smear / flesh type override

**Ideology Integration**
- Shapeshifting precept with 5 stages (Blasphemy → Divine Blessing)
- Self-initiated transform blocking at forbidden stage
- Sacred animal form mood effects
- Nudity/exposure thought suppression

**Trigger Methods**
| Method | Description |
|--------|-------------|
| Ability | Self / target / AoE cast |
| Drug | Transform on ingestion |
| Item (Use) | Consumable scroll/artifact |
| Item (Target) | Targetable scroll |
| Projectile | Transform on hit (+ AoE radius) |
| Equipment Grant | Ability granted on equip |
| Gene | Biotech gene-based ability |
| AutoShift | Auto-transform on health/combat/light conditions |

**Integration API**
- Events: `OnFormApplied` / `OnFormRemoved` callbacks
- Methods: `GiveShiftHediff()`, `RemoveForm()`, `TryGetCore()`, `ExtendDuration()`

### Compatibility

| Mod | Status |
|-----|--------|
| **RimWorld 1.6** | Required |
| **Harmony** | Required |
| **HAR (Humanoid Alien Races)** | Compatible — body addon scaling, head addon detection |
| **Facial Animation** | Compatible — face component backup/restore |
| **Simple Sidearms** | Compatible — weapon memory backup/restore |
| **Pocket Sand** | Compatible — gizmo filtering on transform |
| **Combat Extended** | Compatible — XPath patches for ToolCE/VerbPropertiesCE |
| **Anomaly DLC** | Compatible — per-form mutant eligibility filtering |
| **Pawnmorpher** | **Incompatible** — overlapping transformation systems |

### Documentation

| Document | Language |
|----------|----------|
| [FormDef Creation Guide](FORMDEF_GUIDE_EN.md) | English |
| [FormDef 제작 가이드](FORMDEF_GUIDE_KO.md) | 한국어 |

### Build

```bash
cd "Source/ShapeshifterFramework v1.6/ShapeshifterFramework"
dotnet build ShapeshifterFramework.csproj
```

### Credits

- **TRIBeagle** — Design, implementation
- AI-assisted development (Claude)

### License

All rights reserved. This framework is not yet released.

---

## 소개 (KOR)

RimWorld 1.6 전용 범용 변신 모드 프레임워크.
모더가 **XML만으로** 커스텀 변신 폼을 정의할 수 있으며, 비주얼/스탯/장비/VFX/사운드 및 모든 생명주기 관리를 프레임워크가 처리합니다.

### 구조

```
HediffDef (진입점 — 스탯/severity)
  └─ HediffCompProperties_ShapeshiftCore
       └─ formDef ──▶ ShapeshiftFormDef (비주얼/장비/사운드/VFX 데이터)
```

- **HediffDef**: 스탯 보정(statOffsets/statFactors/capMods)과 severity 정의
- **ShapeshiftFormDef**: 비주얼/장비/사운드/VFX 읽기 전용 데이터 시트
- **N:1 매핑**: 여러 HediffDef가 하나의 FormDef를 공유 가능 (같은 외형, 다른 스탯)

### 주요 기능

**변신 시스템**
- 시간제/영구 변신 + 자발적 해제 옵션
- 쓰러짐 시 해제, 유지 조건(의류/무기/헤디프/유전자), 자동 변신 트리거
- 다단계 변신 체인 (`allowedFromForms`)
- 해제 부산물 (아이템 드랍, 헤디프 부여)

**비주얼**
- 몸/머리/머리카락/수염/문신 파트별 제어 (기본/숨김/교체)
- 동물 폰 body 텍스처 교체 지원 (PawnRenderNode_AnimalPart)
- 성별 텍스처 및 수영 변형
- 스케일/오프셋/초상화 스케일/그림자 오버라이드
- 체형/머리형/헤어색/피부색 오버라이드
- 파트별 셰이더 오버라이드 (예: 투명)
- PawnRenderNode 지원 (귀, 꼬리, 날개 등)
- 의류/무기/유전자/헤디프 레이어 가시성 필터링 (와일드카드 `All`, `*` 패턴)

**스탯 & 전투**
- HediffDef stages를 통한 스탯 보정 (statOffsets, statFactors, capMods)
- 커스텀 verb & tool + verb별 자동공격 토글 기즈모
- verb별 신경열(엔트로피) 비용 (로열티 DLC)
- verb별 변신 시간 차감 비용
- `replaceNativeVerbs` / `replaceNativeTools`로 바닐라 완전 대체
- 근접 사운드 오버라이드 (폰 타격/건물 타격/빗나감)
- 상처 라벨에 종족명 대신 폼 이름 자동 표시
- 작업 유형/작업 태그 제한

**장비**
- 슬롯별 장비 처리: 유지/인벤토리/드랍
- 장비 잠금: 자동/잠금/해제 (의류/무기 독립)
- 변신 시 장비 생성 (재질 지정 가능)
- 해제 시 이전 장비 자동 재착용

**VFX & 사운드**
- 진입/해제 사운드, 이펙터, 플렉 (횟수/스케일/딜레이/쿨다운)
- 앰비언트 이펙터 & 플렉 (변신 중 지속)
- 보이스 오버라이드 (호출/부상/사망/분노/식사)
- 혈흔/혈흔 번짐/살점 유형 오버라이드

**이데올로기 연동**
- 변신 규율 5단계 (섭리에 대한 모독 → 신이 내린 축복)
- 금지 단계에서 자기 주도 변신 차단
- 성스러운 동물 폼 기분 효과
- 노출 감정 억제

**트리거 방법**
| 방법 | 설명 |
|------|------|
| 어빌리티 | 자기/타인/AoE 시전 |
| 약물 | 복용 시 변신 |
| 아이템(사용) | 소비 아이템(스크롤/유물) |
| 아이템(대상) | 대상 지정 스크롤 |
| 투사체 | 명중 시 변신 (+ AoE 반경) |
| 장비 부여 | 장착 시 어빌리티 부여 |
| 유전자 | Biotech 유전자 기반 어빌리티 |
| 자동 변신 | 체력/전투/밝기 조건 자동 변신 |

**외부 연동 API**
- 이벤트: `OnFormApplied` / `OnFormRemoved` 콜백
- 메서드: `GiveShiftHediff()`, `RemoveForm()`, `TryGetCore()`, `ExtendDuration()`

### 호환성

| 모드 | 상태 |
|------|------|
| **RimWorld 1.6** | 필수 |
| **Harmony** | 필수 |
| **HAR (Humanoid Alien Races)** | 호환 — body addon 스케일링, head addon 감지 |
| **Facial Animation** | 호환 — 얼굴 컴포넌트 백업/복원 |
| **Simple Sidearms** | 호환 — 무기 메모리 백업/복원 |
| **Pocket Sand** | 호환 — 변신 시 기즈모 필터링 |
| **Combat Extended** | 호환 — XPath 패치로 ToolCE/VerbPropertiesCE 대응 |
| **Anomaly DLC** | 호환 — 폼별 뮤턴트 적격성 필터링 |
| **Pawnmorpher** | **비호환** — 변신 시스템 충돌 |

### 문서

| 문서 | 언어 |
|------|------|
| [FormDef Creation Guide](FORMDEF_GUIDE_EN.md) | English |
| [FormDef 제작 가이드](FORMDEF_GUIDE_KO.md) | 한국어 |

### 빌드

```bash
cd "Source/ShapeshifterFramework v1.6/ShapeshifterFramework"
dotnet build ShapeshifterFramework.csproj
```

### 크레딧

- **TRIBeagle** — 설계, 구현
- AI 보조 개발 (Claude)

### 라이선스

All rights reserved. 아직 미출시 프레임워크입니다.
