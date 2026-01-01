// Assets/Script/loader/EnemySnapshot.cs
using UnityEngine;

[System.Serializable]
public struct EnemySnapshot
{
    public string instanceId;
    public Vector2 position;
    public float rotationZ;
    public Vector2 velocity;
    public string animStateName;
    public float animNormalizedTime;

    public static EnemySnapshot Capture(GameObject enemy)
    {
        var snap = new EnemySnapshot();

        var id = enemy.GetComponent<EnemyInstanceId>();
        snap.instanceId = id ? id.Id : enemy.name;

        var t = enemy.transform;
        snap.position = t.position;
        snap.rotationZ = t.eulerAngles.z;

        var rb = enemy.GetComponent<Rigidbody2D>();
        snap.velocity = rb ? rb.velocity : Vector2.zero;

        var anim = enemy.GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            snap.animStateName = anim.GetCurrentAnimatorClipInfo(0).Length > 0
                ? anim.GetCurrentAnimatorClipInfo(0)[0].clip.name
                : "";
            snap.animNormalizedTime = info.normalizedTime % 1f;
        }
        else
        {
            snap.animStateName = "";
            snap.animNormalizedTime = 0f;
        }

        return snap;
    }

    public void ApplyTo(GameObject enemy)
    {
        var t = enemy.transform;
        t.position = position;
        t.rotation = Quaternion.Euler(0, 0, rotationZ);

        var rb = enemy.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = velocity;

        var anim = enemy.GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController && !string.IsNullOrEmpty(animStateName))
        {
            // 같은 이름의 클립이 있으면 타임을 맞춰 재생 (없으면 무시)
            anim.Play(animStateName, 0, Mathf.Repeat(animNormalizedTime, 1f));
        }
    }
}
