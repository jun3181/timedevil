// Assets/Script/Trigger/Trigger_UI/TriggerStep_IllustrationPanel.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TriggerStep_IllustrationPanel : TriggerStepBase
{
    [Header("UI Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image illustrationImage;
    [SerializeField] private TMP_Text messageText;

    [Header("Content")]
    [SerializeField] private Sprite illustrationSprite;

    [TextArea]
    [SerializeField] private string message;

    [Header("Flow")]
    [SerializeField] private bool closeOnInteractKey = true;
    [SerializeField] private KeyCode closeKey = KeyCode.E;
    [SerializeField] private bool waitUntilClosed = true;
    [Min(0f)][SerializeField] private float autoCloseDelay = 0f;

    [Header("Input Lock")]
    [SerializeField] private bool lockPlayerInputWhileOpen = true;

    private bool _heldLock;

    private void Reset()
    {
        if (!panel) panel = gameObject;
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!panel)
        {
            Debug.LogWarning("[TriggerStep_IllustrationPanel] panel is not assigned.", this);
            yield break;
        }

        if (lockPlayerInputWhileOpen && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            _heldLock = true;
        }

        if (illustrationImage != null)
            illustrationImage.sprite = illustrationSprite;

        if (messageText != null)
            messageText.text = message;

        panel.SetActive(true);

        bool closed = false;
        float elapsed = 0f;

        while (!closed)
        {
            if (closeOnInteractKey && Input.GetKeyDown(closeKey))
                closed = true;

            if (autoCloseDelay > 0f)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= autoCloseDelay)
                    closed = true;
            }

            if (!waitUntilClosed)
                break;

            yield return null;
        }

        panel.SetActive(false);

        if (_heldLock && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _heldLock = false;
        }
    }
}
