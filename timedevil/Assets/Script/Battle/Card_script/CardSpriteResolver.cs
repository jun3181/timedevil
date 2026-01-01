using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TimeDevil/Card Sprite Resolver")]
public class CardSpriteResolver : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string cardId;          // 예: "Card1"
        public Sprite sprite;          // 직접 지정하면 이걸 사용
        public string resourcesPath;   // 비워두면 "my_asset/<cardId>"를 기본 경로로 시도
    }

    [SerializeField] private List<Entry> table = new List<Entry>();

    private Dictionary<string, Sprite> map;

    void OnEnable()
    {
        BuildMap();
    }

    private void BuildMap()
    {
        map = new Dictionary<string, Sprite>();
        foreach (var e in table)
        {
            if (!string.IsNullOrEmpty(e.cardId) && e.sprite != null)
                map[e.cardId] = e.sprite;
        }
    }

    public Sprite GetSprite(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;

        // 1) 인스펙터에 직접 매핑되어 있으면 사용
        if (map != null && map.TryGetValue(cardId, out var sp) && sp != null)
            return sp;

        // 2) 폴백: Resources 에서 로드 (기본: my_asset/Card1)
        string path = $"my_asset/{cardId}";
        var loaded = Resources.Load<Sprite>(path);
        return loaded;
    }
}
