using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WantedPoster : MonoBehaviour, IInteractable
{
    private static int instanceCounter = 0;
    public static int InstanceCounter { get { return instanceCounter; } private set { instanceCounter = value; } }

    private static int detachingCounter = 0;
    public static int DetachingCounter
    {
        private set
        {
            detachingCounter = value;
            Debug.Log(detachingCounter);
            if(detachingCounter==instanceCounter) {
                NPCPatrollerController.instance.spawningRegularlyAfterInstantSpawn = false;
            }
        }
        get
        {
            return detachingCounter;
        }
    }

    [SerializeField]
    [Header("제거시 대사")]
    private Dialogue dialogue;

    void Start() {
        instanceCounter++;
        gameObject.layer = LayerMask.NameToLayer("Dialog");

        Collider2D cd2d = GetComponent<Collider2D>();
        cd2d.isTrigger = false;

    }

    public void Interact() {
        DetachingCounter++;

        NPCPatrollerController.instance.StopSpawningRegularly();
        NPCPatrollerController.instance.IdleAllTroops();
        DialogueManager.OnDialogueEnd += OnDialogueEndEventHandler;

        DialogueManager.instance.StartDialogue(dialogue);

        Destroy(gameObject);
    }

    private void OnDialogueEndEventHandler() {
        DialogueManager.OnDialogueEnd -= OnDialogueEndEventHandler;
        NPCPatrollerController.instance.ResumeAllTroops();
        NPCPatrollerController.instance.StartSpawningInstantly();
    }
}
