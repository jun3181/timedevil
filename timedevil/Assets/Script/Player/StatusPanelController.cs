using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusPanelController : MonoBehaviour
{
    private GameObject statusPanel;

    void Start() {
        statusPanel = transform.Find("StatusPanel").gameObject;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
            statusPanel.SetActive(!statusPanel.activeInHierarchy);
    }
}
