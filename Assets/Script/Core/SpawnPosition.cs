using UnityEngine;

public class SpawnPosition : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnParent;

    private GameObject spawnedPlayer;
    private bool isInitialized;

    public void Initialize()
    {
        isInitialized = true;
    }

    [ContextMenu("Spawn Player")]
    public GameObject SpawnPlayer()
    {
        if (!isInitialized)
            Initialize();

        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnPosition] Player Prefab is not assigned.", this);
            return null;
        }

        if (spawnedPlayer != null)
            return spawnedPlayer;

        spawnedPlayer = Instantiate(playerPrefab, transform.position, transform.rotation, spawnParent);
        Debug.Log("[SpawnPosition] Spawned player: " + spawnedPlayer.name, this);
        return spawnedPlayer;
    }
}
