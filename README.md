# Shapeshifter Framework

## Status
⚠️ This mod is currently in development (WIP).
Not yet released on Steam Workshop. Features and implementation may change.

## Description
A RimWorld 1.6 mod framework for shapeshifting mechanics. Define custom transformation forms via XML — visuals, stats, combat, equipment, and VFX are all configurable.

## Features

**Transformation System**
- XML-driven form definitions with 3 abstract base types (Animal / Humanoid / Armored)
- Multi-stage transformation chains via `addAbilities` (e.g., stage 1 → stage 2)
- Duration timer, voluntary/forced revert, sustain conditions (`sustainHediffs`, `sustainMode`)
- `revertOnDowned` — auto-revert on incapacitation

**Visuals & Rendering**
- Full body/head texture replacement with gender & swimming variants
- Body/head draw scale, portrait scale, position offsets, shadow overrides
- Shader override (e.g., Transparent), hair/skin color override
- Custom render nodes (ears, tails, etc.) via RimWorld's PawnRenderNode system
- Granular apparel/weapon/gene/hediff graphic hide/show with wildcard filters

**Stats & Combat**
- Stats and capacities defined via `linkedHediff` (vanilla HediffDef pattern)
- Custom verbs (ranged/melee) and tools with per-verb gizmo options (icon, label, auto-attack toggle)
- Work tag and work type restrictions during transformation

**Equipment Handling**
- Per-form gear policy: Keep / Inventory / Drop for apparel and weapons
- Equipment spawning on transform (with material/stuff support), destroyed on revert
- Equipment lock (prevent gear changes while transformed)

**VFX & Sound**
- Transform enter/exit: Fleck particles, Effecters, sounds with delay and cooldown
- Voice overrides: call, wounded, death, angry, eating
- Melee sound overrides: hit (pawn/building), miss
- Blood/flesh type overrides

**Ability Acquisition**
- 6 trigger sources: Gene, Hediff, Item (equipped/inventory), Drug, Scroll/UseItem, AoE Projectile
- Race/mutant cast restrictions via `CompProperties_AbilityShiftTarget`
- Per-form race filtering (`applicableRaces`)

## Documentation
- [FormDef Guide (English)](FORMDEF_GUIDE_EN.md)
- [FormDef 가이드 (한국어)](FORMDEF_GUIDE_KO.md)

## Installation
Not yet available. Will be released via Steam Workshop and GitHub when ready.

## Compatibility
- Target: RimWorld 1.6
- Should be loaded **before mods that depend on shapeshifting features**
- **Humanoid Alien Races (HAR)**: Body addon visibility control during transformation
- **Facial Animation**: Face type backup/restore and form-specific overrides
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

## 주요 기능

**변신 시스템**
- XML 기반 폼 정의 + 3가지 추상 기본 폼 (동물형 / 인간형 / 아머드형)
- `addAbilities` 기반 다단 변신 체인 (1단계 → 2단계)
- 지속시간 타이머, 자발/강제 해제, 유지 조건 (`sustainHediffs`, `sustainMode`)
- `revertOnDowned` — 의식 상실 시 자동 해제

**비주얼 & 렌더링**
- 전신/머리 텍스처 교체 (성별/수영 분기 지원)
- 몸/머리 크기 배수, 포트레잇 배수, 위치 오프셋, 그림자 오버라이드
- 셰이더 오버라이드 (예: Transparent), 머리카락/피부색 오버라이드
- 커스텀 렌더 노드 (귀, 꼬리 등) — 림월드 PawnRenderNode 시스템 활용
- 의류/무기/유전자/헤디프 그래픽 숨김/표시 와일드카드 필터

**스탯 & 전투**
- `linkedHediff` 기반 스탯/능력치 보정 (바닐라 HediffDef 패턴)
- 커스텀 Verb(원거리/근접) + Tool + Verb별 기즈모 옵션 (아이콘, 라벨, 자동공격 토글)
- 변신 중 작업 태그/작업 타입 제한

**장비 처리**
- 폼별 장비 정책: Keep / Inventory / Drop (의류/무기 각각)
- 변신 시 장비 소환 (재질 지정 가능), 해제 시 자동 파괴
- 장비 잠금 (변신 중 교체 방지)

**이펙트 & 사운드**
- 변신 진입/해제: Fleck 파티클, Effecter, 사운드 (딜레이/쿨다운 지원)
- 보이스 오버라이드: 울음/부상/사망/분노/식사
- 근접 사운드 오버라이드: 히트(폰/건물)/미스
- 혈흔/살점 타입 오버라이드

**어빌리티 획득**
- 6가지 트리거 소스: 유전자, 헤디프, 아이템(장비/소지), 약물, 스크롤/사용아이템, AoE 투사체
- 종족/뮤턴트 캐스트 제한 (`CompProperties_AbilityShiftTarget`)
- 폼별 종족 필터링 (`applicableRaces`)

## 문서
- [FormDef Guide (English)](FORMDEF_GUIDE_EN.md)
- [FormDef 가이드 (한국어)](FORMDEF_GUIDE_KO.md)

## 설치
아직 다운로드 불가. 개발이 완료되면 스팀 워크샵 및 깃헙에 공개될 예정입니다.

## 호환성
- 대상 버전: RimWorld 1.6
- 변신 기능을 사용하는 모드보다 먼저 로드해야 함
- **Humanoid Alien Races (HAR)**: 변신 시 BodyAddon 표시 제어
- **Facial Animation**: 얼굴 타입 백업/복원 및 폼별 오버라이드
- **Simple Sidearms**: 변신 시 무기 메모리 백업/복원으로 충돌 방지

## 크레딧
- 제작: **TRIBeagle**
- 문서화 및 코드 일부는 AI 도구의 도움을 받음
