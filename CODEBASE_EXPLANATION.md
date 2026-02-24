# 타임데빌 프로젝트 구현 요약

이 문서는 **"무엇을 구현한 프로젝트인지"**와 **"코드가 실제로 어떻게 동작하는지"**를 중심으로, 핵심 스크립트 흐름만 추려 정리한 설명서입니다.

---

## 1) 이 프로젝트가 구현한 것(게임 구조)

이 저장소는 Unity 2D 기반의 **스토리/탐색 + 상호작용 + 트리거 이벤트 + 턴제 카드 전투 + 저장/복귀** 구조를 가진 RPG 형태로 보입니다.

핵심적으로 다음 5개 축이 맞물려 동작합니다.

1. **월드 탐색/입력 제어**: 플레이어 이동, 상호작용(E), 메뉴(Q/W), 컷씬 중 입력 잠금
2. **대화/연출 트리거**: Trigger 라우터가 step 시퀀스를 실행하고, DialogueManager가 대사 UI/타이핑 처리
3. **전투 진입/복귀**: 적 충돌 시 전투씬 이동, 전투 종료 또는 도주 후 원래 씬 좌표로 복귀
4. **턴제 카드 전투**: 손패/코스트/카드 사용/적 턴 처리/튜토리얼 게이트
5. **저장 시스템**: 진행 상황(progress), 카드/아이템/플레이어 데이터 통합 저장

---

## 2) 큰 실행 흐름(플레이 기준)

### A. 월드에서 이동/상호작용
- `PlayerMainManager`가 입력을 단일 창구로 받아 현재 상태(대화/컷씬/메뉴/월드)에 따라 분기합니다.
- 월드 상태에서는 방향키 이동, `E` 상호작용, `Q` 메뉴 열기가 동작합니다.
- 컷씬/행동잠금 상태에서는 이동 입력을 차단합니다.

관련 코드:
- `PlayerMainManager` (`Update`, 상태 분기)
- `PlayerMove` (물리 이동)
- `PlayerInteractor` (정면 Cast로 상호작용 대상 탐색)
- `GameManager` (`LockAction`/`UnlockAction`으로 행동 잠금 카운트 관리)

### B. 상호작용 대상 처리
- `IInteractable` 구현체(예: `ObjectInteraction`, `SavePointInteractable`)가 실제 동작을 수행합니다.
- `ObjectInteraction`은 대화를 시작하고, 특정 레이어(`item_get`)면 카드 보유 상태를 런타임에 추가합니다.
- `SavePointInteractable`은 `SaveSystem.SaveAll()`을 호출해 현재 상태를 저장합니다.

### C. 트리거 기반 이벤트 시퀀스
- `TriggerGet`이 플레이어 충돌을 감지해 `routeKey`를 발행합니다.
- `TriggerRouter`가 key에 해당하는 `TriggerStepBase` 리스트를 순차 실행합니다.
- 예를 들어 `TriggerStep_Dialogue`는 대사를 시작하고, 필요 시 입력 잠금 + 자동 진행까지 처리합니다.

### D. 전투 진입
- `EnemyBattleTrigger`가 플레이어 진입을 감지하면 전투 전환을 시작합니다.
- `BattleSceneLoader.Go()`가
  1) 전투 대상 적 ID 저장,
  2) 돌아올 월드 씬/좌표 저장,
  3) 전투씬 로드
  를 수행합니다.

### E. 전투 진행
- `BattleBootstrap`이 전투씬 시작 시 적/플레이어 런타임 데이터를 보강하고 UI를 바인딩합니다.
- `TurnManager`가 플레이어 턴 ↔ 적 턴 전환, 초과 손패 버리기, 튜토리얼 인트로/게이트를 제어합니다.
- `CardUseOrchestrator`가 카드 사용의 실제 타이밍(코스트 지불 → 손패 제거 → 프리뷰/효과 실행 → 선택 복귀)을 관리합니다.
- `EndController`와 `RunController`가 각각 턴 종료/도주 입력을 연결합니다.

### F. 전투 후 복귀
- `SceneLoader.GoBackToReturnScene()`로 월드 씬 복귀를 요청합니다.
- `PlayerReturnManager`가 씬 로드 후 플레이어 좌표를 복원하고, 필요하면 카메라/트리거 억제/유예시간(grace)까지 처리합니다.
- 이 덕분에 복귀 직후 같은 전투 트리거 재진입을 막는 흐름이 구성되어 있습니다.

---

## 3) 시스템별 책임 정리

## 3-1. 입력/상태 게이트(월드 공통)
- **핵심 의도**: "지금 플레이어 입력을 받아도 되는가"를 일관되게 제어
- `PlayerMainManager`: 상태 머신처럼 입력 분기
- `GameManager`: 액션 잠금을 카운트 기반으로 관리
- `DialogueManager.blockInput`: 컷씬 중 일반 E 입력을 막고, 컷씬 전용 호출만 허용

## 3-2. 대화 시스템
- `DialogueManager`:
  - 대화 큐 구성
  - 타이프라이터 출력
  - 포트레이트 표시/포커싱
  - 외부(컷씬) 제어용 API (`ForceCompleteTyping`, `Cutscene_DisplayNextSentence`)
- `Dialogue` 데이터(스크립터블/직렬화 객체)와 결합해 씬 전역 단일 매니저처럼 작동

## 3-3. 트리거/컷씬 라우팅
- `TriggerGet` + `TriggerRouter` + `TriggerStepBase` 조합은
  **키 기반 step 시퀀서** 역할을 합니다.
- 설계상 장점:
  - 트리거 조건(충돌)과 실제 행동(step 체인)을 분리
  - 스텝 단위 재사용 가능
  - 코루틴 기반으로 "끝날 때까지 대기" 연출 구성 가능

## 3-4. 전투 진입/적 로딩
- 월드에서 전투로 넘어갈 때 적 식별자(`enemyId`)를 런타임 컨텍스트에 기록
- 전투씬에서 `BattleBootstrap`/`BattleEnemyLoader`/`EnemyBootstrapper` 계열이
  이 ID를 읽어 적 SO를 로딩하고 `EnemyRuntime`을 초기화

## 3-5. 턴제 카드 전투
- `TurnManager`: 턴 상태 전환의 중심
- `BattleDeckRuntime`, `HandUI`, `CostController`: 손패/코스트 규칙
- `CardUseOrchestrator`: 카드 1회 사용의 오케스트레이션
- `EnemyTurnController`, `EnemyDeckRuntime`, `EnemyHandUI`: 적 행동/패 관리

특히 `Move_Tutorial` 씬 전용으로
- 인트로 1회 노출,
- 게이트 문구/진행 제어,
- PlayerPrefs 기반 "한 번만 보기" 플래그
를 포함한 튜토리얼 제어가 들어가 있습니다.

## 3-6. 저장/불러오기
- `SaveSystem.SaveAll()`이 저장 진입점
  - progress
  - cards
  - items
  - player
  를 한 번에 저장
- `ProgressSaveStore`가 `progress.json` 입출력을 담당
- `SavePointInteractable`이 인게임 저장 포인트 인터페이스를 제공

## 3-7. 씬 전환/카메라 복원
- `SceneLoader`가 씬 로드 + 복귀 컨텍스트 저장 담당
- `SceneVisitEffectRunner`가 입장/퇴장 효과를 걸고 씬 전환
- `CameraManager`가 Follow/Fixed/Cutscene 모드, VCam 재획득, 복귀 스냅샷 적용을 담당

---

## 4) 데이터 흐름(핵심 컨텍스트)

이 프로젝트는 "정적 컨텍스트 + 런타임 싱글톤" 패턴을 많이 씁니다.

- 전투 대상: `ObjectNameRuntime`, `SelectedEnemyRuntime`, `SceneLoadContext` 계열
- 복귀 정보: `PlayerReturnContext` (돌아갈 씬, 좌표, grace, 카메라 복원 정보)
- 전역 제어: `GameManager`, `DialogueManager`, `CameraManager`

즉, 씬이 바뀌어도 유지해야 하는 최소 상태를 컨텍스트로 들고,
씬 진입 시 각 bootstrapper가 그 값을 다시 실제 컴포넌트에 적용하는 구조입니다.

---

## 5) 한 줄 결론

이 코드베이스는
- **탐색/상호작용 중심의 월드 파트**와
- **카드 기반 턴제 전투 파트**를
씬 전환 컨텍스트로 연결한 구조이며,

여기에
- 트리거 라우팅(연출 시퀀스),
- 대화/입력 잠금,
- 저장/복귀/카메라 복원
을 붙여 **"스토리 진행형 RPG 루프"**를 구현한 프로젝트로 정리할 수 있습니다.
