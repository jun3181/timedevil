using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PartyFollower2D : MonoBehaviour
{
    private const string DefaultParamIsChange = "isChange";
    private const string DefaultParamHAxisRaw = "hAxisRaw";
    private const string DefaultParamVAxisRaw = "vAxisRaw";

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private bool followOnEnable = false;

    [Header("Follow")]
    [SerializeField, Min(0.1f)] private float followDistance = 1.2f;
    [SerializeField, Min(0f)] private float minimumTargetDistance = 0.85f;
    [SerializeField, Min(0f)] private float stopDistance = 0.08f;
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0.01f)] private float sampleSpacing = 0.08f;
    [SerializeField, Min(8)] private int maxSamples = 240;
    [SerializeField] private bool keepZ = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string paramIsChange = DefaultParamIsChange;
    [SerializeField] private string paramHAxisRaw = DefaultParamHAxisRaw;
    [SerializeField] private string paramVAxisRaw = DefaultParamVAxisRaw;

    [Header("Physics")]
    [SerializeField] private bool syncPhysicsAfterMove = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private readonly List<Vector3> _trail = new();
    private bool _isFollowing;
    private Vector3 _lastTargetPosition;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (!animator)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (followOnEnable)
            BeginFollow();
        else
            SetIdle();
    }

    private void OnDisable()
    {
        _isFollowing = false;
    }

    private void Update()
    {
        if (!_isFollowing)
            return;

        if (!target && autoFindPlayer)
            target = ResolvePlayerTransform();

        if (!target)
        {
            SetIdle();
            return;
        }

        AddTargetSampleIfNeeded();

        Vector3 desired = ResolveDesiredPosition();
        Vector3 current = transform.position;
        if (keepZ)
            desired.z = current.z;

        Vector3 next = Vector3.MoveTowards(current, desired, moveSpeed * Time.deltaTime);
        next = EnforceMinimumDistance(next);

        Vector3 delta = next - current;
        if (delta.sqrMagnitude <= stopDistance * stopDistance)
        {
            SetIdle();
            return;
        }

        transform.position = next;
        ApplyWalkAnimation(delta);

        if (syncPhysicsAfterMove)
            Physics2D.SyncTransforms();
    }

    public void BeginFollow(Transform targetOverride = null)
    {
        if (targetOverride)
            target = targetOverride;

        if (!target && autoFindPlayer)
            target = ResolvePlayerTransform();

        if (!target)
        {
            _isFollowing = false;
            SetIdle();
            Debug.LogWarning("[PartyFollower2D] Follow target not found.", this);
            return;
        }

        SeedTrail();
        _isFollowing = true;

        if (debugLog)
            Debug.Log($"[PartyFollower2D] BeginFollow target='{target.name}'", this);
    }

    public void StopFollow()
    {
        _isFollowing = false;
        SetIdle();
    }

    public void SetFollowing(bool active, Transform targetOverride = null)
    {
        if (active) BeginFollow(targetOverride);
        else StopFollow();
    }

    private void SeedTrail()
    {
        _trail.Clear();

        Vector3 targetPos = target.position;
        Vector3 facing = ResolveTargetFacing();
        Vector3 behind = targetPos - facing.normalized * Mathf.Max(followDistance, minimumTargetDistance);

        _trail.Add(behind);
        _trail.Add(targetPos);
        _lastTargetPosition = targetPos;
    }

    private void AddTargetSampleIfNeeded()
    {
        Vector3 currentTarget = target.position;
        if (_trail.Count == 0)
        {
            SeedTrail();
            return;
        }

        if ((currentTarget - _lastTargetPosition).sqrMagnitude < sampleSpacing * sampleSpacing)
            return;

        _trail.Add(currentTarget);
        _lastTargetPosition = currentTarget;

        while (_trail.Count > maxSamples)
            _trail.RemoveAt(0);
    }

    private Vector3 ResolveDesiredPosition()
    {
        if (_trail.Count == 0)
            return target ? target.position : transform.position;

        float remaining = Mathf.Max(followDistance, minimumTargetDistance);
        for (int i = _trail.Count - 1; i > 0; i--)
        {
            Vector3 newer = _trail[i];
            Vector3 older = _trail[i - 1];
            float segment = Vector2.Distance(newer, older);
            if (segment <= 0.0001f)
                continue;

            if (segment >= remaining)
            {
                float t = remaining / segment;
                return Vector3.Lerp(newer, older, t);
            }

            remaining -= segment;
        }

        return _trail[0];
    }

    private Vector3 EnforceMinimumDistance(Vector3 candidate)
    {
        if (!target || minimumTargetDistance <= 0f)
            return candidate;

        Vector3 targetPos = target.position;
        Vector3 fromTarget = candidate - targetPos;
        fromTarget.z = 0f;

        float distance = fromTarget.magnitude;
        if (distance >= minimumTargetDistance)
            return candidate;

        if (distance <= 0.0001f)
            fromTarget = -ResolveTargetFacing();

        Vector3 adjusted = targetPos + fromTarget.normalized * minimumTargetDistance;
        if (keepZ)
            adjusted.z = candidate.z;

        return adjusted;
    }

    private Vector3 ResolveTargetFacing()
    {
        if (target)
        {
            var move = target.GetComponent<PlayerMove>();
            if (!move) move = target.GetComponentInChildren<PlayerMove>(true);
            if (move && move.Facing.sqrMagnitude > 0.0001f)
                return move.Facing;

            Vector3 delta = target.position - _lastTargetPosition;
            delta.z = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                return delta.normalized;
        }

        return Vector3.down;
    }

    private Transform ResolvePlayerTransform()
    {
        var pmm = FindObjectOfType<PlayerMainManager>(true);
        if (pmm) return pmm.transform;

        var pm = FindObjectOfType<PlayerMove>(true);
        if (pm) return pm.transform;

        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }

    private bool HasAnimParams()
    {
        if (!animator) return false;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        bool hasChange = false;
        bool hasH = false;
        bool hasV = false;

        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.name == pChange && p.type == AnimatorControllerParameterType.Bool) hasChange = true;
            else if (p.name == pH && p.type == AnimatorControllerParameterType.Int) hasH = true;
            else if (p.name == pV && p.type == AnimatorControllerParameterType.Int) hasV = true;
        }

        return hasChange && hasH && hasV;
    }

    private void ApplyWalkAnimation(Vector3 delta)
    {
        if (!HasAnimParams())
            return;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        int h = 0;
        int v = 0;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            h = delta.x >= 0f ? 1 : -1;
        else
            v = delta.y >= 0f ? 1 : -1;

        animator.SetInteger(pH, h);
        animator.SetInteger(pV, v);
        animator.SetBool(pChange, true);
    }

    private void SetIdle()
    {
        if (!HasAnimParams())
            return;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        animator.SetBool(pChange, false);
    }
}
