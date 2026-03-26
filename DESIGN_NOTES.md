# Shapeshifter Framework — 설계 노트 (의도적 비표준 패턴)

이 문서는 코드에서 일반적이지 않지만 **의도적으로 설계된 패턴**들을 정리한 것입니다.
"버그처럼 보이지만 건드리면 안 되는 코드"를 목록화하여, 향후 리팩토링 시 참고합니다.

---

## 수정된 이슈

### ✅ ApplyForm 내부 폼 전환 시 severity=0 잔류 (2026-03-26 수정)

**위치:** `HediffComp_ShapeshiftCore.cs` — ApplyForm `if (isTransformed)` 블록

**원인:** RemoveForm의 finally가 항상 severity=0을 설정하여 바닐라 자동 제거를 유도.
이는 단독 RemoveForm에서는 정확하지만, ApplyForm 내부에서 폼 전환(RemoveForm→새 폼 적용)
시 severity=0이 잔류하여 다음 틱에 hediff가 자동 제거됨.

**수정:** RemoveForm 호출 후 `parent.Severity = parent.def.initialSeverity > 0 ? initialSeverity : 1f` 복원.
severity=0 자체는 바닐라 엔진 흐름을 타는 올바른 설계이므로 RemoveForm.finally는 유지.

---

## 의도적 패턴 목록 (수정 금지)

### 1. 재진입 가드 플래그 일시 해제

**위치:** `HediffComp_ShapeshiftCore.cs` — ApplyForm (L271-280)

```csharp
_isApplyingOrRemoving = false;   // 일시 해제
RemoveForm();                     // RemoveForm 내부 재진입 검사 우회
_isApplyingOrRemoving = true;    // 복원
```

**이유:** ApplyForm 안에서 RemoveForm을 호출하려면 재진입 가드를 잠시 풀어야 함.
RemoveForm 예외 시 외부 finally가 false를 보장하므로 안전.

---

### 2. Severity=0 간접 제거 (CompPostTick 내)

**위치:** `HediffComp_AutoShift.cs` (L57-61)

```csharp
parent.Severity = 0f;  // RemoveHediff 대신
```

**이유:** CompPostTick 실행 중 RemoveHediff를 호출하면 hediff 리스트 순회 도중 변조 발생.
severity=0으로 설정하면 바닐라의 별도 패스에서 안전하게 제거.

---

### 3. VerbTracker.ExposeData에서 verbs null 처리

**위치:** `Patch_VerbTracker_ExposeData_StripFormVerbs.cs` (L22-41)

```csharp
___verbs = null;  // ResolvingCrossRefs 단계에서
```

**이유:** 변신 폼 verb의 loadID가 현재 ThingDef tools와 불일치 → "Replaced verb" 경고 폭탄.
null로 밀어서 매칭 자체를 스킵하고, 이후 InitVerbsFromZero가 올바르게 재구축.
Priority.High로 바닐라 로직보다 먼저 실행.

---

### 4. ConditionalWeakTable + 인스턴스 교체

**위치:** `ShapeshiftRuntimeCaches.cs` (L14-24, L53-60)

```csharp
CallByPawn = new ConditionalWeakTable<Pawn, SoundDef>();  // Clear() 없으므로 교체
```

**이유:** Dictionary 대신 ConditionalWeakTable 사용 → Pawn GC 시 캐시도 자동 해제 → 메모리 누수 방지.
ConditionalWeakTable에 Clear()가 없어서 전체 초기화 시 새 인스턴스로 교체.

---

### 5. 5단계 생성자 폴백 (리플렉션)

**위치:** `ShapeshiftFormDynamicPawnRenderNodeSetup.cs` (L96-160)

```
(Pawn, Props, Tree) → (Props, Tree) → (Props) → (Tree) → ()
```

**이유:** 서드파티 모드의 PawnRenderNode 하위 클래스마다 생성자 시그니처가 다름.
가장 완전한 것부터 시도하고, 실패 시 빈 생성자 + 리플렉션으로 private 필드 직접 주입.
생성자 캐시(ConcurrentDictionary)로 최초 1회만 탐색.

---

### 6. 리플렉션 실패 별도 캐싱

**위치:** `ShapeshiftReflectionCache.cs` (L73-75, L83-99)

```csharp
private static readonly ConcurrentDictionary<(Type, string), bool> FieldNotFound;
if (FieldNotFound.ContainsKey(key)) return null;  // 즉시 탈출
```

**이유:** 리플렉션 조회 실패는 비용이 높음(예외 스택 생성).
실패를 별도 딕셔너리에 기록하여 재시도 없이 즉시 반환.
FieldCache(성공)와 FieldNotFound(실패)를 분리한 이유: TryGetValue에서 null 반환과 "키 없음"을 구분.

---

### 7. Priority.Last Harmony 패치 (스케일/오프셋)

**위치:** 다수 패치 파일

```csharp
[HarmonyPriority(Priority.Last)]  // 다른 모드 연산 끝난 후 최종 적용
```

**이유:** 폼 스케일/오프셋은 다른 애니메이션 모드(FA, HAR 등)의 연산이 끝난 후 곱셈으로 적용해야 정확.
Priority.Last-1도 사용 (IngestBlock — 다른 모드의 FloatMenu 옵션 추가가 끝난 후 차단).

---

### 8. [ThreadStatic] 임시 컬렉션

**위치:** `Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner.cs` (L21-22)

```csharp
[ThreadStatic] static HashSet<string> _tmpTagSet;
```

**이유:** RimWorld 1.6 ParallelPreRenderPawnAt에서 렌더 스레드가 병렬 실행.
ThreadStatic으로 스레드별 독립 컬렉션 보장. 매 프레임 GC 할당 없이 재사용.

---

### 9. 리플렉션/호환 코드의 의도적 예외 무시

**위치:** `ShapeshiftReflectionCache.cs`, Compat 폴더 다수

```csharp
catch { /* 리플렉션 폴백 — 타입 불일치 무시 */ }
```

**이유:** 타 모드 API를 리플렉션으로 접근할 때 모드 미설치/버전 변경으로 실패 가능.
실패해도 게임 크래시를 방지해야 하므로 의도적으로 삼킴. 각 catch에 한국어 사유 주석 필수.

---

### 10. ParentHolder 체인 홉 제한

**위치:** `ShapeshiftReflectionCache.cs` (L22-45)

```csharp
while (h != null && guard++ < maxHops)  // 최대 8홉
```

**이유:** ParentHolder 순환 참조 시 무한루프 방지. 실제로 Pawn→Inventory→Caravan→World 등
4~5단계면 충분하지만, 타 모드의 커스텀 홀더를 감안해 8홉 여유.

---

### 11. CompatMod 1회 리포트 후 즉시 경고 전환

**위치:** `CompatManager.cs` (L36-41, L46-64)

```csharp
if (reported) Log.Warning(...);  // 스타트업 후엔 즉시 출력
```

**이유:** 초기화 중 실패를 모아서 1회 요약 출력 (로그 스팸 방지).
ReportOnce() 이후 런타임에 발생하는 실패는 즉시 경고 (놓치지 않기 위해).

---

### 12. Messages.Message try-catch 감싸기

**위치:** `HediffComp_ShapeshiftCore.cs` — ApplyForm (L265)

```csharp
try { Messages.Message(...); } catch (Exception ex) { Log.Warning(...); }
```

**이유:** 번역 키 누락이나 MessageTypeDefOf 미등록 등으로 메시지 표시가 실패해도
hediff 생명주기 처리(severity=0 설정)는 반드시 진행되어야 함.

---

### 13. nullable 필드 = "FormDef 기본값 사용" 패턴

**위치:** `HediffCompProperties_ShapeshiftCore.cs`

```csharp
public int? durationTicks;   // null이면 FormDef.durationTicks 사용
public bool? lockEquipment;  // null이면 FormDef.lockEquipment 사용
```

**이유:** XML Def에서 HediffDef 수준 오버라이드를 선택적으로 지정.
null = "FormDef 기본값 사용", 값 지정 = "이 HediffDef에서만 오버라이드".

---

## 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-03-26 | 초안 작성 — 전수조사 기반 13개 패턴 문서화, 잠재 이슈 1건 |
