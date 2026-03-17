# Shapeshifter Framework

## Status
⚠️ This mod is currently in development (WIP).
Not yet released on Steam Workshop. Features and implementation may change.

## Description
A RimWorld 1.6 mod framework for shapeshifting mechanics. Define custom transformation forms via XML — visuals, stats, combat, equipment, and VFX are all configurable.

## Architecture

**HediffDef-driven transformation** — The framework uses a HediffComp (`HediffComp_ShapeshiftCore`) as the core entry point. Each transformation is a HediffDef with this comp attached; the comp references a `FormDef` that serves as a reusable data template for visuals, verbs, equipment rules, and effects.

- **HediffDef** = entry point. Apply it to a pawn to trigger transformation. Contains behavioral overrides (duration, revert rules, sustain conditions) that can differ per HediffDef.
- **FormDef** = data sheet / template. Defines the visual appearance, verbs, equipment rules, VFX, and other shared properties of a form. Multiple HediffDefs can reference the same FormDef.
- **N:1 mapping**: The same FormDef can be used by different HediffDefs, each with different stat offsets, durations, or conditions — enabling variant forms from a single template.
- **No ThingDef patching required**: Unlike the previous CompShapeshifter (ThingComp) architecture, there is no need to patch pawn ThingDefs. Simply apply the HediffDef to any pawn to transform them.
- **Vanilla GiveHediff compatibility**: Transformation works with standard RimWorld hediff application (e.g., `GiveHediff` in AbilityDefs, scripted hediff application, etc.).

## Features

**Transformation System**
- XML-driven form definitions with 3 abstract base types (Animal / Humanoid / Armored)
- Multi-stage transformation chains via `addAbilities` (e.g., stage 1 → stage 2)
- Duration timer, voluntary/forced revert, sustain conditions (`sustainHediffs`, `sustainMode`)
- `revertOnDowned` — auto-revert on incapacitation
- Revert byproducts: item drops (`revertDrops`) and hediff application (`revertAddHediffs`) on form removal
- Per-HediffDef behavioral overrides: duration, revert rules, and sustain conditions can be overridden independently of the FormDef defaults

**Visuals & Rendering**
- Full body/head texture replacement with gender & swimming variants
- Body/head draw scale, portrait scale, position offsets, shadow overrides
- Shader override (e.g., Transparent), hair/skin color override
- Custom render nodes (ears, tails, etc.) via RimWorld's PawnRenderNode system
- Granular apparel/weapon/gene/hediff graphic hide/show with wildcard filters

**Stats & Combat**
- Stats and capacities defined via the HediffDef's own stages (standard vanilla HediffDef pattern)
- Custom verbs (ranged/melee) and tools with per-verb gizmo options (icon, label, auto-attack toggle)
- Work tag and work type restrictions during transformation

**Equipment Handling**
- Per-form gear policy: Keep / Inventory / Drop for apparel and weapons
- Equipment spawning on transform (with material/stuff support), destroyed on revert
- Equipment lock (prevent gear changes while transformed)

**VFX & Sound**
- Transform enter/exit: Fleck particles, Effecters, sounds with delay and cooldown
- Ambient VFX: persistent Effecter and periodic Fleck during transformation
- Voice overrides: call, wounded, death, angry, eating
- Melee sound overrides: hit (pawn/building), miss
- Blood/flesh type overrides

**Ability Acquisition**
- 6 trigger sources: Gene, Hediff, Item (equipped/inventory), Drug, Scroll/UseItem, AoE Projectile
- Conditional auto-shift: `HediffComp_AutoShift` triggers transformation on health/mental state/night/combat conditions
- Race/mutant cast restrictions via `CompProperties_AbilityShapeshift`
- Per-form race filtering (`formAllowedRaces`, `formDisallowedRaces`)

**External Mod Integration**
- C# event hooks for external mods: `ShapeshiftCoreUtility.OnFormApplied` / `ShapeshiftCoreUtility.OnFormRemoved`
- Subscribe to these events to react to transformations without patching framework internals

## Documentation
- [FormDef Guide (English)](FORMDEF_GUIDE_EN.md)
- [FormDef 가이드 (한국어)](FORMDEF_GUIDE_KO.md)

## Installation
Not yet available. Will be released via Steam Workshop and GitHub when ready.

## Compatibility
- Target: RimWorld 1.6
- Should be loaded **before mods that depend on shapeshifting features**
- **Humanoid Alien Races (HAR)**: Body addon visibility control during transformation (via `HARFormExtension` DefModExtension on FormDef)
- **Facial Animation**: Face type backup/restore and form-specific overrides (via `FAFormExtension` DefModExtension on FormDef)
- **Simple Sidearms**: Weapon memory backup/restore to prevent conflicts during transformation

## Credits
- Developed by **TRIBeagle**
- Documentation and code assistance with AI tools

---

# Shapeshifter Framework (한국어)

## 상태
⚠️ 현재 개발 진행 중(WIP)인 모드입니다.
아직 스팀 워크샵에 공개되지 않았으며, 기능과 구현은 변경될 수 있습니다.

## 설명
림월드 1.6 변신 메커니즘 프레임워크 모드. XML로 변신 폼을 정의하여 비주얼, 스탯, 전투, 장비, 이펙트를 자유롭게 설정할 수 있습니다.

## 아키텍처

**HediffDef 기반 변신** — 프레임워크의 핵심 진입점은 HediffComp(`HediffComp_ShapeshiftCore`)입니다. 각 변신은 이 Comp가 부착된 HediffDef이며, Comp는 비주얼·Verb·장비 규칙·이펙트 등의 재사용 가능한 데이터 템플릿인 `FormDef`를 참조합니다.

- **HediffDef** = 진입점. Pawn에 적용하면 변신이 시작됩니다. 지속시간, 해제 규칙, 유지 조건 등 행동 오버라이드를 HediffDef별로 다르게 설정할 수 있습니다.
- **FormDef** = 데이터 시트 / 템플릿. 폼의 비주얼, Verb, 장비 규칙, VFX 등 공유 속성을 정의합니다. 여러 HediffDef가 같은 FormDef를 참조할 수 있습니다.
- **N:1 매핑**: 같은 FormDef를 서로 다른 HediffDef에서 사용하면서, 각각 다른 스탯 보정·지속시간·조건을 부여할 수 있습니다.
- **ThingDef 패치 불필요**: 이전 CompShapeshifter(ThingComp) 아키텍처와 달리, Pawn ThingDef 패치 없이 HediffDef만 적용하면 변신됩니다.
- **바닐라 GiveHediff 호환**: AbilityDef의 GiveHediff 등 표준 림월드 hediff 적용 방식으로 변신이 작동합니다.

## 주요 기능

**변신 시스템**
- XML 기반 폼 정의 + 3가지 추상 기본 폼 (동물형 / 인간형 / 아머드형)
- `addAbilities` 기반 다단 변신 체인 (1단계 → 2단계)
- 지속시간 타이머, 자발/강제 해제, 유지 조건 (`sustainHediffs`, `sustainMode`)
- `revertOnDowned` — 의식 상실 시 자동 해제
- 변신 해제 부산물: 아이템 드랍(`revertDrops`), hediff 부여(`revertAddHediffs`)
- HediffDef별 행동 오버라이드: FormDef 기본값과 독립적으로 지속시간, 해제 규칙, 유지 조건을 오버라이드 가능

**비주얼 & 렌더링**
- 전신/머리 텍스처 교체 (성별/수영 분기 지원)
- 몸/머리 크기 배수, 포트레잇 배수, 위치 오프셋, 그림자 오버라이드
- 셰이더 오버라이드 (예: Transparent), 머리카락/피부색 오버라이드
- 커스텀 렌더 노드 (귀, 꼬리 등) — 림월드 PawnRenderNode 시스템 활용
- 의류/무기/유전자/헤디프 그래픽 숨김/표시 와일드카드 필터

**스탯 & 전투**
- HediffDef 자체 단계(stages)를 통한 스탯/능력치 보정 (바닐라 HediffDef 패턴)
- 커스텀 Verb(원거리/근접) + Tool + Verb별 기즈모 옵션 (아이콘, 라벨, 자동공격 토글)
- 변신 중 작업 태그/작업 타입 제한

**장비 처리**
- 폼별 장비 정책: Keep / Inventory / Drop (의류/무기 각각)
- 변신 시 장비 소환 (재질 지정 가능), 해제 시 자동 파괴
- 장비 잠금 (변신 중 교체 방지)

**이펙트 & 사운드**
- 변신 진입/해제: Fleck 파티클, Effecter, 사운드 (딜레이/쿨다운 지원)
- 앰비언트 VFX: 변신 중 지속 Effecter + 주기적 Fleck
- 보이스 오버라이드: 울음/부상/사망/분노/식사
- 근접 사운드 오버라이드: 히트(폰/건물)/미스
- 혈흔/살점 타입 오버라이드

**어빌리티 획득**
- 6가지 트리거 소스: 유전자, 헤디프, 아이템(장비/소지), 약물, 스크롤/사용아이템, AoE 투사체
- 조건부 자동 변신: `HediffComp_AutoShift` — 체력/정신상태/밤/전투 조건 충족 시 자동 변신
- 종족/뮤턴트 캐스트 제한 (`CompProperties_AbilityShapeshift`)
- 폼별 종족 필터링 (`formAllowedRaces`, `formDisallowedRaces`)

**외부 모드 연동**
- C# 이벤트 훅: `ShapeshiftCoreUtility.OnFormApplied` / `ShapeshiftCoreUtility.OnFormRemoved`
- 프레임워크 내부 패치 없이 이벤트 구독만으로 변신에 반응 가능

## 문서
- [FormDef Guide (English)](FORMDEF_GUIDE_EN.md)
- [FormDef 가이드 (한국어)](FORMDEF_GUIDE_KO.md)

## 설치
아직 다운로드 불가. 개발이 완료되면 스팀 워크샵 및 깃헙에 공개될 예정입니다.

## 호환성
- 대상 버전: RimWorld 1.6
- 변신 기능을 사용하는 모드보다 먼저 로드해야 함
- **Humanoid Alien Races (HAR)**: 변신 시 BodyAddon 표시 제어 (FormDef의 `HARFormExtension` DefModExtension 사용)
- **Facial Animation**: 얼굴 타입 백업/복원 및 폼별 오버라이드 (FormDef의 `FAFormExtension` DefModExtension 사용)
- **Simple Sidearms**: 변신 시 무기 메모리 백업/복원으로 충돌 방지

## 크레딧
- 제작: **TRIBeagle**
- 문서화 및 코드 일부는 AI 도구의 도움을 받음
