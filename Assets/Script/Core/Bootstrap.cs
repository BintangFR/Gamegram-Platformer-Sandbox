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
    [SerializeField] private string sandboxIdKey = "sandbox_id";
    [SerializeField] private string creatorIdKey = "creator_id";

    [Header("API Endpoints")]
    [SerializeField] private bool fetchOnStart = true;
    [SerializeField] private string bootstrapEndpoint = "https://gamegram-test.onrender.com/test/getjson";

    [Header("Scene Routing")]
    [SerializeField] private string editModeSceneName = "Editmode";
    [SerializeField] private string playModeSceneName = "Playmode";

    [Header("Local JSON")]
    [SerializeField] private string cacheFolder = "LevelData";
    [SerializeField] private string fallbackFileName = "level_01_tiles.json";
    [SerializeField] private bool useLevelIdAsFileName = true;

    [Header("Parsed Values (Read Only)")]
    // Defaults can be kept here for easy Editor testing without URL parameters
    [SerializeField] private string mode = "edit";
    private string sandboxId = "test_sandbox_456";
    private string creatorId = "test_creator_789";
    [SerializeField] private string levelJsonUrl;
    [SerializeField] private string cachedFilePath;

    [Header("Optional UI Debug")]
    [SerializeField] private TextMeshProUGUI statusText;

    public static string Mode { get; private set; }
    public static string SandboxId { get; private set; }
    public static string CreatorId { get; private set; }
    public static string ActiveLevelFileName { get; private set; }
    public static string CachedLevelFilePath { get; private set; }
    public static string PendingLevelJson { get; private set; }

    public static bool IsNoEditMode => string.Equals(Mode, "noedit", StringComparison.OrdinalIgnoreCase);
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

    private void ApplyDebugOverrides()
    {
        if (debugStartMode == DebugStartMode.Play)
            mode = "noedit";
        else if (debugStartMode == DebugStartMode.Edit)
            mode = "edit";

        Mode = mode;
        SandboxId = sandboxId;
        CreatorId = creatorId;

        Debug.Log("[Bootstrap] Debug override active | overridden_mode=" + Mode, this);
    }

    private void Start()
    {
        if (!fetchOnStart)
            return;

        StartCoroutine(CallModeEndpointRoutine());
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

        if (TryGetQueryParam(url, sandboxIdKey, out string parsedSandboxId))
            sandboxId = parsedSandboxId;

        if (TryGetQueryParam(url, creatorIdKey, out string parsedCreatorId))
            creatorId = parsedCreatorId;

        Mode = mode;
        SandboxId = sandboxId;
        CreatorId = creatorId;

        Debug.Log(
            "[Bootstrap] mode=" + Mode +
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
        // Only call getjson if started in play mode ("noedit")
        if (IsNoEditMode)
        {
            if (string.IsNullOrWhiteSpace(sandboxId))
            {
                Debug.LogError("[Bootstrap] sandbox_id is empty. Cannot build bootstrap URL.");
                UpdateStatusText("Missing sandbox id.");
                yield break; 
            }

            if (string.IsNullOrWhiteSpace(bootstrapEndpoint))
            {
                Debug.LogError("[Bootstrap] bootstrapEndpoint is empty. Set it in the Inspector.");
                UpdateStatusText("Missing bootstrap endpoint.");
                yield break;
            }

            string urlParams = "?sandbox_id=" + Uri.EscapeDataString(sandboxId) + "&creator_id=" + Uri.EscapeDataString(creatorId);
            string urlToCall = bootstrapEndpoint + urlParams;

            UpdateStatusText("Fetching level JSON...");
            string responseText;

            using (UnityWebRequest request = UnityWebRequest.Get(urlToCall))
            {
                LogApiRequest("GET", urlToCall);

                yield return request.SendWebRequest();

                responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                LogApiResponse(request);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[Bootstrap] GET failed: " + request.error, this);
                    UpdateStatusText("GET failed. Transitioning anyway...");
                    RouteByModeAfterSuccess();
                    yield break;
                }
            }

            string trimmed = string.IsNullOrWhiteSpace(responseText) ? string.Empty : responseText.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                SaveLevelJson(trimmed);
                PendingLevelJson = trimmed;
                UpdateStatusText("Level JSON received.");
                RouteByModeAfterSuccess();
                yield break;
            }

            if (!TryExtractLevelJsonUrl(responseText, out string extractedUrl))
            {
                Debug.LogError("[Bootstrap] Response is neither level JSON nor URL.", this);
                UpdateStatusText("Invalid level response. Transitioning anyway...");
                RouteByModeAfterSuccess();
                yield break;
            }

            string downloadedJson;
            using (UnityWebRequest levelRequest = UnityWebRequest.Get(extractedUrl))
            {
                LogApiRequest("GET", extractedUrl);

                yield return levelRequest.SendWebRequest();

                LogApiResponse(levelRequest);

                if (levelRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[Bootstrap] Level JSON download failed: " + levelRequest.error, this);
                    UpdateStatusText("Level JSON download failed. Transitioning anyway...");
                    RouteByModeAfterSuccess();
                    yield break;
                }

                downloadedJson = levelRequest.downloadHandler.text;
            }

            SaveLevelJson(downloadedJson);
            PendingLevelJson = downloadedJson;
            UpdateStatusText("Level JSON received.");
            RouteByModeAfterSuccess();
        }
        else
        {
            // Edit mode: Skip fetching JSON and just go straight to the Edit scene
            UpdateStatusText("Edit mode: skipping GET JSON in Bootstrap.");
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

    private static string TruncateForLog(string value, int maxLength = 4000)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "\n\n...[TRUNCATED: original length " + value.Length + "]";
    }

    private static string GetResponseBody(UnityWebRequest request)
    {
        if (request == null || request.downloadHandler == null)
            return string.Empty;

        return request.downloadHandler.text ?? string.Empty;
    }

    private void LogApiRequest(string method, string url, string payload = null)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            Debug.Log("[Bootstrap][API][Request]\nURL: " + method + " " + url, this);
            return;
        }

        Debug.Log(
            "[Bootstrap][API][Request]\nURL: " + method + " " + url + "\n" +
            "Payload:\n" + TruncateForLog(payload),
            this);
    }

    private void LogApiResponse(UnityWebRequest request)
    {
        string body = GetResponseBody(request);
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
        string statusLabel = isSuccess ? "[SUCCESS]" : "[FAILED]";

        string logMessage =
            "[Bootstrap][API][Response] " + statusLabel + "\n" +
            "URL: " + request.method + " " + request.url + "\n" +
            "Status: HTTP " + request.responseCode + " | UnityResult: " + request.result + "\n" +
            "Error Details: " + (string.IsNullOrEmpty(request.error) ? "None" : request.error) + "\n" +
            "Response Body:\n" + TruncateForLog(body);

        if (isSuccess)
            Debug.Log(logMessage, this);
        else
            Debug.LogError(logMessage, this); 
    }
}