using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaymodeManager : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string editSceneName = "EditModeScene";

    [Header("Play References")]
    [SerializeField] private PlayerControlUI playerControlUi;
    [SerializeField] private SpawnPosition spawnPosition;
    [SerializeField] private Timer timer;
    [SerializeField] private FinishScreen finishScreen;
    [SerializeField] private PlayerController player;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Awake()
    {
        ResolveReferences();
        LogReferences("Awake");
    }

    private void Start()
    {
        StartPlaySession();
    }

    public void StartPlaySession()
    {
        ResolveReferences();
        Log("StartPlaySession() begin");

        if (spawnPosition == null)
        {
            LogWarning("SpawnPosition not found in scene.");
            return;
        }

        spawnPosition.Initialize();
        EnsurePlayer();

        if (player == null)
        {
            LogWarning("Player is still null after spawn attempt.");
            return;
        }

        if (finishScreen != null)
            finishScreen.Hide();

        player.Initialize();
        player.enabled = true;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        if (playerControlUi != null)
        {
            playerControlUi.Initialize(player);
            playerControlUi.gameObject.SetActive(true);
        }

        InitializeEnemies();

        if (timer != null)
            timer.ResetTimer(true);

        Log("StartPlaySession() end");
    }

    public void RestartPlayMode()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToEditMode()
    {
        if (string.IsNullOrWhiteSpace(editSceneName))
        {
            LogWarning("Edit scene name is empty.");
            return;
        }

        SceneManager.LoadScene(editSceneName);
    }

    private void ResolveReferences()
    {
        if (playerControlUi == null)
            playerControlUi = FindFirstObjectByType<PlayerControlUI>(FindObjectsInactive.Include);

        if (spawnPosition == null)
            spawnPosition = FindFirstObjectByType<SpawnPosition>(FindObjectsInactive.Include);

        if (timer == null)
            timer = FindFirstObjectByType<Timer>(FindObjectsInactive.Include);

        if (finishScreen == null)
            finishScreen = FindFirstObjectByType<FinishScreen>(FindObjectsInactive.Include);

        if (player == null)
            player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    private void EnsurePlayer()
    {
        if (player == null)
        {
            GameObject spawned = spawnPosition.SpawnPlayer();
            if (spawned != null)
                player = spawned.GetComponent<PlayerController>();
        }

        if (player != null)
            player.transform.SetPositionAndRotation(spawnPosition.transform.position, spawnPosition.transform.rotation);
    }

    private static void InitializeEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].Initialize();
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log("[PlaymodeManager] " + message, this);
    }

    private void LogWarning(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.LogWarning("[PlaymodeManager] " + message, this);
    }

    private void LogReferences(string context)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log(
            "[PlaymodeManager] " + context +
            " | spawnPosition=" + (spawnPosition != null) +
            " | player=" + (player != null) +
            " | playerControlUi=" + (playerControlUi != null) +
            " | timer=" + (timer != null) +
            " | finishScreen=" + (finishScreen != null),
            this
        );
    }
}
