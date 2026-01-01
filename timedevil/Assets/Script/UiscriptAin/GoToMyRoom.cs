using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToMyRoom : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SceneManager.LoadScene("MyRoom");
        }
    }
}
