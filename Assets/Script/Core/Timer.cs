using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Timer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Behavior")]
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool stopOnPlayerTrigger = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Events")]
    [SerializeField] private UnityEvent onTimerStopped;

    private float elapsedTime;
    private bool isRunning;

    public float ElapsedTime => elapsedTime;
    public bool IsRunning => isRunning;

    private void OnEnable()
    {
        if (startOnEnable)
            StartTimer();
        else
            RefreshText();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        elapsedTime += Time.deltaTime;
        RefreshText();
    }

    public void StartTimer()
    {
        isRunning = true;
        RefreshText();
    }

    public void StopTimer()
    {
        if (!isRunning)
            return;

        isRunning = false;
        onTimerStopped?.Invoke();
        RefreshText();
    }

    public void ResetTimer(bool startImmediately = false)
    {
        elapsedTime = 0f;
        isRunning = startImmediately;
        RefreshText();
    }

    public void NotifyGoalReached()
    {
        StopTimer();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!stopOnPlayerTrigger)
            return;

        if (!other.CompareTag(playerTag))
            return;

        StopTimer();
    }

    private void RefreshText()
    {
        if (timerText == null)
            return;

        int minutes = (int)(elapsedTime / 60f);
        float seconds = elapsedTime % 60f;
        timerText.text = string.Format("{0:00}:{1:00.00}", minutes, seconds);
    }
}
