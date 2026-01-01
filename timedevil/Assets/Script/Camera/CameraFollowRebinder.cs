// CameraFollowRebinder.cs
using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollowRebinder : MonoBehaviour
{
    private void OnEnable()
    {
        // 페이드 인이 끝났을 때(=씬의 오브젝트들이 모두 살아난 뒤) 처리
        SceneFader.OnFadeInComplete += OnFadeInComplete;
        // 혹시 페이더가 없거나 이벤트 타이밍이 안 맞는 경우를 대비한 백업
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneFader.OnFadeInComplete -= OnFadeInComplete;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 페이더가 없는 씬에서도 작동하도록 1프레임 뒤 시도
        StartCoroutine(TryBindNextFrame());
    }

    private void OnFadeInComplete()
    {
        // 페이드 인 콜백에서도 시도
        StartCoroutine(TryBindNextFrame());
    }

    private IEnumerator TryBindNextFrame()
    {
        // 씬 내 객체들이 Start/OnEnable를 마칠 시간 확보
        yield return null;

        if (!PlayerReturnContext.CameraRebindRequested) yield break;

        // “돌아갈 씬”과 현재 씬이 일치할 때만
        if (!string.IsNullOrEmpty(PlayerReturnContext.ReturnSceneName) &&
            PlayerReturnContext.ReturnSceneName != SceneManager.GetActiveScene().name)
            yield break;

        // Player 찾기
        var player = FindObjectOfType<PlayerAction>();
        if (!player) yield break;

        // vcam 찾기(이름 지정 > 씬 내 첫 번째)
        CinemachineVirtualCamera vcam = null;
        if (!string.IsNullOrEmpty(PlayerReturnContext.TargetVcamName))
        {
            var go = GameObject.Find(PlayerReturnContext.TargetVcamName);
            if (go) vcam = go.GetComponent<CinemachineVirtualCamera>();
        }
        if (!vcam) vcam = FindObjectOfType<CinemachineVirtualCamera>(true);
        if (!vcam) yield break;

        // 바인딩
        vcam.Follow = player.transform;

        // Confiner가 있으면 캐시 갱신
        var confiner = vcam.GetComponent<CinemachineConfiner2D>();
        if (confiner) confiner.InvalidateCache();

        // 한 번만 수행하도록 플래그 리셋
        PlayerReturnContext.CameraRebindRequested = false;
        PlayerReturnContext.TargetVcamName = null;

#if UNITY_EDITOR
        Debug.Log("[CameraFollowRebinder] vcam.Follow ← Player (돌아오기 전용)");
#endif
    }
}
