using UnityEngine;
using UnityEngine.Events;

public class BattleMenuController : MonoBehaviour
{
    [System.Serializable] public class IntEvent : UnityEvent<int> { }

    [Header("Order (2x2 grid): 0=Card, 1=Item / 2=End, 3=Run")]
    [SerializeField] private GameObject[] entries;

    [Header("Input")]
    [SerializeField] private bool inputEnabled = true;

    [Header("Events")]
    public IntEvent onFocusChanged = new IntEvent();
    public IntEvent onSubmit = new IntEvent();

    private int index = 0;
    public int Index => index;
    public int CurrentIndex => index;

    void Awake()
    {
        // 하이라이트만 먼저 적용(리스너가 없을 수도 있는 초기 프레임)
        ApplyHighlight(index);
    }

    void OnEnable()
    {
        // ✅ 구독 타이밍과 상관없이 항상 현재 포커스 1회 통지
        onFocusChanged?.Invoke(index);
    }

    void Start()
    {
        // 남겨두어도 무방하지만 OnEnable에서 이미 쏨
        // ApplyHighlight(index); // Awake에서 처리
        // onFocusChanged?.Invoke(index);
    }

    void Update()
    {
        if (!inputEnabled || entries == null || entries.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveHoriz(+1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveHoriz(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveVert(+1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveVert(-1);

        if (Input.GetKeyDown(KeyCode.E))
        {
            string name = index switch { 0 => "Card", 1 => "Item", 2 => "End", 3 => "Run", _ => $"Idx{index}" };
            Debug.Log($"[BattleMenu] E pressed → {name} selected (index={index})");
            onSubmit?.Invoke(index);
        }
    }

    public void EnableInput(bool on)
    {
        inputEnabled = on;

        // ✅ 입력 가능/불가 전환 시에도 현재 포커스를 통지해
        // (ItemHand/Hand/설명 패널이 즉시 숨김/표시를 반영)
        onFocusChanged?.Invoke(index);
    }

    /// <summary>외부에서 강제로 현재 포커스 알리기(동기화용).</summary>
    public void ForceNotifyFocus()
    {
        onFocusChanged?.Invoke(index);
    }

    public void SetFocus(int newIndex)
    {
        if (entries == null || entries.Length == 0) return;
        newIndex = Mathf.Clamp(newIndex, 0, entries.Length - 1);
        if (newIndex == index) return;

        index = newIndex;
        ApplyHighlight(index);
        onFocusChanged?.Invoke(index);
    }

    private void MoveHoriz(int dir)
    {
        if (entries == null || entries.Length == 0) return;

        if (entries.Length == 4)
        {
            int row = index / 2;
            int col = index % 2;
            col = (col + (dir > 0 ? 1 : -1) + 2) % 2;
            SetFocus(row * 2 + col);
        }
        else
        {
            int count = entries.Length;
            SetFocus((index + (dir > 0 ? 1 : -1) + count) % count);
        }
    }

    private void MoveVert(int dir)
    {
        if (entries == null || entries.Length == 0) return;

        if (entries.Length == 4)
        {
            int row = index / 2;
            int col = index % 2;
            row = (row + (dir > 0 ? 1 : -1) + 2) % 2;
            SetFocus(row * 2 + col);
        }
        else
        {
            int count = entries.Length;
            SetFocus((index + (dir > 0 ? 2 : -2) % count + count) % count);
        }
    }

    private void ApplyHighlight(int cur)
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            var go = entries[i];
            if (!go) continue;
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (!img) continue;
            img.color = (i == cur) ? new Color(0.7f, 1f, 0.7f, 1f) : Color.white;
        }
    }
}
