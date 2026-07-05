using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCPatroller : NPCMover, INPCMovement
{
    public enum NPCPatrollerDirection {
        Left, Right
    }

    public override float Speed
    {
        set
        {
            if(value < 0) return;
            
            base.Speed = value;
            if(direction==NPCPatrollerDirection.Left) {
                velocityPerRoutine = Vector2.left * value;
            } else {
                velocityPerRoutine = Vector2.right * value;
            }
        }

        get
        {
            return base.Speed;
        }
    }

    public NPCPatrollerDirection Direction
    {
        set
        {
            if(patrollingCoroutine != null) return;
            if(value == NPCPatrollerDirection.Left)
                velocityPerRoutine = Vector2.left * base.Speed;
            else
                velocityPerRoutine = Vector2.right * base.Speed;

            direction = value;
        }

        get
        {
            return direction;
        }
    }

    private NPCPatrollerDirection direction = NPCPatrollerDirection.Left;
    private Vector2 velocityPerRoutine;

    private IEnumerator patrollingCoroutine = null;
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
            patrollingCoroutine = null;
        }
    }

    public new void Idle() {
        base.Idle();
        if(patrollingCoroutine != null) {
            StopCoroutine(patrollingCoroutine);
            patrollingCoroutine = null;
        }
    }

    private IEnumerator Patrol() {
        yield break;
    }
}
