using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NPCMove 컴포넌트를 활용해 움직임을 구현하는 컴포넌트와의 계약
interface INPCMoveController
{
    public void Idle(); // NPC 일시정지
    public void Resume(); // NPC 움직임 재게
}
