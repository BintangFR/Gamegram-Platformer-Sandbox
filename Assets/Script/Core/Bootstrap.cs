using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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

    private enum DebugStartMode
    {
        UseLaunchMode = 0,
        Play = 1,
        Edit = 2
    }

    [Header("Debug Override")]
    [SerializeField] private bool debugMode;
    [SerializeField] private DebugStartMode debugStartMode = DebugStartMode.UseLaunchMode;

    [Header("Launch Query Keys")]
    [SerializeField] private string modeKey = "mode";
    [SerializeField] private string levelIdKey = "level_id";
    [SerializeField] private string sandboxIdKey = "sandbox_id";
    [SerializeField] private string creatorIdKey = "creator_id";

    [Header("API Endpoints")]
    [SerializeField] private bool fetchOnStart = true;
    [SerializeField] private string bootstrapEndpoint = "https://gamegram-test.onrender.com/test/getjson";
    [SerializeField] private string createSandboxEndpoint = "http://127.0.0.1:8000/sandboxes/create";

    [Header("Scene Routing")]
    [SerializeField] private string editModeSceneName = "Editmode";
    [SerializeField] private string playModeSceneName = "Playmode";

    [Header("Local JSON")]
    [SerializeField] private string cacheFolder = "LevelData";
    [SerializeField] private string fallbackFileName = "level_01_tiles.json";
    [SerializeField] private bool useLevelIdAsFileName = true;

    [Header("Debug")]
    [SerializeField] private bool loadJSONlocallyDebug = false;

    [Header("Parsed Values (Read Only)")]
    [SerializeField] private string mode;
    [SerializeField] private string levelId;
    [SerializeField] private string sandboxId;
    [SerializeField] private string creatorId;
    [SerializeField] private string levelJsonUrl;
    [SerializeField] private string cachedFilePath;

    [Header("Optional UI Debug")]
    [SerializeField] private TextMeshProUGUI statusText;

    public static string Mode { get; private set; }
    public static string LevelId { get; private set; }
    public static string SandboxId { get; private set; }
    public static string CreatorId { get; private set; }
    public static string ActiveLevelFileName { get; private set; }
    public static string CachedLevelFilePath { get; private set; }
    public static string PendingLevelJson { get; private set; }

    public static bool IsNoEditMode => string.Equals(Mode, "noedit", StringComparison.OrdinalIgnoreCase);
    public static bool LoadJSONLocallyDebug { get; private set; }
    public static string LocalMemoryLevelJson { get; set; }
    public static bool IsDebugMode { get; private set; }

    public static string ConsumePendingLevelJson()
    {
        string json = PendingLevelJson;
        PendingLevelJson = null;
        return json;
    }

    private void Awake()
    {
        IsDebugMode = debugMode;

        ParseLaunchUrl();

        if (IsDebugMode)
            ApplyDebugOverrides();

        UpdateStatusText("Launch params parsed.");
    }

    private void Start()
    {
        if (IsDebugMode)
        {
            PendingLevelJson = null;
            ActiveLevelFileName = null;
            CachedLevelFilePath = null;
            cachedFilePath = null;

            UpdateStatusText("Debug mode active. API calls skipped.");
            RouteByModeAfterSuccess();
            return;
        }

        if (!fetchOnStart)
            return;

        StartCoroutine(CallModeEndpointRoutine());
    }

    private void ApplyDebugOverrides()
    {
        if (debugStartMode == DebugStartMode.Play)
            mode = "noedit";
        else if (debugStartMode == DebugStartMode.Edit)
            mode = "edit";

        Mode = mode;
        LevelId = levelId;
        SandboxId = sandboxId;
        CreatorId = creatorId;

        Debug.Log("[Bootstrap] Debug override active | mode=" + Mode, this);
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

        if (TryGetQueryParam(url, sandboxIdKey, out string parsedSandboxId))
            sandboxId = parsedSandboxId;

        if (TryGetQueryParam(url, creatorIdKey, out string parsedCreatorId))
            creatorId = parsedCreatorId;

        Mode = mode;
        LevelId = levelId;
        SandboxId = sandboxId;
        CreatorId = creatorId;

        Debug.Log(
            "[Bootstrap] mode=" + Mode +
            " | level_id=" + LevelId +
            " | sandbox_id=" + SandboxId +
            " | creator_id=" + CreatorId,
            this);
    }

    private static void AddFormFieldIfHasValue(List<IMultipartFormSection> formData, string fieldName, string value)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return;

        if (string.IsNullOrWhiteSpace(value))
            return;

        formData.Add(new MultipartFormDataSection(fieldName, value));
    }

    private IEnumerator CallModeEndpointRoutine()
    {
        // noedit: get JSON directly from backend endpoint (GET)
        if (IsNoEditMode)
        {
            if (string.IsNullOrWhiteSpace(bootstrapEndpoint))
            {
                Debug.LogError("[Bootstrap] bootstrapEndpoint is empty.");
                yield break;
            }

            UpdateStatusText("Fetching level JSON...");

            string responseText;
            using (UnityWebRequest request = UnityWebRequest.Get(bootstrapEndpoint))
            {
                yield return request.SendWebRequest();

                responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                Debug.Log("[Bootstrap] GET response: " + responseText, this);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[Bootstrap] GET failed: " + request.error, this);
                    UpdateStatusText("GET failed.");
                    yield break;
                }
            }

            // If backend returns JSON string directly, use it immediately
            string trimmed = string.IsNullOrWhiteSpace(responseText) ? string.Empty : responseText.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                SaveLevelJson(trimmed);
                PendingLevelJson = trimmed;
                UpdateStatusText("Level JSON received.");
                RouteByModeAfterSuccess();
                yield break;
            }

            // Optional fallback: backend returned URL instead of JSON
            if (!TryExtractLevelJsonUrl(responseText, out string extractedUrl))
            {
                Debug.LogError("[Bootstrap] Response is neither level JSON nor URL.", this);
                UpdateStatusText("Invalid level response.");
                yield break;
            }

            string downloadedJson;
            using (UnityWebRequest levelRequest = UnityWebRequest.Get(extractedUrl))
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
            PendingLevelJson = downloadedJson;
            UpdateStatusText("Level JSON received.");
            RouteByModeAfterSuccess();
            yield break;
        }

        // edit/create flow: keep existing multipart POST
        if (string.IsNullOrWhiteSpace(createSandboxEndpoint))
        {
            Debug.LogError("[Bootstrap] createSandboxEndpoint is empty.");
            yield break;
        }

        string fallbackJsonString = "{\"levelName\":\"Initial\",\"tiles\":[]}";
        byte[] levelFileBytes = System.Text.Encoding.UTF8.GetBytes(fallbackJsonString);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        AddFormFieldIfHasValue(formData, "sandbox_id", sandboxId);
        AddFormFieldIfHasValue(formData, "current_user", creatorId);
        AddFormFieldIfHasValue(formData, "edit", mode);
        AddFormFieldIfHasValue(formData, "level_id", levelId);
        formData.Add(new MultipartFormFileSection("level_file", levelFileBytes, "level.json", "application/json"));

        UpdateStatusText("Sending form-data...");

        using (UnityWebRequest request = UnityWebRequest.Post(createSandboxEndpoint, formData))
        {
            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            Debug.Log("[Bootstrap] Endpoint Response: " + responseText, this);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Bootstrap] Form-data request failed: " + request.error, this);
                UpdateStatusText("Form-data request failed.");
                yield break;
            }

            UpdateStatusText("Sandbox created.");
            RouteByModeAfterSuccess();
        }
    }

    // Update SaveLevelJson to stop doing disk IO and just hold it in memory
    private void SaveLevelJson(string json)
    {
        // Save dynamically into memory instead of a physical file
        LocalMemoryLevelJson = json;
        PendingLevelJson = json;

        Debug.Log("[Bootstrap] Level JSON saved directly to memory.", this);
    }

    private void RouteByModeAfterSuccess()
    {
        string targetScene = IsNoEditMode ? playModeSceneName : editModeSceneName;
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("[Bootstrap] Target scene is empty.");
            return;
        }

        SceneManager.LoadScene(targetScene);
    }

    private string GetLocalLevelJsonFilePath()
    {
        string localFileName = fallbackFileName;

        if (!string.IsNullOrWhiteSpace(ActiveLevelFileName))
            localFileName = ActiveLevelFileName;

        return Path.Combine(Application.persistentDataPath, cacheFolder, localFileName);
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
            "sandbox_id=" + sandboxId + "\n" +
            "creator_id=" + creatorId + "\n" +
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