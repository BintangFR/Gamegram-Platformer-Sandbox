using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Bootstrap : MonoBehaviour
{
    [Serializable]
    private class BootstrapResponse
    {
        public string url;
        public string levelUrl;
        public string level_url;
        public string levelJsonUrl;
        public string level_json_url;
    }

    [Header("Launch Query Keys")]
    [SerializeField] private string modeKey = "mode";
    [SerializeField] private string levelIdKey = "level_id";

    [Header("Bootstrap API")]
    [SerializeField] private bool fetchOnStart = true;
    [SerializeField] private string bootstrapEndpoint = "http://127.0.0.1:8000/bootstrap";
    [SerializeField] private string cacheFolder = "LevelData";
    [SerializeField] private string fallbackFileName = "level_01_tiles.json";
    [SerializeField] private bool useLevelIdAsFileName = true;

    [Header("Parsed Values (Read Only)")]
    [SerializeField] private string mode;
    [SerializeField] private string levelId;
    [SerializeField] private string levelJsonUrl;
    [SerializeField] private string cachedFilePath;

    [Header("Optional UI Debug")]
    [SerializeField] private TextMeshProUGUI statusText;

    public static string Mode { get; private set; }
    public static string LevelId { get; private set; }
    public static string ActiveLevelFileName { get; private set; }
    public static string CachedLevelFilePath { get; private set; }

    public static bool IsNoEditMode => string.Equals(Mode, "noedit", StringComparison.OrdinalIgnoreCase);

    private void Awake()
    {
        ParseLaunchUrl();
        UpdateStatusText("Launch params parsed.");
    }

    private void Start()
    {
        if (!fetchOnStart)
            return;

        StartCoroutine(BootstrapLevelRoutine());
    }

    private void ParseLaunchUrl()
    {
        string url = Application.absoluteURL;
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[Bootstrap] Application.absoluteURL is empty.");
            return;
        }

        if (TryGetQueryParam(url, modeKey, out string parsedMode))
            mode = parsedMode;

        if (TryGetQueryParam(url, levelIdKey, out string parsedLevelId))
            levelId = parsedLevelId;

        Mode = mode;
        LevelId = levelId;

        Debug.Log("[Bootstrap] mode=" + Mode + " | level_id=" + LevelId, this);
    }

    private IEnumerator BootstrapLevelRoutine()
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            Debug.LogWarning("[Bootstrap] level_id is empty. Skip bootstrap API call.", this);
            yield break;
        }

        string endpoint = bootstrapEndpoint.TrimEnd('/');
        string bootstrapUrl = endpoint + "/" + UnityWebRequest.EscapeURL(levelId);

        UpdateStatusText("Requesting bootstrap URL...");

        using (UnityWebRequest bootstrapRequest = UnityWebRequest.Get(bootstrapUrl))
        {
            yield return bootstrapRequest.SendWebRequest();

            if (bootstrapRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Bootstrap] Bootstrap API request failed: " + bootstrapRequest.error, this);
                UpdateStatusText("Bootstrap API failed.");
                yield break;
            }

            string responseText = bootstrapRequest.downloadHandler.text;
            if (!TryExtractLevelJsonUrl(responseText, out string extractedUrl))
            {
                Debug.LogError("[Bootstrap] Failed to parse level JSON URL from bootstrap response: " + responseText, this);
                UpdateStatusText("Invalid bootstrap response.");
                yield break;
            }

            levelJsonUrl = extractedUrl;
        }

        UpdateStatusText("Downloading level JSON...");

        string downloadedJson;
        using (UnityWebRequest levelRequest = UnityWebRequest.Get(levelJsonUrl))
        {
            yield return levelRequest.SendWebRequest();

            if (levelRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Bootstrap] Level JSON download failed: " + levelRequest.error, this);
                UpdateStatusText("Level JSON download failed.");
                yield break;
            }

            downloadedJson = levelRequest.downloadHandler.text;
        }

        SaveLevelJson(downloadedJson);
        UpdateStatusText("Level JSON cached and ready.");
    }

    private void SaveLevelJson(string json)
    {
        string fileName = useLevelIdAsFileName && !string.IsNullOrWhiteSpace(levelId)
            ? BuildLevelFileName(levelId)
            : fallbackFileName;

        string folderPath = Path.Combine(Application.persistentDataPath, cacheFolder);
        Directory.CreateDirectory(folderPath);

        string fullPath = Path.Combine(folderPath, fileName);
        File.WriteAllText(fullPath, json);

        ActiveLevelFileName = fileName;
        CachedLevelFilePath = fullPath;
        cachedFilePath = fullPath;

        Debug.Log("[Bootstrap] Level JSON saved: " + fullPath, this);
    }

    private static string BuildLevelFileName(string rawLevelId)
    {
        string safe = rawLevelId;
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
            safe = safe.Replace(invalid[i], '_');

        return safe + ".json";
    }

    private static bool TryExtractLevelJsonUrl(string responseText, out string url)
    {
        url = null;
        if (string.IsNullOrWhiteSpace(responseText))
            return false;

        string trimmed = responseText.Trim().Trim('"');
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = trimmed;
            return true;
        }

        BootstrapResponse payload = JsonUtility.FromJson<BootstrapResponse>(responseText);
        if (payload == null)
            return false;

        url = FirstNonEmpty(
            payload.url,
            payload.levelJsonUrl,
            payload.level_json_url,
            payload.levelUrl,
            payload.level_url);

        return !string.IsNullOrWhiteSpace(url);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return null;
    }

    private void UpdateStatusText(string message)
    {
        if (statusText == null)
            return;

        statusText.text =
            "mode=" + mode + "\n" +
            "level_id=" + levelId + "\n" +
            message;
    }

    private static bool TryGetQueryParam(string url, string key, out string value)
    {
        value = null;

        int queryStart = url.IndexOf('?');
        if (queryStart < 0)
            return false;

        string query = url.Substring(queryStart + 1);
        int hashIndex = query.IndexOf('#');
        if (hashIndex >= 0)
            query = query.Substring(0, hashIndex);

        string[] pairs = query.Split('&');
        for (int i = 0; i < pairs.Length; i++)
        {
            string pair = pairs[i];
            if (string.IsNullOrEmpty(pair))
                continue;

            int eqIndex = pair.IndexOf('=');
            string rawKey = eqIndex >= 0 ? pair.Substring(0, eqIndex) : pair;
            string decodedKey = Uri.UnescapeDataString(rawKey);

            if (!string.Equals(decodedKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            string rawValue = eqIndex >= 0 ? pair.Substring(eqIndex + 1) : string.Empty;
            value = Uri.UnescapeDataString(rawValue.Replace("+", " "));
            return true;
        }

        return false;
    }
}