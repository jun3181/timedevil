// Assets/Script/Player/item/ReturnToPreviousOnQ.cs
using UnityEngine;

public class ReturnToPreviousOnQ : MonoBehaviour
{
    [Header("Return options")]
    [SerializeField] private float graceSeconds = 0.5f;
    [SerializeField] private bool useFaderIfExists = true;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;

        if (string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName))
        {
            Debug.LogWarning("[ReturnToPreviousOnQ] ReturnSceneName이 비어있습니다. 복귀할 씬이 없습니다.");
            return;
        }

        // SceneLoader가 책임지고 (있으면) SceneFader로 FadeOut 후 로드까지 처리
        SceneLoader.GoBackToReturnScene(graceSeconds, useFaderIfExists);
    }
}
