// PlayerDataRuntime.cs
using UnityEngine;

public class PlayerDataRuntime : MonoBehaviour
{
    public static PlayerDataRuntime Instance { get; private set; }

    [Header("Auto Save 옵션")]
    public bool saveOnDisable = false;
    public bool saveOnQuit = false;

    [Header("Load 옵션")]
    [SerializeField] private bool loadSavedDataOnAwake = true;

    [Header("Data")]
    public PlayerData Data;   // 인스펙터에서 기본값 설정 가능

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureRootForDontDestroy();
        DontDestroyOnLoad(gameObject);   // 씬 전환 생존

        LoadInitialData();
    }

    private void EnsureRootForDontDestroy()
    {
        if (transform.parent != null)
            transform.SetParent(null, true);
    }

    private void LoadInitialData()
    {
        if (loadSavedDataOnAwake)
        {
            PlayerData saved = PlayerDataStore.Load();
            if (saved != null)
            {
                Data = saved;
                return;
            }
        }

        if (Data == null)
            ResetToDefaults();
    }

    public void ResetToDefaults()
    {
        Data = new PlayerData();
        Data.InitDefaults();
    }

    public bool LoadFromDisk()
    {
        PlayerData saved = PlayerDataStore.Load();
        if (saved == null) return false;

        Data = saved;
        return true;
    }

    public void SaveNow()
    {
        if (Data == null) ResetToDefaults();
        PlayerDataStore.Save(Data);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnDisable()
    {
        if (saveOnDisable) SaveNow();
    }

    void OnApplicationQuit()
    {
        if (saveOnQuit) SaveNow();
    }
}
