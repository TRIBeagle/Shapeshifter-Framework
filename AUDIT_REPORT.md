# SSF Code Audit Report

> Multi-agent audit (30 agents / 124 .cs files): per-area review -> adversarial verify -> doc sync.
>
> Total 75 findings -> confirmed 67 / rejected (false positive) 8.
> Severity shown is the post-verification (effective) severity; '(was X)' marks a downgrade by the verifier.

## Summary (by effective severity)

- **HIGH**: 0
- **MEDIUM**: 10
- **LOW**: 57

## Confirmed Findings (67)

### Hediffs-Core

- [ ] **[MEDIUM / bug]** verbProps.label / VerbGizmoOption.label 에 .Translate() 적용 — 표시 라벨을 번역 키로 오인
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.Verbs.cs:245, 242, 261-262`
  - Detail: GetVerbLabel L245 `string label = string.IsNullOrEmpty(vp?.label) ? "SSF_Verb_Attack".Translate() : vp.label.Translate();` 와 L242 `o.label`, GetVerbDesc L262 `o.description`/`o.toggleDescription` 모두 `.Translate()`를 호출합니다. 그러나 이 값들은 번역 키가 아니라 표시용 리터럴 문자열입니다 — TestMod ShiftForms.xml의 `<label>assault rifle</label>`, `<toggleLabel>Auto-rifle</toggleLabel>` 및 verbProps.label(예: "assault rifle")이 그 근거입니다. RimWorld의 string.Translate()는 키 미존재 시 원문을 반환하므로 영어에선 대개 동작하지만, (1) 라벨이 우연히 실제 번역 키(예: 단어 'shoot' 등)와 충돌하면 엉뚱한 번역으로 치환되고, (2) dev 모드의 미사용 번역키 리포팅 시 가짜 경고가 발생합니다. 바닐라는 verbProps.label을 번역 없이 그대로 CapitalizeFirst만 합니다.
  - Fix: 리터럴 표시 문자열에는 .Translate()를 제거하고 CapitalizeFirst()만 적용. 번역 키를 받고 싶다면 별도 필드(예: labelKey)로 구분하거나, `.TranslateSimple()` 대신 키 존재 검사 후 분기. 최소한 vp.label.Translate() → vp.label, o.label.Translate() → o.label 로 변경 (이미 CapitalizeFirst 호출됨).
  - Verify [high]: 코드가 주장과 정확히 일치함 (L245 vp.label.Translate(), L242 o.label/toggleLabel.Translate(), L262 o.description/toggleDescription.Translate()). TestMod ShiftForms.xml은 이 값들이 번역 키가 아니라 표시 리터럴임을 증명함 (<label>assault rifle</label>, <toggleLabel>Auto-rifle</toggleLabel>, <description>Fire a burst...</description>), FormDef.cs 필드 주석도 "verb 명령 라벨"로 명시. 디컴파일 Verb.cs(ReportLabel => verbProps.label)는 바닐라가 .Translate() 없이 raw 사용함을 확인. 결정적으로 디컴파일 Translator.cs L60-64: Prefs.DevMode일 때 키 미존재 시 PseudoTranslated()로 모든 글자를 변형(assault rifle→àșșàùſṭ ṟìfſè)하므로, 모더가 Dev Mode에서 테스트하면 모든 변신 기즈모 라벨/설명이 깨져 보임 — 이는 결정적·가시적 버그임. (감사 보고의 "dev 모드 가짜 경고" 세부 주장은 부정확하나 — TryGetTextFromKey는 per-call 경고를 안 냄 — 실제 dev 모드 증상은 더 심각함.) 릴리스 영어 빌드에선 키를 그대로 반환해 우연히 동작하지만, 단어 라벨이 실제 번역키와 충돌하면 엉뚱한 치환도 발생. DESIGN_NOTES의 의도적 패턴 목록에 해당 없음. 제안된 수정(.Translate() 제거)이 정확. 핫패스 아님(기즈모 UI)이나 정정 무관하게 correctness 버그.

- [ ] **[LOW / bug]** SpawnRevertDrops: stackCount가 thingDef.stackLimit를 초과하면 단일 Thing에 과도한 스택 설정
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.cs:657-670`
  - Detail: SpawnRevertDrops L666-667 `Thing thing = ThingMaker.MakeThing(entry.thingDef); thing.stackCount = entry.count;` 후 GenPlace.TryPlaceThing으로 배치합니다. entry.count가 thingDef.stackLimit(예: 75)를 초과하면 stackCount가 한계를 넘는 비정상 Thing이 됩니다. GenPlace.TryPlaceThing은 초과분을 분할 배치하려 시도하지만, 단일 MakeThing 결과의 stackCount를 직접 limit 초과로 설정하는 것은 바닐라 규약 위반이며 일부 경로에서 아이템 유실/경고를 유발할 수 있습니다.
  - Fix: entry.count를 stackLimit 단위로 분할 루프 돌며 여러 Thing 생성, 또는 GenSpawn.SpawnThingDef류 헬퍼 사용. 최소한 `thing.stackCount = Mathf.Min(entry.count, entry.thingDef.stackLimit)` 후 잔량 반복 배치.

- [ ] **[LOW / design]** ResolveGearFromIds가 첫 CompPostTick에서만 실행 — 로드 직후 비스폰(캐러밴 상단 등) 상태면 맵 탐색 실패
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.ExposeData.cs:258-306`
  - Detail: ResolveGearFromIds(Tick.cs L49-52에서 needsGearResolve 시 1회 호출)는 L260에서 즉시 needsGearResolve=false로 끄고, pawn.Map이 null이면 캐러밴 인벤토리만 탐색합니다. 로드 직후 폰이 비스폰(WorldPawn/포드 이동 중)이고 캐러밴에도 없으면 prevApparels/Weapons 복원에 실패하고 L300-303 경고만 남긴 뒤 tmpPrevApIds/WpIds를 null로 폐기합니다. 이후 스폰돼도 재시도가 없어, 변신 해제 시 이전 장비 재착용이 영구 불가합니다.
  - Fix: 맵·캐러밴 모두에서 못 찾았고 폰이 비스폰이면 needsGearResolve를 유지(또는 tmpPrevIds 보존)하여 스폰 후 OnPawnSpawned/다음 Tick에서 재시도. 또는 OnPawnSpawned(respawningAfterLoad)에서 needsGearResolve=true 재설정.

- [ ] **[LOW / design]** sustain 실패 시 Stance_Warmup이면 해제를 무기한 연기 — 영구 사용자 잠금 가능
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.Tick.cs:78`
  - Detail: CompPostTick L78 `if (!CheckSustainConditions(...) && !(pawn.stances?.curStance is Stance_Warmup))` — sustain 조건이 깨졌어도 폰이 공격 워밍업 중이면 RemoveForm을 건너뜁니다. 의도(공격 모션 중 갑작스런 해제 방지)는 이해되나, 워밍업이 반복/지속되는 상황(연속 사격 등)에서는 sustain 조건이 영구 위반된 채 변신이 유지될 수 있습니다. 다음 60틱 검사 때 또 워밍업이면 계속 연기됩니다.
  - Fix: 워밍업으로 인한 연기 횟수/누적 틱에 상한을 두거나, '연기됨' 플래그를 세워 워밍업 종료 직후 1회 강제 재검사. 의도적 설계라면 DESIGN_NOTES에 근거 추가 권장.

- [ ] **[LOW / bug]** TickAmbientVfx에서 pawn.Map/PositionHeld 분기 불일치로 NRE 여지
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.Tick.cs:145-151`
  - Detail: L134 가드는 `!pawn.Spawned`만 검사하고 return합니다. Spawned면 pawn.Map은 non-null이므로 FleckMaker.Static(pawn.DrawPos, pawn.Map, ...) (L145)은 안전합니다. 그러나 ambientSound 재생(L150)은 `new TargetInfo(pawn.PositionHeld, pawn.MapHeld)`를 사용합니다. Spawned 폰의 PositionHeld/MapHeld는 정상이지만, ambientEffecter 분기(L135-140)는 pawn 자신을 타겟으로만 쓰므로 일관성이 어긋납니다. 실질 NRE 위험은 낮으나 Fleck은 DrawPos/Map, Sound는 PositionHeld/MapHeld로 좌표 소스가 갈려 향후 비Spawned 허용 변경 시 깨질 수 있습니다.
  - Fix: 좌표 소스를 Spawned 가드 하에서 통일(둘 다 pawn.DrawPos/pawn.Map 또는 PositionHeld/MapHeld). 현재는 Spawned 가드로 안전하므로 일관성/가독성 차원의 정리.

- [ ] **[LOW / bug]** ExposeData 필드 순서: hasSavedColors가 originalBodyType/HeadType Scribe_Defs.Look 사이가 아닌 뒤에 위치 — 부분 백업 로드 시 컬러 미적용
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.ExposeData.cs:25-31`
  - Detail: Saving 시 originalHairColor/SkinColor는 hasSavedColors와 독립적으로 hasOriginalHairColor/SkinColor 플래그(L35,42)로 저장됩니다. 그러나 RestoreAppearance(core.cs L639) 는 `if (hasSavedColors)` 일 때만 컬러를 복원합니다. ApplyForm은 hasSavedColors와 컬러를 한꺼번에 세팅하므로 정상 흐름에선 동기화되지만, OnPawnSpawned(core.cs L242-251)는 form.hairColor/skinColor만 재적용하고 originalHairColor를 사용하지 않습니다. 변신 중 저장→로드 시 originalSkinColor=null이고 hasSavedColors=true면 RestoreAppearance가 skinColorOverride=null로 정상 복원하지만, hasOriginalSkinColor=false인데 hasSavedColors=true인 엣지(외부 모드가 변신 전 skinColorOverride=null이었던 경우)는 originalSkinColor=null로 저장/로드되어 복원 시 null 대입 — 이는 의도와 일치합니다. 정합성은 유지되나 hasSavedColors와 hasOriginalXColor 두 플래그의 의미 중첩이 혼동을 유발.
  - Fix: hasSavedColors 하나로 컬러 백업 유무를 표현하거나, 컬러별 has 플래그만 사용하도록 단일화. 현 구현은 정합하나 두 플래그 동기화 가정이 깨질 경우(다른 코드가 hasSavedColors만 토글) 컬러 복원이 어긋날 수 있어 방어 코멘트 추가 권장.

- [ ] **[LOW / perf]** YieldVerbGizmos: 인스턴스 _tmpSeenVerbs를 yield 이터레이터 본문에서 사용 — 재진입 시 손상 가능 (실사용은 안전)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.Gizmos.cs:62, 88`
  - Detail: _tmpSeenVerbs는 인스턴스 필드(core.cs L88)로, YieldVerbGizmos(Gizmos.cs L62)에서 Clear 후 L70 Add로 채워집니다. GetGizmosExtra/YieldVerbGizmos는 지연 IEnumerable이므로, 동일 comp의 기즈모를 이전 열거가 끝나기 전 다시 열거하면 HashSet이 손상됩니다. RimWorld 기즈모 수집은 프레임당 순차·완전 소진이라 실무상 안전하지만, 'Command_Toggle.isActive/toggleAction 람다가 idx,v를 캡처'(L85-86)하는 패턴과 결합해 다중 선택 UI에서 미묘한 버그 소지가 있습니다.
  - Fix: _tmpSeenVerbs를 YieldVerbGizmos 내부 지역 변수(new HashSet 또는 [ThreadStatic])로 이동하거나, 중복 제거를 BuildVerbKeyCache처럼 사전 계산. GC 부담이 걱정이면 작은 List + 선형검사로도 충분(원거리 verb 수는 보통 소수).

### Hediffs-Other

- [ ] **[LOW / bug]** Gathered가 unspawned 폰에서 호출되면 Pawn.Map NRE 가능 (IsActive에 Spawned 검사 없음)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_Harvestable.cs:92-106, 154-179`
  - Detail: IsActive(line 95-106)는 pawn.Dead/Faction/Suspended/gender만 검사하고 Spawned는 보지 않습니다. Gathered(line 154-179)는 Pawn.Map(line 161,163)과 doer.Map(line 174)에 접근하는데, IsActive=true이지만 Spawned=false인 폰(캐러밴 중 동면 등)에서 호출되면 Pawn.Map이 null이 되어 MoteMaker.ThrowText/GenPlace에서 NRE 가능. 현재는 WorkGiver(HasJobOnThing→ActiveAndFull, CanReserve)가 spawned를 간접 보장하지만, Gathered는 public이라 다른 호출자가 생기면 깨집니다. AutoSpawnResource(line 140)는 !pawn.Spawned를 명시적으로 가드하는 것과 대조적입니다.
  - Fix: Gathered 진입부에 if (Pawn == null || !Pawn.Spawned || Pawn.Map == null) return; 가드를 추가하거나, IsActive에 pawn.Spawned 검사를 포함.

- [ ] **[LOW / design]** static Dictionary<Pawn,...> _cache — 폰 사망/폐기 시 엔트리 누수 가능 (CompPostPostRemoved에만 의존)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_Harvestable.cs:59-82, 181-201`
  - Detail: _cache(line 59)는 Pawn을 키로 강참조하며 등록은 CompPostPostAdd(line 184)·PostLoadInit(line 200), 해제는 CompPostPostRemoved(line 190)에서만 합니다. 폰 사망 시 hediff는 시체에 남아 CompPostPostRemoved가 즉시 불리지 않으므로(또는 폰이 완전 폐기되어 RemoveHediff 경로를 안 타는 경우) 죽은 Pawn 참조가 _cache에 잔류해 GC를 막을 수 있습니다. FinalizeInit의 ClearCache(ShapeshiftTransformFxRunner line 65)가 로드/맵전환 시 정리하므로 영향은 제한적이지만, 단일 세션 장기 플레이에서 점진 누수 여지가 있습니다. ShapeshiftRegistry와 동일 패턴이라 의도적일 수 있어 design으로 분류.
  - Fix: TryGetCached에서 조회 시 pawn.Discarded/Destroyed면 _cache.Remove 후 null 반환하는 lazy 정리, 또는 주기적(예: ClearAll 외) sweep. 또는 현행이 의도면 DESIGN_NOTES에 누수 허용 근거 명시.

- [ ] **[LOW / perf]** CompPostTick이 매 틱 fullness 누적 — IsHashIntervalTick 미사용 (의도적이나 확인 필요)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_Harvestable.cs:113-120`
  - Detail: CompPostTick(line 108)은 IsActive·간격 체크 없이 매 틱 1f/intervalTicks * BodyResourceGrowthSpeed를 누적합니다(line 114-120). PawnUtility.BodyResourceGrowthSpeed는 매 틱 호출돼도 가벼운 편이고, 매 틱 누적은 바닐라 CompHasGatherableBodyResource와 동일 패턴이라 정합성상 옳습니다. 다만 프로젝트 성능 규칙('Tick 무거운 로직은 IsHashIntervalTick(60) 이상')과 형식상 충돌하므로, N틱마다 N*growth를 누적하는 방식으로 바꾸면 호출 빈도를 줄일 수 있습니다. 바닐라 호환 유지가 목적이면 현행 유지가 타당하여 severity low.
  - Fix: 필요 시 if(!parent.pawn.IsHashIntervalTick(60)) return; 후 growthPerTick*=60 으로 변경. 단 바닐라 체득 곡선과 동일 유지가 목적이면 현행 유지 + 성능 규칙 예외로 주석 명시.

- [ ] **[LOW / doc]** 주석/문서가 OR만 설명 — AND(requireAllConditions) 모드 누락
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_AutoShift.cs:53`
  - Detail: CompPostTick line 53 주석('조건 판정 (OR: 하나라도 충족 시 트리거)')은 OR만 언급합니다. 실제 AnyConditionMet는 requireAllConditions로 AND/OR 둘 다 처리합니다(line 76, 159-163). AnyConditionMet의 XML doc(line 73)은 정확하지만 호출부 인라인 주석이 동작과 불일치하여 오해를 유발합니다.
  - Fix: line 53 주석을 'requireAllConditions에 따라 OR/AND 판정'으로 수정.

- [ ] **[LOW / bug]** SpawnThing — stuff 필요 ThingDef에서 ThingMaker.MakeThing 예외 가능 + 결과 null 미체크
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_PermanentTransform.cs:131-138`
  - Detail: SpawnThing은 ThingMaker.MakeThing(Props.thingDef)를 stuff 인자 없이 호출합니다(line 133). thingDef가 MadeFromStuff인 경우(무기/일부 가구 등) 바닐라 MakeThing은 예외 또는 경고를 발생시킬 수 있고, 모더가 임의 ThingDef를 지정할 수 있는 범용 프레임워크 특성상 입력이 신뢰되지 않습니다. ExecuteTransform 전체가 try-catch로 감싸여 있지 않아(SpawnAnimal/SpawnThing 호출부 line 83-90) 예외 시 CompPostTick이 throw되고, 이는 위 #1의 순회 컨텍스트에서 더 위험합니다.
  - Fix: thingDef.MadeFromStuff일 때 GenStuff.DefaultStuffFor(thingDef)로 stuff를 전달하거나 ConfigErrors에서 MadeFromStuff thingDef를 거부하세요. SpawnThing/SpawnAnimal 호출을 try-catch로 감싸 실패 시 Log.Warning("[SSF] ...") 폴백 후 안전 종료.

- [ ] **[LOW / convention]** HediffCompProperties_PermanentTransform에 ConfigErrors 검증 부재 (Harvestable과 비대칭)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffCompProperties_PermanentTransform.cs:43-52`
  - Detail: 동일 폴더의 HediffCompProperties_Harvestable은 ConfigErrors(line 43-52)로 resourceDef null, intervalTicks<=0, resourceAmount<=0를 검증합니다. 반면 PermanentTransform Props는 animalKind/thingDef 둘 다 null이거나 thingCount<=0인 경우를 검증하지 않습니다. 둘 다 null이면 CompPostTick line 53에서 조용히 return하여 모더가 오설정을 인지하지 못합니다. thingCount<=0이면 SpawnThing이 stackCount=0인 Thing을 만듭니다.
  - Fix: ConfigErrors(HediffDef) 오버라이드 추가: animalKind==null && thingDef==null이면 오류, thingCount<=0이면 오류, severityThreshold<=0 경고.

- [ ] **[LOW / design]** AND 모드에서 Spawned 필요 조건(밝기/전투)이 caravan 폰을 영구 차단
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_AutoShift.cs:121-157`
  - Detail: AnyConditionMet의 triggerSunGlowBelow(line 125: pawn.Spawned && pawn.Map != null)와 triggerInCombat(line 137: pawn.Spawned) 조건은 폰이 스폰되지 않은 상태(캐러밴/포드 이동 중)에서는 절대 passed++ 되지 않습니다. requireAllConditions=true(AND)일 때 이 조건이 defined에 포함되면 passed==defined가 영원히 거짓이 되어, 다른 조건(체력/severity)을 충족해도 변신이 발동하지 않습니다. OR 모드에서는 문제 없습니다.
  - Fix: 의도된 동작이면 FORMDEF_GUIDE에 'AND 모드 + 밝기/전투 조건은 맵 스폰 폰 전용'임을 문서화하세요. 아니면 unspawned일 때 해당 조건을 defined에서 제외(미평가)하도록 분기.

### Patches-Render-A

- [ ] **[LOW / convention]** Postfix 본문 try-catch / [SSF] 폴백 누락 (렌더 핫패스, 리플렉션성 SizeFactorResolver 호출 포함)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderer_BaseHeadOffsetAt.cs:15-44`
  - Detail: line 27 `ShapeshiftSizeFactorResolver.Effective(pawn)` 등 폼 데이터에 의존하는 연산을 수행하나 본문이 try-catch로 감싸이지 않았다. 규칙상 패치 본문은 try-catch + Log.Warning("[SSF]") 폴백을 둔다. 동일 영역의 GetDrawParms·GetBodyPos·HumanlikeMeshPoolUtility·Thing_DrawSize 패치도 모두 try-catch가 없다(레포 전반 ~50개 중 7개만 try-catch라 일괄 미적용 상태). 렌더 예외가 [SSF] 식별 없이 바닐라로 전파될 수 있다.
  - Fix: 최소한 SizeFactorResolver/폼 필드 접근 구간을 try-catch로 감싸고 catch에서 Log.Warning("[SSF] ...") 후 return. 일괄 적용이 부담되면 렌더 패치군에 공통 헬퍼(SafeRun)를 도입.

- [ ] **[LOW / doc]** 해시 알고리즘 주석 오기 — 17/31 방식인데 'DJB2 변형'으로 표기
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderer_DrawShadowInternal.cs:26`
  - Detail: line 26 주석 `// 소수 시드 + 31 곱 해시 (표준 DJB2 변형)` 인데, 실제 구현은 시드 17 + 31 곱(Josh Bloch/표준 .NET 합성 해시 패턴)이다. DJB2는 시드 5381 + 33 곱(또는 *31 아님, 비트시프트 기반)으로 별개 알고리즘이다. 동작에는 영향 없으나 한국어 주석 정확성 규칙상 오기.
  - Fix: 주석을 `(시드 17 + 31 곱 합성 해시, 표준 패턴)` 등으로 정정. 'DJB2' 표현 제거.

- [ ] **[LOW / convention]** catch에서 [SSF] 경고 로그 없이 무음 폴백 — 반복 실패 진단 불가
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderer_CachedFrameScaling.cs:75`
  - Detail: 리플렉션 Invoke 실패 시 line 75 `catch { return true; }` 로 바닐라 폴백하는데 로그가 전혀 없다. 정적 생성자(line 39-42)는 MethodInfo/FieldInfo가 null일 때만 1회 경고하므로, 그 둘이 non-null이지만 Invoke가 매 프레임 던지는(예: 시그니처 미세 변경, SetValue 타입 불일치) 경우 무음으로 매 렌더 폴백만 반복되어 원인 파악이 어렵다. 규칙은 catch에서 Log.Warning("[SSF] ...") 폴백을 요구한다.
  - Fix: catch (Exception ex) 로 받아 1회성 플래그(static bool _warned)로 가드한 뒤 `Log.WarningOnce("[SSF] CachedFrameScaling invoke failed: "+ex, 고정해시)` 출력. 매 프레임 로그 폭주는 WarningOnce/플래그로 방지.

- [ ] **[LOW / design]** 포트레잇에서 bodyOffset 평행이동이 portraitDrawScale로 스케일되지 않음 (오프셋·스케일 적용 순서)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderer_GetDrawParms.cs:36-47`
  - Detail: 섹션 B(line 43-46)는 `m.m03 += add.x; m.m23 += add.z;` 로 평행이동 성분을 먼저 더하고, 섹션 C(line 58-63)는 `m.m00 *= s; m.m11 *= s; m.m22 *= s;` 로 기저 벡터만 스케일한다. m03/m23(평행이동)은 C에서 곱해지지 않으므로, 포트레잇 스케일을 키워도 bodyOffset이 셀 단위 그대로 남아 폼이 커질수록 오프셋이 상대적으로 작아 보인다. 인게임(비포트레잇)에서는 portraitDrawScale 분기가 타지 않아 문제 없고, 포트레잇은 통상 angle=0·South라 시각 차이는 작다. 의도된 동작일 수 있어 low/design.
  - Fix: 포트레잇에서 오프셋도 함께 스케일하려면 C 블록에서 m03/m23에도 s를 곱하거나, 스케일을 먼저 적용한 뒤 오프셋을 더하도록 순서를 재배치. 의도라면 주석으로 '포트레잇 오프셋은 비스케일'임을 명시.

- [ ] **[LOW / design]** 포트레잇 스케일을 매트릭스 대각 성분만 곱함 — 회전(angle) 비0 시 비균일 왜곡 가능
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderer_GetDrawParms.cs:58-63`
  - Detail: C 블록은 `m.m00 *= s; m.m11 *= s; m.m22 *= s;` 로 대각 성분만 곱한다. GetDrawParms의 matrix가 TRS(회전 포함)일 때 회전이 있으면 스케일이 m01/m10 등 비대각 성분에도 분산되므로, 대각만 곱하면 균일 스케일이 되지 않는다. 포트레잇은 일반적으로 angle≈0이라 실무상 문제는 드물지만, 회전 포트레잇/특수 케이스에서 형태 왜곡 위험이 있다.
  - Fix: 정확한 균일 스케일은 `__result.matrix = m * Matrix4x4.Scale(new Vector3(s, s, s));` (로컬 스케일 후행 곱) 또는 회전 비0 케이스를 고려한 곱셈으로 대체. angle=0 전제를 코드 주석으로 명시하는 것도 차선.

- [ ] **[LOW / convention]** Postfix 본문 try-catch / Log.Warning("[SSF]") 폴백 누락 (렌더 핫패스 패치)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_DynamicDrawManager_DrawDynamicThings.cs:26-78`
  - Detail: 코드 규칙상 패치 본문은 try-catch로 감싸고 catch에서 Log.Warning("[SSF] ...") 폴백을 둔다. 이 Postfix는 line 70에서 DynamicDrawPhaseAt를 호출하지만 본문 전체가 try-catch로 감싸여 있지 않다(스냅샷 해제용 try-finally만 존재, catch 없음). 렌더 중 예외 발생 시 [SSF] 폴백 로그 없이 바닐라 드로우 루프로 예외가 전파될 수 있다. (CachedFrameScaling·WoundDrawer 등 리플렉션 패치에는 try-catch가 있어 핵심 케이스는 보호됨 — 본 건은 convention/low.)
  - Fix: try-finally를 try-catch-finally로 확장하거나, for 루프 본문을 try-catch로 감싸 catch에서 `Log.Warning("[SSF] DrawDynamicThings extra-draw failed: "+ex)` 후 계속/스킵하도록 보강.

### Patches-Render-B

- [ ] **[MEDIUM / bug]** TargetMethods()가 AccessTools.Method() 결과를 null 체크 없이 yield → PatchAll 전체 중단 위험
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderNode_GraphicFor_Parts.cs:22-31`
  - Detail: TargetMethods()는 7개 PawnRenderNode 서브클래스의 "GraphicFor"를 AccessTools.Method(...)로 찾아 그대로 yield return 한다. 컨벤션은 'AccessTools.Field()/Method() 결과 null 체크 필수'이며, HarmonyInit.cs는 harmony.PatchAll(...)을 try-catch 없이 호출한다(HarmonyInit.cs:19). 향후 RimWorld 버전이 이 클래스 중 하나에서 GraphicFor override를 제거/리네임하면 AccessTools.Method가 null을 반환하고, Harmony가 PatchAll 중 null MethodBase에서 예외를 던져 SSF의 '모든' 패치 적용이 중단된다. 현재 1.6 디컴파일 대조 결과 7개 모두 'public override Graphic GraphicFor(Pawn pawn)'이 실제로 존재하고 오버로드도 없어 지금은 정상 동작하지만(검증 완료), 버전 변화에 취약하다. 파일 헤더 주석 자체가 '1.6 DLL 대조 감사 완료'라고 명시하나 런타임 방어는 없다.
  - Fix: 각 yield 전에 null 체크: 'var m = AccessTools.Method(typeof(...), "GraphicFor"); if (m != null) yield return m; else Log.Warning("[SSF] GraphicFor not found on ...");' 형태로 변경하거나, 헬퍼로 묶어 null이면 경고 후 스킵하도록 한다.
  - Verify [high]: Code matches the claim exactly: Patch_PawnRenderNode_GraphicFor_Parts.cs lines 24-30 yield AccessTools.Method results with no null check, while code_conventions.md:27 mandates null-checking AccessTools.Method() and the sibling file Patch_Pawn_HealthTracker_DropBloodFilth.cs:38-43 applies that exact guard. I decompiled the runtime Harmony (Workshop 294100, Current = v2.4.1) and confirmed the failure mechanism end-to-end: PatchClassProcessor.GetBulkMethods() detects a null element (list2.Any(m => m == null)) and throws; Patch() re-throws via ReportException as HarmonyException; PatchAll(Assembly) iterates with DoIf -> Where().Do() (plain while(MoveNext()), no try-catch), and HarmonyInit.cs:19 calls PatchAll with no try-catch -- so one null aborts ALL remaining SSF patches from a StaticConstructorOnStartup. This is a latent robustness defect, not a current crash: all 7 types exist in Verse with public override Graphic GraphicFor(Pawn pawn) in 1.6 Assembly-CSharp (verified), so it works today; TargetMethods runs once at startup so the "render hot path / no null-check" note is irrelevant and the fix is free; and DESIGN_NOTES.md does not list this as an intentional pattern.

- [ ] **[LOW / perf]** 비변신 폰 short-circuit 부재 — 모든 Pawn에 대해 Effective() 프레임캐시 연산 수행
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_AttachPointTracker_GetRotatedOffset.cs:23-28`
  - Detail: Postfix는 pawn != null 확인 직후 곧바로 ShapeshiftSizeFactorResolver.Effective(pawn)을 호출한다(28행). 같은 폴더의 다른 렌더 패치들(Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner.cs:82, Patch_PawnRenderNodeWorker_Overlay_ScaleFor.cs:22)은 무거운 연산 전에 ShapeshiftRegistry.IsActive(pawn)로 O(1) 즉시 스킵하는데, 이 패치만 그 가드가 없다. Effective()는 ConcurrentDictionary.TryGetValue + (미스 시)TryAdd를 매 프레임 폰마다 수행하므로(ShapeshiftSizeFactorResolver.cs:84-90), 변신하지 않은 폰(특히 HoldingPlatform에 포획된 엔티티 등 GetRotatedOffset(type,rot) 경로)에 대해서도 캐시 삽입/조회 비용이 발생한다. 결과적으로 target==vanilla라 30행에서 no-op이 되지만 연산 자체는 낭비된다.
  - Fix: 23행 pawn null 체크 직후 'if (!ShapeshiftRegistry.IsActive(pawn)) return;'을 추가해 비변신 폰을 즉시 스킵한다(다른 렌더 패치와 일관).

- [ ] **[LOW / convention]** GetOrResolveOwner의 worker 파라미터 미사용 (dead parameter)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner.cs:39`
  - Detail: GetOrResolveOwner(PawnRenderNode node, PawnRenderNodeWorker worker)의 두 번째 인자 worker는 메서드 본문(40-71행)에서 한 번도 참조되지 않는다. 과거 owner 탐색을 worker 기반 리플렉션으로 하던 흔적이며, 현재는 node.gene/apparel/hediff public 필드 직접 접근(47-61행)으로 리팩터된 상태라 불필요하다. 호출부(85행)도 __instance를 넘기지만 의미 없다.
  - Fix: worker 파라미터를 제거하고 호출부 GetOrResolveOwner(node)로 단순화한다.

- [ ] **[LOW / convention]** 패치 Postfix 본문에 try-catch + Log.Warning("[SSF]") 폴백 누락
  - Loc: `Patches/ (담당 9개 파일 전체)`
  - Detail: CLAUDE.md 컨벤션은 '패치 본문은 try-catch로 감싸고 catch에서 Log.Warning("[SSF] ...") 폴백'을 요구한다. 담당 9개 파일 중 try-catch를 가진 파일은 0개다(Patches 폴더 전체 50개 중 26개가 try-catch 사용 — 부분 채택 상태). 다만 호출 대상 헬퍼(ShapeshiftReflectionCache, ShapeshiftRenderUtility, ShapeshiftRegistry, ShapeshiftVisualFilter)는 내부에서 예외를 삼키고 null/default를 반환하도록 방어적으로 작성되어 있어 실제 NRE/예외 표면은 작다. 그래서 severity는 low로 둔다.
  - Fix: 렌더 핫패스 성능을 고려하되, 최소한 리플렉션/캐스팅이 직접 일어나는 Patch_AttachPointTracker_GetRotatedOffset과 Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner는 try-catch로 감싸고 catch에서 Log.Warning("[SSF] ...")으로 폴백하도록 컨벤션을 맞춘다. 헬퍼가 이미 방어 처리한다면 DESIGN_NOTES.md에 '핫패스 패치는 헬퍼 내부 방어로 try-catch 생략' 근거를 명시해 컨벤션과 정합.

### Patches-Combat-Gear

- [ ] **[MEDIUM / convention]** 파일명과 클래스명/패치 대상 불일치 (1파일=1패치 네이밍 규칙 위반)
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Pawn_MeleeVerbs_TryGetUpdatedMeleeVerb.cs:21-22`
  - Detail: 파일명은 Patch_Pawn_MeleeVerbs_TryGetUpdatedMeleeVerb.cs 인데, 내부 클래스는 `internal static class Patch_Pawn_MeleeVerbs_TryGetMeleeVerb` 이고 패치 대상도 `[HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryGetMeleeVerb))]` 이다. 실제로 패치하는 메서드는 TryGetMeleeVerb 인데 파일명만 'TryGetUpdatedMeleeVerb'를 가리킨다. 같은 파일 헤더 1행도 'Patch_Pawn_MeleeVerbs_TryGetUpdatedMeleeVerb.cs'로 적혀 있어 헤더/클래스명/실제 대상이 모두 어긋난다. 코드 규칙(파일명=클래스명, 패치 대상 일치)을 위반하고, grep/유지보수 시 혼동을 유발한다.
  - Fix: 파일을 Patch_Pawn_MeleeVerbs_TryGetMeleeVerb.cs 로 rename하고 1행 헤더도 동일하게 맞춘다. csproj <Compile Include> 경로도 동기화한다. (협력 패치 헤더 주석의 'Patch_Pawn_MeleeVerbs_TryGetMeleeVerb' 표기는 이미 올바르므로 파일명만 맞추면 일관됨.)

- [ ] **[LOW / convention]** 착용 잠금 Prefix에 try-catch/[SSF] 폴백 부재 (Translate/Message 예외 전파 가능)
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Pawn_ApparelTracker_Wear.cs:18-37`
  - Detail: 코드 규칙은 '패치 본문은 try-catch로 감싸고 catch에서 Log.Warning("[SSF] ...") 폴백'을 요구한다. 이 Wear Prefix는 try-catch가 없어 L31 `"SSF_Message_CannotWear".Translate(pawn.Named("PAWN"))` 또는 Messages.Message 호출이 예외(번역키 누락/MessageTypeDefOf 문제)를 던지면 바닐라 Wear 호출자에게 전파된다. DESIGN_NOTES #12에서 ApplyForm의 Messages.Message는 try-catch로 감싸도록 한 선례와도 어긋난다. AddEquipment 패치(Patch_Pawn_EquipmentTracker_AddEquipment.cs L18-36)도 동일하게 try-catch가 없다.
  - Fix: 두 Prefix 본문을 try-catch로 감싸고 catch에서 Log.Warning("[SSF] Wear/AddEquipment Prefix failed: ...") 후 return true(바닐라 계속)로 폴백. 최소한 Messages.Message 호출만이라도 try-catch로 보호.

- [ ] **[LOW / design]** Hide(Prefix return false)와 Scale(Transpiler)가 동일 메서드를 패치 — 우선순위 미지정
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_Hide.cs:15-26`
  - Detail: DrawEquipmentAiming에는 같은 폴더의 두 패치가 동시에 걸린다: 이 파일의 Prefix(숨김 시 return false로 본문 스킵)와 Patch_PawnRenderUtility_DrawEquipmentAiming_Scale.cs의 Transpiler(본문 IL 수정). Hide가 false면 transpiled 본문은 실행되지 않으므로 기능적 충돌은 없으나, 두 패치 모두 HarmonyPriority가 없어 Prefix 실행 순서/타 모드와의 상호작용이 명시되지 않았다. 동일 메서드 다중 패치는 회귀 위험이 있어 의도를 코드로 못박는 편이 안전하다.
  - Fix: 두 파일 중 하나의 헤더에 '동일 메서드 협력 패치(Hide Prefix + Scale Transpiler)' 관계를 명시하고, 필요 시 Hide Prefix에 명시적 HarmonyPriority를 부여(예: 다른 가시성 모드보다 먼저/나중). 기능 회귀 테스트를 TEST_CHECKLIST에 추가.

- [ ] **[LOW / bug]** 영구(무한) 변신 폼에서 cost>0이어도 차감되지 않으나 디버그 로그는 'deducted' 표기
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Verb_TryCastShot_DurationCost.cs:58-76`
  - Detail: L59 `core.ExtendDuration(-cost, true)` 호출 시, ExtendDuration은 ResolvedDurationTicks가 없거나 <=0이면(영구 변신) 즉시 return하여 실제로 아무것도 차감하지 않는다(HediffComp_ShapeshiftCore.cs L131-133). 그런데 L74 디버그 로그는 무조건 `deducted {cost} ticks`로 출력하고, entropy는 별도로 항상 적용된다. 기능상 치명적이진 않으나, durationCostTicks를 설정한 폼이 영구 변신이면 비용이 조용히 무시되어 모더가 의도와 다른 결과를 디버깅하기 어렵다.
  - Fix: 영구 변신이면 durationCostTicks가 적용 불가함을 ExtendDuration 반환값(차감된 실제 틱)이나 RemainingShapeshiftTicks 변화로 감지해 로그에 반영하거나, FORMDEF_GUIDE에 'durationCostTicks는 유한 시간 폼에만 적용'을 명시. 최소한 로그 문구를 실제 차감 여부에 맞게 조정.

- [ ] **[LOW / convention]** 파일명(TryCastShot)과 실제 패치 클래스/대상(TryCastNextBurstShot) 불일치
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Verb_TryCastShot_DurationCost.cs:17-18`
  - Detail: 파일명은 Patch_Verb_TryCastShot_DurationCost.cs 이고 1행 헤더도 동일하나, 실제 클래스는 `Patch_Verb_TryCastNextBurstShot_DurationCost` 이고 패치 대상도 `[HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]` 이다. 헤더 주석 L6에 'TryCastShot()은 abstract라 패치 불가 → TryCastNextBurstShot() 패치'라고 사유가 명시되어 의도는 분명하지만, 파일명/클래스명이 어긋나 네이밍 규칙(파일명=클래스명) 위반이다.
  - Fix: 클래스명에 맞춰 파일명을 Patch_Verb_TryCastNextBurstShot_DurationCost.cs 로 rename(csproj 동기화). 사유 주석은 헤더에 유지.

- [ ] **[LOW / bug]** burstShotsLeft 비교가 발사 성공을 오판할 수 있는 엣지(__state 기본값/리로드 경로)
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Verb_TryCastShot_DurationCost.cs:32`
  - Detail: Postfix는 L32 `if (___burstShotsLeft >= __state) return;`로 '감소했으면 성공'을 판정한다. Prefix가 항상 `__state = ___burstShotsLeft`를 세팅하므로 정상 흐름에서는 동작한다. 다만 TryCastNextBurstShot 내부에서 발사 성공과 무관하게 burstShotsLeft가 0으로 리셋되는 경우(예: warmup 취소/타깃 소실로 버스트 중단)에도 'state>현재'가 성립해 비용이 차감될 수 있다. 핵심 게임플레이 비용(durationCostTicks/entropy) 차감이라 오판 시 변신 시간이 부당하게 깎인다.
  - Fix: 가능하면 발사 성공을 더 확실히 판별(예: 정확히 1 감소 `__state - ___burstShotsLeft == 1`)하거나, 바닐라 TryCastNextBurstShot가 false 반환/중단 시 burstShotsLeft 변화를 디컴파일로 재확인 후 조건을 보강. 현재 `>=` 비교의 가정(중단 시 감소 안 함)을 헤더 주석에 근거와 함께 남길 것.

- [ ] **[LOW / design]** 툴팁 계산 경로에서 verb.caster를 영구 변경하는 부작용
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_TooltipUtility_ShotCalculationTipString.cs:49`
  - Detail: L49 `if (v.caster == null) v.caster = sel;`는 마우스오버 툴팁(ShotCalculationTipString Postfix, 매 프레임 호출 가능)에서 폼 VerbTracker의 verb 상태(caster)를 직접 변경한다. ShapeshiftVerbTracker getter는 init 시 caster를 pawn으로 세팅하므로(Verbs.cs L78-82) 보통 null이 아니지만, 어떤 경로로 caster가 비어 있으면 렌더/툴팁 경로에서 게임플레이 상태를 변조하게 된다. 읽기 전용이어야 할 툴팁 계산이 부작용을 갖는 설계 위험.
  - Fix: caster가 null이면 verb.caster를 영구 대입하는 대신, HitReportFor에 넘길 임시 caster만 지역 변수로 사용하거나(예: `var caster = v.caster ?? sel;` 후 ShotReport.HitReportFor(caster, v, target)), null이면 picked=null로 스킵. 영구 상태 변경은 피한다.

### Patches-Behavior-A

- [ ] **[MEDIUM / convention]** 파일명·헤더가 실제 패치 메서드(AddIngestionEffects)와 불일치
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Toils_Ingest_ChewIngestible.cs:1-14`
  - Detail: 파일명 Patch_Toils_Ingest_ChewIngestible.cs, 헤더 1행도 동일하나 실제 [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.AddIngestionEffects))]이고 클래스명은 Patch_Toils_Ingest_AddIngestionEffects. ChewIngestible 메서드는 패치하지 않음. 'Patch_클래스명_메서드명.cs' 규칙 및 헤더-파일명 일치 규칙 위반.
  - Fix: 파일명·헤더 1행을 Patch_Toils_Ingest_AddIngestionEffects.cs로 변경해 클래스명과 일치시키고 csproj <Compile Include> 동기화.

- [ ] **[MEDIUM / convention]** 파일명·클래스명·헤더가 실제 패치 대상과 불일치
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Thing_TakeDamage.cs:1-22`
  - Detail: 파일명은 Patch_Thing_TakeDamage.cs, 헤더 1행도 동일하지만, 실제 TargetMethod()은 `AccessTools.Method(typeof(DamageWorker_AddInjury), "ApplyToPawn")`이고 클래스명은 Patch_DamageWorker_AddInjury_SourceLabel이다. Thing.TakeDamage는 전혀 패치하지 않는다. code_conventions.md의 '파일 1개=패치 1개, 파일명: Patch_클래스명_메서드명.cs' 및 헤더 1행 '[파일].cs' 규칙 위반이며, 향후 유지보수 시 대상 메서드 오인 위험.
  - Fix: 파일명·헤더 1행을 Patch_DamageWorker_AddInjury_ApplyToPawn.cs(또는 _SourceLabel)로 통일하고 클래스명과 일치시킬 것. csproj <Compile Include>도 동기화.

- [ ] **[LOW / convention]** TargetMethod의 AccessTools.Method 결과 null 체크 누락
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_DamageWorker_AddInjury_PlayWoundedVoiceSound.cs:18-22`
  - Detail: TargetMethod()이 `AccessTools.Method(t, "PlayWoundedVoiceSound", ...)`를 null 체크 없이 반환. private 메서드명 문자열 기반 조회라 vanilla 변경 시 null 가능 → Harmony가 패치 등록 시점에 예외. code_conventions.md 'AccessTools.Method() 결과 null 체크 필수' 위반. 같은 폴더 DropBloodFilth 패치는 TargetMethods에서 null 가드를 함.
  - Fix: 조회 결과가 null이면 Log.Warning("[SSF] PlayWoundedVoiceSound not found — vanilla version mismatch?") 후 패치 스킵(TargetMethods yield 패턴 또는 null 반환 가드).

- [ ] **[LOW / convention]** TargetMethod의 AccessTools.Method 결과 null 체크 누락
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_FilthMaker_TryMakeFilth.cs:18-31`
  - Detail: TryMakeFilth(7-파라미터 오버로드)를 문자열+시그니처로 조회 후 null 체크 없이 반환. vanilla 시그니처 변경(예: FilthSourceFlags 파라미터 추가/제거) 시 null → Harmony 등록 예외. 'AccessTools.Method() 결과 null 체크 필수' 위반.
  - Fix: 조회 결과 null이면 Log.Warning("[SSF] ...") 후 패치 스킵. Prefix는 ref ThingDef만 다루므로 본문 예외 위험은 낮아 try-catch 우선순위는 낮음.

- [ ] **[LOW / bug]** sourceLabel 교체 Postfix가 기존(이전 타격)의 부상까지 소급 재라벨 (was high)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Thing_TakeDamage.cs:42-48`
  - Detail: Postfix는 ApplyToPawn 호출 시마다 pawn.health.hediffSet.hediffs '전체'를 역순 순회하며 `injury.sourceLabel == attacker.def.label`인 모든 Hediff_Injury의 sourceLabel을 formLabel로 덮어쓴다. 그러나 vanilla(디컴파일 Verse/DamageWorker_AddInjury.FinalizeAndAddInjury)는 맨손 공격 시 `sourceLabel = dinfo.Weapon?.label ?? ""` = 종족 def label(예 "human")을 넣는다. 따라서 동일 종족(예 다른 평범한 인간)에게 과거에 물린 기존 부상의 sourceLabel도 "human"이라, 변신 폰이 같은 피해자를 나중에 때리면 그 피해자의 '과거' 상처 라벨까지 폼 이름으로 바뀐다. 매칭이 '이번 타격으로 추가된 부상'으로 한정되지 않고 라벨 문자열 전역 매칭이라 발생하는 정합성 버그.
  - Fix: 이번 ApplyToPawn에서 새로 추가된 부상만 대상으로 한정할 것. 예: Prefix에서 hediffs.Count를 ___기록하거나 dinfo로 식별, 또는 Postfix에서 가장 최근 추가분(추가된 개수만큼 끝에서부터)만 검사. 최소한 sourceDef==attacker.def && Part==dinfo.HitPart 등 추가 조건으로 매칭 범위를 좁혀 기존 상처 오염을 방지.
  - Verify [high]: 코드(L42-48)는 주장대로 이번 타격 부상으로 한정하지 않고 pawn.health.hediffSet.hediffs '전체'를 순회하며 sourceLabel==attacker.def.label 인 모든 Hediff_Injury의 sourceLabel을 formLabel로 덮어쓴다(HitPart/sourceDef 등 추가 가드 없음). 바닐라가 확인됨: Verb_MeleeAttackDamage L47에서 맨손 시 source=CasterPawn.def, DamageWorker_AddInjury L201에서 sourceLabel=Weapon?.label("human")로 설정되므로, 같은 종족이 과거에 낸 맨손 상처(또는 변신 전 같은 폰의 상처)도 동일 라벨이라 소급 재라벨된다 — 정합성 버그 사실. 다만 sourceLabel은 건강탭 툴팁에만 쓰이는 순수 표시용 문자열로 게임플레이/스탯/세이브 영향이 전혀 없고, DESIGN_NOTES 의도 패턴도 아니며(오히려 주석이 코드와 반대로 '이번 타격만'이라 명시), 영향이 미미하고 일시적인 시각 오라벨에 그치므로 심각도를 high→low로 하향한다.

- [ ] **[LOW / convention]** AccessTools.Method 결과 null 체크 누락 + try-catch 폴백 없음
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Thing_TakeDamage.cs:19-22`
  - Detail: TargetMethod()이 `AccessTools.Method(typeof(DamageWorker_AddInjury), "ApplyToPawn")`를 null 체크 없이 반환한다. code_conventions.md는 'AccessTools.Method() 결과 null 체크 필수' 및 '모든 패치는 try-catch로 감싸고 Log.Warning("[SSF] ...") 폴백'을 규정. 같은 폴더 Patch_Pawn_HealthTracker_DropBloodFilth는 TargetMethods()에서 null 가드를 하지만 본 파일은 누락. ApplyToPawn은 protected라 시그니처가 안정적이라 NRE 위험은 낮으나 규칙 미준수.
  - Fix: TargetMethod()에서 null이면 Log.Warning("[SSF] ...") 후 패치 스킵(또는 TargetMethods로 yield 가드), Postfix 본문을 try-catch로 감싸 폴백 로깅 추가.

- [ ] **[LOW / perf]** 모든 섭취 폰에 대해 사운드 델리게이트 무조건 재할당(비변신 포함)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Toils_Ingest_ChewIngestible.cs:16-38`
  - Detail: Postfix가 조기 탈출(폼 보유 여부 선검사) 없이 모든 chewer에 대해 toil.PlaySustainerOrSound(...)로 클로저 델리게이트를 매번 새로 생성·할당하여 vanilla 델리게이트를 덮어쓴다. 비변신 폰(게임 내 대다수 식사 토일)도 매 식사 토일마다 클로저 할당이 발생. 동작은 vanilla와 동치(디컴파일 Toils_Ingest.AddIngestionEffects의 PlaySustainerOrSound 로직 재구현 일치 확인)이나 불필요한 할당. 다만 토일 생성은 매 틱 핫패스가 아니라 1회성이므로 영향은 경미.
  - Fix: 선택: ShapeshiftCoreUtility.TryGetCore(chewer, ...)로 폼 보유가 확인될 때만 델리게이트를 교체하고, 비변신이면 vanilla 델리게이트를 그대로 둬 할당을 회피. 단 1회성 경로라 우선순위는 낮음.

- [ ] **[LOW / convention]** 패치 본문 try-catch/Log.Warning 폴백 누락
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Toils_Ingest_ChewIngestible.cs:16-38`
  - Detail: code_conventions.md '모든 패치는 try-catch로 감싸고 Log.Warning("[SSF] ...") 폴백' 규칙에 대해 Postfix 본문에 try-catch가 없다. 본문은 toil.actor.CurJob.GetTarget(...) 등 RimWorld API를 직접 호출하므로 예외 가능. (폴더 50개 패치 중 약 절반이 미준수라 광범위한 갭이나 본 파일은 규칙 대상.)
  - Fix: PlaySustainerOrSound에 전달하는 람다 내부 또는 Postfix 전체를 try-catch로 감싸 실패 시 Log.Warning("[SSF] ...").

### Patches-Behavior-B

- [ ] **[LOW / design]** SortGenes Prefix가 호출자 소유 리스트를 영구 변형(RemoveAll)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_GeneUtility_SortGenes.cs:15-20`
  - Detail: GeneUtility.SortGenes는 반환형 void로 전달된 List<Gene>를 in-place 정렬함(리플렉션 확인). Prefix에서 genes.RemoveAll(g => g==null || g.def==null || g.def.displayCategory==null)을 같은 리스트에 수행하므로, 호출자가 넘긴 라이브 컬렉션에서 엔트리가 영구 삭제됨. 제거 대상이 손상/유령 유전자(null·def null)에 한정되고 헤더 주석 L2-3에 의도가 명시돼 있어(바닐라 정렬 NRE 방어) 정상 데이터는 영향 없지만, '정렬 보조' 패치가 입력 컬렉션을 파괴적으로 수정한다는 점은 호출 맥락에 따라 부작용 위험이 있음.
  - Fix: 현 동작이 의도라면 그대로 두되, 헤더 주석에 '입력 리스트를 영구 변형함(호출자 리스트에서 손상 엔트리 제거)'을 명시. 더 보수적으로 가려면 손상 엔트리 존재 시에만 RemoveAll을 수행(이미 그러함)하고, 정상 케이스(corrupt 없음)에서는 RemoveAll 자체를 건너뛰도록 사전 스캔으로 불필요한 변형/할당을 회피.

- [ ] **[LOW / doc]** 주석과 코드 불일치: 'static HashSet 제거 / 로컬 할당' 이라면서 ThreadStatic 필드 사용
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Pawn_GetDisabledWorkTypes.cs:28-30`
  - Detail: L28 주석 '재진입 안전을 위해 static HashSet 제거, 호출 빈도가 낮아 로컬 할당 허용'이라고 적혀 있으나 바로 다음 줄 L29-30은 [System.ThreadStatic] private static HashSet<WorkTypeDef> _tmpExisting; 로 '로컬 할당'이 아니라 '스레드 정적 필드 재사용'을 함(L51-52에서 null이면 new, 아니면 Clear). 주석이 실제 구현(ThreadStatic 재사용)과 반대로 기술돼 향후 유지보수자가 혼동할 수 있음. 동작 자체는 DESIGN_NOTES #8(ThreadStatic 임시 컬렉션) 철학과 일치하므로 코드는 정상.
  - Fix: 주석을 실제 구현에 맞게 수정: 예) '재진입/병렬 안전을 위해 ThreadStatic HashSet 재사용 — 매 호출 GC 할당 회피'. 또는 의도가 정말 로컬 할당이라면 필드 대신 메서드 내 지역 변수로 변경(단 현재 ThreadStatic 재사용이 성능상 더 우수).

- [ ] **[LOW / convention]** 패치 클래스 접근제한자 internal/public 혼용 (문서 규칙은 internal static class)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches:각 클래스 선언부`
  - Detail: code_conventions.md L18 '클래스명: Patch_클래스명_메서드명 (internal static class)'로 명시. 담당 파일 중 Patch_GeneUIUtility_DrawGene(L17), Patch_GeneUtility_SortGenes(L13), Patch_Pawn_DoKillSideEffects(L16), Patch_Pawn_GetDisabledWorkTypes(L14·L23), Patch_Pawn_GetReasonsForDisabledWorkType(L14), Patch_ThoughtWorker_Precept_GroinUncovered(L18)는 public static class. 반면 Patch_Pawn_CallTracker_DoCall/Patch_Pawn_Kill/Patch_Pawn_PathFollower.../Patch_Pawn_SpawnSetup은 internal static로 규칙 준수. 단 레포 전체 50개 패치 중 약 절반이 public이라 이는 프로젝트 전반의 기존 불일치이며 기능 영향은 없음(Harmony는 둘 다 패치 가능).
  - Fix: 일괄적으로 internal static class로 통일하거나, public을 허용하는 것으로 code_conventions.md L18을 갱신해 문서와 코드를 일치시킬 것. 기능 변경은 불필요.

- [ ] **[LOW / convention]** 패치 본문 try-catch/Log.Warning("[SSF]") 폴백 누락 (이동 핫패스)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Pawn_PathFollower_TryEnterNextPathCell.cs:15-44`
  - Detail: code_conventions.md L20 '모든 패치는 try-catch로 감싸고 Log.Warning("[SSF] ...") 폴백' 규칙이 명시돼 있으나 Prefix/Postfix 모두 try-catch가 없음. 이 패치는 Pawn_PathFollower.TryEnterNextPathCell — 폰이 한 칸 전진할 때마다 호출되는 경로이므로, GetTerrain/ShouldRun/TryGetBodySwimmingReplacementPath 중 어디서든 예외가 나면 폰 이동 전체가 Harmony에 의해 중단/스팸 로그가 될 수 있음. 동일 로직의 MapComponent 버전(ShapeshiftWaterTileGraphicsDirty.IsOnWaterTile L92-101)은 try-catch로 GetTerrain을 방어하는데 이 패치는 같은 GetTerrain 호출(L22, L39)을 무방어로 둠 — 방어 수준이 비대칭.
  - Fix: Prefix/Postfix 본문을 try-catch로 감싸고 catch에서 Log.Warning("[SSF] TryEnterNextPathCell water-graphics hook error: ...") 후 안전 복귀(Prefix는 __state=false 유지). 최소한 GetTerrain 호출을 ShapeshiftWaterTileGraphicsDirty.IsOnWaterTile와 동일하게 try-catch로 감쌀 것.

- [ ] **[LOW / convention]** 로드 시 전 폰 대상 Postfix에 try-catch 폴백 누락
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Pawn_SpawnSetup.cs:21-39`
  - Detail: code_conventions.md L20의 try-catch/Log.Warning("[SSF]") 폴백 규칙 미적용. Pawn.SpawnSetup Postfix는 맵 로드 시 '모든' 폰에 대해 실행되며, core.OnPawnSpawned() 내부에서 그래픽/포트레이트 dirty 처리를 수행함(OnPawnSpawned 내부는 개별 try-catch가 있으나 hediffs 순회/OnPawnSpawned 진입 전 구간 L27-37은 무방어). 한 폰에서 예외가 나면 SpawnSetup 체인이 끊겨 다른 모드 Postfix/스폰 후처리가 누락될 수 있음.
  - Fix: Postfix 본문 전체를 try-catch로 감싸고 catch에서 Log.Warning("[SSF] SpawnSetup re-register error: ..."). 예외가 나도 바닐라/타 모드 스폰 흐름은 계속되도록 보장.

- [ ] **[LOW / convention]** Postfix 첫 줄 null 가드 없음 + try-catch 폴백 누락
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_ThoughtWorker_Precept_GroinUncovered.cs:30-34`
  - Detail: code_conventions.md L28 '패치 Prefix/Postfix 첫 줄에서 null 가드'와 L20 try-catch 규칙 미적용. Postfix(Pawn p, ref bool __result)가 p에 대한 명시적 null 가드 없이 바로 ShapeshiftRegistry.TryGet(p,...) 호출. 실제로 ShapeshiftRegistry.TryGet 내부가 pawn==null을 처리하므로 NRE는 나지 않지만(현재는 안전), 컨벤션상 'if (p == null) return;' 선두 가드가 빠져 있어 다른 패치들과 패턴이 어긋남.
  - Fix: 본문 첫 줄에 'if (p == null) return;' 추가. 사상 4종을 한 Postfix로 묶은 구조 자체는 TargetMethods() 명시 열거로 올바름(헤더 주석 L4-5의 Harmony2 merge 주의와 일치).

### Utilities-State

- [ ] **[LOW / convention]** 클래스 본문 주석/로그가 영어 — 한국어 주석 규칙 위반(ShapeshiftDiagnostics.Info 메시지 다수)
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftApplyHediffUtility.cs:1-12`
  - Detail: 코드 규칙상 주석은 한국어, RimWorld API 용어만 영어다. 그러나 본 파일의 진단 로그 문자열이 영어 문장으로 작성돼 있다. 예: line 42 `Skip addedPart on FullBody`, line 61 `Apply: +{applied} hediff(s)`, line 119 `Skip non-added on missing part`, line 134 `Update existing (not tracked)`, line 304 `Cleanup null-part hediff`, line 314 `Cleaned up literal null hediffs and dirtied cache.`. 이는 사용자/모더가 보는 디버그 로그이며 한국어 주석/문구 규칙과 어긋난다. (Log.Warning의 [SSF] 폴백 형식 자체는 규칙 준수.)
  - Fix: ShapeshiftDiagnostics.Info에 전달하는 한글화 가능한 설명 문구를 한국어로 통일(API 용어 addedPart/hediff/FullBody 등은 유지). 프로젝트 전반 로그 언어 정책이 영어라면 CLAUDE.md 주석 규칙에 로그 문자열 예외를 명시.

- [ ] **[LOW / design]** _cleanupRemoveBuffer 정적 재사용 버퍼 — ApplyHediffEntries 재진입 시 충돌 가능
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftApplyHediffUtility.cs:265-308`
  - Detail: line 265 `_cleanupRemoveBuffer`는 static 단일 인스턴스이며 CleanupNullPartHediffs에서 Clear→수집→RemoveHediff(line 303) 루프에 쓰인다. RemoveHediff는 내부에서 다른 comp 콜백/이벤트를 트리거할 수 있고, 만약 그 경로에서 동일 유틸의 ApplyHediffEntries→CleanupNullPartHediffs가 재진입하면 같은 static 버퍼를 Clear/추가하여 바깥 루프의 인덱스가 깨질 수 있다. RimWorld 단일 스레드라 스레드 경합은 없으나 재진입(reentrancy) 위험은 남는다. 현재 호출 그래프상 재진입이 없다면 실측 무해.
  - Fix: 버퍼를 메서드 로컬 List로 바꾸거나(할당 1회/Cleanup), 재진입이 구조적으로 불가능함을 주석으로 단언할 것. 정적 재사용을 유지하려면 '재진입 금지' 가드 주석을 명시.

- [ ] **[LOW / convention]** IsMutantAllowed 주석과 실제 우선순위(블랙>화이트)가 코드 흐름과 어긋나 보임
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftEligibility.cs:53-69`
  - Detail: line 60-61 화이트리스트 검사가 먼저 실행되어 '화이트리스트에 없으면 즉시 false'이고, 그 뒤 line 64-66 블랙리스트 검사가 온다. 그런데 line 63 주석은 '블랙리스트: 화이트리스트보다 우선'이라고 적혀 있다. 실제로는 화이트리스트 미포함이 먼저 차단하므로 '블랙이 우선'이라는 주석과 코드 순서가 상충한다. 두 목록이 겹치지 않으면 결과는 같지만, 같은 mutantDef가 양쪽에 있을 때 코드는 화이트 통과 후 블랙 차단이라 결과적으로 차단(블랙 우선과 동일)된다 — 즉 주석 의도와 동작은 우연히 일치하나 흐름 설명이 오해를 부른다.
  - Fix: 블랙리스트 검사를 화이트리스트보다 앞에 배치해 주석('블랙 우선')과 코드 순서를 일치시키거나, 주석을 '화이트리스트 미포함 시 차단 → 이후 블랙리스트 차단(결과적으로 블랙 우선)'으로 수정.

- [ ] **[LOW / perf]** TryGetHolderPawn: 'pawn 필드 없음' 타입에 대해 매 프레임 GetField 재실행 (렌더 핫패스)
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftReflectionCache.cs:30-35`
  - Detail: line 31 `if (!HolderPawnField.TryGetValue(t, out fi) || fi == null)`는 캐시에 null을 저장(line 34 HolderPawnField[t]=fi)해도 다음 호출에서 fi==null 조건 때문에 t.GetField(...)를 다시 수행한다. 이 메서드는 Patch_PawnRenderUtility_DrawEquipmentAiming_Hide/_Scale Prefix(매 프레임, 장비당)에서 호출되는 렌더 핫패스다. ParentHolder 체인 중 'pawn' 필드가 없는 홀더 타입(Map 등)이 끼면 그 타입에 대해 매 프레임 리플렉션 GetField가 반복된다. 다른 캐시들(FieldNotFound 등)은 실패도 캐싱하는데 여기만 누락. 통상 장비는 pawn 필드를 가진 Pawn_EquipmentTracker가 직접 부모라 실측 영향은 작다.
  - Fix: 별도 'NotFound' 마커(예: typeof(void) 같은 센티넬 FieldInfo 대용 또는 ConcurrentDictionary<Type,bool> HolderPawnNotFound)를 두어 실패를 한 번만 기록하고 재탐색을 막을 것. 최소한 TryGetValue가 키를 보유하면(값이 null이어도) GetField 재시도를 스킵하도록 조건 분리.

- [ ] **[LOW / design]** IsAlreadyTransformed가 레지스트리만 의존 — 미등록/스테일 상태에서 false 위험
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftEligibility.cs:107-113`
  - Detail: IsAlreadyTransformed(line 111)는 ShapeshiftRegistry.TryGet만 사용한다. TryGet은 _reinitializing가 아닐 때 hediff 폴백을 하지 않으므로, 변신 core hediff가 실재하지만 레지스트리에 아직/더 이상 등록되지 않은 순간(등록 타이밍 갭, FinalizeInit 외 시점의 누락)에는 false를 반환한다. 같은 프로젝트의 ShapeshiftCoreUtility.TryGetCore(line 100-114)는 동일 조회에 hediff 폴백을 가진 것과 비대칭이다. 이 헬퍼는 약물/UI 진입점의 차단 판정(GetExtendDrugBlockReason 등)에 쓰여, 드물게 '변신 중인데 변신 안 함'으로 오판 가능. 레지스트리 등록 규약(파일 헤더)상 정상 경로에서는 채워지므로 실측 빈도는 낮다.
  - Fix: IsAlreadyTransformed도 TryGet 실패 시 ShapeshiftCoreUtility.TryGetCore 또는 동일 hediff 폴백으로 보강하거나, 두 조회 경로의 폴백 정책을 문서로 명시해 일관화할 것.

### Utilities-Render-FX

- [ ] **[MEDIUM / bug]** 공유 static object[] _args3/_args2/_args1 — 병렬 렌더 경로에서 데이터 레이스
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftFormDynamicPawnRenderNodeSetup.cs:112-163`
  - Detail: TryMakeNode가 생성자 Invoke 인자 배열을 static 필드 _args3/_args2/_args1(line 112-114)로 재사용한다. 호출부에서 `_args3[0]=pawn; _args3[1]=props; _args3[2]=tree;` 로 값을 채운 뒤 곧바로 `cached.ctor.Invoke(_args3)` 한다(line 138-162). 이 setup은 PawnRenderTree 노드 해석 경로(GetDynamicNodes)에서 호출되는데, 같은 파일의 _ctorCache가 ConcurrentDictionary로 선언된 점(line 108)과 DESIGN_NOTES.md(line 130-131 "RimWorld 1.6 ParallelPreRenderPawnAt에서 렌더 스레드가 병렬 실행")은 이 모드가 렌더 경로를 멀티스레드로 간주함을 보여준다. 두 스레드가 서로 다른 Pawn의 노드를 동시에 만들면 같은 _args3 배열에 교차 기록되어, A스레드의 pawn + B스레드의 props/tree가 섞인 채 Invoke될 수 있다(잘못된 노드 생성 또는 캐스팅 예외). 같은 파일이 _ctorCache는 동시성 보호하면서 인자 버퍼는 보호하지 않아 정합성이 어긋난다.
  - Fix: static 공유 배열 대신 메서드 지역 변수 `var args = new object[] { pawn, props, tree }` 를 쓰거나, 재사용이 꼭 필요하면 [ThreadStatic] static object[] 로 선언해 스레드별 독립 버퍼를 보장할 것. (DESIGN_NOTES 8번 [ThreadStatic] 패턴과 동일.) 멀티스레드 노출이 확실치 않다면 최소한 지역 배열로 바꾸는 것이 안전하며 GC 부담은 노드 생성 시점에만 발생해 무시 가능.
  - Verify [medium]: 코드 일치 확인: static 공유 _args3/_args2/_args1(L112-114)에 값 기록 후 즉시 ctor.Invoke(L139-162)하며, 같은 파일이 _ctorCache는 ConcurrentDictionary(L108)로 보호하면서 인자 버퍼만 비보호 — 정합성 불일치 실재. 병렬 노출도 디컴파일 1.6로 검증됨: GetDynamicNodes는 TrySetupGraphIfNeeded→SetupDynamicNodes 경로로만 호출되는데, PawnRenderTree.ParallelPreDraw(병렬 잡 스레드)의 TryGetMatrix→TrySetupGraphIfNeeded(PawnRenderTree.cs L228)로 도달 가능하고, SSF 자체 패치 Patch_PawnRenderer_ParallelPreRenderPawnAt_DisableCache가 [ThreadStatic] 버퍼까지 쓰며 병렬 스레드에서 disableCache=true로 ParallelPreDraw를 강제하므로 같은 모드가 렌더 경로를 멀티스레드로 명시 취급함. 다만 DynamicDrawManager.DrawDynamicThings(L196-202)는 EnsureInitialized 단계를 병렬 ParallelPreDraw(L253) 이전에 단일스레드로 먼저 실행해 정상 경로에선 트리가 이미 Resolved 상태라 TryMakeNode가 메인스레드에서만 돌고, 병렬 재진입은 노드 누락/재더티 엣지에서만 발생 — 매프레임 보장 크래시는 아니므로 medium 타당. DESIGN_NOTES #5는 생성자 폴백만 의도적 패턴으로 명시할 뿐 공유 static 인자 버퍼를 정당화하지 않으며, 동일 레포 패턴(#8 [ThreadStatic])과도 어긋나므로 무해 처리 불가.

- [ ] **[LOW / convention]** IsOnWaterTile의 빈 catch — [SSF] 폴백 로그 없이 예외 무음 처리
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftWaterTileGraphicsDirty.cs:90-101`
  - Detail: `catch { return false; }` (line 97-100)로 모든 예외를 삼킨다. 프로젝트 컨벤션은 예외 폴백 시 Log.Warning("[SSF] ...") 를 남기는 것인데(예: 같은 폴더 ShapeshiftTransformFxRunner.cs line 212, ShapeshiftFormDynamicPawnRenderNodeSetup.cs line 167) 여기서는 진단 로그가 전혀 없어, GetTerrain 관련 실제 버그가 발생해도 추적 불가하다. 매 틱이 아니라 60틱 간격 호출이므로 1회성 경고는 스팸도 아니다.
  - Fix: catch(Exception e) 로 받아 ShapeshiftDiagnostics 또는 Log.Warning("[SSF] WaterTile IsOnWaterTile failed: " + e) 한 줄 폴백을 추가(스팸 우려 시 1회/스로틀).

### Comps

- [ ] **[MEDIUM / bug]** TryRevokeAbility가 다른 아이템이 부여한 동일 AbilityDef도 무조건 회수 (다중 소스 충돌)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Comps/CompGiveAbility_Shapeshift.cs:76-81, 45, 86, 93`
  - Detail: TryRevokeAbility(pawn)는 pawn이 해당 AbilityDef를 보유하기만 하면 출처 검증 없이 pawn.abilities.RemoveAbility(Props.ability)를 호출한다(라인 79-80). 같은 AbilityDef를 부여하는 아이템 두 개(예: 어깨갑옷 + 무기)를 한 폰이 동시에 장비하고 그 중 하나를 해제/드롭/파괴하면, UpdateAbilityGrant(라인 45)·PostDeSpawn(라인 86)·PostDestroy(라인 93)에서 그 어빌리티를 통째로 제거하여, 아직 다른 아이템이 부여 중인데도 어빌리티가 사라진다. GainAbility 측도 GetAbility로 중복만 막을 뿐 참조 카운트가 없다(라인 72-73).
  - Fix: 회수 전 다른 장비/착용 아이템 중 동일 ability를 부여하는 CompGiveAbility_Shapeshift가 남아있는지 검사(같은 폰의 WornApparel/AllEquipmentListForReading 순회, 자기 parent 제외)한 뒤, 없을 때만 RemoveAbility 호출. 또는 부여 출처를 카운트/집합으로 추적.
  - Verify [high]: 코드가 주장과 라인 단위로 일치한다: TryRevokeAbility(L76-81)는 GetAbility(Props.ability)!=null만 확인하고 출처 검증 없이 RemoveAbility를 호출하며, UpdateAbilityGrant(L45)·PostDeSpawn(L86)·PostDestroy(L93)가 이를 호출한다. TryGrantAbility(L72-73)는 GetAbility로 중복 추가만 막아 어빌리티 인스턴스가 1개만 존재하고, RimWorld의 Pawn_AbilityTracker.RemoveAbility(AbilityDef)에는 참조 카운트가 없으므로 동일 AbilityDef를 부여하는 두 아이템(FORMDEF_GUIDE 5.5에 무기/착용 양쪽 부여가 공식 문서화됨, 형제 comp FindGrantingItem도 WornApparel+AllEquipment를 동시 순회해 다중 소스 상태를 전제) 중 하나만 해제/드롭/파괴해도 어빌리티가 통째로 사라진다. 게다가 생존 아이템의 UpdateAbilityGrant는 소유권 변경 시에만 재부여하므로(boundPawn이 이미 소유자와 일치하면 재부여 안 함) 복구도 신뢰할 수 없다. DESIGN_NOTES의 13개 의도적 패턴에 없고 핫패스 성능 이슈도 아닌 실제 정합성 버그다. 다소 드문 구성(동일 AbilityDef 2개 장비 아이템)이 전제이지만 발생 시 어빌리티가 조용히 소실되므로 medium 유지가 타당하다.

- [ ] **[LOW / bug]** CanBeUsedBy 안에서 Messages.Message 부수효과 발생 — 판정 메서드가 중복/조기 알림 유발 (was medium)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Comps/CompUseEffect_Shapeshift.cs:36-42`
  - Detail: CanBeUsedBy는 순수 판정(AcceptanceReport 반환)이어야 하나, 타겟터블 분기에서 라인 40에 Messages.Message("SSF_Message_AlreadyTransformed"...)를 띄우고 라인 41에서 같은 문자열을 reason으로 반환한다. 바닐라는 거부 사유(reason)를 FloatMenu/UI에 자체 표출하므로 사용자에게 동일 메시지가 두 번 노출된다. 또한 CanBeUsedBy는 FloatMenu 구성·hover 등에서 여러 번 호출될 수 있어, jobTarget이 이미 변신 상태인 동안 메뉴를 열 때마다 토스트가 반복 발생할 수 있다. (DoEffect 라인 91의 Messages.Message는 실제 실행 시점 1회이므로 정당.)
  - Fix: 라인 40의 Messages.Message 호출 제거. 거부 사유는 반환 AcceptanceReport 문자열(라인 41)만으로 충분하다.
  - Verify [high]: Code matches (CompUseEffect_Shapeshift.cs:40-41): a Messages.Message side-effect sits inside the pure CanBeUsedBy judgment method, which is improper, and DoEffect:91 already emits this exact message at the real use moment, making line 40 redundant — so the recommended removal is correct. However, the finding's rationale is largely wrong: this is a CompTargetable item, so targeting/hover goes through Targeter -> CompTargetable.ValidateTarget (Targeter.cs:203/266/445; CompTargetable.cs:130), never this override, and at FloatMenu-build time (CompUsable.cs:78) no UseItem job exists yet so pawn.CurJob.targetB is not the shapeshift target and line 40 cannot fire. The only site where line 40's condition holds is JobDriver_UseItem's per-tick FailOn (JobDriver_UseItem.cs:42), which discards the returned reason (checks only .Accepted), so no double-display ever occurs; the genuine but minor harm is that this FailOn can re-fire line 40 across ticks if the target transforms mid-job, and Messages.AcceptsMessage (Messages.cs:126-147) dedups the visible toast but still replays the RejectInput sound each repeat. The sibling CompUseEffect_ExtendShapeshift.CanBeUsedBy emits no message, confirming line 40 is an inconsistency rather than an intentional pattern (no DESIGN_NOTES entry covers it).

- [ ] **[LOW / design]** 자기 변신 시 sourceItems를 새 단일 리스트로 덮어써 다중 출처/기존 출처 손실 가능
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Comps/CompAbilityEffect_GiveHediff_Shapeshift.cs:126-133`
  - Detail: Apply에서 base.Apply가 hediff를 부여한 뒤(라인 124), 자기 캐스트일 때 FindGrantingItem 결과 1개를 core.sourceItems = new List<Thing>{ grantingItem }로 통째 교체한다(라인 132). FindGrantingItem은 첫 매칭 아이템만 반환하므로(라인 138-164), 동일 ability를 부여하는 아이템이 여럿이거나 다른 경로로 sourceItems가 이미 채워진 경우 정보가 유실된다. 신규 변신 진입에서는 보통 의도된 동작이라 design/low로 분류.
  - Fix: 다중 출처를 추적할 의도라면 교체 대신 추가(core.sourceItems에 Add, 중복 방지) 고려. 단일 출처가 설계 의도라면 DESIGN_NOTES에 명시. 확신 없으므로 severity 낮춤.

- [ ] **[LOW / perf]** parent.GetComp<CompTargetable>()가 CanBeUsedBy/DoEffect에서 매번 선형 탐색
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Comps/CompUseEffect_Shapeshift.cs:25, 57`
  - Detail: GetComp<T>()는 parent.AllComps를 순회하는 선형 탐색이다. CanBeUsedBy는 FloatMenu 구성·hover 시 빈번히 호출되며 라인 25·57에서 매 호출마다 CompTargetable을 재탐색한다. 핫패스는 아니지만 메뉴가 자주 갱신되는 상황에서 불필요한 반복 탐색이다.
  - Fix: 여기서는 영향이 작으므로 필수는 아니나, 빈번한 호출이 우려되면 첫 조회 결과를 인스턴스 필드에 1회 캐시(또는 TryGetComp 사용). 취향성에 가까우므로 우선순위 낮음.

- [ ] **[LOW / convention]** 클래스 XML doc summary 누락 (다른 Comps 클래스와 불일치)
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Comps/IngestionOutcomeDoer_Shapeshift.cs:1-12`
  - Detail: 동일 폴더의 다른 모든 Comp 클래스(CompUseEffect_Shapeshift, CompAbilityEffect_ShapeshiftDurationCost 등)는 클래스 선언 위에 /// <summary> 한국어 한 줄 설명을 둔다. IngestionOutcomeDoer_Shapeshift는 라인 20 클래스 선언 위에 summary가 없다(파일 헤더 주석만 존재). 자매 클래스 IngestionOutcomeDoer_ExtendShapeshift는 라인 21에 summary가 있어 더 두드러진다.
  - Fix: 라인 20 클래스 선언 위에 /// <summary>약물 섭취 시 폰을 변신시키는 IngestionOutcomeDoer.</summary> 추가하여 다른 클래스와 일관성 유지.

### Compat

- [ ] **[MEDIUM / perf]** 렌더 핫패스 Postfix가 예외 시 매 프레임 Log.Warning 스팸
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Compat/Compat_FacialAnimation_HeadWorker_ScaleFor.cs:99-103`
  - Detail: ScaleFor Postfix는 렌더 경로(매 프레임, 1.6 병렬 렌더 포함)에서 호출되는데, catch 블록이 `Log.Warning($"{CompatManager.LOG_FA} Head scale postfix failed: {e}")`를 1회 가드 없이 실행한다. 동일 폼/폰에서 예외가 재현되면 매 프레임 전체 스택트레이스를 기록하여 로그를 폭주시키고 프레임을 떨어뜨린다. 같은 폴더의 Compat_HAR_BodyAddon_ScaleFor.cs(L108-109)는 동일 상황에서 `if (!CompatManager.HAR.HasFailed("ScaleFor:PostfixException")) CompatManager.HAR.Failed(...)`로 1회만 기록하도록 가드한다 — 이 파일만 누락.
  - Fix: HAR ScaleFor와 동일하게 `if (!CompatManager.FA.HasFailed("HeadScale:PostfixException")) CompatManager.FA.Failed("HeadScale:PostfixException", e.Message);` 패턴으로 교체하여 핫패스 로그 스팸을 제거.
  - Verify [high]: Lines 99-103 confirmed verbatim: the catch runs `Log.Warning($"{CompatManager.LOG_FA} Head scale postfix failed: {e}")` (full stack trace, no guard) inside a Postfix on `PawnRenderNodeWorker.ScaleFor`, which is a per-frame render path explicitly documented as running under 1.6 parallel `ParallelPreRenderPawnAt` (DESIGN_NOTES §8 L130). The sibling HAR file (L108-109) and 8+ other Compat catches — including line 149 of this same file — guard the identical situation with `if (!CompatManager.X.HasFailed(...)) Failed(...)`, which dedupes to one log (CompatManager.cs L37-44); only this render-path catch is unguarded, so a reproducible throw on an on-screen shapeshifted FA pawn spams a full stack trace every frame. Medium severity is correct because the catch sits after early-exit guards (non-shapeshifted/non-FA-head nodes return first), so spam is conditional on an actively-shapeshifted FA pawn rather than universal.

- [ ] **[MEDIUM / design]** 폼 전환(A→B) 시 SS 메모리 백업/클리어 순서로 인해 B에서 원본 사이드암 메모리가 부활
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Compat/Compat_SimpleSidearms_MemoryHook.cs:362-383, 343-360`
  - Detail: SS 훅은 백업/클리어를 ApplyForm의 Prefix(L363)에서, 복원을 RemoveForm의 Postfix(L393)에서 수행한다. 그러나 HediffComp_ShapeshiftCore.ApplyForm(폼 B)은 이미 변신 중(폼 A)이면 본문 내부에서 RemoveForm()을 호출한다(HediffComp_ShapeshiftCore.cs L292-303). 실행 순서: ① 외부 ApplyForm(B) Prefix가 먼저 실행되는데 이때 SS 메모리는 폼 A 적용 시 이미 클리어되어 비어 있으므로 BackupMemory는 빈 백업을 만들고 Store는 `!backup.IsEmpty` 조건(SSMemoryStore.Store L314)으로 저장을 스킵 → 스토어에는 A 적용 때 저장된 '원본' 백업이 그대로 남음. ② 내부 RemoveForm()의 Postfix(L393-405)가 그 원본 백업을 RestoreMemory로 되살리고 store.Remove(pawn)으로 삭제. 결과적으로 폼 B 활성 중에 변신 전 원본 사이드암 메모리가 복구된 채 남고, 스토어에는 백업이 없어 B 해제 시 다시 복원할 것도 없다. 이는 파일 헤더가 명시한 '폼 전용 무기가 SS 메모리에 잔류하지 않도록 관리'(L4) 목적과 어긋나며, B에서 SS가 더 이상 존재하지 않는 무기를 강제 장착 시도(L73 주석에서 우려한 동작)할 수 있다.
  - Fix: FA 훅(Compat_FacialAnimation_OverridesHook)처럼 백업/클리어를 RemoveForm 내부 호출 이후로 미루거나, ApplyForm Postfix에서 (내부 RemoveForm 종료 후) 현재 메모리를 다시 백업→클리어하도록 변경. 최소한 폼 전환 케이스에서 store에 B용 백업이 보장되도록 BackupMemory가 빈 결과여도 Store가 기존 엔트리를 덮어쓰지 않는 현재 동작을 검토할 것.
  - Verify [high]: 코드 추적 결과 주장이 정확하다. SS 훅은 ApplyForm Prefix(L363-383)에서 백업/클리어, RemoveForm Postfix(L393-405)에서 복원한다. A→B 폼 전환 시: (1) 외부 ApplyForm(B) Prefix가 먼저 실행되지만 SS 메모리는 A 적용 때 이미 클리어되어 비어 있어 BackupMemory는 빈 백업을 만들고 Store(L314 !backup.IsEmpty)가 스킵 → store에는 A 적용 때의 원본 백업이 잔류; (2) ApplyForm 본문이 isTransformed=true이므로 내부 RemoveForm()(HediffComp_ShapeshiftCore.cs L292-303)을 호출, 그 RemoveForm Postfix가 store의 원본 백업을 라이브 SS 메모리로 복원하고 store.Remove → 폼 B 활성 중 원본 사이드암 메모리가 부활하고 store는 비게 된다. SS 메모리 조작은 이 훅 파일에만 존재(grep 확인)하므로 본문 어디서도 재클리어되지 않는다. 결정적 반증: 같은 코드베이스의 FA 훅(Compat_FacialAnimation_OverridesHook L492/L535)은 정반대로 ApplyForm Postfix + RemoveForm Prefix를 써서 동일 전환 경로를 올바르게 처리한다 — 저자가 옳은 패턴을 이미 알고 있었다는 증거이며 SS 훅의 Prefix/Postfix 배치가 의도가 아닌 결함임을 뒷받침한다. DESIGN_NOTES에도 이 동작은 의도 패턴으로 기재돼 있지 않다. Harmony는 패치된 메서드를 인플레이스로 수정하므로 내부 RemoveForm() 호출도 Postfix를 발동한다(인라인 안 됨, 대형 public 메서드). 핫패스 아님(변신 1회성). 심각도 medium 적정: 크래시는 아니나 SimpleSidearms+직접 폼전환+무기 Inventory/Drop 조합에서 폼이 벗긴 원본 무기를 SS가 강제 재장착 시도할 수 있어 파일 헤더 L4/L73 목적과 정면 충돌. 다만 결함 영향은 폼 B 활성 구간에 한정되고 B 해제 후 상태는 (이미 일찍 복원돼) 비교적 무해해 영향 범위는 다소 제한적.

- [ ] **[LOW / convention]** 예외 삼키는 빈 catch에 한국어 사유 주석/로그 폴백 누락
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Compat/Compat_PocketSand_GizmoFilter.cs:57-65`
  - Detail: `try { ... } catch { }` (L65)가 ShapeshiftRegistry/LockWeapons 호출 예외를 완전히 삼키지만, DESIGN_NOTES.md #9 '리플렉션/호환 코드의 의도적 예외 무시 — 각 catch에 한국어 사유 주석 필수' 규칙과 컨벤션의 try-catch 폴백 규칙(`Log.Warning("[SSF] ...")`)을 따르지 않는다. 같은 폴더의 다른 패치들은 catch에서 CompatManager.PS.Failed/Log.Warning으로 폴백한다.
  - Fix: catch 본문에 한국어 사유 주석을 추가하고(예: /* 레지스트리 조회 실패 시 필터링 생략 */), 필요하면 `if (!CompatManager.PS.HasFailed("GizmoFilter:Exception")) CompatManager.PS.Failed("GizmoFilter:Exception", ...)` 1회 가드 로그를 추가.

- [ ] **[LOW / bug]** FA 눈 색상 읽기 실패 시 default(Color)(검정)를 유효 백업으로 저장
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Compat/Compat_FacialAnimation_OverridesHook.cs:121-145, 95-104`
  - Detail: BackupCurrent는 eyeComp가 존재하면 색상 값과 무관하게 `dst.eyeColor = c1; dst.eyeColorSet = true;`로 항상 백업한다(L142-143). 그러나 c1/c2는 ShapeshiftReflectionCache.GetInstanceProperty<Color>로 읽으며, 프로퍼티가 없거나 읽기 예외가 나면 캐시 헬퍼가 default(T) 즉 default(Color)=검정을 반환한다(ShapeshiftReflectionCache.cs L133-139, 실패 시 default 반환). 이 경우 '읽기 실패'와 '실제 검정색'이 구분되지 않고 eyeColorSet=true로 저장되어, 변신 해제 시 Restore가 눈을 검정으로 강제 설정(L225-231)할 수 있다. ExposeData도 동일 값을 영속화한다(L95-104).
  - Fix: GetInstanceProperty 대신 성공 여부를 반환하는 경로(예: 프로퍼티 존재/CanRead 선검사 또는 TryGet 헬퍼)로 읽고, 읽기에 성공한 경우에만 eyeColorSet=true로 표시. FaceColor/FaceSecondColor 프로퍼티 자체가 없으면 백업하지 않도록 변경.

### Core-Ideology-Misc

- [ ] **[LOW / convention]** Harmony 패치가 Patches/ 폴더 밖(Ideology/)에 위치 — 비-Compat 패치 중 유일
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Ideology/Patch_ShapeshiftIdeologyTaboo.cs:16-31`
  - Detail: 코드 컨벤션상 비조건부 Harmony 패치는 Patches/ 폴더(1파일=1패치)에 두고, 타 모드 의존 패치만 Compat/에 둡니다. 레포 전체에서 [HarmonyPatch]를 가진 .cs 중 Patches/·Compat/ 밖에 있는 것은 이 파일뿐입니다(나머지 6개는 모두 Compat/). 기능별(Ideology) 그룹핑 의도로 보이나 구조 규칙과는 어긋납니다. 동작 자체(FireFormApplied Postfix, IdeologyActive 가드, null 가드)는 정상이며 PatchAll 자동등록 대상이라 작동에는 문제 없습니다.
  - Fix: Patches/Patch_ShapeshiftCoreUtility_FireFormApplied.cs 등으로 이동하거나, Ideology 전용 패치 배치를 의도한 것이라면 DESIGN_NOTES.md에 근거를 명시해 컨벤션 예외임을 문서화.

- [ ] **[LOW / bug]** CurrentStateInternal에서 p null 가드 누락 — 형제 워커들과 불일치
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Ideology/ThoughtWorker_SacredAnimalForm.cs:18-27`
  - Detail: `CurrentStateInternal(Pawn p)`가 `if (!ModsConfig.IdeologyActive) return ...;` 다음 곧바로 `if (p.Ideo == null) return ...;`로 p를 역참조합니다. p가 null이면 NRE. 같은 폴더의 ThoughtWorker_SacredAnimalForm_Social.cs(26행), ThoughtWorker_Precept_Shapeshifted_Social.cs(24행)는 모두 `p == null || p.Ideo == null`로 p를 먼저 가드하고 있어 일관성이 깨집니다. 실제로 바닐라 ThoughtWorker.CurrentState는 보통 non-null pawn을 넘기므로 발생 확률은 낮지만, 형제 코드가 방어하는 패턴을 이 파일만 누락했습니다.
  - Fix: `if (p == null || p.Ideo == null) return ThoughtState.Inactive;`로 통일.

### Docs

- [ ] **[MEDIUM / doc]** 이데올로기 변신 규율 단계 이름이 실제 PreceptDef와 불일치 (stale 라벨)
  - Loc: `TestMod_SSF/TEST_CHECKLIST.md:14-M05 ~ 14-M08 (섹션 14 호환성)`
  - Detail: 체크리스트 14-M05는 "섭리에 대한 모독", 14-M06은 "부자연스러운 힘", 14-M07은 "특별한 재능", 14-M08은 "신이 내린 축복"이라는 규율 단계 이름을 사용합니다. 그러나 실제 정의(1.6/Defs/PreceptDefs/SSF_Precepts.xml + Common/Languages/Korean/DefInjected/PreceptDef/SSF_Precepts.xml)의 규율 라벨은 abhorrent/절대적으로 혐오함, disapproved/못마땅함, don't care/신경쓰지 않음, respected/중시함, sublime/숭고함 입니다. grep 결과 "섭리에 대한 모독/부자연스러운 힘/특별한 재능/신이 내린 축복" 문자열은 문서(README, TEST_CHECKLIST, FORMDEF_GUIDE)에만 존재하고 코드/Def에는 전혀 없습니다. 단, 각 단계에 연결된 기분/의견 수치(-10/-20, -5/-10, +5/+10, +10/+20)와 목격 감정(-8/-4/+4/+8), 성스러운 동물 폼(-8/-3/+2/+5/+8)은 SSF_Thoughts_Ideology.xml과 정확히 일치합니다 — 즉 단계 매핑 자체는 맞고 '이름'만 옛 명명입니다. 또한 자기 주도 변신 차단 단계는 ShapeshiftEligibility.cs:84에서 SSF_Shapeshifting_Abhorrent(혐오함)로 판정하므로, '섭리에 대한 모독' 단계가 차단한다는 14-M05 서술의 단계명도 어긋납니다.
  - Fix: 14-M05~M08의 단계명을 실제 Def 라벨로 교체: '섭리에 대한 모독'→'절대적으로 혐오함', '부자연스러운 힘'→'못마땅함', '특별한 재능'→'중시함', '신이 내린 축복'→'숭고함'. (수치 기준값은 그대로 두면 됨.)

- [ ] **[LOW / doc]** README의 변신 규율 단계 끝점 이름(Blasphemy → Divine Blessing / 섭리에 대한 모독 → 신이 내린 축복)이 실제 규율 라벨과 불일치
  - Loc: `README.md:67-72 (Ideology Integration) / 189-193 (이데올로기 연동)`
  - Detail: README는 'Shapeshifting precept with 5 stages (Blasphemy → Divine Blessing)' / '변신 규율 5단계 (섭리에 대한 모독 → 신이 내린 축복)'로 단계 양 끝점을 표기합니다. 단계 개수 5는 맞습니다(1.6/Defs/PreceptDefs/SSF_Precepts.xml: Abhorrent/Disapproved/DontCare/Respected/Sublime). 그러나 양 끝점 라벨은 실제로 abhorrent(절대적으로 혐오함) → sublime(숭고함)이며, 'Blasphemy'/'Divine Blessing'/'섭리에 대한 모독'/'신이 내린 축복'이라는 라벨은 코드·Def 어디에도 없고 문서에만 존재합니다(grep으로 확인). 또한 'Self-initiated transform blocking at forbidden stage'의 forbidden stage는 ShapeshiftEligibility.IsIdeologyForbidden이 SSF_Shapeshifting_Abhorrent로 판정하므로 '혐오함' 단계입니다.
  - Fix: 끝점 라벨을 실제 Def 라벨에 맞춰 'Abhorrent → Sublime' / '절대적으로 혐오함 → 숭고함'으로 수정. (Sublime의 description이 'divine blessing'을 언급하긴 하나, 단계 '라벨'은 sublime/숭고함임.)

- [ ] **[LOW / doc]** EN/KO 불일치: KO 3.4에 '유전자 화이트리스트' XML 예시 누락
  - Loc: `FORMDEF_GUIDE_KO.md:3.4 그래픽 가시성 필터 (lines 229-233); cf. FORMDEF_GUIDE_EN.md lines 234-237`
  - Detail: EN 가이드 3.4(Graphic Visibility Filters)에는 두 개의 XML 예시가 있습니다: (1) 'Hide all apparel except power armor', (2) 'Hide all genes except specific ones' (EN 234-237줄: renderHideGeneExclusionTags=All + renderShowGeneDefNames=Gene_ToughSkin). KO 가이드 3.4(229-233줄)에는 첫 번째 의류 예시만 있고 두 번째 유전자 예시가 빠져 있습니다. 두 문서의 섹션 길이 차이(EN 210-237=28줄 vs KO 210-233=24줄, 정확히 4줄 차이)가 이 누락 때문입니다. 필드 표 자체는 양쪽 동일하므로 기능 설명 누락이 아니라 예제 누락입니다.
  - Fix: KO 3.4에 EN과 동일한 유전자 화이트리스트 예시 블록을 추가하여 EN/KO 동기화:
```xml
<!-- 특정 유전자만 남기고 모두 숨김 -->
<renderHideGeneExclusionTags><li>All</li></renderHideGeneExclusionTags>
<renderShowGeneDefNames><li>Gene_ToughSkin</li></renderShowGeneDefNames>
```

- [ ] **[LOW / doc]** 기본값 표기 부정확: bodyOffset/headOffset 코드 기본값은 null (문서는 (0,0))
  - Loc: `FORMDEF_GUIDE_EN.md / FORMDEF_GUIDE_KO.md:3.2 Scale & Offset (EN 147-153 / KO 147-153)`
  - Detail: 문서 3.2 표에서 bodyOffset/headOffset의 Default 열을 '(0,0)'로 표기합니다(EN 151-152 / KO 151-152). 그러나 코드(ShapeshiftFormDef.cs 147-148줄)에서 이 두 필드는 `public Vector2? bodyOffset = null;` / `public Vector2? headOffset = null;` 로 타입이 Vector2? 이고 실제 기본값은 null입니다. 동작상 null은 '오프셋 없음 = (0,0)'과 동일해 결과는 같지만, 같은 표의 bodyDrawScale/headDrawScale은 Default를 '1.0'으로 적으면서 본문 머리말에서 '생략 시 바닐라 기본값'이라고만 안내하므로, 엄밀히는 nullable 필드의 기본값이 null임을 반영하는 편이 코드와 1:1 일치합니다.
  - Fix: Default 열을 'null ((0,0)로 처리)' / 'null ((0,0) treated)'로 표기하거나, 최소한 타입이 Vector2?(nullable)이며 미지정 시 null임을 각주로 명시. bodyDrawScale/headDrawScale(float?, 기본 null→1.0 처리)도 동일하게 정리 가능.

## Rejected (8) - false positives (verifier refuted)

- **HandleConflictingGear에서 인벤토리로 넣은 아이템에 SetForbidden 호출 — 미스폰 Thing 금지 시도**
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_ShapeshiftCore.Gear.cs:222-235`
  - Why rejected: 코드 확인 결과 무해하며 버그가 아님. TryForbidDropped(L213-215)는 `if (dropped == null || !dropped.Spawned) return;` 가드로 시작하므로, HandleConflictingGear의 인벤토리 성공(미스폰) 경로나 TryDropThing 실패(holdingOwner.Remove → 미스폰) 경로 모두에서 SetForbidden(L218)에 도달하지 않고 즉시 반환됨 — 미스폰 Thing에 대해 실제로 호출되는 것은 .Spawned 필드 게터뿐이며 이는 홀딩 상태와 무관하게 항상 안전함. 발견사항 본문 스스로 "무해하지만", "크래시는 없음"이라고 인정하므로 medium 버그가 아닌 단순 가독성/인텐트 nit이며, 변신 1회성 코드라 핫패스도 아님.

- **CompPostTick 내부에서 pawn.Kill() 직접 호출 — hediff 순회 중 컬렉션 변조 위험**
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Hediffs/HediffComp_PermanentTransform.cs:49-104`
  - Why rejected: 코드 인용은 정확하나(CompPostTick L49 → ExecuteTransform → pawn.Kill(null) L104, 동기 호출), 핵심 위험 주장은 RimWorld 1.6 엔진 동작과 모순됩니다. Pawn_HealthTracker.HealthTick(디컴파일 L1028-1031)은 라이브 hediffSet.hediffs가 아니라 스냅샷(tmpHediffs.AddRange 후 foreach)을 순회하므로 순회 중 컬렉션을 비워도 'Collection was modified' 예외가 발생할 수 없습니다. 또한 Pawn.Kill은 SetDead()로 healthState=Dead를 동기 설정(L745)하고, PostTick() 직후 HealthTick의 if(Dead) return(L1054)이 즉시 루프를 안전하게 종료시켜 재진입이 차단됩니다. AutoShift L65 비교는 거짓 등가입니다 — RemoveHediff는 같은 폰의 후속 hediff 순회에 영향을 주지만 Kill은 Dead 가드로 순회 자체를 종료하며, 바닐라 스스로도 tick 경로의 CheckForStateChange에서 pawn.Kill(dinfo, hediff)를 호출(L515)하므로 자기 hediff tick 중 폰을 죽이는 것은 엔진이 지원하는 표준 패턴입니다. 제안된 LongEventHandler 지연 수정은 불필요합니다.

- **form==null 분기가 도달 불가능 — TryGetBodyShadowOverride가 항상 실행되지 않음 (논리상 무해하나 죽은 코드/의도 불명확)**
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_PawnRenderer_DrawShadowInternal.cs:84-86`
  - Why rejected: 코드 추적으로 확인: ShouldRun(L25-32)은 TryGet이 true일 때만 true를 반환하고, TryGet(Registry.cs L40-55, 폴백 L77-100)은 form!=null인 경로에서만 true를 반환하므로 ShouldRun==true ⟹ form!=null이 보장됨. 따라서 L63의 `&& form != null`은 항상 참(또는 단락)인 잉여 조건이 맞고, 동작은 정상이라는 발견사항의 핵심은 사실. 그러나 (1) 발견사항 자체가 "버그는 아니나"라고 인정하며 기능/성능 영향이 0인데 category=bug/severity=medium로 과대 분류됐고, (2) 헤드라인 "form==null 분기 도달 불가능"은 부정확함 — L81의 form==null은 비변신 폰(ShouldRun==false→form=null)마다 실제 도달하는 load-bearing 분기임. 적대적 기준에서 medium 버그가 아니라 기껏해야 low 가독성 정리.

- **오프스크린 폰을 DrawPhase.Draw만 호출 — 바닐라가 같은 프레임에 EnsureInitialized/ParallelPreDraw를 돌리지 않은 상태일 수 있음**
  - Loc: `Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_DynamicDrawManager_DrawDynamicThings.cs:70`
  - Why rejected: 디컴파일된 RimWorld 1.6 소스로 검증함. 발견사항의 핵심 우려(오프스크린 폰이 이번 프레임에 EnsureInitialized/ParallelPreDraw를 안 거쳐 렌더 트리가 스테일/미초기화 상태로 Draw됨)는 PawnRenderer.RenderPawnAt(PawnRenderer.cs:221-227)이 `if (!results.valid){ EnsureGraphicsInitialized(); ParallelPreRenderPawnAt(...); }`로 자가 치유하기 때문에 성립하지 않음 — Draw 페이즈 호출 시 results.valid가 false면(매 Draw 종료 시 라인 276에서 default로 리셋됨) Draw 내부가 앞 두 페이즈를 직접 실행한 뒤 그림. DefaultRenderFlagsNow도 매번 폰 현재 상태에서 재계산되어 프레임 캐시 의존이 없음. 게다가 이 단독 Draw 페이즈 호출은 바닐라가 Thing.DrawNowAt→DynamicDrawPhaseAt(DrawPhase.Draw,...)(Thing.cs:1303,1316)에서 쓰는 정식 계약과 동일하므로 SSF의 line 70 사용(Draw 단일 페이즈)은 올바름. 스테일/첫프레임 누락 위험 없음.

- **TryDropEquipment Prefix에서 pawn.equipment 미점검 + try-catch 폴백 부재로 NRE 가능**
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Pawn_EquipmentTracker_TryDropEquipment.cs:25`
  - Why rejected: L25 matches the claim verbatim, but the NRE is not reachable on any vanilla path. Cecil IL inspection of Assembly-CSharp.dll shows Pawn.equipment is stored exactly once across the whole assembly — in PawnComponentsUtility.CreateInitialComponents — and is never nulled afterward; the tracker uses the back-reference ctor Pawn_EquipmentTracker(Pawn newPawn), so __instance IS pawn.equipment and is non-null whenever TryDropEquipment runs on it. L25 is gated behind ShapeshiftRegistry.TryGet(pawn) success + IsGeneratedWeapon(eq), which requires a fully-initialized pawn that already passed CreateInitialComponents, so pawn.equipment is guaranteed non-null there; the "PostDestroy / non-standard Pawn" cases are speculative with no demonstrated caller. The missing try-catch and pawn.equipment guard are a project-wide style convention that the sibling AddEquipment patch also omits, making this a low-value defensive-hardening nit rather than a medium correctness bug.

- **빔 데미지 폴백이 beamTotalDamage<=0일 때 cellCount로 나누지 않아 셀당 과다 데미지**
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Patches/Patch_Verb_ShootBeam.cs:73-75`
  - Why rejected: SSF의 폴백 분기(L75 `beamDamageDef.defaultDamage * damageFactor`)는 바닐라 `Verb_ShootBeam.ApplyDamage`(디컴파일 L327-330 else 분기 `float amount = verbProps.beamDamageDef.defaultDamage * damageFactor`)와 라인 단위로 동일하다 — 바닐라도 `beamTotalDamage<=0`일 때 `pathCells.Count`로 나누지 않고 셀당 고정 defaultDamage를 적용하며, `>0` 분기에서만 `beamTotalDamage/pathCells.Count`로 분배한다(바닐라 L321-325). 따라서 발견사항이 'SSF만의 결함'으로 지목한 두 분기의 비대칭은 바닐라 자체의 의도된 설계이고, SSF는 이를 충실히 복제한 것이다. 게다가 이 패치는 EquipmentSource==null(맨몸 변신, L53)에서만 동작해 NRE를 막으면서 바닐라 데미지 산식을 그대로 보존하는 것이 명시된 목적(파일 헤더 L2-3)이므로, '폴백 분배 누락'이라는 주장은 바닐라 동작에 대한 오해다.

- **GetSnapshot 재진입 가드가 bool 단일 플래그라 중첩 해제 시 공유 리스트 손상 가능**
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Utilities/ShapeshiftRegistry.cs:116-135`
  - Why rejected: 코드 라인 묘사는 정확하다(L112 bool _snapshotInUse, L119-125 중첩 시 새 copy, L127-131 공유본 Clear+재적재, L135 무조건 false). 그러나 주장하는 손상 시나리오는 세 개의 겹치는 GetSnapshot 호출(공유본 순회 외부 + 중첩 소비자 + 중첩 해제 후 진입하는 제3 호출)을 동시에 요구하는데, 전체 코드베이스의 유일한 호출처는 Patch_DynamicDrawManager_DrawDynamicThings.Postfix 한 곳뿐이고(나머지 grep 히트는 정의부와 테스트 문서), 이 패치는 DynamicDrawManager.DrawDynamicThings의 메인스레드 비재진입 렌더 패스에서 1회 실행되며 루프 본문(L42-72)이 GetSnapshot을 다시 호출하지 않으므로 _snapshotInUse는 항상 진입 시 false → get→iterate→finally ReleaseSnapshot 경로만 실행된다. 가드에 lock이 없어 스레드 안전 장치도 아니며 단일 스레드라 동시 제3 호출이 발생할 수 없다. 발견사항 스스로 "유일 호출처는 중첩이 없어 실증 버그는 아니다"라고 인정하고, DESIGN_NOTES.md에도 이 패턴 항목이 없다. 실제 버그·성능 결함이 아니라 향후 둘째 호출처가 생길 경우의 잠재적 API 견고성/문서 표현 흠집에 불과하다.

- **AoE 순회 중 컬렉션 변경 가능 — Drop 모드 폼 + aoeRadius 조합에서 GenRadial 열거 깨질 위험**
  - Loc: `d:/SteamLibrary/steamapps/common/RimWorld/Mods/Shapeshifter-Framework/Source/ShapeshifterFramework v1.6/ShapeshifterFramework/Projectiles/Projectile_GiveHediff_Shapeshift.cs:42-61`
  - Why rejected: 발견사항의 핵심 인과사슬(AddHediff→ApplyForm 동기 실행→GenRadial 열거 중 셀에 아이템 스폰)이 코드와 불일치한다. ShapeshiftCoreUtility.GiveShiftHediff(L78)의 AddHediff는 CompPostPostAdd(HediffComp_ShapeshiftCore.cs:178-205)를 트리거하지만, 이 메서드는 ApplyForm을 호출하지 않고 needsInit=true만 설정한다(파일 헤더 L5: "CompPostPostAdd에서 ApplyForm을 직접 호출하지 않음—재진입 방지"). 실제 ApplyForm과 모든 기어 Drop/GenPlace.TryPlaceThing 스폰(HandleGearOnTransform)은 이후 별도 게임 틱의 첫 CompPostTick(Tick.cs:31-40)에서만 실행되므로, Impact()의 foreach가 완전히 반환된 뒤에 발생한다—열거 중 컬렉션 변경 불가능. CompPostPostAdd 내 유일한 동기 부작용인 RemoveForm(L195)도 isTransformed일 때만이지만, ApplyHediffToTarget(투사체 L70)이 IsAlreadyTransformed로 사전 차단하므로 이 경로에서 도달 불가. 또한 발견사항이 인용한 ShapeshiftInventoryReequipUtility.cs:62는 재장착 실패 복구 분기로 Drop 경로와 무관하다.

