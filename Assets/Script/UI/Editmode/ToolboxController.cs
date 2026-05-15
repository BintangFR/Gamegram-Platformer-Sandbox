using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ToolboxController : MonoBehaviour
{
    [SerializeField] private MapTileEditor mapTileEditor;

    [Header("Tile Buttons (5 + Eraser)")]
    [SerializeField] private Button wallButton;
    [SerializeField] private Button enemyButton;
    [SerializeField] private Button coinButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button goalButton;
    [SerializeField] private Button eraserButton;

    [Header("Tile Count Labels")]
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI startCountText;
    [SerializeField] private TextMeshProUGUI goalCountText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Action Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button saveAndPlayButton;
    [SerializeField] private string playModeSceneName = "Playmode";

    [Header("Cloud Save")]
    [SerializeField] private bool callCreateSandboxAfterSave = true;
    private string createSandboxEndpoint = "https://gamegram-test.onrender.com/test/create";
    [SerializeField] private string editFormValue = "edit";
    [SerializeField] private string levelFileFormFieldName = "level_file";

    [Header("Availability UI")]
    [SerializeField] private float availableAlpha = 1f;
    [SerializeField] private float unavailableAlpha = 0.5f;

    [Header("Warning Animation")]
    [SerializeField] private float warningFadeInDuration = 0.2f;
    [SerializeField] private float warningVisibleDuration = 1.5f;
    [SerializeField] private float warningFadeOutDuration = 0.3f;

    private Coroutine warningRoutine;
    private Color warningBaseColor = Color.white;
    private bool isSaveRoutineRunning;

    // First, add a string variable to hold our local save at the class level
    private string localSavedJsonString = string.Empty;

    private void Awake()
    {
        if (mapTileEditor == null)
            mapTileEditor = FindFirstObjectByType<MapTileEditor>(FindObjectsInactive.Include);

        if (warningText != null)
        {
            warningBaseColor = warningText.color;
            SetWarningAlpha(0f);
            warningText.gameObject.SetActive(false);
        }

        RegisterButtonListeners();

        if (mapTileEditor != null)
            mapTileEditor.PlacementRemainingChanged += OnPlacementRemainingChanged;
    }

    private void Start()
    {
        if (mapTileEditor == null)
            return;

        OnPlacementRemainingChanged(
            mapTileEditor.RemainingStart,
            mapTileEditor.RemainingCoin,
            mapTileEditor.RemainingGoal);

        SetWarningText(string.Empty);
    }

    private void OnDestroy()
    {
        if (mapTileEditor != null)
            mapTileEditor.PlacementRemainingChanged -= OnPlacementRemainingChanged;

        if (warningRoutine != null)
            StopCoroutine(warningRoutine);

        UnregisterButtonListeners();
    }

    private void RegisterButtonListeners()
    {
        AddButtonListener(wallButton, SelectWall);
        AddButtonListener(enemyButton, SelectEnemy);
        AddButtonListener(coinButton, SelectCoin);
        AddButtonListener(startButton, SelectStart);
        AddButtonListener(goalButton, SelectGoal);
        AddButtonListener(eraserButton, SelectEraser);
        AddButtonListener(saveButton, Save);
        AddButtonListener(saveAndPlayButton, SaveAndPlay);
    }

    private void UnregisterButtonListeners()
    {
        RemoveButtonListener(wallButton, SelectWall);
        RemoveButtonListener(enemyButton, SelectEnemy);
        RemoveButtonListener(coinButton, SelectCoin);
        RemoveButtonListener(startButton, SelectStart);
        RemoveButtonListener(goalButton, SelectGoal);
        RemoveButtonListener(eraserButton, SelectEraser);
        RemoveButtonListener(saveButton, Save);
        RemoveButtonListener(saveAndPlayButton, SaveAndPlay);
    }

    private void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
    }

    private void OnPlacementRemainingChanged(int startRemaining, int coinRemaining, int goalRemaining)
    {
        SetCountText(startCountText, startRemaining);
        SetCountText(coinCountText, coinRemaining);
        SetCountText(goalCountText, goalRemaining);

        SetAvailabilityVisual(startButton, startCountText, startRemaining > 0);
        SetAvailabilityVisual(coinButton, coinCountText, coinRemaining > 0);
        SetAvailabilityVisual(goalButton, goalCountText, goalRemaining > 0);

        if (mapTileEditor != null && mapTileEditor.StartTileCount > 0 && mapTileEditor.GoalTileCount > 0)
            SetWarningText(string.Empty);
    }

    private void SetCountText(TextMeshProUGUI label, int value)
    {
        if (label == null)
            return;

        label.text = value.ToString();
    }

    private void SetAvailabilityVisual(Button button, TextMeshProUGUI label, bool isAvailable)
    {
        float alpha = isAvailable ? availableAlpha : unavailableAlpha;

        if (button != null && button.image != null)
        {
            Color buttonColor = button.image.color;
            buttonColor.a = alpha;
            button.image.color = buttonColor;
        }

        if (label != null)
        {
            Color labelColor = label.color;
            labelColor.a = alpha;
            label.color = labelColor;
        }
    }

    private void SetWarningText(string message)
    {
        if (warningText == null)
            return;

        if (warningRoutine != null)
            StopCoroutine(warningRoutine);

        warningRoutine = StartCoroutine(AnimateWarningText(message));
    }

    private IEnumerator AnimateWarningText(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            if (warningText.gameObject.activeSelf)
                yield return FadeWarningAlphaTo(0f, warningFadeOutDuration);

            warningText.gameObject.SetActive(false);
            warningRoutine = null;
            yield break;
        }

        warningText.text = message;
        warningText.gameObject.SetActive(true);

        yield return FadeWarningAlphaTo(1f, warningFadeInDuration);

        if (warningVisibleDuration > 0f)
            yield return new WaitForSeconds(warningVisibleDuration);

        yield return FadeWarningAlphaTo(0f, warningFadeOutDuration);
        warningText.gameObject.SetActive(false);
        warningRoutine = null;
    }

    private IEnumerator FadeWarningAlphaTo(float targetAlpha, float duration)
    {
        float startAlpha = warningText.color.a;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            SetWarningAlpha(targetAlpha);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetWarningAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetWarningAlpha(targetAlpha);
    }

    private void SetWarningAlpha(float alpha)
    {
        if (warningText == null)
            return;

        Color c = warningBaseColor;
        c.a = alpha;
        warningText.color = c;
    }

    private bool CanSaveMap(out string warningMessage)
    {
        warningMessage = string.Empty;

        if (mapTileEditor == null)
            return false;

        bool hasStartTile = mapTileEditor.StartTileCount > 0;
        bool hasGoalTile = mapTileEditor.GoalTileCount > 0;

        if (hasStartTile && hasGoalTile)
            return true;

        if (!hasStartTile && !hasGoalTile)
            warningMessage = "Place at least 1 Start tile and 1 Goal tile before saving.";
        else if (!hasStartTile)
            warningMessage = "Place at least 1 Start tile before saving.";
        else
            warningMessage = "Place at least 1 Goal tile before saving.";

        return false;
    }

    private bool TrySaveMap(out string savedFilePath)
    {
        savedFilePath = null;

        if (!CanSaveMap(out string warningMessage))
        {
            SetWarningText(warningMessage);
            return false;
        }

        SetWarningText(string.Empty);
        mapTileEditor.SaveMapToJson();
        savedFilePath = mapTileEditor.GetFilePath();
        return true;
    }

    private IEnumerator SaveFlowRoutine(bool loadPlaySceneAfterSave)
    {
        if (isSaveRoutineRunning)
            yield break;

        isSaveRoutineRunning = true;

        if (!TrySaveMap(out string savedFilePath))
        {
            isSaveRoutineRunning = false;
            yield break;
        }

        // Keep in memory if needed by play scene
        if (File.Exists(savedFilePath))
            Bootstrap.LocalMemoryLevelJson = File.ReadAllText(savedFilePath);

        // Always try server save after local save.
        if (callCreateSandboxAfterSave)
        {
            yield return UploadSavedLevelToSandbox(savedFilePath, createSandboxEndpoint);
        }

        if (loadPlaySceneAfterSave && !string.IsNullOrWhiteSpace(playModeSceneName))
            SceneManager.LoadScene(playModeSceneName);

        isSaveRoutineRunning = false;
    }

    [Serializable]
    private class DateData
    {
        public string date;
    }

    private static void AddFormFieldIfHasValue(List<IMultipartFormSection> formData, string fieldName, string value)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return;

        if (string.IsNullOrWhiteSpace(value))
            return;

        formData.Add(new MultipartFormDataSection(fieldName, value));
    }

    private IEnumerator UploadSavedLevelToSandbox(string levelFilePath, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Debug.LogWarning("[ToolboxController] save endpoint is empty.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(levelFilePath) || !File.Exists(levelFilePath))
        {
            Debug.LogError("[ToolboxController] Saved level file not found: " + levelFilePath, this);
            yield break;
        }

        string levelJson = File.ReadAllText(levelFilePath);
        if (string.IsNullOrWhiteSpace(levelJson))
        {
            Debug.LogError("[ToolboxController] Saved level file is empty.", this);
            yield break;
        }

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        string finalSandboxId = string.IsNullOrEmpty(Bootstrap.SandboxId) ? "test_sandbox_456" : Bootstrap.SandboxId;
        string finalCreatorId = string.IsNullOrEmpty(Bootstrap.CreatorId) ? "test_creator_789" : Bootstrap.CreatorId;

        AddFormFieldIfHasValue(formData, "sandbox_id", finalSandboxId);
        AddFormFieldIfHasValue(formData, "creator_id", finalCreatorId); 
        
        // Add as a string data section to avoid the boundary parser 'unknown error'
        AddFormFieldIfHasValue(formData, "level_file", levelJson);

        LogApiRequest(
            endpoint,
            "sandbox_id=" + finalSandboxId + "\n" +
            "creator_id=" + finalCreatorId + "\n" +
            "level_file (chars)=" + levelJson.Length);

        using (UnityWebRequest request = UnityWebRequest.Post(endpoint, formData))
        {
            yield return request.SendWebRequest();

            LogApiResponse(request);

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogError("[ToolboxController] savejsonfile failed, continuing. Error: " + request.error, this);
        }
    }

    public void SelectWall()
    {
        if (mapTileEditor == null)
            return;

        mapTileEditor.SetSelectedTileType(MapTileEditor.TileType.Wall);
    }

    public void SelectEnemy()
    {
        if (mapTileEditor == null)
            return;

        mapTileEditor.SetSelectedTileType(MapTileEditor.TileType.Enemy);
    }

    public void SelectCoin()
    {
        if (mapTileEditor == null)
            return;

        mapTileEditor.SetSelectedTileType(MapTileEditor.TileType.Coin);
    }

    public void SelectStart()
    {
        if (mapTileEditor == null)
            return;

        mapTileEditor.SetSelectedTileType(MapTileEditor.TileType.Start);
    }

    public void SelectGoal()
    {
        if (mapTileEditor == null)
            return;

        mapTileEditor.SetSelectedTileType(MapTileEditor.TileType.Goal);
    }

    public void SelectEraser()
    {
        if (mapTileEditor == null)
            return;

        mapTileEditor.SelectEraser();
    }

    public void Save()
    {
        StartCoroutine(SaveFlowRoutine(false));
    }

    public void SaveAndPlay()
    {
        StartCoroutine(SaveFlowRoutine(true));
    }

    private static bool IsSameEndpoint(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return string.Equals(
            left.TrimEnd('/'),
            right.TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string TruncateForLog(string value, int maxLength = 800)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "...(truncated)";
    }

    private void LogApiRequest(string endpoint, string detail)
    {
        Debug.Log(
            "[ToolboxController][API][Request] POST " + endpoint + "\n" +
            detail,
            this);
    }

    private void LogApiResponse(UnityWebRequest request)
    {
        string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        Debug.Log(
            "[ToolboxController][API][Response] " + request.method + " " + request.url + "\n" +
            "HTTP " + request.responseCode + " | Result=" + request.result + "\n" +
            "Error: " + (string.IsNullOrEmpty(request.error) ? "-" : request.error) + "\n" +
            "Body: " + TruncateForLog(responseText),
            this);
    }
}
