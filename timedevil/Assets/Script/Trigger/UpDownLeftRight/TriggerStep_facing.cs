using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_facing : TriggerStepBase
{
    private const string DefaultParamIsChange = "isChange";
    private const string DefaultParamHAxisRaw = "hAxisRaw";
    private const string DefaultParamVAxisRaw = "vAxisRaw";

    public enum FacingTargetSource
    {
        ExplicitObject,
        Player,
        Instigator
    }

    public enum FacingLookMode
    {
        Direction,
        OtherObject
    }

    public enum FacingDirection
    {
        Up,
        Down,
        Left,
        Right,
        Custom
    }

    public enum FacingAnimation
    {
        AutoFromFacing,
        DownIdle,
        UpIdle,
        LeftIdle,
        RightIdle,
        None
    }

    [Header("1. Which Object")]
    [Tooltip("바라보는 방향을 바꿀 대상 오브젝트를 어디서 가져올지 선택합니다.")]
    [SerializeField] private FacingTargetSource targetSource = FacingTargetSource.ExplicitObject;

    [Tooltip("targetSource=ExplicitObject 일 때 방향을 바꿀 대상 오브젝트입니다.")]
    [SerializeField] private Transform targetObject;

    [Header("2. What To Face")]
    [Tooltip("정해진 방향을 볼지, 다른 오브젝트를 바라볼지 선택합니다.")]
    [SerializeField] private FacingLookMode lookMode = FacingLookMode.Direction;

    [Tooltip("lookMode=Direction 일 때 사용할 기본 방향입니다.")]
    [SerializeField] private FacingDirection direction = FacingDirection.Down;

    [Tooltip("direction=Custom 일 때 사용할 방향 벡터입니다. 0이면 아래쪽으로 처리합니다.")]
    [SerializeField] private Vector2 customDirection = Vector2.down;

    [Tooltip("lookMode=OtherObject 일 때 바라볼 대상 오브젝트입니다.")]
    [SerializeField] private Transform lookTarget;

    [Header("3. Animation")]
    [Tooltip("비워두면 1번 오브젝트에서 Animator를 자동으로 찾습니다.")]
    [SerializeField] private Animator animatorOverride;

    [Tooltip("AutoFromFacing이면 2번에서 계산된 방향에 맞춰 Idle 방향을 자동 선택합니다.")]
    [SerializeField] private FacingAnimation facingAnimation = FacingAnimation.AutoFromFacing;

    [Tooltip("Animator 파라미터를 적용할지 여부입니다.")]
    [SerializeField] private bool applyAnimatorParameters = true;

    [Tooltip("Animator 적용 시 isChange를 false로 만들어 Idle 상태를 보게 합니다.")]
    [SerializeField] private bool forceIdle = true;

    [SerializeField] private string paramIsChange = DefaultParamIsChange;
    [SerializeField] private string paramHAxisRaw = DefaultParamHAxisRaw;
    [SerializeField] private string paramVAxisRaw = DefaultParamVAxisRaw;

    [Header("Transform Facing (Optional)")]
    [Tooltip("체크하면 Transform의 Z 회전도 계산된 방향으로 맞춥니다. 기본 2D 캐릭터 애니메이션만 필요하면 끄세요.")]
    [SerializeField] private bool rotateTransformToDirection = false;

    [Tooltip("스프라이트가 오른쪽을 바라보는 것을 0도로 보는 기준입니다.")]
    [SerializeField] private float rotationOffsetDegrees = 0f;

    [Header("4. Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        Transform target = ResolveTarget(ctx);
        if (!target)
        {
            if (debugLog) Debug.LogWarning("[TriggerStep_facing] 대상 오브젝트를 찾지 못했습니다.", this);
            yield break;
        }

        Vector2 faceDir = ResolveFacingDirection(target);
        if (faceDir.sqrMagnitude <= 0.000001f)
            faceDir = Vector2.down;

        faceDir.Normalize();

        if (rotateTransformToDirection)
            ApplyTransformRotation(target, faceDir);

        if (applyAnimatorParameters)
        {
            Animator anim = animatorOverride ? animatorOverride : target.GetComponent<Animator>();
            if (!anim)
            {
                if (debugLog) Debug.LogWarning($"[TriggerStep_facing] Animator를 찾지 못했습니다. target='{target.name}'", target);
            }
            else if (HasAnimParams(anim))
            {
                ApplyFacingAnimation(anim, ResolveAnimation(faceDir));
            }
        }

        if (debugLog)
        {
            Debug.Log(
                $"[TriggerStep_facing] target='{target.name}', mode={lookMode}, dir=({faceDir.x:0.###},{faceDir.y:0.###}), " +
                $"anim={facingAnimation}, rotate={rotateTransformToDirection}",
                target
            );
        }

        yield break;
    }

    private Transform ResolveTarget(TriggerContext ctx)
    {
        switch (targetSource)
        {
            case FacingTargetSource.Player:
                if (ctx != null && ctx.player) return ctx.player;
                PlayerMove pm = Object.FindObjectOfType<PlayerMove>(true);
                return pm ? pm.transform : null;

            case FacingTargetSource.Instigator:
                return (ctx != null && ctx.instigator) ? ctx.instigator.transform : null;

            case FacingTargetSource.ExplicitObject:
            default:
                return targetObject;
        }
    }

    private Vector2 ResolveFacingDirection(Transform target)
    {
        if (lookMode == FacingLookMode.OtherObject)
        {
            if (!lookTarget)
            {
                if (debugLog) Debug.LogWarning("[TriggerStep_facing] lookTarget이 비어 있어 direction 설정을 대신 사용합니다.", this);
                return ResolveDirection(direction, customDirection);
            }

            return (Vector2)(lookTarget.position - target.position);
        }

        return ResolveDirection(direction, customDirection);
    }

    private Vector2 ResolveDirection(FacingDirection dir, Vector2 custom)
    {
        switch (dir)
        {
            case FacingDirection.Up: return Vector2.up;
            case FacingDirection.Down: return Vector2.down;
            case FacingDirection.Left: return Vector2.left;
            case FacingDirection.Right: return Vector2.right;
            case FacingDirection.Custom: return custom.sqrMagnitude > 0.000001f ? custom : Vector2.down;
            default: return Vector2.down;
        }
    }

    private FacingAnimation ResolveAnimation(Vector2 faceDir)
    {
        if (facingAnimation != FacingAnimation.AutoFromFacing)
            return facingAnimation;

        if (Mathf.Abs(faceDir.x) >= Mathf.Abs(faceDir.y))
            return faceDir.x >= 0f ? FacingAnimation.RightIdle : FacingAnimation.LeftIdle;

        return faceDir.y >= 0f ? FacingAnimation.UpIdle : FacingAnimation.DownIdle;
    }

    private bool HasAnimParams(Animator anim)
    {
        if (!anim) return false;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        bool hasChange = !forceIdle;
        bool hasH = false;
        bool hasV = false;

        var pars = anim.parameters;
        for (int i = 0; i < pars.Length; i++)
        {
            var p = pars[i];
            if (p.name == pChange && p.type == AnimatorControllerParameterType.Bool) hasChange = true;
            else if (p.name == pH && p.type == AnimatorControllerParameterType.Int) hasH = true;
            else if (p.name == pV && p.type == AnimatorControllerParameterType.Int) hasV = true;
        }

        if (!hasChange || !hasH || !hasV)
        {
            if (debugLog)
                Debug.LogWarning($"[TriggerStep_facing] Animator 파라미터 누락: '{pChange}', '{pH}', '{pV}'", anim);
            return false;
        }

        return true;
    }

    private void ApplyFacingAnimation(Animator anim, FacingAnimation animType)
    {
        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        if (!anim || animType == FacingAnimation.None)
        {
            if (anim && forceIdle) anim.SetBool(pChange, false);
            return;
        }

        int h = 0;
        int v = 0;

        switch (animType)
        {
            case FacingAnimation.DownIdle: v = -1; break;
            case FacingAnimation.UpIdle: v = 1; break;
            case FacingAnimation.LeftIdle: h = -1; break;
            case FacingAnimation.RightIdle: h = 1; break;
        }

        anim.SetInteger(pH, h);
        anim.SetInteger(pV, v);
        if (forceIdle) anim.SetBool(pChange, false);
    }

    private void ApplyTransformRotation(Transform target, Vector2 faceDir)
    {
        float z = Mathf.Atan2(faceDir.y, faceDir.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
        target.rotation = Quaternion.Euler(0f, 0f, z);
    }
}
