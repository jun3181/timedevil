using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INPCMovement
{
    public bool Move();
    public void Stop();
    public void Idle();
}
