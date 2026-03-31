using UnityEngine;
using UnityEngine.SceneManagement;

public class TriggerMonster : MonoBehaviour
{
    public GameObject monster;
    public Transform monsterTransform;
    public float speed = 3f;

    // 트리거 영역에 플레이어 들어오면 몬스터 활성화
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            monster.SetActive(true);
        }
    }

    // TriggrStep이 몬스터 따라가기
    void Update()
    {
        if (monster != null && monster.activeSelf)
        {
            transform.position = Vector3.MoveTowards(transform.position, monsterTransform.position, speed * Time.deltaTime);
        }
    }

    // 플레이어와 충돌 시 씬 전환
    private void OnTriggerEnterBattle(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("BattleTutorial");
        }
    }
}