using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MonsterTriggerZone : MonoBehaviour
{
    [Header("Activate Targets")]
    public GameObject[] monstersToActivate;

    [Header("Trigger")]
    public bool triggerOnce = true;

    private bool hasBeenTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasBeenTriggered)
            return;

        var playerAction = other.GetComponent<PlayerAction>();
        if (playerAction == null)
            return;

        hasBeenTriggered = true;

        foreach (GameObject monster in monstersToActivate)
        {
            if (monster == null) continue;

            monster.SetActive(true);

            MonsterMover mover = monster.GetComponent<MonsterMover>();
            if (mover != null)
                mover.StartChase(playerAction.transform);
        }

        if (triggerOnce)
            gameObject.SetActive(false);
    }
}
