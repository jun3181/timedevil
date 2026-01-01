using System; // ★ 추가
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAnimationController : MonoBehaviour
{
    [Header("Tile Visual (Pool)")]
    [SerializeField] private string resourcesFolder = "my_asset";
    [SerializeField] private string spriteName = "attack";   // Resources/my_asset/attack.png
    [SerializeField, Range(0.2f, 1.5f)] private float visualScale = 0.8f;
    [SerializeField] private float tileWidth = 1.3f;
    [SerializeField] private float tileHeight = 1.3f;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 50;
    [SerializeField] private float tileZ = 0f;

    [Header("Fade")]
    [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.75f;
    [SerializeField] private float minWindow = 0.06f;   // 너무 짧은 값 보정
    [SerializeField] private bool useUnscaledTime = false;

    private Sprite _sprite;
    private readonly List<GameObject> _pool = new List<GameObject>();
    private Coroutine[] _routines;    // 타일별 진행 코루틴
    private int[] _seq;               // 타일별 버전 토큰(레이스 방지)

    public event Action<int> OnTilePeak;


    void Awake()
    {
        _sprite = Resources.Load<Sprite>($"{resourcesFolder}/{spriteName}");
        if (_sprite == null)
            Debug.LogWarning($"[AttackAnim] Sprite not found at Resources/{resourcesFolder}/{spriteName}.png");
    }

    public void EnsurePool(int need)
    {
        if (_sprite == null) return;

        while (_pool.Count < need)
        {
            var go = new GameObject($"AttackTile_{_pool.Count:D2}");
            go.transform.SetParent(transform, worldPositionStays: false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;

            // 초기 투명
            var c = sr.color; c.r = 0f; c.g = 0f; c.b = 0f; c.a = 0f;
            sr.color = c;

            go.transform.localScale = ComputeSpriteScale(sr);
            go.SetActive(false);
            _pool.Add(go);
        }

        if (_routines == null || _routines.Length < _pool.Count) _routines = new Coroutine[_pool.Count];
        if (_seq == null || _seq.Length < _pool.Count) _seq = new int[_pool.Count];
    }

    /// <summary> 마스크에 맞춰 위치시키고 '표시 준비(알파0)' </summary>
    public void PlaceAndShowMask(bool[] mask16, Vector3[] centers16)
    {
        EnsurePool(16);
        for (int i = 0; i < _pool.Count; i++)
        {
            var go = _pool[i];
            if (!go) continue;

            bool on = (mask16 != null && i < mask16.Length && mask16[i]
                    && centers16 != null && i < centers16.Length);

            if (on)
            {
                var pos = centers16[i]; pos.z = tileZ;
                go.transform.position = pos;

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr)
                {
                    var c = sr.color; c.r = 1f; c.g = 0f; c.b = 0f; c.a = 0f;
                    sr.color = c;
                    go.transform.localScale = ComputeSpriteScale(sr);
                    sr.sortingLayerName = sortingLayer;
                    sr.sortingOrder = sortingOrder;
                }

                go.SetActive(true);
            }
            else
            {
                CancelIfAny(i);
                if (go) go.SetActive(false);
            }
        }
    }

    /// <summary> 모두 끄기(코루틴 중지 + 비활성) </summary>
    public void HideAll()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            CancelIfAny(i);
            var go = _pool[i];
            if (go) go.SetActive(false);
        }
    }

    /// <summary>
    /// idx 타일에 대해 windowSeconds 동안 페이드(인/아웃 각각 1/2씩).
    /// windowSeconds가 작으면 minWindow로 보정.
    /// </summary>
    public void StartPulseWindow(int idx, float windowSeconds, float? peakOverride = null)
    {
        if (idx < 0 || idx >= _pool.Count) return;
        var go = _pool[idx];
        if (!go || !go.activeInHierarchy) return;

        float win = Mathf.Max(minWindow, windowSeconds);
        float fin = win * 0.5f;
        float fout = win * 0.5f;
        float peak = Mathf.Clamp01(peakOverride ?? peakAlpha);

        _seq[idx]++;                         // 새 버전
        CancelIfAny(idx);                    // 기존 중지
        _routines[idx] = StartCoroutine(PulseRoutine(idx, fin, fout, peak, _seq[idx]));
    }

    // ---------------- internal ----------------

    private void CancelIfAny(int idx)
    {
        if (_routines == null || idx < 0 || idx >= _routines.Length) return;
        if (_routines[idx] != null)
        {
            StopCoroutine(_routines[idx]);
            _routines[idx] = null;
        }
    }

    private IEnumerator PulseRoutine(int idx, float fadeInSec, float fadeOutSec, float peak, int mySeq)
    {
        if (idx < 0 || idx >= _pool.Count) yield break;
        var go = _pool[idx]; if (!go || !go.activeInHierarchy) yield break;

        var sr = go.GetComponent<SpriteRenderer>(); if (!sr) yield break;

        var c = sr.color; c.a = 0f; sr.color = c;

        // IN
        if (fadeInSec > 0f)
        {
            float t = 0f;
            while (t < fadeInSec)
            {
                if (!go.activeInHierarchy || mySeq != _seq[idx]) yield break;
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                c.a = Mathf.Lerp(0f, peak, t / fadeInSec);
                sr.color = c;
                yield return null;
            }
        }

        // ★ 피크 스냅 + 알림
        c.a = peak; sr.color = c;
        OnTilePeak?.Invoke(idx);

        // OUT
        if (fadeOutSec > 0f)
        {
            float t = 0f;
            while (t < fadeOutSec)
            {
                if (!go.activeInHierarchy || mySeq != _seq[idx]) yield break;
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                c.a = Mathf.Lerp(peak, 0f, t / fadeOutSec);
                sr.color = c;
                yield return null;
            }
        }

        // 종료 스냅
        c.a = 0f; sr.color = c;
    }

    private Vector3 ComputeSpriteScale(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return Vector3.one;
        var s = sr.sprite.bounds.size;
        float sx = (s.x > 0f) ? (tileWidth / s.x) : 1f;
        float sy = (s.y > 0f) ? (tileHeight / s.y) : 1f;
        return new Vector3(sx * visualScale, sy * visualScale, 1f);
    }
}
