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
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private string resourcesFolder = "my_asset";

    [Header("Layout")]
    [SerializeField] private float leftPadding = 8f;
    [SerializeField] private float rightPadding = 8f; //  추가

    [SerializeField] private float cardWidth = 120f;
    [SerializeField] private float cardSpacing = 150f;

    [Header("Reveal")]
    [SerializeField] private bool revealFaces = true;      // false면 뒷면만
    [SerializeField] private Sprite cardBackSprite;        // 뒷면 스프라이트

    private readonly List<GameObject> spawned = new();

    private void EnsureCardDatabase()
    {
        if (cardDatabase) return;

        var orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (orchestrator && orchestrator.CardDatabase)
            cardDatabase = orchestrator.CardDatabase;
    }

    private BaseCardSO GetCardById(string id)
    {
        EnsureCardDatabase();
        return cardDatabase ? cardDatabase.GetById(id) : null;
    }

    private Sprite GetFaceSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        BaseCardSO card = GetCardById(id);
        if (card && card.mainArtwork) return card.mainArtwork;

        Sprite sprite = Resources.Load<Sprite>($"{resourcesFolder}/{id}");
        if (sprite) return sprite;

        string typeFolder = GetCardTypeFolder(id);
        return !string.IsNullOrEmpty(typeFolder)
            ? Resources.Load<Sprite>($"{resourcesFolder}/{typeFolder}/{id}")
            : null;
    }

    private static string GetCardTypeFolder(string id)
    {
        if (id.StartsWith("AttackCard")) return "AttackCard";
        if (id.StartsWith("DrawCard")) return "DrawCard";
        if (id.StartsWith("MoveCard")) return "MoveCard";
        return null;
    }

    void Awake()
    {
        if (!row) row = (RectTransform)transform;
        EnsureCardDatabase();
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
            step = Mathf.Min(cardSpacing, Mathf.Max(0f, needed));
        }

        ClearSpawned();
        for (int i = 0; i < n; i++)
        {
            string id = ids[i];
            var go = Instantiate(cardPrefab, row);
            go.name = $"EnemyHand_{(string.IsNullOrEmpty(id) ? "NULL" : id)}";
            spawned.Add(go);

            BaseCardSO card = revealFaces ? GetCardById(id) : null;
            Sprite faceSprite = revealFaces ? GetFaceSprite(id) : null;

            var templateView = go.GetComponentInChildren<CardTemplateView>(true);
            if (templateView && revealFaces)
            {
                templateView.Bind(card, faceSprite);
            }
            else
            {
                var img = go.GetComponentInChildren<Image>() ?? go.AddComponent<Image>();
                img.sprite = revealFaces ? faceSprite : cardBackSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

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
        var rt = EnemyDeckRuntime.Instance;
        if (spawned.Count == 0 && rt != null && rt.GetHandIds().Count > 0)
            RebuildFromHand();

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
