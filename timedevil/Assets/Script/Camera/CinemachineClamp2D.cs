using UnityEngine;
using Cinemachine;

[DisallowMultipleComponent]
public class CinemachineClamp2D : CinemachineExtension
{
    [SerializeField] private Collider2D boundsShape;
    [SerializeField] private bool debugDraw = false;

    // ¡Ú Ãß°¡
    public Collider2D BoundsShape => boundsShape;

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
        Vector3 clamped = new Vector3(
            Mathf.Clamp(pos.x, minX, maxX),
            Mathf.Clamp(pos.y, minY, maxY),
            pos.z
        );

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
