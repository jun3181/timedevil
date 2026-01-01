// Assets/Script/Battle/EnemyHandUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EnemyHandUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform row;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private string resourcesFolder = "my_asset";

    [Header("Layout")]
    [SerializeField] private float leftPadding = 8f;
    [SerializeField] private float rightPadding = 8f; // 👈 추가

    [SerializeField] private float cardWidth = 120f;

    [Header("Reveal")]
    [SerializeField] private bool revealFaces = true;      // false면 뒷면만
    [SerializeField] private Sprite cardBackSprite;        // 뒷면 스프라이트

    private readonly List<GameObject> spawned = new();

    void Awake()
    {
        if (!row) row = (RectTransform)transform;
        HideAll();
    }

    void OnEnable()
    {
        if (EnemyDeckRuntime.Instance != null)
            EnemyDeckRuntime.Instance.OnHandChanged += RebuildFromHand;

        RebuildFromHand();
    }

    void OnDisable()
    {
        if (EnemyDeckRuntime.Instance != null)
            EnemyDeckRuntime.Instance.OnHandChanged -= RebuildFromHand;
    }

    public void RebuildFromHand()
    {
        if (!row) row = (RectTransform)transform;
        if (!cardPrefab) return;

        var rt = EnemyDeckRuntime.Instance;
        if (rt == null) { HideAll(); return; }

        var ids = rt.GetHandIds();
        ClearSpawned();

        int n = ids.Count;
        float rowW = row.rect.width;
        float usable = Mathf.Max(0f, rowW - leftPadding - rightPadding);

        float step = 0f;
        if (n <= 1)
        {
            step = 0f;
        }
        else
        {
            float maxSpan = Mathf.Max(0f, usable - cardWidth);
            float needed = maxSpan / (n - 1);
            step = Mathf.Min(cardWidth, Mathf.Max(0f, needed));
        }

        ClearSpawned();
        for (int i = 0; i < n; i++)
        {
            string id = ids[i];
            var go = Instantiate(cardPrefab, row);
            go.name = $"EnemyHand_{(string.IsNullOrEmpty(id) ? "NULL" : id)}";
            spawned.Add(go);

            var img = go.GetComponentInChildren<Image>() ?? go.AddComponent<Image>();
            img.sprite = revealFaces && !string.IsNullOrEmpty(id)
                ? Resources.Load<Sprite>($"{resourcesFolder}/{id}")
                : cardBackSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var rtItem = (RectTransform)go.transform;
            rtItem.anchorMin = rtItem.anchorMax = new Vector2(0f, 0.5f);
            rtItem.pivot = new Vector2(0f, 0.5f);

            float x = leftPadding + step * i;
            rtItem.anchoredPosition = new Vector2(x, 0f);
            rtItem.sizeDelta = new Vector2(cardWidth, rtItem.sizeDelta.y);
        }

        ShowAll();
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }

    public void ShowAll()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(true);
        gameObject.SetActive(true);
    }

    public void HideAll()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(false);
        gameObject.SetActive(false);
    }

    public List<RectTransform> GetAllCardRects()
    {
        var list = new List<RectTransform>();
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) list.Add((RectTransform)spawned[i].transform);
        return list;
    }
}
