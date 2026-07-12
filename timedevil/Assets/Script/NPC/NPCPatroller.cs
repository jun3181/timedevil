using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCPatroller : NPCMover, INPCMovement
{
    public enum NPCPatrollerDirection
    {
        Left, Right
    }

    public static float disappearanceXDistance;

    public delegate void NPCPatrollerEventHandler(int id);
    public static event NPCPatrollerEventHandler OnDisappearing;

    private static int patrollerCounter = 0;

    private static GameObject player;
    private static Rigidbody2D playerRigidbody2D;
    private static PlayerMove playerMove;

    public int PatrollerID { get; private set; }

    private Vector2 playerFirstPoint;
    private Vector2 firstDestinationPoint;
    private Vector2 extraDestinationPoint;

    private NPCPatrollerDirection direction;

    private IEnumerator patrollingCoroutine = null;

    protected override void Awake() {
        base.Awake();
        PatrollerID = patrollerCounter++;

        firstDestinationPoint = new(float.PositiveInfinity, float.PositiveInfinity);
        extraDestinationPoint = new(float.PositiveInfinity, float.PositiveInfinity);

        if(player == null) {
            player = GameObject.FindWithTag("Player");
            playerRigidbody2D = player.GetComponent<Rigidbody2D>();
            playerMove = player.GetComponent<PlayerMove>();
        }
    }

    public bool Move() {
        if(patrollingCoroutine != null) return false;

        patrollingCoroutine = Patrol();
        StartCoroutine(patrollingCoroutine);

        return true;
    }

    public new void Stop() {
        base.Stop();
        if(patrollingCoroutine!=null) {
            StopCoroutine(patrollingCoroutine);
        }
        ResetFields();
    }

    public new void Idle() {
        base.Idle();
        if(patrollingCoroutine != null) {
            StopCoroutine(patrollingCoroutine);
            patrollingCoroutine = null;
        }
    }

    private IEnumerator Patrol() {
        float xOffset;
        if(firstDestinationPoint.x==float.PositiveInfinity) {
            playerFirstPoint = playerRigidbody2D.position;

            xOffset = (playerFirstPoint.x>rb2d.position.x) ?  disappearanceXDistance : -disappearanceXDistance;
            if(xOffset < 0)
                direction = NPCPatrollerDirection.Left;
            else
                direction = NPCPatrollerDirection.Right;

            Debug.Log($"{gameObject.name}의 좌표 {rb2d.position}");
            firstDestinationPoint = rb2d.position;
            firstDestinationPoint.x += xOffset;
            Debug.Log(firstDestinationPoint);
            yield return MoveTo(firstDestinationPoint);
        } else {
            yield return Resume();
        }

        float xDistance = Mathf.Abs(playerRigidbody2D.position.x - rb2d.position.x);
        Debug.Log(disappearanceXDistance);
        if(xDistance>disappearanceXDistance) {
            ResetFields();
            gameObject.SetActive(false);
            OnDisappearing?.Invoke(PatrollerID);

            yield break;
        }

        xOffset = (direction==NPCPatrollerDirection.Left) ? -disappearanceXDistance : disappearanceXDistance;
        extraDestinationPoint = firstDestinationPoint;
        while(true) {
            extraDestinationPoint.x += xOffset;
            yield return MoveTo(extraDestinationPoint);

            xDistance = Mathf.Abs(playerRigidbody2D.position.x - rb2d.position.x);
            if(xDistance > disappearanceXDistance) {
                ResetFields();
                gameObject.SetActive(false);
                OnDisappearing?.Invoke(PatrollerID);

                yield break;
            }
        }
    }

    private void ResetFields() {
        patrollingCoroutine = null;

        firstDestinationPoint.x = float.PositiveInfinity;
        extraDestinationPoint.x = float.PositiveInfinity;
    }
}
