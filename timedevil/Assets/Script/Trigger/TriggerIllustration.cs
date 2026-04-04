using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TriggerStep_IllustrationPanel_New : TriggerStepBase
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image illustrationImage;
    [SerializeField] private TMP_Text messageText;

    [Header("Content")]
    [SerializeField] private Sprite illustrationSprite;
    [TextArea]
    [SerializeField] private string message;

    [Header("Flow")]
    [SerializeField] private bool closeWithKey = true;
    [SerializeField] private KeyCode closeKey = KeyCode.E;
    [SerializeField] private bool waitUntilClosed = true;
    [Min(0f)] [SerializeField] private float autoCloseDelay = 0f;

    [Header("Player Input")]
    [SerializeField] private bool lockPlayerInput = true;

    private bool _isOpen;
    private bool _locked;
    private Coroutine _autoClose;

    private void Reset()
    {
        if (panel == null) panel = gameObject;
    }

    private void OnDisable() => CloseImmediate();
    private void OnDestroy() => CloseImmediate();

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (panel == null)
        {
            Debug.LogWarning("[TriggerStep_IllustrationPanel_New] panel is null.", this);
            yield break;
        }

        if (_isOpen)
            yield break;

        Open();

        if (!waitUntilClosed)
            yield break;

        if (!closeWithKey && autoCloseDelay <= 0f)
        {
            Debug.LogWarning("[TriggerStep_IllustrationPanel_New] no close condition configured. Closing immediately.", this);
            CloseImmediate();
            yield break;
        }

        while (_isOpen)
        {
            if (closeWithKey && Input.GetKeyDown(closeKey))
                CloseImmediate();

            yield return null;
        }
    }

    private void Open()
    {
        if (lockPlayerInput && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            _locked = true;
        }

        if (illustrationImage != null)
            illustrationImage.sprite = illustrationSprite;

        if (messageText != null)
            messageText.text = message;

        panel.SetActive(true);
        _isOpen = true;

        if (autoCloseDelay > 0f)
            _autoClose = StartCoroutine(CoAutoClose());
    }

    private IEnumerator CoAutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (_isOpen)
            CloseImmediate();
    }

    private void CloseImmediate()
    {
        if (_autoClose != null)
        {
            StopCoroutine(_autoClose);
            _autoClose = null;
        }

        if (panel != null)
            panel.SetActive(false);

        _isOpen = false;

        if (_locked && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _locked = false;
        }
    }
}

