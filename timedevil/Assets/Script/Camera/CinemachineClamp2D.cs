using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class CinemachineClamp2D : CinemachineExtension
{
    [SerializeField] private Collider2D boundsShape; // BoxCollider2D 권장
    [Tooltip("PolygonCollider2D를 테두리/벽처럼 두고, collider가 채우지 않은 빈 공간을 카메라 영역으로 사용합니다.")]
    [SerializeField] private bool useEmptyPolygonSpace = true;
    [SerializeField] private bool debugDraw = false;

    // 추가: CameraManager 스냅샷용
    public Collider2D CurrentBounds => boundsShape;

    private readonly List<float> _intersections = new List<float>(16);
    private readonly List<Range> _ranges = new List<Range>(8);
    private PolygonCollider2D _cachedPolygon;
    private Vector2[][] _cachedPolygonPaths;
    private bool _hasLastPolygonX;
    private float _lastPolygonX;

    private struct Range
    {
        public readonly float min;
        public readonly float max;

        public Range(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    public void SetBounds(Collider2D shape)
    {
        boundsShape = shape;
        CachePolygonPaths(shape as PolygonCollider2D);
        ResetPolygonRangeState();
    }

    public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
    {
        ResetPolygonRangeState();
        base.OnTargetObjectWarped(target, positionDelta);
    }

    private void OnDisable()
    {
        ResetPolygonRangeState();
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body) return;
        if (!enabled) return;
        if (!boundsShape) return;
        if (!state.Lens.Orthographic) return;

        Bounds b = boundsShape.bounds;

        float halfH = state.Lens.OrthographicSize;
        float halfW = halfH * state.Lens.Aspect;

        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minY = b.min.y + halfH;
        float maxY = b.max.y - halfH;

        if (minX > maxX)
        {
            float cx = (b.min.x + b.max.x) * 0.5f;
            minX = maxX = cx;
        }
        if (minY > maxY)
        {
            float cy = (b.min.y + b.max.y) * 0.5f;
            minY = maxY = cy;
        }

        Vector3 pos = state.FinalPosition;
        Vector2 clamped2D = new Vector2(
            Mathf.Clamp(pos.x, minX, maxX),
            Mathf.Clamp(pos.y, minY, maxY)
        );

        if (boundsShape is PolygonCollider2D polygon && polygon.pathCount > 0)
        {
            clamped2D.x = ClampPolygonXWhenSafe(polygon, clamped2D.x, clamped2D.y, halfW);
        }
        else
        {
            ResetPolygonRangeState();
        }

        Vector3 clamped = new Vector3(clamped2D.x, clamped2D.y, pos.z);

        state.PositionCorrection += (clamped - pos);

        if (debugDraw)
        {
            Debug.DrawLine(new Vector3(b.min.x, b.min.y, pos.z), new Vector3(b.max.x, b.min.y, pos.z), Color.green);
            Debug.DrawLine(new Vector3(b.max.x, b.min.y, pos.z), new Vector3(b.max.x, b.max.y, pos.z), Color.green);
            Debug.DrawLine(new Vector3(b.max.x, b.max.y, pos.z), new Vector3(b.min.x, b.max.y, pos.z), Color.green);
            Debug.DrawLine(new Vector3(b.min.x, b.max.y, pos.z), new Vector3(b.min.x, b.min.y, pos.z), Color.green);
        }
    }

    private float ClampPolygonXWhenSafe(PolygonCollider2D polygon, float x, float y, float halfW)
    {
        EnsurePolygonPathCache(polygon);
        BuildHorizontalCenterRanges(polygon, y, halfW);

        if (_ranges.Count == 0)
            return KeepLastPolygonXIfPossible(x);

        float clampedX;
        if (TryGetContainingRange(x, _ranges, out Range targetRange))
        {
            clampedX = x;
        }
        else if (_hasLastPolygonX && TryGetContainingRange(_lastPolygonX, _ranges, out Range lastRange))
        {
            clampedX = Mathf.Clamp(x, lastRange.min, lastRange.max);
        }
        else
        {
            clampedX = ClampToNearestRange(x, _ranges);
        }

        RememberPolygonX(clampedX);
        return clampedX;
    }

    private float KeepLastPolygonXIfPossible(float fallbackX)
    {
        if (_hasLastPolygonX)
            return _lastPolygonX;

        RememberPolygonX(fallbackX);
        return fallbackX;
    }

    private void RememberPolygonX(float x)
    {
        _hasLastPolygonX = true;
        _lastPolygonX = x;
    }

    private void ResetPolygonRangeState()
    {
        _hasLastPolygonX = false;
        _lastPolygonX = 0f;
    }

    private void BuildHorizontalCenterRanges(PolygonCollider2D polygon, float y, float halfW)
    {
        _intersections.Clear();
        _ranges.Clear();

        for (int pathIndex = 0; pathIndex < polygon.pathCount; pathIndex++)
        {
            Vector2[] path = _cachedPolygonPaths[pathIndex];
            int count = path != null ? path.Length : 0;
            if (count < 3) continue;

            Vector2 prev = ToWorldPoint(polygon, path[count - 1]);
            for (int i = 0; i < count; i++)
            {
                Vector2 curr = ToWorldPoint(polygon, path[i]);
                if (CrossesHorizontalLine(prev, curr, y))
                {
                    float t = (y - prev.y) / (curr.y - prev.y);
                    _intersections.Add(Mathf.Lerp(prev.x, curr.x, t));
                }

                prev = curr;
            }
        }

        BuildCenterRangesFromIntersections(polygon, y, halfW);
    }

    private void EnsurePolygonPathCache(PolygonCollider2D polygon)
    {
        if (_cachedPolygon == polygon &&
            _cachedPolygonPaths != null &&
            _cachedPolygonPaths.Length == polygon.pathCount)
        {
            return;
        }

        CachePolygonPaths(polygon);
    }

    private void CachePolygonPaths(PolygonCollider2D polygon)
    {
        _cachedPolygon = polygon;

        if (!polygon || polygon.pathCount <= 0)
        {
            _cachedPolygonPaths = null;
            return;
        }

        _cachedPolygonPaths = new Vector2[polygon.pathCount][];
        for (int i = 0; i < polygon.pathCount; i++)
            _cachedPolygonPaths[i] = polygon.GetPath(i);
    }

    private void BuildCenterRangesFromIntersections(PolygonCollider2D polygon, float y, float halfExtent)
    {
        if (_intersections.Count < 2)
            return;

        _intersections.Sort();

        for (int i = _intersections.Count - 2; i >= 0; i--)
        {
            if (Mathf.Abs(_intersections[i + 1] - _intersections[i]) < 0.001f)
                _intersections.RemoveAt(i + 1);
        }

        float fallbackMin = 0f;
        float fallbackMax = 0f;
        float fallbackWidth = 0f;

        for (int i = 0; i + 1 < _intersections.Count; i++)
        {
            float min = _intersections[i];
            float max = _intersections[i + 1];
            float width = max - min;
            if (width >= halfExtent * 2f && width > fallbackWidth)
            {
                fallbackMin = min;
                fallbackMax = max;
                fallbackWidth = width;
            }

            float mid = (min + max) * 0.5f;
            bool overlapsCollider = polygon.OverlapPoint(new Vector2(mid, y));
            if (useEmptyPolygonSpace ? overlapsCollider : !overlapsCollider)
                continue;

            float centerMin = min + halfExtent;
            float centerMax = max - halfExtent;

            if (centerMin <= centerMax)
                _ranges.Add(new Range(centerMin, centerMax));
        }

        if (_ranges.Count == 0 && fallbackWidth > 0f)
            _ranges.Add(new Range(fallbackMin + halfExtent, fallbackMax - halfExtent));
    }

    private static bool CrossesHorizontalLine(Vector2 a, Vector2 b, float y)
    {
        return (a.y <= y && b.y > y) || (b.y <= y && a.y > y);
    }

    private static Vector2 ToWorldPoint(PolygonCollider2D polygon, Vector2 localPoint)
    {
        return polygon.transform.TransformPoint(localPoint + polygon.offset);
    }

    private static bool TryGetContainingRange(float value, List<Range> ranges, out Range result)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            Range range = ranges[i];
            if (value >= range.min && value <= range.max)
            {
                result = range;
                return true;
            }
        }

        result = new Range(0f, 0f);
        return false;
    }

    private static float ClampToNearestRange(float value, List<Range> ranges)
    {
        if (ranges == null || ranges.Count == 0)
            return value;

        float best = value;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < ranges.Count; i++)
        {
            Range range = ranges[i];
            float candidate = Mathf.Clamp(value, range.min, range.max);
            float distance = Mathf.Abs(candidate - value);

            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }
}
