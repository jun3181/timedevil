using UnityEngine;

// 이 스크립트는 2D 콜라이더를 필수로 요구합니다.
[RequireComponent(typeof(Collider2D))]
public class MonsterTriggerZone : MonoBehaviour
{
    [Header("활성화시킬 몬스터")]
    [Tooltip("이 구역에 들어갔을 때 나타나서 움직일 몬스터 오브젝트들")]
    public GameObject[] monstersToActivate;

    [Header("트리거 설정")]
    [Tooltip("체크하면 이 트리거는 딱 한 번만 작동합니다.")]
    public bool triggerOnce = true;

    private bool hasBeenTriggered = false; // 이미 발동했는지 확인

    // 이 오브젝트의 'IsTrigger' 콜라이더에 무언가 들어왔을 때 호출됨
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 이미 발동했고, 한 번만 작동하는 옵션이 켜져있다면 무시
        if (triggerOnce && hasBeenTriggered)
        {
            return;
        }

        // 2. 들어온 오브젝트가 'PlayerAction' 스크립트(즉, 플레이어)를 가졌는지 확인
        if (other.GetComponent<PlayerAction>() != null)
        {
            Debug.Log("플레이어가 트리거 존에 진입!");
            hasBeenTriggered = true; // 발동했다고 표시

            // 3. 연결된 모든 몬스터에 대해 반복
            foreach (GameObject monster in monstersToActivate)
            {
                if (monster != null)
                {
                    // 3a. 몬스터를 '나타나게' 함 (활성화)
                    monster.SetActive(true);

                    // 3b. 몬스터의 UndeadMover 스크립트를 찾음
                    UndeadMover mover = monster.GetComponent<UndeadMover>();
                    if (mover != null)
                    {
                        // 3c. 몬스터의 'StartPatrol()' 함수를 호출하여 이동 시작
                        mover.StartPatrol();
                    }
                }
            }

            // 4. (선택 사항) 딱 한 번만 발동하는 경우, 트리거 존 자체를 꺼버림
            if (triggerOnce)
            {
                gameObject.SetActive(false);
            }
        }
    }
}