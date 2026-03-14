using UnityEngine;

public class PlaymodeManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerControlUI controlUI;
    [SerializeField] private SpawnPosition spawnPosition;

    private bool isInitialized;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (isInitialized)
            return;

        ResolveReferences();
        EnsurePlayer();

        if (playerController != null)
        {
            playerController.Initialize();
            Debug.Log("[PlaymodeManager] PlayerController initialized: " + playerController.name, this);
        }
        else
        {
            Debug.LogError("[PlaymodeManager] PlayerController is null. Player spawn failed or PlayerController is missing on spawned object.", this);
        }

        if (controlUI != null)
            controlUI.Initialize(playerController);
        else
            Debug.LogWarning("[PlaymodeManager] PlayerControlUI is not found.", this);

        isInitialized = true;
    }

    private void EnsurePlayer()
    {
        if (playerController != null)
        {
            Debug.Log("[PlaymodeManager] Existing player found: " + playerController.name, this);
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
        if (spawnPosition == null)
            spawnPosition = FindFirstObjectByType<SpawnPosition>(FindObjectsInactive.Include);

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (controlUI == null)
            controlUI = FindFirstObjectByType<PlayerControlUI>(FindObjectsInactive.Include);
    }
}