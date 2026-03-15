using UnityEngine;

public class PlaymodeManager : MonoBehaviour
{
    [SerializeField] private LevelConstructor levelConstructor;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerControlUI controlUI;
    [SerializeField] private SpawnPosition spawnPosition;
    [SerializeField] private bool forceSpawnAtStart = true;
    [SerializeField] private bool initializeOnStart = true;

    private bool isInitialized;

    private void Start()
    {
        if (!initializeOnStart)
            return;

        string pendingJson = Bootstrap.ConsumePendingLevelJson();
        InitializeFromJson(pendingJson);
    }

    public void Initialize()
    {
        InitializeFromJson(null);
    }

    public void InitializeFromJson(string levelJson)
    {
        if (isInitialized)
            return;

        ResolveReferences();

        if (levelConstructor != null)
        {
            if (!string.IsNullOrWhiteSpace(levelJson))
                levelConstructor.ConstructLevelFromJsonString(levelJson);
            else
                levelConstructor.ConstructLevelFromJson();

            if (levelConstructor.SpawnPosition != null)
                spawnPosition = levelConstructor.SpawnPosition;
        }
        else
        {
            Debug.LogWarning("[PlaymodeManager] LevelConstructor is not found.", this);
        }

        if (spawnPosition == null)
            spawnPosition = FindFirstObjectByType<SpawnPosition>(FindObjectsInactive.Include);

        if (forceSpawnAtStart)
            playerController = null;

        EnsurePlayer();

        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.Initialize();
            Debug.Log("[PlaymodeManager] PlayerController initialized: " + playerController.name, this);
        }
        else
        {
            Debug.LogError("[PlaymodeManager] PlayerController is null. Player spawn failed or PlayerController is missing on spawned object.", this);
        }

        if (controlUI != null)
        {
            controlUI.Initialize(playerController);
            controlUI.SetControlEnable(true);
        }
        else
        {
            Debug.LogWarning("[PlaymodeManager] PlayerControlUI is not found.", this);
        }

        isInitialized = true;
    }

    private void EnsurePlayer()
    {
        if (playerController != null && playerController.gameObject.activeInHierarchy)
        {
            Debug.Log("[PlaymodeManager] Existing active player found: " + playerController.name, this);
            return;
        }

        if (spawnPosition == null)
        {
            Debug.LogError("[PlaymodeManager] SpawnPosition is null. Cannot spawn player.", this);
            return;
        }

        spawnPosition.Initialize();
        GameObject spawnedPlayer = spawnPosition.SpawnPlayer();
        if (spawnedPlayer == null)
        {
            Debug.LogError("[PlaymodeManager] SpawnPosition returned null. Player was not spawned.", this);
            return;
        }

        playerController = spawnedPlayer.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[PlaymodeManager] Spawned player does not have PlayerController component: " + spawnedPlayer.name, spawnedPlayer);
            return;
        }

        Debug.Log("[PlaymodeManager] Player spawned successfully: " + spawnedPlayer.name, spawnedPlayer);
    }

    private void ResolveReferences()
    {
        if (levelConstructor == null)
            levelConstructor = FindFirstObjectByType<LevelConstructor>(FindObjectsInactive.Include);

        if (spawnPosition == null)
            spawnPosition = FindFirstObjectByType<SpawnPosition>(FindObjectsInactive.Include);

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (controlUI == null)
            controlUI = FindFirstObjectByType<PlayerControlUI>(FindObjectsInactive.Include);
    }
}