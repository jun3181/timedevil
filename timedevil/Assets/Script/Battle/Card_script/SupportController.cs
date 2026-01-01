using System.Collections;
using UnityEngine;

public class SupportController : MonoBehaviour
{
    public IEnumerator Execute(SupportCardSO so, Faction self, Faction foe)
    {
        Debug.Log($"[SupportController] Execute: id={so.id}, name={so.displayName}, action={so.action}, self={self}, foe={foe}");
        // TODO: 버프/디버프 지속/수치 적용
        yield return null;
    }
}
