using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FinishScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Timer timer;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private string winTitle = "You Win";
    [SerializeField] private string loseTitle = "You Lose";

    [Header("Coin UI (same behavior as CoinUI)")]
    [SerializeField] private List<Image> coinImages = new List<Image>();
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private float activeAlpha = 1f;

    [Header("Completion Time")]
    [SerializeField] private TextMeshProUGUI completionTimeText;

    [Header("Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonLabel;
    [SerializeField] private string backToEditLabel = "Back to Edit Mode";
    [SerializeField] private string restartLabel = "Restart";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onBackToEditModeRequested;
    [SerializeField] private UnityEvent onRestartRequested;

    private bool isLoseState;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (timer == null)
            timer = FindFirstObjectByType<Timer>(FindObjectsInactive.Include);

        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);

        if (enableDebugLogs)
            Debug.Log("[FinishScreen] Awake | InstanceId=" + GetInstanceID(), this);
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(OnActionButtonClicked);
    }

    public void Show()
    {
        ShowInternal(false);
    }

    public void ShowLose()
    {
        ShowInternal(true);
    }

    private void ShowInternal(bool lose)
    {
        isLoseState = lose;

        if (enableDebugLogs)
            Debug.Log("[FinishScreen] Show called | LoseState=" + isLoseState + " | InstanceId=" + GetInstanceID(), this);

        if (timer != null)
            timer.StopTimer();

        gameObject.SetActive(true);

        RefreshTitle();
        RefreshCoins();
        RefreshCompletionTime();
        RefreshActionButton();
    }

    public void Hide()
    {
        if (enableDebugLogs)
            Debug.Log("[FinishScreen] Hide called | InstanceId=" + GetInstanceID(), this);

        gameObject.SetActive(false);
    }

    private void RefreshTitle()
    {
        if (titleText == null)
            return;

        titleText.text = isLoseState ? loseTitle : winTitle;
    }

    private void RefreshCoins()
    {
        int collectedCount = player != null ? player.CoinCount : 0;

        for (int i = 0; i < coinImages.Count; i++)
        {
            Image image = coinImages[i];
            if (image == null)
                continue;

            Color color = image.color;
            color.a = i < collectedCount ? activeAlpha : inactiveAlpha;
            image.color = color;
        }
    }

    private void RefreshCompletionTime()
    {
        if (completionTimeText == null)
            return;

        float elapsedTime = timer != null ? timer.ElapsedTime : 0f;
        int minutes = (int)(elapsedTime / 60f);
        float seconds = elapsedTime % 60f;
        completionTimeText.text = string.Format("{0:00}:{1:00.00}", minutes, seconds);
    }

    private void RefreshActionButton()
    {
        bool isEditModeAvailable = true;

        if (actionButtonLabel != null)
            actionButtonLabel.text = isEditModeAvailable ? backToEditLabel : restartLabel;
    }

    private void OnActionButtonClicked()
    {
        bool isEditModeAvailable = true;

        if (isEditModeAvailable)
        {
            onBackToEditModeRequested?.Invoke();
            return;
        }

        onRestartRequested?.Invoke();

        if (onRestartRequested == null || onRestartRequested.GetPersistentEventCount() == 0)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
