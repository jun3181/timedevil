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

