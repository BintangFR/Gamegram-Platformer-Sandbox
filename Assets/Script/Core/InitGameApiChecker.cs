using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class InitGameApiChecker : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string initGameEndpoint = "https://your-api/initgame";
    [SerializeField] private string gameID = "game_001";
    [SerializeField] private bool callOnBoot = true;

    [Header("Local Fallback (Mobile Safe)")]
    [SerializeField] private bool useLocalFallback = true;
    [SerializeField] private string cacheFolder = "LevelData";
    [SerializeField] private string cacheFileName = "initgame_cache.json";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;

    public bool IsApiSuccess { get; private set; }
    public bool IsEditModeAvailable { get; private set; } = true;
    public string LevelJson { get; private set; }

    [System.Serializable]
    private class InitGameResponse
    {
        public string json;
        public string levelJson;
        public string message;
        public bool IsEdit;
        public bool isEdit;
    }

    [System.Serializable]
    private class InitGameCache
    {
        public string levelJson;
        public bool isEdit;
    }

    private void Start()
    {
        SetStatus("(api not called)");

        if (callOnBoot)
            CallInitGame();
    }

    public void CallInitGame()
    {
        StartCoroutine(CallInitGameRoutine(gameID));
    }

    public void CallInitGame(string targetGameID)
    {
        StartCoroutine(CallInitGameRoutine(targetGameID));
    }

    private IEnumerator CallInitGameRoutine(string targetGameID)
    {
        IsApiSuccess = false;
        SetStatus("API Calling...");

        string url = initGameEndpoint + "?gameID=" + UnityWebRequest.EscapeURL(targetGameID);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (TryLoadFromLocalCache())
                {
                    SetStatusSuccess("API failed, loaded local JSON");
                    yield break;
                }

                SetStatusFailed("Initgame failed");
                Debug.LogError("InitGame failed: " + request.error);
                yield break;
            }

            InitGameResponse response = JsonUtility.FromJson<InitGameResponse>(request.downloadHandler.text);
            if (response == null)
            {
                if (TryLoadFromLocalCache())
                {
                    SetStatusSuccess("API invalid, loaded local JSON");
                    yield break;
                }

                SetStatusFailed("Initgame failed");
                Debug.LogError("InitGame parse failed. Raw: " + request.downloadHandler.text);
                yield break;
            }

            LevelJson = string.IsNullOrEmpty(response.levelJson) ? response.json : response.levelJson;
            if (string.IsNullOrEmpty(LevelJson))
            {
                if (TryLoadFromLocalCache())
                {
                    SetStatusSuccess("API empty JSON, loaded local JSON");
                    yield break;
                }

                SetStatusFailed("Initgame failed");
                Debug.LogError("InitGame failed: response does not contain json/levelJson.");
                yield break;
            }

            IsApiSuccess = true;
            IsEditModeAvailable = response.IsEdit || response.isEdit;

            SaveToLocalCache(LevelJson, IsEditModeAvailable);

            if (IsEditModeAvailable)
                SetStatusSuccess("API Success: Edit Mode");
            else
                SetStatusSuccess("API Success: Play Mode");

            Debug.Log("InitGame success. IsEdit=" + IsEditModeAvailable + ", Message=" + response.message);
        }
    }

    private void SaveToLocalCache(string levelJson, bool isEditMode)
    {
        if (string.IsNullOrEmpty(levelJson))
            return;

        InitGameCache cache = new InitGameCache
        {
            levelJson = levelJson,
            isEdit = isEditMode
        };

        string path = GetCachePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(cache, true));
    }

    private bool TryLoadFromLocalCache()
    {
        if (!useLocalFallback)
            return false;

        string path = GetCachePath();
        if (!File.Exists(path))
            return false;

        string raw = File.ReadAllText(path);
        if (string.IsNullOrEmpty(raw))
            return false;

        InitGameCache cache = JsonUtility.FromJson<InitGameCache>(raw);
        if (cache == null || string.IsNullOrEmpty(cache.levelJson))
            return false;

        LevelJson = cache.levelJson;
        IsEditModeAvailable = cache.isEdit;
        IsApiSuccess = false;

        Debug.Log("Loaded InitGame cache from: " + path);
        return true;
    }

    private string GetCachePath()
    {
        return Path.Combine(Application.persistentDataPath, cacheFolder, cacheFileName);
    }

    private void SetStatus(string value)
    {
        if (statusText == null)
            return;

        statusText.text = value;
        statusText.color = Color.white;
    }

    private void SetStatusSuccess(string value)
    {
        if (statusText == null)
            return;

        statusText.text = value;
        statusText.color = Color.green;
    }

    private void SetStatusFailed(string value)
    {
        if (statusText == null)
            return;

        statusText.text = value;
        statusText.color = Color.red;
    }
}