using UnityEngine;

[System.Serializable]
public struct EnemySnapshot
{
    public string instanceId;
    public string transformPath;
    public bool activeSelf;
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
        snap.transformPath = BuildTransformPath(enemy.transform);
        snap.activeSelf = enemy.activeSelf;

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
        if (!enemy) return;

        enemy.SetActive(activeSelf);

        var t = enemy.transform;
        t.position = position;
        t.rotation = Quaternion.Euler(0, 0, rotationZ);

        var rb = enemy.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = velocity;

        var anim = enemy.GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController && !string.IsNullOrEmpty(animStateName))
        {
            anim.Play(animStateName, 0, Mathf.Repeat(animNormalizedTime, 1f));
        }
    }

    private static string BuildTransformPath(Transform t)
    {
        if (t == null) return string.Empty;
        var stack = new System.Collections.Generic.Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack.ToArray());
    }
}
