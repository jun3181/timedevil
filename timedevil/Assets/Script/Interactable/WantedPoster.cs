using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WantedPoster : MonoBehaviour, IInteractable
{
    [SerializeField]
    [Header("제거시 대사")]
    private Dialogue dialogue;

    void Start() {
        gameObject.layer = LayerMask.NameToLayer("Dialog");

        Collider2D cd2d = GetComponent<Collider2D>();
        cd2d.isTrigger = false;

    }

    public void Interact() {
        NPCPatrollerController.instance.StopSpawningRegularly();
        NPCPatrollerController.instance.IdleAllTroops();
        DialogueManager.OnDialogueEnd += OnDialogueEndEventHandler;

        DialogueManager.instance.StartDialogue(dialogue);

        Destroy(gameObject);
    }

    private void OnDialogueEndEventHandler() {
        DialogueManager.OnDialogueEnd -= OnDialogueEndEventHandler;
        NPCPatrollerController.instance.StartSpawningInstantly();
    }
}
