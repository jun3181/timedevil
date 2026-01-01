// Assets/Script/Enemy/EnemyInstanceId.cs
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyInstanceId : MonoBehaviour
{
    [SerializeField] private string instanceId;

    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(instanceId))
                instanceId = gameObject.name;
            return instanceId;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(instanceId))
            instanceId = gameObject.name;
    }
#endif
}
