using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToMyRoom : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SceneTransitionService.EnterMyroom(MyroomEntryPoint.Spawn_Room2_LoadGame_PlayerDead);
        }
    }
}
