using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCPatrollerPlayerDetector : MonoBehaviour
{
    [SerializeField]
    [Header("플래이어 발견시 대사")]
    private Dialogue dialogue;

    void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player") && !BaseHideout.Hiding) {
            NPCPatrollerController.instance.StopSpawningRegularly();
            NPCPatrollerController.instance.StopAllCoroutines();
            NPCPatrollerController.instance.IdleAllTroops();

            DialogueManager.OnDialogueEnd += Handler;
            DialogueManager.instance.StartDialogue(dialogue);
        }
    }

    private void Handler() {
        DialogueManager.OnDialogueEnd -= Handler;

        SceneLoader.Load("Mainmenu");
    }
}
