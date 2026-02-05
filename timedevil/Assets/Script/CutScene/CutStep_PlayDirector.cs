// Assets/Script/Cutscene/Production/Steps/CutStep_PlayDirector.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using UnityEngine.Timeline;

[DisallowMultipleComponent]
public class CutStep_PlayDirector : CutProductionStepBase
{
    [Header("PlayableDirector")]
    public PlayableDirector director;

    [Header("Play Options")]
    public bool restartFromZero = true;
    public bool evaluateBeforePlay = true;

    [Tooltip("waitForCompletion=false여도, Manager 입력잠금을 'director 종료까지' 유지시키고 싶으면 체크")]
    public bool holdManagerLockUntilStopped = true;  // ✅ 다시 추가 (컴파일 에러 해결 포인트)

    [Header("Action Lock via Timeline Signals (GameManager)")]
    public bool useActionLockSignals = true;
    public SignalAsset disableSignal;
    public SignalAsset enableSignal;

    public PlayableDirector Director => director;

    private UnityAction _onDisableAction;
    private UnityAction _onEnableAction;

    public override IEnumerator Execute(CutProductionContext ctx)
    {
        if (!director)
            yield break;

        if (useActionLockSignals)
            BindActionLockSignals();

        if (restartFromZero)
            director.time = 0;

        if (evaluateBeforePlay)
            director.Evaluate();

        bool stopped = false;
        void OnStopped(PlayableDirector d) => stopped = true;

        director.stopped += OnStopped;
        director.Play();

        if (waitForCompletion)
        {
            while (!stopped)
                yield return null;
        }

        director.stopped -= OnStopped;
    }

    private void BindActionLockSignals()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (disableSignal == null && enableSignal == null) return;

        var receiver = director.GetComponent<SignalReceiver>();
        if (receiver == null)
            receiver = director.gameObject.AddComponent<SignalReceiver>();

        // GameManager에 이 메서드들이 있어야 함(없으면 다음 컴파일 에러로 뜸)
        _onDisableAction ??= gm.DisableControls;
        _onEnableAction ??= gm.EnableControls;

        if (disableSignal != null)
        {
            var evt = receiver.GetReaction(disableSignal);
            if (evt == null)
            {
                evt = new UnityEvent();
                receiver.AddReaction(disableSignal, evt);
            }
            evt.RemoveListener(_onDisableAction);
            evt.AddListener(_onDisableAction);
        }

        if (enableSignal != null)
        {
            var evt = receiver.GetReaction(enableSignal);
            if (evt == null)
            {
                evt = new UnityEvent();
                receiver.AddReaction(enableSignal, evt);
            }
            evt.RemoveListener(_onEnableAction);
            evt.AddListener(_onEnableAction);
        }
    }
}
