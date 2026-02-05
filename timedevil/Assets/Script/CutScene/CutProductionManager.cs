// Assets/Script/Cutscene/Production/CutProductionManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class CutProductionManager : MonoBehaviour
{
    [Serializable]
    public class Route
    {
        public string key;
        public List<CutProductionStepBase> steps = new();
        public bool oneShot = false;

        [NonSerialized] public bool played = false;
    }

    public static CutProductionManager Instance { get; private set; }

    [Header("Routes (Key -> Steps)")]
    public List<Route> routes = new();

    [Header("Input Lock (Optional)")]
    public bool lockPlayerInputWhileRunning = true; // GameManager.isAction

    [Header("Debug")]
    public bool debugLog = true;

    private readonly Dictionary<string, Route> _map = new();
    private bool _running = false;

    // director 재생이 "비동기"로 돌 때도 입력잠금 유지하고 싶으면 잡아두는 리스트
    private readonly HashSet<PlayableDirector> _heldDirectors = new();

    private bool _prevGameAction;
    private bool _lockApplied;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildMap();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildMap()
    {
        _map.Clear();

        foreach (var r in routes)
        {
            if (r == null) continue;
            if (string.IsNullOrWhiteSpace(r.key)) continue;

            if (_map.ContainsKey(r.key))
            {
                Debug.LogWarning($"[CutProductionManager] duplicate key '{r.key}' (keeps first)");
                continue;
            }
            _map.Add(r.key, r);
        }

        if (debugLog)
            Debug.Log($"[CutProductionManager] BuildMap routes={_map.Count}", this);
    }

    public bool Play(string key, GameObject instigator = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_running)
            return false;

        if (!_map.TryGetValue(key, out var route) || route == null)
        {
            // 혹시 인스펙터 수정/리로드 등으로 맵이 오래됐으면 재빌드
            BuildMap();
            _map.TryGetValue(key, out route);
        }

        if (route == null)
        {
            Debug.LogWarning($"[CutProductionManager] route not found key='{key}'", this);
            return false;
        }

        if (route.oneShot && route.played)
            return false;

        route.played = true;

        StartCoroutine(CoRunRoute(route, key, instigator));
        return true;
    }

    private IEnumerator CoRunRoute(Route route, string key, GameObject instigator)
    {
        _running = true;
        ApplyLock();

        var ctx = new CutProductionContext
        {
            key = key,
            instigator = instigator,
            manager = this
        };

        if (debugLog) Debug.Log($"[CutProductionManager] Run key='{key}' steps={route.steps.Count}", this);

        foreach (var step in route.steps)
        {
            if (step == null) continue;

            // PlayableDirector step이면 (비동기 재생 시에도) 입력잠금 유지 옵션 처리
            if (step is CutStep_PlayDirector pdStep && pdStep.holdManagerLockUntilStopped && pdStep.Director != null)
            {
                HoldUntilDirectorStopped(pdStep.Director);
            }

            var e = step.Execute(ctx);
            if (e == null) continue;

            if (step.waitForCompletion)
                yield return StartCoroutine(e);
            else
                StartCoroutine(e);
        }

        _running = false;
        TryReleaseLock();
    }

    private void ApplyLock()
    {
        if (!_lockApplied && lockPlayerInputWhileRunning && GameManager.Instance != null)
        {
            _prevGameAction = GameManager.Instance.isAction;
            GameManager.Instance.isAction = true;
            _lockApplied = true;
        }
    }

    private void TryReleaseLock()
    {
        // route 끝났고, "잠금 유지중인 director"도 없다면 해제
        if (_lockApplied && !_running && _heldDirectors.Count == 0 && GameManager.Instance != null)
        {
            GameManager.Instance.isAction = _prevGameAction;
            _lockApplied = false;
        }
    }

    private void HoldUntilDirectorStopped(PlayableDirector d)
    {
        if (d == null) return;
        if (_heldDirectors.Contains(d)) return;

        _heldDirectors.Add(d);
        d.stopped += OnHeldDirectorStopped;
    }

    private void OnHeldDirectorStopped(PlayableDirector d)
    {
        if (d != null)
            d.stopped -= OnHeldDirectorStopped;

        _heldDirectors.Remove(d);
        TryReleaseLock();
    }

#if UNITY_EDITOR
    // 인스펙터에서 routes 수정하면 자동 반영(플레이중 아니라면)
    private void OnValidate()
    {
        if (!Application.isPlaying)
            BuildMap();
    }
#endif
}
