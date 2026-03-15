using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelConstructor : MonoBehaviour
{
    private enum TileType
    {
        Wall = 0,
        Enemy = 1,
        Coin = 2,
        Start = 3,
        Goal = 4
    }

    [Serializable]
    private class TileRecord
    {
        public int x;
        public int y;
        public int type;
    }

    [Serializable]
    private class LevelTileData
    {
        public string levelName;
        public List<TileRecord> tiles = new List<TileRecord>();
    }

    [Header("Load Settings")]
    [SerializeField] private string fileName = "level_01_tiles.json";
    [SerializeField] private string inputFolder = "LevelData";

    [Header("References")]
    [SerializeField] private GridLayout gridLayout;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private SpawnPosition spawnPosition;

    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject startPrefab;
    [SerializeField] private Transform spawnedParent;

    [Header("Disable Edit Mode UI/Input In Playmode")]
    [SerializeField] private GameObject editModeRoot;
    [SerializeField] private bool loadJSONlocallyDebug = false;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    public SpawnPosition SpawnPosition => spawnPosition;

    public void ConstructLevelFromJson()
    {
        if (!Application.isPlaying)
            return;

        string path = GetFilePath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("[LevelConstructor] JSON file not found: " + path, this);
            return;
        }

        string json = File.ReadAllText(path);
        ConstructLevelFromJsonString(json);
    }

    public void ConstructLevelFromJsonString(string json)
    {
        if (!Application.isPlaying)
            return;

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[LevelConstructor] Input JSON is empty.", this);
            return;
        }

        DisableEditModeSystems();
        ResolveReferences();

        LevelTileData data = JsonUtility.FromJson<LevelTileData>(json);
        if (data == null || data.tiles == null)
        {
            Debug.LogError("[LevelConstructor] Failed to parse level JSON.", this);
            return;
        }

        ClearConstructedLevel();

        for (int i = 0; i < data.tiles.Count; i++)
        {
            TileRecord record = data.tiles[i];
            Vector3Int cell = new Vector3Int(record.x, record.y, 0);
            Vector3 worldPosition = GetCellCenterWorld(cell);

            switch ((TileType)record.type)
            {
                case TileType.Wall:
                    PlaceWall(cell);
                    break;
                case TileType.Enemy:
                    SpawnEnemy(worldPosition);
                    break;
                case TileType.Coin:
                    SpawnCoin(worldPosition);
                    break;
                case TileType.Start:
                    SetStart(worldPosition);
                    break;
                case TileType.Goal:
                    SpawnGoal(worldPosition);
                    break;
            }
        }

        Debug.Log("[LevelConstructor] Level constructed from JSON for play mode.", this);
    }

    public void ClearConstructedLevel()
    {
        if (tilemap != null)
            tilemap.ClearAllTiles();

        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
                Destroy(spawnedObjects[i]);
        }

        spawnedObjects.Clear();
    }

    private void DisableEditModeSystems()
    {
        if (editModeRoot != null)
            editModeRoot.SetActive(false);

        MapTileEditor[] editors = FindObjectsByType<MapTileEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < editors.Length; i++)
            editors[i].enabled = false;

        ToolboxController[] toolboxes = FindObjectsByType<ToolboxController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < toolboxes.Length; i++)
            toolboxes[i].enabled = false;
    }

    private void ResolveReferences()
    {
        if (gridLayout == null)
            gridLayout = FindFirstObjectByType<GridLayout>(FindObjectsInactive.Include);

        if (tilemap == null)
            tilemap = FindFirstObjectByType<Tilemap>(FindObjectsInactive.Include);

        if (spawnPosition == null)
            spawnPosition = FindFirstObjectByType<SpawnPosition>(FindObjectsInactive.Include);
    }

    private void PlaceWall(Vector3Int cell)
    {
        if (tilemap == null || wallTile == null)
            return;

        tilemap.SetTile(cell, wallTile);
    }

    private void SpawnEnemy(Vector3 worldPosition)
    {
        if (enemyPrefab == null)
            return;

        GameObject enemy = Instantiate(enemyPrefab, worldPosition, Quaternion.identity, spawnedParent);
        spawnedObjects.Add(enemy);

        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
            enemyController.Initialize();
    }

    private void SpawnCoin(Vector3 worldPosition)
    {
        if (coinPrefab == null)
            return;

        GameObject coin = Instantiate(coinPrefab, worldPosition, Quaternion.identity, spawnedParent);
        spawnedObjects.Add(coin);
    }

    private void SpawnGoal(Vector3 worldPosition)
    {
        if (goalPrefab == null)
            return;

        GameObject goal = Instantiate(goalPrefab, worldPosition, Quaternion.identity, spawnedParent);
        spawnedObjects.Add(goal);
    }

    private void SetStart(Vector3 worldPosition)
    {
        if (startPrefab != null)
        {
            GameObject startInstance = Instantiate(startPrefab, worldPosition, Quaternion.identity, spawnedParent);
            spawnedObjects.Add(startInstance);

            SpawnPosition prefabSpawnPosition = startInstance.GetComponent<SpawnPosition>();
            if (prefabSpawnPosition == null)
                prefabSpawnPosition = startInstance.GetComponentInChildren<SpawnPosition>();

            if (prefabSpawnPosition != null)
            {
                spawnPosition = prefabSpawnPosition;
                return;
            }

            Debug.LogWarning("[LevelConstructor] Start prefab has no SpawnPosition component.", startInstance);
        }

        if (spawnPosition != null)
            spawnPosition.transform.position = worldPosition;
    }

    private Vector3 GetCellCenterWorld(Vector3Int cell)
    {
        if (tilemap != null)
            return tilemap.GetCellCenterWorld(cell);

        if (gridLayout != null)
            return gridLayout.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

        return Vector3.zero;
    }

    private string GetFilePath()
    {
        string targetFileName = fileName;

        if (!loadJSONlocallyDebug && !string.IsNullOrWhiteSpace(Bootstrap.ActiveLevelFileName))
            targetFileName = Bootstrap.ActiveLevelFileName;

        return Path.Combine(Application.persistentDataPath, inputFolder, targetFileName);
    }
}
