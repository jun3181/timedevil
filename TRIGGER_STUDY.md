# Trigger 관련 학습 노트

이 문서는 현재 프로젝트(`timedevil`)의 **트리거(Trigger) 실행 구조**를 빠르게 파악하기 위한 요약입니다.

## 1) 핵심 컴포넌트

- `TriggerGet`
  - `Collider2D` 트리거 진입을 감지해 `routeKey` 기준으로 `TriggerRouter`에 실행 요청.
  - `maxCalls`로 호출 횟수 제한 가능(0=무제한).
  - 컷씬 실행 중 진입 시 즉시 실행하지 않고 pending으로 보류했다가 컷씬 종료 후 같은 콜라이더가 영역 안에 있으면 재개.
  - 전투 복귀 직후 재진입 방지(`blockDuringGracePeriod`) 지원.
  - 호출 카운트를 static dictionary로 유지해 씬 재로드 후에도 소모 상태를 복원.

- `TriggerRouter`
  - `key -> steps(List<TriggerStepBase>)` 라우팅 테이블을 구축.
  - 동일 key 재진입 방지(`allowReentrySameKey=false`) 정책.
  - route 실행 코루틴에서 step 단위 진행.
  - route 별로 입력 잠금(`blockPlayerInputWhileRunning`) 처리.
  - `WorldNPCStateService`에 진행상태를 저장/복구해 씬 전환 후 중단 지점에서 재개.

- `TriggerContext`
  - route 실행에 필요한 컨텍스트(`trigger`, `router`, `instigator`, `playerMove`) 전달용 immutable 객체.

- `TriggerSuppressTag`
  - 일정 시간 콜라이더/트리거를 비활성화해 연속 재트리거를 막는 유틸리티.
  - key 매칭 방식 + 위치 기반 근접 억제(`SuppressNearPoint`) 방식 둘 다 제공.

## 2) 실행 플로우(요약)

1. 플레이어가 `TriggerGet`의 `OnTriggerEnter2D` 진입.
2. 검증:
   - router 존재
   - grace period 차단 여부
   - 호출 횟수 제한
   - 플레이어 판정(`PlayerMove`)
3. 컷씬 진행 중이면 pending 저장 후 대기.
4. 실행 가능하면 `TriggerContext` 생성 후 `TriggerRouter.RequestRoute(routeKey, ctx)` 호출.
5. `TriggerRouter`는 해당 key route를 코루틴으로 실행:
   - step 시작 전/후 진행률 저장
   - 필요 시 입력 잠금
   - 모든 step 완료 시 진행률 clear

## 3) 실무 체크포인트

- Trigger가 안 먹을 때
  - `Collider2D.isTrigger=true`인지 확인
  - `TriggerGet.router` 연결/자동 탐색 여부 확인
  - `routeKey`가 `TriggerRouter.routes[].key`와 정확히 일치하는지 확인
  - `maxCalls` 소모로 disabled 되었는지 확인

- 전투/컷씬 이후 중복 발동 이슈
  - `blockDuringGracePeriod` 사용 여부 점검
  - `TriggerSuppressTag` 적용 범위/시간 점검

- 디버깅 권장
  - `TriggerGet.debugLog`, `TriggerRouter.debugLog`를 켜고 key 단위 로그로 추적

## 4) 코드 읽기 우선순위

1. `Assets/Script/Trigger/TriggerGet.cs`
2. `Assets/Script/Trigger/TriggerRouter.cs`
3. `Assets/Script/Trigger/TriggerSuppressTag.cs`
4. `Assets/Script/loader/WorldNPCStateService.cs`

## 5) Trigger / Interaction 작업 상세 메모

이 문서는 `chapter1` 씬에서 사용한 `Trigger`, `interaction`, `TeleportTransition`, `TriggerRouter` 흐름을 다른 PC에서도 그대로 이해하고 재현하기 위한 정리입니다.

기준 프로젝트:

- Unity: `timedevil`
- 씬: `Assets/Scenes/chapter1.unity`
- 핵심 스크립트:
  - `Assets/Script/Trigger/TriggerRouter.cs`
  - `Assets/Script/Trigger/TriggerGet.cs`
  - `Assets/Script/Interactable/InteractTriggerRouteCaller.cs`
  - `Assets/Script/Interactable/TeleportTransition.cs`
  - `Assets/Script/Trigger/Dialogue_triger/TriggerStep_Dialogue.cs`
  - `Assets/Script/Trigger/UpDownLeftRight/TriggerStep_facing.cs`
  - `Assets/Script/Trigger/Move/TriggerStep_PivotSequenceMove.cs`
  - `Assets/Script/Trigger/tilemap/TriggerStep_SetActiveSwap.cs`

## 핵심 개념

이 프로젝트의 이벤트는 대부분 아래 구조로 작동합니다.

```text
플레이어가 밟음 / 상호작용함
-> routeKey 호출
-> TriggerRouter가 같은 key의 Route 실행
-> Route 안의 TriggerStep들을 위에서 아래 순서대로 실행
```

트리거와 상호작용은 호출 방식만 다르고, 결국 둘 다 `TriggerRouter.RequestRoute(routeKey, ctx)`로 들어갑니다.

## 컴포넌트 역할

### TriggerRouter

`TriggerRouter`는 이벤트 라우터입니다.

- 위치 예시:
  - `Trigger` 루트
  - `Entity`
  - `partyEntity`
- Inspector의 `Routes`에 `key`와 `steps`를 등록합니다.
- `key`는 `TriggerGet` 또는 `TriggerRouterInteraction`의 `routeKey`와 반드시 같아야 합니다.
- `blockPlayerInputWhileRunning`을 켜면 Route 실행 중 플레이어 입력을 막습니다.

예시:

```text
partyEntity TriggerRouter
- key: entity5
  steps:
  1. TriggerStep_facing
  2. TriggerStep_Dialogue
```

### TriggerGet

`TriggerGet`은 플레이어가 콜라이더를 밟았을 때 Route를 실행합니다.

필수 설정:

- `BoxCollider2D` 또는 다른 `Collider2D`
- `Is Trigger = true`
- `TriggerGet`
- `router` 연결
- `routeKey` 입력
- `maxCalls`
  - `1`: 한 번만 실행
  - `0`: 무제한 실행

주의:

- 플레이어 판정은 기본적으로 `PlayerMove` 컴포넌트를 가진 오브젝트만 통과합니다.
- `maxCalls = 1`이면 한 번 실행 후 `TriggerGet`과 Collider가 꺼집니다.
- 같은 위치에 다시 들어가도 이미 소모된 트리거는 실행되지 않습니다.

### TriggerRouterInteraction

`TriggerRouterInteraction`은 플레이어가 바라보고 `E`로 상호작용했을 때 Route를 실행합니다.

필수 설정:

- 대상 오브젝트에 `Collider2D`
- 대상 오브젝트 Layer가 `PlayerInteractor`가 감지하는 Layer여야 함
  - 현재 필수 Layer: `Dialog`, `teleport`, `item_get`, `Object`, `Save`
- 대상 오브젝트 또는 부모에 `TriggerRouterInteraction`
- `router` 연결
- `routeKey` 입력

주의:

- `PlayerInteractor`는 먼저 맞은 Collider 자기 자신의 `IInteractable`을 찾고, 없으면 부모에서 찾습니다.
- `Entity`와 `partyEntity`처럼 라우터가 여러 개 있을 때는 `router`를 자동 탐색에 맡기지 말고 직접 연결하는 것이 안전합니다.
- `routeKey`가 비어 있거나 라우터에 같은 key가 없으면 아무 일도 일어나지 않습니다.

### TeleportTransition

`TeleportTransition`은 상호작용으로 플레이어를 다른 위치로 이동시키는 컴포넌트입니다.

필수 설정:

- 대상 오브젝트에 `Collider2D`
- 대상 오브젝트 Layer를 보통 `teleport` 또는 `Object`로 설정
- `TeleportTransition`
- `targetPoint`
  - 직접 연결하거나
  - 자식 이름을 `TargetPoint` / `targetPoint`로 만들면 자동 탐색 가능

주요 옵션:

- `fadePanel`: 없으면 씬에서 자동 탐색
- `fadeOutDuration`, `fadeInDuration`: 화면 전환 시간
- `afterMode`: 이동 후 카메라 모드
  - 실내 고정 카메라는 보통 `Fixed`
  - 외부 맵은 보통 `FollowConfined`
- `afterBounds`: `FollowConfined`일 때 카메라 제한 영역
- `fixedCameraAnchorPoint`: `Fixed`일 때 카메라가 고정될 위치
- `applyDarkOverlay`: 검은 배경 상태를 유지할 때 사용
- `darkOverlayAlpha`: 검은 배경 투명도

## TriggerStep 종류

### TriggerStep_Dialogue

대화창을 실행합니다.

주요 설정:

- `dialogue`: 대사 데이터
- `waitUntilDone = true`: 대화가 끝날 때까지 다음 Step으로 가지 않음
- `lockPlayerInput = true`: 대화 중 플레이어 입력 잠금
- `blockWorldAdvance`: 컷씬처럼 플레이어가 직접 넘기지 못하게 할 때 사용
- `autoAdvance`: 자동 넘김이 필요할 때 사용

### TriggerStep_facing

NPC나 플레이어가 특정 방향 또는 특정 오브젝트를 바라보게 합니다.

우리가 NPC 상호작용에 사용한 기본 전개:

```text
1. TriggerStep_facing
2. TriggerStep_Dialogue
```

주요 설정:

- `targetSource`
  - `ExplicitObject`: 지정한 NPC
  - `Player`: 플레이어
  - `Instigator`: 이벤트를 실행한 주체
- `lookMode`
  - `Direction`: Up/Down/Left/Right 직접 지정
  - `OtherObject`: 다른 오브젝트를 바라봄
- `lookTarget`: `OtherObject`일 때 바라볼 대상
- `animatorOverride`: 대상의 Animator를 명확히 지정할 때 사용
- `facingAnimation = AutoFromFacing`: 바라보는 방향에 맞는 idle 자동 적용

Animator 필수 파라미터:

```text
isChange : Bool
hAxisRaw : Int
vAxisRaw : Int
```

### TriggerStep_PivotSequenceMove

오브젝트나 NPC를 지정한 위치까지 이동시키는 Step입니다. `Trigger9`에서 `partyEntity/12`가 왼쪽으로 이동한 뒤 아래 계단으로 내려가는 연출에 사용했습니다.

구성 방식:

```text
TriggerStep_PivotSequenceMove
- Element 0
  - Entry 0: target = NPC, pivot = 왼쪽 이동 목표
- Element 1
  - Entry 0: target = NPC, pivot = 아래 계단 목표
```

주요 설정:

- `target`: 움직일 오브젝트
- `pivot`: 도착 위치
- `duration`: 이동 시간
- `speed`: duration이 0일 때 속도로 시간 계산
- `animatorOverride`: 움직이는 NPC의 Animator
- `setIdleAtEnd`: 이동 후 idle로 전환
- `finalFacing`: 이동 후 바라볼 방향
- `finalFacingApplyMode`
  - `IdleDirect`: 바로 idle 방향 적용
  - `WalkThenIdle`: 걷는 상태를 한 프레임 거친 뒤 idle 적용

걷는 애니메이션이 제대로 보이려면 Animator에 아래 파라미터가 있어야 합니다.

```text
isChange : Bool
hAxisRaw : Int
vAxisRaw : Int
```

또한 걷기 클립은 Loop가 켜져 있어야 합니다.

### TriggerStep_SetActiveSwap

오브젝트를 켜거나 끄는 Step입니다.

사용 예시:

- 연출이 끝난 뒤 NPC를 비활성화
- 특정 타일맵/오브젝트를 숨기고 다른 오브젝트를 활성화

주요 설정:

- `disableObjects`: 끌 오브젝트 목록
- `enableObjects`: 켤 오브젝트 목록
- `disableFirst`: 끄기를 먼저 할지 여부

## TriggerStep 전체 스크립트 역할표

아래는 `Assets/Script/Trigger` 아래의 `TriggerStepBase` 상속 스크립트와 관련 보조 스크립트를 읽고 정리한 역할표입니다.

기본 구조:

```text
ITriggerStep
-> TriggerStepBase
-> 각 TriggerStep_*
```

`TriggerRouter`는 Route의 `steps` 리스트에 들어간 `TriggerStepBase`들을 순서대로 실행합니다. 각 Step은 `Execute(TriggerContext ctx)` 코루틴으로 동작하며, 앞 Step이 끝나야 다음 Step으로 넘어갑니다.

### 공통 기반

#### ITriggerStep

파일: `Assets/Script/Trigger/ITriggerStep.cs`

역할:

- 모든 TriggerStep이 구현해야 하는 최소 인터페이스입니다.
- `IEnumerator Execute(TriggerContext ctx)`만 정의합니다.
- 라우터는 이 함수가 끝날 때까지 기다렸다가 다음 Step을 실행합니다.

#### TriggerStepBase

파일: `Assets/Script/Trigger/TriggerStepBase.cs`

역할:

- 모든 `TriggerStep_*`의 부모 클래스입니다.
- `Execute(ctx)`를 반드시 구현하게 합니다.
- `AllowPlayerInputWhileExecuting` 기본값은 `false`입니다.

특수 사용:

- `TriggerStep_StayThenTeleport`처럼 Step 실행 중에도 플레이어가 움직여야 하는 경우 `AllowPlayerInputWhileExecuting => true`로 바꿉니다.
- `TriggerRouter`에서 Route가 입력 잠금을 걸어도, 이 값이 true인 Step 동안은 잠금을 잠깐 풀었다가 Step 종료 후 다시 잠급니다.

#### TriggerContext

파일: `Assets/Script/Trigger/TriggerContext.cs`

역할:

- Trigger 실행 당시의 상황 정보를 Step에 넘깁니다.
- 포함 정보:
  - `trigger`: 발동한 `TriggerGet`
  - `router`: 실행 중인 `TriggerRouter`
  - `instigator`: 이벤트를 일으킨 오브젝트
  - `instigatorCollider`: 이벤트를 일으킨 Collider
  - `playerMove`: 플레이어의 `PlayerMove`
  - `player`: 플레이어 Transform

사용 예:

- 플레이어를 강제로 이동시키는 Step은 `ctx.player`를 사용합니다.
- 상호작용 기반 `TriggerRouterInteraction`은 `trigger`가 없으므로 `ctx.trigger`가 null일 수 있습니다.

### 대화 / UI Step

#### TriggerStep_Dialogue

파일: `Assets/Script/Trigger/Dialogue_triger/TriggerStep_Dialogue.cs`

역할:

- DialogueManager로 대화창을 실행합니다.
- 컷씬 중 대화, NPC 대화, 나레이션에 사용합니다.

주요 필드:

- `dialogue`: 재생할 대사 데이터
- `waitUntilDone`: true면 대화가 끝날 때까지 다음 Step 대기
- `lockPlayerInput`: 대화 중 플레이어 행동 잠금
- `blockWorldAdvance`: 플레이어가 E로 넘기지 못하게 막음
- `autoAdvance`: `blockWorldAdvance`일 때 자동으로 다음 문장 진행
- `dialogueNpcs`: 대화 중 움직임을 멈출 NPC 목록

언제 쓰나:

- `facing -> dialogue` NPC 상호작용
- 컷씬 중 설명/나레이션
- 텔레포트 후 잠에서 깨는 대사

주의:

- `blockWorldAdvance = true`인데 `autoAdvance = false`면 플레이어가 넘길 수 없어 멈춘 것처럼 보일 수 있습니다.
- `waitUntilDone = false`면 대화가 떠 있는 중에도 다음 Step이 실행됩니다.

#### TriggerStep_UiSequence

파일: `Assets/Script/Trigger/Trigger_UI/TriggerStep_UiSequence.cs`

역할:

- `UiSequencePlayer`를 실행하는 Step입니다.
- 인벤토리/카드/특수 UI 전개처럼 대화창보다 복잡한 UI 시퀀스에 씁니다.

주요 필드:

- `sequence`: 실행할 `UiSequencePlayer`
- `playOnExecute`: Step 실행 시 바로 시작
- `waitUntilFinished`: UI 시퀀스가 끝날 때까지 Route 대기
- `autoAdvanceAfterDialogueWhenWaiting`: UI 시퀀스 내부 대화가 끝나면 자동 진행하도록 보조

언제 쓰나:

- 대화와 UI가 섞인 컷씬
- 카드 선택/획득 같은 UI 연출

주의:

- `sequence`가 비어 있으면 씬에서 자동 탐색하지만, 여러 개 있으면 의도와 다른 UI를 잡을 수 있으니 직접 연결하는 것이 안전합니다.

#### TriggerStep_IllustrationPanel_New

파일: `Assets/Script/Trigger/TriggerIllustration.cs`

역할:

- 일러스트 이미지와 메시지 텍스트를 패널에 띄웁니다.
- 조사 장면, 컷인 이미지, 설명 이미지에 사용합니다.

주요 필드:

- `panel`: 켜고 끌 UI 패널
- `illustrationImage`: 이미지를 넣을 UI Image
- `messageText`: 문구를 넣을 TMP Text
- `illustrationSprite`: 표시할 Sprite
- `message`: 표시할 문구
- `closeWithKey`, `closeKey`: 키 입력으로 닫기
- `autoCloseDelay`: 일정 시간 후 자동 닫기
- `waitUntilClosed`: 닫힐 때까지 다음 Step 대기
- `lockPlayerInput`: 패널 표시 중 플레이어 잠금

언제 쓰나:

- 중요한 장면 이미지 표시
- 문서/그림/힌트 패널 표시

주의:

- `closeWithKey = false`이고 `autoCloseDelay = 0`이면 닫을 조건이 없어 바로 닫도록 방어되어 있습니다.

### 이동 / 추적 / 애니메이션 Step

#### TriggerStep_PlayerMove

파일: `Assets/Script/Trigger/Move/TriggerStep_PlayerMove.cs`

역할:

- 플레이어의 실제 위치를 강제로 이동시킵니다.
- 짧은 연출 이동, 점프처럼 보이는 이동, 플레이어를 한두 칸 걷게 하는 연출에 사용합니다.

주요 필드:

- `segments`: 여러 구간 이동 목록
- `direction`, `customDirection`, `distance`, `duration`: 단일 이동용 레거시 설정
- `walkAnimation`: 이동 중 재생할 방향 애니메이션
- `lockPlayerInput`: 이동 중 입력 잠금
- `disablePlayerMainManagerWhileRunning`: 플레이어 입력/애니메이션 덮어쓰기 방지
- `setIdleAtEnd`: 이동 후 idle 처리
- `zeroVelocityBefore`, `zeroVelocityAfter`: Rigidbody 속도 초기화

언제 쓰나:

- 플레이어가 깜짝 놀라 위로 튀었다 내려오는 연출
- 컷씬 중 플레이어를 지정 방향으로 이동
- 문 앞에서 자동으로 한 발 들어가는 연출

주의:

- 실제 Transform을 움직입니다.
- 벽 충돌을 세밀하게 보지 않고 Lerp로 이동하므로, 긴 이동에는 주의합니다.
- Animator에 `isChange`, `hAxisRaw`, `vAxisRaw`가 있으면 걷기 파라미터도 같이 구동합니다.

#### TriggerStep_PlayerWalkAnimation

파일: `Assets/Script/Trigger/Move/TriggerStep_PlayerWalkAnimation.cs`

역할:

- 플레이어 위치는 움직이지 않고 걷는 애니메이션만 일정 시간 재생합니다.

주요 필드:

- `segments`: 방향과 지속시간 목록
- `lockActionViaGameManager`: 애니메이션 중 입력 잠금
- `disablePlayerMainManagerWhileRunning`: 플레이어 컨트롤러가 애니메이션 파라미터를 덮어쓰지 못하게 함
- `zeroRigidbodyVelocity`: Rigidbody 속도 0 유지
- `setIdleAtEnd`: 끝나면 idle로 전환

언제 쓰나:

- 제자리에서 걷는 척하는 컷씬
- 컨베이어/꿈/기절 연출처럼 움직임 없이 애니메이션만 필요할 때

주의:

- 실제 이동은 하지 않습니다.
- 이동이 필요하면 `TriggerStep_PlayerMove`를 씁니다.

#### TriggerStep_PivotSequenceMove

파일: `Assets/Script/Trigger/Move/TriggerStep_PivotSequenceMove.cs`

역할:

- NPC나 오브젝트를 지정한 Pivot 위치까지 이동시킵니다.
- 여러 대상 이동을 순차 또는 병렬로 실행할 수 있습니다.

주요 필드:

- `elements`: 이동 묶음 목록
- `playMode`: `Sequential` 또는 `Parallel`
- `target`: 움직일 대상
- `pivot`: 도착 지점
- `duration`, `speed`: 이동 시간 또는 속도
- `animatorOverride`: 대상 Animator
- `setIdleAtEnd`: 이동 후 idle
- `finalFacing`: 도착 후 바라볼 방향
- `finalFacingApplyMode`: `IdleDirect` 또는 `WalkThenIdle`
- `lockPlayerInput`: 이동 중 플레이어 입력 잠금

언제 쓰나:

- `partyEntity/12`가 왼쪽으로 간 뒤 계단을 내려가는 연출
- NPC가 플레이어를 안내하며 이동
- 여러 NPC가 동시에 자리로 이동하는 연출

주의:

- 대상이 움직일 길을 직접 Pivot으로 찍어줘야 합니다.
- 걷기 애니메이션을 쓰려면 Animator 파라미터 `isChange`, `hAxisRaw`, `vAxisRaw`가 필요합니다.
- 걷기 Clip은 Loop가 켜져 있어야 자연스럽습니다.

#### TriggerStep_Follow

파일: `Assets/Script/Trigger/Move/TriggerStep_Follow.cs`

역할:

- 특정 오브젝트가 다른 대상 위치를 계속 따라가게 합니다.
- 추격 몬스터, 따라오는 NPC, 접촉하면 전투 진입하는 대상에 사용합니다.

주요 필드:

- `movingTarget`: 움직일 오브젝트
- `followTarget`: 따라갈 대상
- `moveSpeed`: 이동 속도
- `stopDistanceToFollow`: 따라갈 대상과 이 거리 이하가 되면 종료
- `stopPoint`: 특정 지점에 도달하면 종료
- `movingCollider`: 충돌 검사에 쓸 Collider2D
- `collisionMask`: 충돌 감지 Layer
- `onCollisionStep`: 충돌 시 실행할 추가 Step
- `maxFollowSeconds`: 최대 추적 시간

언제 쓰나:

- 괴물이 플레이어를 쫓아오다 닿으면 전투
- NPC가 플레이어를 따라오다 특정 지점에서 멈춤

주의:

- `BattleCollisionTransition`이 붙어 있으면 충돌 시 전투 진입을 외부에서 호출할 수 있습니다.
- `movingCollider`가 없으면 충돌 검사가 약해지므로 직접 연결 권장입니다.

#### TriggerStep_HandDrop

파일: `Assets/Script/Trigger/Move/TriggerStep_HandDrop.cs`

역할:

- 손/오브젝트가 빠르게 내려오거나 특정 방향으로 움직이는 연출용 Step입니다.
- 단일 오브젝트 또는 여러 오브젝트를 동시에/순차로 움직일 수 있습니다.

주요 필드:

- `handObject`: 기본 대상 오브젝트
- `moveDistanceX`, `moveDistanceY`, `dropDuration`: 레거시 단일 이동
- `useMoveSequence`, `moveSequence`: 방향/거리/시간 기반 이동 시퀀스
- `driveAnimatorLikePlayerMove`: PlayerMove식 Animator 파라미터 구동
- `forceDeactivateThenActivate`: 시작 시 껐다 켜서 등장 연출
- `useMultiTarget`: 여러 대상 모드
- `executionMode`: `Simultaneous` 또는 `Sequential`
- `targets`: 여러 대상 목록

언제 쓰나:

- 손이 내려치는 연출
- 여러 오브젝트가 동시에 떨어지는 연출
- 갑자기 나타나서 이동하는 오브젝트 연출

주의:

- 대상 오브젝트가 자기 자신이면 `SetActive(false)`로 코루틴이 끊길 수 있어 방어 로그가 뜹니다.
- 이동 시작 위치를 캐시하므로, 반복 실행 시 처음 위치로 되돌릴지 설정을 확인해야 합니다.

### 방향 / 회전 / 반전 Step

#### TriggerStep_facing

파일: `Assets/Script/Trigger/UpDownLeftRight/TriggerStep_facing.cs`

역할:

- NPC, 플레이어, 실행 주체가 특정 방향 또는 특정 오브젝트를 바라보게 합니다.

주요 필드:

- `targetSource`: `ExplicitObject`, `Player`, `Instigator`
- `targetObject`: 직접 지정 대상
- `lookMode`: `Direction` 또는 `OtherObject`
- `direction`: Up/Down/Left/Right/Custom
- `customDirection`: 커스텀 방향
- `lookTarget`: 바라볼 다른 오브젝트
- `animatorOverride`: Animator 직접 지정
- `facingAnimation`: 방향에 맞는 idle 지정
- `rotateTransformToDirection`: Transform 자체 회전 여부

언제 쓰나:

- NPC와 대화할 때 NPC가 플레이어를 바라봄
- 컷씬에서 캐릭터가 문/계단/다른 인물을 바라봄

주의:

- 일반 2D 캐릭터는 Transform 회전보다 Animator 파라미터로 방향을 바꾸는 쪽이 안전합니다.

#### TriggerStep_Angle

파일: `Assets/Script/Trigger/UpDownLeftRight/TriggerStep_Angle.cs`

역할:

- 대상 Transform을 일정 각도 회전시킵니다.
- 원하면 Pivot 기준으로 위치까지 같이 회전시킬 수 있습니다.

주요 필드:

- `targets`: 회전 대상 목록
- `direction`: Clockwise / CounterClockwise
- `angleDegrees`: 회전 각도
- `duration`: 회전에 걸리는 시간
- `useLocalRotation`: localRotation 기준인지 world rotation 기준인지
- `useCustomPivotPoint`: Pivot 기준 위치 회전 사용 여부
- `customPivotPoint`: 대상 로컬 기준 Pivot 좌표
- `useSelfWhenTargetsEmpty`: 대상이 비면 자기 자신 사용

언제 쓰나:

- 문이 열리는 회전 연출
- 오브젝트가 돌아가는 장치
- 중심축을 기준으로 도는 발판/장애물

주의:

- `useCustomPivotPoint`를 켜면 회전뿐 아니라 위치도 바뀝니다.

#### TriggerStep_Flip

파일: `Assets/Script/Trigger/UpDownLeftRight/TriggerStep_Flip.cs`

역할:

- 대상 Transform의 localScale 부호를 바꿔 좌우/상하 반전합니다.

주요 필드:

- `targets`: 반전 대상 목록
- `axis`: Horizontal / Vertical / Both
- `mode`: Toggle / ForceFlipped / ForceNormal
- `useSelfWhenTargetsEmpty`: 대상이 비면 자기 자신 사용

언제 쓰나:

- 스프라이트 좌우 방향 전환
- 오브젝트를 특정 방향으로 뒤집어 고정

주의:

- Transform scale을 직접 바꾸므로, 자식 오브젝트나 Collider 방향에도 영향이 갈 수 있습니다.

### 카메라 / 텔레포트 / 씬 전환 Step

#### TriggerStep_PlayerTeleport

파일: `Assets/Script/Trigger/teleport/TriggerStep_PlayerTeleport.cs`

역할:

- Route 중간에서 플레이어를 특정 Transform 위치로 텔레포트합니다.
- `TeleportTransition`과 달리 상호작용 컴포넌트가 아니라 TriggerStep입니다.

주요 필드:

- `targetPoint`: 이동 위치
- `offset`: 도착 위치 보정
- `useFade`, `fadePanel`, `fadeOutDuration`, `fadeInDuration`: 페이드 전환
- `lockPlayerInput`: 텔레포트 중 입력 잠금
- `afterMode`: 텔레포트 후 카메라 모드
- `afterBounds`: FollowConfined용 카메라 제한 영역
- `fixedCameraAnchorPoint`: Fixed/Cutscene용 카메라 고정 앵커
- `afterOrthoSize`: 텔레포트 후 카메라 크기
- `notifyWarpToCinemachine`, `snapCameraWhenFixed`: 카메라 워프 보정

언제 쓰나:

- 대화 후 플레이어를 다른 방으로 이동
- 수면/컷씬 중 화면을 검게 만든 뒤 위치 이동
- TriggerGet으로 밟은 이벤트 안에서 텔레포트

주의:

- 문/계단을 E로 눌러 바로 이동하는 용도면 `TeleportTransition`이 더 간단합니다.
- Route 중간에 텔레포트를 끼워 넣고 싶을 때 이 Step을 씁니다.

#### TriggerStep_StayThenTeleport

파일: `Assets/Script/Trigger/TriggerStep_StayThenTeleport.cs`

역할:

- 플레이어가 특정 Trigger 영역 안에 일정 시간 계속 머물면 텔레포트합니다.

주요 필드:

- `staySeconds`: 머물러야 하는 시간
- `stayArea`: 검사할 영역 Collider2D
- `targetPoint`, `offset`: 텔레포트 도착점
- `useFade`, `fadePanel`: 페이드 사용
- `lockPlayerInputDuringTeleport`: 실제 텔레포트 순간 입력 잠금
- `afterMode`, `afterBounds`, `fixedCameraAnchorPoint`, `afterOrthoSize`: 텔레포트 후 카메라
- `zeroVelocityAfterTeleport`: 이동 후 Rigidbody 속도 0

언제 쓰나:

- 특정 위치에 몇 초 서 있으면 이동
- 압력판, 잠시 기다리는 연출, 자동 방 이동

주의:

- 이 Step은 `AllowPlayerInputWhileExecuting = true`라서 Route 실행 중에도 플레이어가 움직일 수 있습니다.
- 플레이어가 영역 밖으로 나가면 텔레포트가 취소됩니다.

#### TriggerStep_CameraMove

파일: `Assets/Script/Trigger/camera_effect/TriggerStep_CameraMove.cs`

역할:

- 카메라 모드를 바꾸거나 특정 위치로 이동시킵니다.
- `TriggerGet`에 `cameraMoveStep`으로 병렬 연결해서 Route 실행 중 카메라를 따로 움직이는 방식도 지원합니다.

주요 모드:

- `FollowPlayer`: 플레이어 또는 지정 대상을 따라감
- `FixedPosition`: 특정 위치에 카메라 고정
- `MoveToPosition`: 특정 위치까지 부드럽게 이동

주요 필드:

- `followTargetOverride`, `followOrthoSize`
- `fixedAnchor`, `fixedWorldPosition`, `fixedOrthoSize`
- `moveTarget`, `moveTargetWorldPosition`, `moveDuration`, `moveEase`
- `restoreDuration`, `restoreEase`
- `runAsync`: true면 Step은 바로 끝나고 카메라 이동은 비동기 진행

언제 쓰나:

- 컷씬에서 카메라가 먼저 다른 곳을 비춤
- TriggerGet 발동 중 카메라를 특정 위치로 이동 후 Route 종료 뒤 원래 상태 복귀

주의:

- `runAsync = true`면 다음 Step이 바로 실행될 수 있습니다.
- 카메라 이동을 기다려야 한다면 `runAsync = false`를 사용합니다.

#### TriggerStep_CameraShake

파일: `Assets/Script/Trigger/camera_effect/TriggerStep_CameraShake.cs`

역할:

- 카메라 흔들림 효과를 줍니다.
- Cinemachine 이후 LateUpdate에서 최종 카메라 위치에 오프셋을 더합니다.

주요 필드:

- `targetCamera`: 비우면 Camera.main
- `duration`: 흔들림 시간
- `amplitude`: 흔들림 세기
- `frequency`: 흔들림 빈도
- `fadeOut`: 시간이 지날수록 약해지게 함
- `waitUntilDone`: 흔들림 끝까지 다음 Step 대기

언제 쓰나:

- 충격, 폭발, 놀람, 문이 닫히는 연출

주의:

- `waitUntilDone = false`면 흔들림 중 다음 Step이 바로 실행됩니다.

#### TriggerStep_Scene

파일: `Assets/Script/Trigger/SceneConversion/TriggerStep_Scene.cs`

역할:

- 다른 Scene으로 이동합니다.
- 꿈/수면 로드, 배틀 진입, 컷씬 시작 예약, 복귀 위치 저장까지 포함하는 큰 전환 Step입니다.

주요 필드:

- `sceneName`: 이동할 씬
- `loadMode`: Single/Additive
- `useSceneVisitEffectRunner`: 씬 방문 효과 러너 사용
- `lockPlayerInput`: 씬 전환 중 입력 잠금
- `markAsSleepLoad`: 수면 로드 플래그
- `loadSceneFromProgress`: progress의 lastSceneName으로 이동
- `fallbackDreamSceneName`: progress가 비었을 때 이동할 씬
- `overrideCutsceneStartKey`, `cutsceneStartKey`: 다음 씬 컷씬 시작 key 예약
- `saveReturnContext`: 배틀 복귀 정보 저장
- `returnPointOverride`, `graceSeconds`: 복귀 위치와 재진입 방지 시간
- `useReturnCameraOverride`, `captureCameraSnapshot`: 복귀 카메라 상태 저장

언제 쓰나:

- 현재 씬에서 다른 씬으로 넘어감
- 침대에서 꿈/챕터 씬으로 이동
- 배틀 씬 진입 전 복귀 정보 저장

주의:

- 같은 씬 안의 방 이동은 `TeleportTransition` 또는 `TriggerStep_PlayerTeleport`가 더 적합합니다.
- 실제 Unity Build Settings에 대상 Scene이 등록되어 있어야 합니다.

### 상태 변경 / 보상 Step

#### TriggerStep_SetActiveSwap

파일: `Assets/Script/Trigger/tilemap/TriggerStep_SetActiveSwap.cs`

역할:

- 오브젝트 목록을 켜거나 끕니다.

주요 필드:

- `disableObjects`: 끌 오브젝트 목록
- `enableObjects`: 켤 오브젝트 목록
- `disableFirst`: 끄기를 먼저 수행
- `waitOneFrameBeforeApply`: 적용 전 한 프레임 대기

언제 쓰나:

- NPC 퇴장 후 비활성화
- 타일맵/오브젝트 상태 전환
- 컷씬 후 문 열림/닫힘 상태 변경

주의:

- 자기 자신을 비활성화하면 그 뒤 Step 실행이 끊길 수 있으니 Route 구조를 주의합니다.

#### TriggerStep_PlayerSetActive

파일: `Assets/Script/Trigger/Active/TriggerStep_PlayerSetActive.cs`

역할:

- 플레이어 또는 지정 오브젝트를 활성/비활성화합니다.
- 잠깐 플레이어를 숨기거나, 특정 오브젝트를 잠시 껐다 켤 때 씁니다.

주요 필드:

- `op`: `Deactivate`, `Activate`, `DeactivateForSeconds`
- `targetScope`: `Player`, `Objects`, `PlayerAndObjects`
- `targetObjects`: 대상 오브젝트 목록
- `seconds`: `DeactivateForSeconds` 유지 시간
- `lockAction`: 실행 중 입력 잠금
- `syncPhysicsAfterEnable`: 다시 켠 뒤 물리 싱크
- `resetVelocity`, `clearMoveInput`: 플레이어 움직임 잔상 제거

언제 쓰나:

- 순간 사라짐/등장 연출
- 텔레포트 전후 플레이어 숨김
- 특정 오브젝트를 잠깐 비활성화

주의:

- 플레이어를 직접 끄면 참조/물리 상태가 꼬일 수 있어 `PlayerActiveService`를 통해 처리합니다.

#### TriggerStep_Card

파일: `Assets/Script/Trigger/Card/TriggerStep_Card.cs`

역할:

- 지정한 카드들을 `CardStateRuntime`에 소유 카드로 추가합니다.

주요 필드:

- `db`: 카드 유효성 검사용 `CardDatabaseSO`
- `cards`: 지급할 카드 목록

언제 쓰나:

- 이벤트 보상으로 카드 지급
- 튜토리얼에서 초기 카드 지급

주의:

- Awake에서 DB에 없는 카드나 null 카드는 제거합니다.
- 중복 카드는 HashSet으로 정리됩니다.

## TriggerStep 보조 스크립트

아래 스크립트들은 `TriggerStepBase`를 상속하지는 않지만, TriggerStep과 같이 쓰이거나 Trigger 시스템 동작을 보조합니다.

### TriggerSuppressTag

파일: `Assets/Script/Trigger/TriggerSuppressTag.cs`

역할:

- 특정 TriggerGet이나 Collider를 일정 시간 비활성화해서 재발동을 막습니다.
- 배틀 복귀 직후 같은 트리거가 다시 밟히는 문제를 막는 데 사용합니다.

주요 기능:

- `SuppressByKey(key, seconds)`: 같은 routeKey를 가진 트리거 억제
- `SuppressNearPoint(pos, radius, seconds, mask)`: 특정 위치 주변 트리거 억제
- `behavioursToDisable`이 비어 있으면 자동으로 `Collider2D`와 `TriggerGet`을 끕니다.

언제 쓰나:

- 전투 후 같은 위치로 복귀했는데 트리거가 즉시 재실행되는 경우
- 특정 이벤트 직후 잠깐 TriggerGet을 막아야 하는 경우

### BattleCollisionTransition

파일: `Assets/Script/Trigger/BattleCollisionTransition.cs`

역할:

- 플레이어와 충돌하면 배틀 씬으로 전환합니다.
- 복귀 위치, 복귀 카메라, 추적 오브젝트 상태를 저장합니다.
- `TriggerStep_Follow`에서 충돌이 발생했을 때 외부 호출로 전투 진입할 수 있습니다.

주요 기능:

- `OnTriggerEnter2D`, `OnCollisionEnter2D` 등으로 플레이어 충돌 감지
- `TryEnterFromExternal(Collider2D other)`: 다른 Step에서 전투 진입 호출 가능
- `PlayerReturnContext`에 복귀 씬/위치/카메라 상태 저장
- 배틀 복귀 후 재진입 방지 cooldown 처리
- 추격 오브젝트 재활성/지연 활성 처리

언제 쓰나:

- 괴물이 플레이어와 닿으면 전투
- 추적 이벤트 후 배틀 씬으로 넘어가기

주의:

- 플레이어 판정은 `playerTransform` 또는 태그 fallback 설정에 의존합니다.
- `enemyId` 또는 `encounterEnemy`가 DB에 맞게 설정되어야 합니다.

### TriggerMonster

파일: `Assets/Script/Trigger/TriggerMonster.cs`

역할:

- 오래된/단순 몬스터 트리거 스크립트로 보입니다.
- 플레이어가 3D Trigger에 들어오면 몬스터를 활성화하고, Update에서 특정 위치로 이동시킵니다.

주의:

- `OnTriggerEnter(Collider other)`를 쓰는 3D 물리 기반입니다.
- 현재 2D Trigger 시스템의 `TriggerGet`, `TriggerRouter`, `BattleCollisionTransition` 흐름과는 별개로 보는 것이 좋습니다.
- 새 이벤트는 가능하면 `TriggerGet + TriggerRouter + TriggerStep_Follow/BattleCollisionTransition` 구조를 권장합니다.

## Layer / Collider 규칙

### Trigger 방식

플레이어가 밟아서 실행하는 영역:

```text
GameObject
- BoxCollider2D
  - Is Trigger = true
- TriggerGet
```

Layer는 꼭 상호작용 Layer일 필요는 없지만, Collider가 플레이어와 충돌 이벤트를 받을 수 있어야 합니다.

### Interaction 방식

플레이어가 바라보고 `E`를 눌러 실행하는 대상:

```text
GameObject
- BoxCollider2D
- TriggerRouterInteraction 또는 TeleportTransition
```

Layer는 `PlayerInteractor`가 감지하는 Layer여야 합니다.

현재 자동 포함 Layer:

```text
Dialog
teleport
item_get
Object
Save
```

## 씬 Hierarchy 규칙

### Trigger

씬 전체 연출이나 지역 진입 트리거를 모아둡니다.

예시:

```text
Trigger
- TriggerRouter
- Trigger9
- Map5BedSleep
- Map32AfterChiefHouseExit
```

### interaction

방/오브젝트/침대/문처럼 플레이어가 직접 상호작용하는 오브젝트를 모아둡니다.

예시:

```text
interaction
- Room
- Room (1)
- Room (2)
- PartyRoom
- Save1
```

### Entity

일반 맵의 생명체 NPC를 모아둡니다.

규칙:

- `Entity` 루트에 `TriggerRouter`
- 자식 NPC는 `TriggerRouterInteraction`
- routeKey는 보통 `entity2`, `entity3`처럼 번호에 맞춤
- 생명체가 아닌 이벤트 오브젝트는 `Entity`에서 빼는 것이 좋음

### partyEntity

축제 맵의 생명체 NPC를 모아둡니다.

규칙:

- `partyEntity` 루트에 `TriggerRouter`
- `partyEntity/2`부터 각 NPC의 routeKey를 `entity2`, `entity3`처럼 연결
- 기본 Step 순서는 `facing -> dialogue`
- 축제용 NPC 대사는 이 라우터의 각 Route에 넣음

## 우리가 만든 실제 패턴

### partyEntity NPC 대화

`partyEntity`의 Route는 아래 구조로 정리했습니다.

```text
partyEntity
- TriggerRouter
  - entity2: facing -> dialogue
  - entity3: facing -> dialogue
  - entity4: facing -> dialogue
  - entity5: facing -> dialogue
  - entity7: facing -> dialogue
  - entity8: facing -> dialogue
  - entity9: facing -> dialogue
  - entity10: facing -> dialogue
  - entity11: facing -> dialogue
```

`partyEntity/5`는 상점 NPC로 변경했습니다.

```text
상인: 어셔옵쇼!
Lucy: 여기는 뭐하는 곳인가요…?
상인: 여긴 상점이야
```

나머지 NPC는 손님을 맞이하는 축제 분위기 대사로 채웠습니다.

### Map5BedSleep 흐름

침대 상호작용은 생명체가 아니므로 `Entity`가 아니라 `Trigger` 쪽 이벤트로 분리하는 것이 맞습니다.

의도한 흐름:

```text
map5 침대 상호작용
-> 화면 검정 상태
-> partymap5 침대 중앙으로 텔레포트
-> 잠에서 깨는 dialogue
-> Trigger9 연출로 이어짐
```

주의:

- partymap5 침대 중앙으로 텔레포트될 때 침대에 Collider가 있으면 플레이어가 끼거나 바로 상호작용 대상에 걸릴 수 있습니다.
- 따라서 텔레포트 도착 위치 주변 침대 Collider는 제거하거나 플레이어 이동을 막지 않는 구조로 둡니다.

### Trigger9 연출

`Trigger9`는 `partyEntity/12`를 이용한 대화 연출입니다.

현재 흐름:

```text
1. Lucy: 으악!!!!!!
2. 플레이어 점프 연출
3. ???와 Lucy 대화
4. partyEntity/12가 왼쪽으로 이동
5. partyEntity/12가 아래 계단 방향으로 이동
6. partyEntity/12 비활성화
```

NPC 이동에는 `TriggerStep_PivotSequenceMove`를 사용했습니다.

애니메이션 주의:

- `Character_119` 기반 Animation을 사용
- `Assets/Animated/Chapter1/PartyEntity12`에 Controller/Clip 생성
- 걷기 Clip은 Loop를 켜야 합니다.
- Animator Transition에서 같은 상태로 계속 재진입하면 걷기가 튀거나 멈추므로, 필요한 경우 `Can Transition To Self`를 꺼야 합니다.
- Route의 이동 Step에는 `animatorOverride`를 `partyEntity/12`의 Animator로 지정합니다.

### Map32AfterChiefHouseExit

촌장집 밖으로 나간 직후 나레이션을 위해 새 Trigger를 만들었습니다.

구조:

```text
Trigger
- Map32AfterChiefHouseExit
  - BoxCollider2D Is Trigger
  - TriggerGet routeKey = Map32AfterChiefHouseExit
  - Map32AfterChiefHouseExitDialogue
```

Route:

```text
TriggerRouter
- key: Map32AfterChiefHouseExit
  steps:
  1. TriggerStep_Dialogue
```

대사:

```text
Lucy: 여긴 쫓아오는 괴물은 없는거 같네…
나레이션: 촌장집 밖에 나가니, 긴장이 풀리면서 이 마을은 따뜻한 느낌을 받는다.
나레이션: 나는 마을을 둘러보기로 하였다.
나레이션: 사람들은 서로 인사를 나누며 웃고 있었고, 아이들은 뛰어다니며 떠들고 있었다.
나레이션: 햇빛은 따뜻했고, 바람도 부드러웠다.
```

## 다른 맵에 같은 구조를 복제하는 방법

### 1. Route 먼저 만들기

원하는 라우터를 선택합니다.

- 지역 밟기/컷씬: `Trigger` 루트의 `TriggerRouter`
- 일반 NPC: `Entity` 루트의 `TriggerRouter`
- 축제 NPC: `partyEntity` 루트의 `TriggerRouter`

그리고 `Routes`에 새 key를 추가합니다.

```text
key: 새RouteKey
steps:
- 실행할 TriggerStep들
```

### 2. Step 오브젝트 만들기

Route에서 실행할 Step을 자식 오브젝트로 만듭니다.

예시:

```text
Trigger
- MyEvent
  - MyEventDialogue        + TriggerStep_Dialogue
  - MyEventMove            + TriggerStep_PivotSequenceMove
  - MyEventDisable         + TriggerStep_SetActiveSwap
```

그 다음 Route의 `steps` 리스트에 순서대로 넣습니다.

### 3. 밟아서 실행할 때

```text
TriggerArea
- BoxCollider2D Is Trigger = true
- TriggerGet
  - router = 원하는 TriggerRouter
  - routeKey = 새RouteKey
  - maxCalls = 1 또는 0
```

### 4. 상호작용으로 실행할 때

```text
InteractObject
- BoxCollider2D
- TriggerRouterInteraction
  - router = 원하는 TriggerRouter
  - routeKey = 새RouteKey
```

Layer를 `Object`, `Dialog`, `teleport`, `Save`, `item_get` 중 상황에 맞게 설정합니다.

### 5. 텔레포트를 만들 때

문/계단/입구 오브젝트에 `TeleportTransition`을 붙입니다.

```text
RoomDoor
- BoxCollider2D
- TeleportTransition
  - targetPoint = 이동할 위치
  - fadePanel = FadePanel
  - afterMode = Fixed 또는 FollowConfined
  - fixedCameraAnchorPoint = 실내 카메라 앵커
  - afterBounds = 외부 맵 카메라 Bounds
```

대안으로, Route 중간에 텔레포트가 필요하면 `TriggerStep_PlayerTeleport`를 Step으로 넣습니다.

## 애니메이션 세팅 규칙

NPC를 `facing`이나 `PivotSequenceMove`로 움직일 때 Animator Controller는 아래 파라미터를 가져야 합니다.

```text
isChange : Bool
hAxisRaw : Int
vAxisRaw : Int
```

방향 값:

```text
Down  -> hAxisRaw = 0,  vAxisRaw = -1
Up    -> hAxisRaw = 0,  vAxisRaw = 1
Left  -> hAxisRaw = -1, vAxisRaw = 0
Right -> hAxisRaw = 1,  vAxisRaw = 0
Idle  -> isChange = false
Walk  -> isChange = true
```

폴더 규칙:

```text
Assets/Animated/Chapter1/PartyEntity2
Assets/Animated/Chapter1/PartyEntity3
...
Assets/Animated/Chapter1/PartyEntity12
```

새 NPC를 만들 때는 기존 `Entity3` 또는 만들어둔 `PartyEntity` 폴더를 참고해서 Controller/Clip을 복사하거나 새로 만듭니다.

## 다른 컴퓨터로 옮길 때 체크리스트

다른 PC에서 그대로 쓰려면 아래 항목을 같이 가져가야 합니다.

```text
Assets/Scenes/chapter1.unity
Assets/Script/Trigger/**
Assets/Script/Interactable/**
Assets/Script/Player/PlayerInteractor.cs
Assets/Script/Dialogue/**
Assets/Animated/Chapter1/PartyEntity*
Assets/ElvGames/Fantasy Dreamland/Sprites/Characters
ProjectSettings/TagManager.asset
ProjectSettings/ProjectSettings.asset
```

중요:

- `.meta` 파일을 반드시 같이 옮깁니다.
- Unity는 GUID로 참조를 잡기 때문에 `.meta`가 빠지면 Animator, Script, Sprite 연결이 끊길 수 있습니다.
- Git으로 옮기는 것이 가장 안전합니다.
- 압축해서 옮길 경우 `Assets`, `Packages`, `ProjectSettings`를 함께 압축합니다.
- `Library`는 보통 옮기지 않아도 됩니다. 새 PC에서 Unity가 다시 생성합니다.

## 문제 해결

### 상호작용이 안 될 때

확인할 것:

- 대상 오브젝트에 Collider2D가 있는가
- 대상 Layer가 `Object`, `Dialog`, `teleport`, `Save`, `item_get` 중 하나인가
- 대상 또는 부모에 `IInteractable` 컴포넌트가 있는가
  - `TriggerRouterInteraction`
  - `TeleportTransition`
  - `DialogueInteractable`
  - 기타 IInteractable 구현체
- `TriggerRouterInteraction.routeKey`가 라우터의 key와 같은가
- `router`가 올바른 라우터인가
  - `Entity`와 `partyEntity`는 헷갈리기 쉬우니 직접 연결 권장

### TriggerGet이 안 될 때

확인할 것:

- Collider2D의 `Is Trigger`가 켜져 있는가
- `TriggerGet.router`가 연결되어 있는가
- `TriggerGet.routeKey`가 라우터에 존재하는가
- 플레이어 오브젝트에 `PlayerMove`와 Collider2D가 있는가
- `maxCalls = 1`인 트리거를 이미 밟아서 소모한 것은 아닌가
- 배틀 복귀 grace 정책으로 막히는 트리거는 아닌가

### 대화는 뜨는데 다음 Step이 안 갈 때

확인할 것:

- `TriggerStep_Dialogue.waitUntilDone`이 의도와 맞는가
- DialogueManager가 대화 종료 상태로 정상 전환되는가
- `blockWorldAdvance`를 켰는데 `autoAdvance`를 안 켜서 플레이어가 넘길 수 없는 상태는 아닌가

### NPC 방향/걷기 애니메이션이 안 될 때

확인할 것:

- Animator Controller에 `isChange`, `hAxisRaw`, `vAxisRaw`가 있는가
- 파라미터 타입이 정확한가
  - `isChange`: Bool
  - `hAxisRaw`, `vAxisRaw`: Int
- Step의 `animatorOverride`가 올바른 Animator를 가리키는가
- 걷기 Clip의 Loop가 켜져 있는가
- Transition의 `Can Transition To Self` 때문에 같은 상태 재진입이 반복되는 것은 아닌가

### 텔레포트 후 카메라가 이상할 때

확인할 것:

- 실내면 `afterMode = Fixed`
- 실내면 `fixedCameraAnchorPoint` 연결
- 외부면 `afterMode = FollowConfined`
- 외부면 `afterBounds` 연결
- `targetPoint`가 실제 도착 위치에 있는가
- `notifyWarpToCinemachine`, `snapCameraWhenFixed`가 켜져 있는가

## 추천 작업 순서

새 이벤트를 만들 때는 아래 순서로 하면 실수가 적습니다.

```text
1. 어떤 라우터를 쓸지 정한다.
2. Route key 이름을 정한다.
3. TriggerStep 오브젝트를 만든다.
4. Route steps에 Step을 순서대로 연결한다.
5. 밟는 방식이면 TriggerGet을 만든다.
6. 상호작용 방식이면 TriggerRouterInteraction 또는 TeleportTransition을 만든다.
7. Layer와 Collider를 확인한다.
8. Play 모드에서 Console 로그로 routeKey가 실행되는지 확인한다.
9. Unity 씬을 저장한다.
```

## 저장 주의

Unity 에디터 제목에 `*`가 떠 있으면 씬 변경이 아직 저장되지 않은 상태입니다.

다른 PC로 옮기기 전에 반드시:

```text
File > Save
File > Save Project
```

또는 `Ctrl + S`로 씬을 저장한 뒤 옮깁니다.
