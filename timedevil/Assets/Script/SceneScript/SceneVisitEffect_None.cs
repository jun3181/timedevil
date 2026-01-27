// Assets/Script/Scene/VisitEffect/SceneVisitEffect_None.cs
using System.Collections;
using UnityEngine;

public class SceneVisitEffect_None : SceneVisitEffectBase
{
    public override IEnumerator PlayEnter() { yield break; }
    public override IEnumerator PlayExit() { yield break; }
}
