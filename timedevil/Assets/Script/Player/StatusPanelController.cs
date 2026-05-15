using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusPanelController : MonoBehaviour
{
    private static bool isInstanced;
    private GameObject statusPanel;

    void Start() {
        if(isInstanced) {
            Destroy(gameObject);
        }
        isInstanced = true;

        statusPanel = transform.Find("StatusPanel").gameObject;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
            statusPanel.SetActive(!statusPanel.activeInHierarchy);
    }
}
