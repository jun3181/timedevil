// Assets/Script/Trigger/Steps/TriggerStep_Scene.cs
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class TriggerStep_Scene : TriggerStepBase
{
    [Header("Target Scene")]
    [SerializeField] private string sceneName = "Myroom";
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

    [Header("Use SceneVisitEffectRunner (recommended)")]
    [Tooltip("켜면 현재 씬의 SceneVisitEffectRunner를 찾아 'Exit->Load'를 요청합니다.")]
    [SerializeField] private bool useSceneVisitEffectRunner = true;

    [Tooltip("지정하면 이 Runner를 우선 사용. 비우면 씬에서 자동 탐색.")]
    [SerializeField] private MonoBehaviour runnerOverride; // (SceneVisitEffectRunner 넣어도 됨)

    [Header("Lock (optional)")]
    [SerializeField] private bool lockPlayerInput = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[TriggerStep_Scene] sceneName이 비어있습니다.");
            yield break;
        }

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = true;

        if (debugLog)
            Debug.Log($"[TriggerStep_Scene] Request Load scene='{sceneName}' mode={loadMode} useRunner={useSceneVisitEffectRunner}");

        // 1) Runner로 Exit 효과 + Load를 요청 (가장 권장)
        if (useSceneVisitEffectRunner && loadMode == LoadSceneMode.Single)
        {
            MonoBehaviour runner = runnerOverride;

            if (!runner)
                runner = Object.FindObjectOfType<MonoBehaviour>(true) as MonoBehaviour; // 안전용 (아래에서 다시 탐색)

            if (!runner)
            {
                // 실제로는 SceneVisitEffectRunner를 찾아야 하므로, 타입이 있으면 강하게 탐색
                // (runnerOverride가 비어있을 때를 대비)
                var any = Object.FindObjectsOfType<MonoBehaviour>(true);
                foreach (var mb in any)
                {
                    if (!mb) continue;
                    if (mb.GetType().Name == "SceneVisitEffectRunner")
                    {
                        runner = mb;
                        break;
                    }
                }
            }
            else
            {
                // runnerOverride가 들어왔는데 타입이 다른 경우 방지
                if (runner.GetType().Name != "SceneVisitEffectRunner")
                {
                    if (debugLog)
                        Debug.LogWarning($"[TriggerStep_Scene] runnerOverride가 SceneVisitEffectRunner가 아닙니다. type={runner.GetType().Name}");
                    runner = null;
                }
            }

            if (runner)
            {
                // Runner에 “Exit 후 로드”를 요청하는 메서드 이름들이 프로젝트마다 달라질 수 있어서
                // 리플렉션으로 여러 후보를 순서대로 시도합니다.
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                // (1) IEnumerator 코루틴 방식 먼저 시도 (있으면 'Exit 끝까지' 기다릴 수 있음)
                string[] coroutineNames =
                {
                    "CoExitAndLoad", "CoPlayExitAndLoad", "CoLoadSceneWithExit",
                    "CoLoadScene", "CoTransitionTo"
                };

                foreach (var name in coroutineNames)
                {
                    var mi = runner.GetType().GetMethod(name, flags, null, new[] { typeof(string) }, null);
                    if (mi != null && typeof(IEnumerator).IsAssignableFrom(mi.ReturnType))
                    {
                        if (debugLog) Debug.Log($"[TriggerStep_Scene] Runner coroutine '{name}(string)' 호출");
                        var ie = (IEnumerator)mi.Invoke(runner, new object[] { sceneName });
                        yield return runner.StartCoroutine(ie);
                        yield break; // 씬 로드가 여기서 일어남
                    }
                }

                // (2) void 메서드 방식 시도 (호출 후 곧바로 씬 로드가 시작될 것)
                string[] voidNames =
                {
                    "LoadScene", "Load", "RequestLoad",
                    "LoadSceneWithEffect", "PlayExitAndLoad",
                    "ExitAndLoad", "TransitionTo"
                };

                foreach (var name in voidNames)
                {
                    var mi = runner.GetType().GetMethod(name, flags, null, new[] { typeof(string) }, null);
                    if (mi != null && mi.ReturnType == typeof(void))
                    {
                        if (debugLog) Debug.Log($"[TriggerStep_Scene] Runner void '{name}(string)' 호출");
                        mi.Invoke(runner, new object[] { sceneName });
                        yield break;
                    }
                }

                if (debugLog)
                    Debug.LogWarning("[TriggerStep_Scene] SceneVisitEffectRunner를 찾았지만, 호출 가능한 (string) 메서드를 못 찾았습니다. fallback 로드로 진행합니다.");
            }
            else
            {
                if (debugLog)
                    Debug.LogWarning("[TriggerStep_Scene] SceneVisitEffectRunner를 찾지 못했습니다. fallback 로드로 진행합니다.");
            }
        }

        // 2) Fallback: 효과 없이 바로 로드
        SceneManager.LoadScene(sceneName, loadMode);
        yield break;
    }
}
