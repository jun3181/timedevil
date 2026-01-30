using UnityEngine;
using Cinemachine;

[DisallowMultipleComponent]
public class CinemachineClamp2D : CinemachineExtension
{
    [SerializeField] private Collider2D boundsShape; // BoxCollider2D 권장
    [SerializeField] private bool debugDraw = false;

    public void SetBounds(Collider2D shape)
    {
        boundsShape = shape;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Body 단계에서 최종 위치를 보정
        if (stage != CinemachineCore.Stage.Body) return;
        if (!enabled) return;
        if (!boundsShape) return;
        if (!state.Lens.Orthographic) return;

        // Collider2D의 AABB(bounding box) 기반 Clamp
        Bounds b = boundsShape.bounds;

        float halfH = state.Lens.OrthographicSize;
        float halfW = halfH * state.Lens.Aspect;

        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minY = b.min.y + halfH;
        float maxY = b.max.y - halfH;

        // 바운드가 카메라 화면보다 작으면 중앙 고정
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
        Vector3 clamped = new Vector3(
            Mathf.Clamp(pos.x, minX, maxX),
            Mathf.Clamp(pos.y, minY, maxY),
            pos.z
        );

        // 핵심: 최종 결과에 보정값을 더해 카메라를 박스 안으로 “밀어넣음”
        state.PositionCorrection += (clamped - pos);

        if (debugDraw)
        {
            Debug.DrawLine(new Vector3(b.min.x, b.min.y, pos.z), new Vector3(b.max.x, b.min.y, pos.z), Color.green);
            Debug.DrawLine(new Vector3(b.max.x, b.min.y, pos.z), new Vector3(b.max.x, b.max.y, pos.z), Color.green);
            Debug.DrawLine(new Vector3(b.max.x, b.max.y, pos.z), new Vector3(b.min.x, b.max.y, pos.z), Color.green);
            Debug.DrawLine(new Vector3(b.min.x, b.max.y, pos.z), new Vector3(b.min.x, b.min.y, pos.z), Color.green);
        }
    }
}
