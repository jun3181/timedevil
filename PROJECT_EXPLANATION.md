# 카이로스: 타임데빌 프로젝트 설명

## 1) 프로젝트 개요
`카이로스: 타임데빌`은 Unity 기반의 2D 게임 프로젝트로, 월드 탐험/상호작용/대화 시스템과 카드 기반 전투 시스템이 결합된 구조를 갖고 있습니다.

- 메인 흐름: 메인 메뉴 → 월드(예: Myroom) → 이벤트/대화/트리거 → 전투 씬 → 복귀
- 특징: 컷씬 입력 잠금, 타입라이터 대화 UI, ScriptableObject 기반 카드/적 데이터, 저장/불러오기

---

## 2) 기술 스택 및 실행 환경
- 엔진: Unity 2022.3 LTS 계열
- 주요 패키지: 2D Feature, URP, Cinemachine, TextMeshPro, Timeline, UGUI
- 프로젝트 성격: 학기 프로젝트(스토리 연출 + 시스템 구현 중심)

---

## 3) 씬 구성 (빌드 세팅 기준)
현재 빌드 세팅에 등록된 대표 씬은 다음과 같습니다.

1. `Mainmenu`
2. `Myroom`
3. `battle`
4. `InventoryScene`
5. `Card`
6. `chapter1`
7. `Move_Tutorial`

즉, 메뉴/월드/전투/튜토리얼/카드/인벤토리 씬을 분리해 기능 단위로 운영하는 구조입니다.

---

## 4) 핵심 시스템 구조

### A. 플레이어 입력/상태 제어
- 중심 스크립트: `PlayerMainManager`
- 역할:
  - 대화 상태에서 E키로 대사 진행
  - 메뉴 상태(Q/W 닫기, 화살표 이동, E 선택)
  - 컷씬/액션락 상태에서 이동·상호작용 차단
  - 월드 상태에서 화살표 이동 + E 상호작용

입력 우선순위를 명확히 분기해, 대화 중 메뉴가 열리거나 컷씬 중 상호작용이 섞이는 문제를 방지하도록 설계되어 있습니다.

### B. 전역 잠금/상호작용 UI
- 중심 스크립트: `GameManager`
- 역할:
  - `isAction`을 락 카운트 기반으로 관리 (`LockAction`/`UnlockAction`)
  - 씬 전환 시 잠금 상태 정리
  - 간단 상호작용 UI 패널 열기/닫기

`isAction`을 직접 토글하지 않고 카운트 방식으로 운영해, 잠금 해제 누락 버그를 줄이는 패턴을 사용합니다.

### C. 대화/컷씬 시스템
- 중심 스크립트: `DialogueManager`
- 역할:
  - 큐 기반 대사 진행
  - 타입라이터 출력
  - 컷씬 입력 차단(`blockInput`) 및 우회 API 제공

일반 플레이 입력과 컷씬 제어 입력을 분리하여, 연출 중 오입력으로 대사가 스킵되는 문제를 방어합니다.

### D. 카드 전투 시스템
- 핵심 데이터:
  - `BaseCardSO`: 카드 기본 메타(아이디, 이름, 코스트, 설명 등)
  - `CardDatabaseSO`: 카드 id → SO 조회
- 런타임:
  - `BattleDeckRuntime`: 덱/핸드, 셔플, 드로우, 사용/버리기 순환
  - `TurnManager`: 턴 상태/선턴 결정/튜토리얼 인트로 관리
  - `EnemyTurnController`: 적 카드 사용 로직(Draw/Move/Attack 분기 실행)

ScriptableObject 기반 데이터 + 런타임 로직 분리로, 카드 추가/밸런싱이 비교적 용이한 구조입니다.

### E. 전투 진입/복귀
- 중심 스크립트: `BattleTransition`, `BattleBootstrap`
- 역할:
  - 월드에서 전투 진입 시 복귀 씬/위치 저장
  - 전투 씬에서 플레이어/적 런타임 보정 생성
  - 적 DB에서 적 데이터 로드 후 UI 바인딩

### F. 트리거 라우팅
- 중심 스크립트: `TriggerRouter`
- 역할:
  - key 기반으로 step 시퀀스를 실행
  - 재진입 정책, 실행 중복 제어, 디버그 로그 제공

맵 이벤트를 "트리거 키 → 실행 스텝들"로 분리해 시나리오 연출을 유연하게 구성할 수 있습니다.

### G. 저장/불러오기
- 중심 스크립트: `SaveSystem`, `ProgressSaveData`, `ProgressLoadApplier`
- 저장 대상:
  - 카드 상태
  - 아이템 상태
  - 플레이어 데이터
  - 진행도(씬명, 위치, 카메라 상태, 플래그)

`MainMenu.NewGame()`에서는 기존 세이브를 정리해 항상 새 시작이 되도록 보장합니다.

---

## 5) 폴더 관점에서 본 코드 성격
- `Assets/Script/Battle`: 전투, 카드, 적 턴/덱/UI
- `Assets/Script/Player`: 이동/입력/메뉴/카드/아이템 런타임
- `Assets/Script/Dialogue`: 대화 데이터/매니저/상호작용
- `Assets/Script/Trigger`: 트리거 스텝 실행 라우터
- `Assets/Script/Save`: 저장/로드/체크포인트 로직
- `Assets/Script/Camera`, `SceneScript`, `loader`: 씬 연출/카메라/복귀 보조

기능별 폴더 분리가 되어 있어, 신규 기능 추가 시 책임 경계를 비교적 명확히 유지할 수 있습니다.

---

## 6) 프로젝트를 처음 보는 사람을 위한 빠른 진입 순서
1. `ProjectSettings/EditorBuildSettings.asset`에서 씬 흐름 확인
2. `Assets/Script/Player/PlayerMainManager.cs`로 입력 구조 이해
3. `Assets/Script/GameManager.cs`, `Assets/Script/Dialogue/DialougueManager.cs`로 월드/연출 잠금 흐름 파악
4. `Assets/Script/Battle/*`로 카드 전투 구조 파악
5. `Assets/Script/Save/*`로 세이브 포맷/적용 정책 확인

---

## 7) 한 줄 결론
이 프로젝트는 **스토리 연출(대화/컷씬) + 탐험(상호작용/트리거) + 카드 전투(데이터 중심 설계)**를 Unity에서 통합한, 구조화가 잘 된 학기 프로젝트입니다.
