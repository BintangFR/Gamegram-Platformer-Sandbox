using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Goal : MonoBehaviour
{
    [Header("Goal Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Timer timer;
    [SerializeField] private FinishScreen finishScreen;
    [SerializeField] private PlayerControlUI controlUI;
    [SerializeField] private bool disableAfterReached = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onGoalReached;

    private Collider2D coll;
    private bool isReached;

    private void Awake()
    {
        coll = GetComponent<Collider2D>();
        coll.isTrigger = true;

        if (timer == null)
            timer = FindFirstObjectByType<Timer>(FindObjectsInactive.Include);

        if (finishScreen == null)
            finishScreen = FindFirstObjectByType<FinishScreen>(FindObjectsInactive.Include);

        if (controlUI == null)
            controlUI = FindFirstObjectByType<PlayerControlUI>(FindObjectsInactive.Include);

        if (enableDebugLogs)
            Debug.Log("[Goal] Awake | FinishScreenFound=" + (finishScreen != null), this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReached || !other.CompareTag(playerTag))
            return;

        isReached = true;

        if (timer != null)
            timer.StopTimer();

        if (finishScreen != null)
        {
            if (enableDebugLogs)
                Debug.Log("[Goal] Calling FinishScreen.Show() on " + finishScreen.name, this);

            finishScreen.Show();
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("[Goal] FinishScreen is null.", this);
        }

        if (controlUI != null)
            controlUI.SetControlEnable(false);

        onGoalReached?.Invoke();

        if (disableAfterReached && coll != null)
            coll.enabled = false;
    }
}
