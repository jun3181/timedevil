// RunController.cs
using UnityEngine;

public class RunController : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private int runIndex = 3;

    [Header("Options")]
    [SerializeField] private float graceSeconds = 1.0f;

    // ★ (선택) 월드 씬의 vcam 이름을 지정하면 정확히 그 vcam을 찾음
    [SerializeField] private string worldVcamName = "CM vcam1";

    private bool isReturning = false;

    void Awake()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
    }

    void OnEnable()
    {
        if (menu) menu.onSubmit.AddListener(OnMenuSubmit);
    }
    void OnDisable()
    {
        if (menu) menu.onSubmit.RemoveListener(OnMenuSubmit);
    }

    private void OnMenuSubmit(int idx)
    {
        if (idx == runIndex) OnRunPressed();
    }

    public void OnRunPressed()
    {
        if (isReturning) return;

        var tm = FindObjectOfType<TurnManager>(true);
        if (tm)
        {
            if (tm.currentTurn != TurnState.PlayerTurn) return;
            if (tm.IsPlayerDiscardPhase) return;
        }
        if (string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName))
        {
            Debug.LogWarning("[RunController] 돌아갈 씬 정보가 없습니다.");
            return;
        }

        isReturning = true;
        if (menu) menu.EnableInput(false);

        // ★★★ 핵심: 돌아가면 카메라를 Player에 재바인딩하라는 플래그 세팅
        PlayerReturnContext.CameraRebindRequested = true;
        PlayerReturnContext.TargetVcamName = string.IsNullOrWhiteSpace(worldVcamName) ? null : worldVcamName;

        SceneLoader.GoBackToReturnScene(graceSeconds, useFaderIfExists: true);
    }
}
