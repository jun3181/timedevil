using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TriggerIllustration : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text text;

    [Header("Content")]
    [SerializeField] private Sprite sprite;
    
    [TextArea]
    [SerializeField] private string message;

    private bool isOpen = false;

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.E))
        {
            Close();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Open();
    }

    void Open()
    {
        if (image != null)
            image.sprite = sprite;

        if (text != null)
            text.text = message;

        panel.SetActive(true);
        isOpen = true;
    }

    void Close()
    {
        panel.SetActive(false);
        isOpen = false;
    }
}
