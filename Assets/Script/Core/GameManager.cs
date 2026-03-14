using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string editSceneName = "EditModeScene";
    [SerializeField] private string playSceneName = "PlayModeScene";

    [Header("Runtime References (auto-resolved per scene)")]
    [SerializeField] private Groundeditor groundEditor;
    [SerializeField] private PlayerControlUI playerControlUi;
    [SerializeField] private SpawnPosition spawnPosition;
    [SerializeField] private Timer timer;
    [SerializeField] private FinishScreen finishScreen;
    [SerializeField] private PlayerController player;

    private static GameManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ResolveSceneReferences();
        ApplySceneState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveSceneReferences();
        ApplySceneState();
    }

    public void EnterEditMode()
    {
        LoadSceneByName(editSceneName);
    }

    public void EnterPlayMode()
    {
        LoadSceneByName(playSceneName);
    }

    public void BackToEditMode()
    {
        EnterEditMode();
    }

    public void RestartPlayMode()
    {
        LoadSceneByName(playSceneName);
    }

    private void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[GameManager] Scene name is empty.");
            return;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
            return;

        SceneManager.LoadScene(sceneName);
    }

    private void ResolveSceneReferences()
    {
        groundEditor = FindFirstObjectByType<Groundeditor>(FindObjectsInactive.Include);
        playerControlUi = FindFirstObjectByType<PlayerControlUI>(FindObjectsInactive.Include);
        spawnPosition = FindFirstObjectByType<SpawnPosition>(FindObjectsInactive.Include);
        timer = FindFirstObjectByType<Timer>(FindObjectsInactive.Include);
        finishScreen = FindFirstObjectByType<FinishScreen>(FindObjectsInactive.Include);
        player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    private void ApplySceneState()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;

        bool isEditScene = activeSceneName == editSceneName;
        bool isPlayScene = activeSceneName == playSceneName;

        if (groundEditor != null)
            groundEditor.SetEditMode(isEditScene);

        if (playerControlUi != null)
            playerControlUi.gameObject.SetActive(isPlayScene);

        if (finishScreen != null)
            finishScreen.Hide();

        if (isPlayScene)
            PreparePlayScene();
    }

    private void PreparePlayScene()
    {
        EnsurePlayer();

        if (player != null)
        {
            player.enabled = true;

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
                playerRb.linearVelocity = Vector2.zero;
        }

        if (playerControlUi != null)
            playerControlUi.SetPlayer(player);

        if (timer != null)
            timer.ResetTimer(true);
    }

    private void EnsurePlayer()
    {
        if (player == null && spawnPosition != null)
        {
            GameObject spawned = spawnPosition.SpawnPlayer();
            if (spawned != null)
                player = spawned.GetComponent<PlayerController>();
        }

        if (player != null && spawnPosition != null)
            player.transform.SetPositionAndRotation(spawnPosition.transform.position, spawnPosition.transform.rotation);
    }
}
