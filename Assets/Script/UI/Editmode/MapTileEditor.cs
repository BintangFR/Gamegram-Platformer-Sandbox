using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class MapTileEditor : MonoBehaviour
{
    public enum TileType
    {
        Wall = 0,
        Enemy = 1,
        Coin = 2,
        Start = 3,
        Goal = 4
    }

    [Serializable]
    public class TileRecord
    {
        public int x;
        public int y;
        public int type;
    }

    [Serializable]
    public class LevelTileData
    {
        public string levelName;
        public List<TileRecord> tiles = new List<TileRecord>();
    }

    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GridLayout gridLayout;
    [SerializeField] private Tilemap tilemap;

    [Header("Tile Assets (5 Types)")]
    [SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase enemyTile;
    [SerializeField] private TileBase coinTile;
    [SerializeField] private TileBase startTile;
    [SerializeField] private TileBase goalTile;

    [Header("Placement Limits")]
    [SerializeField] private int maxStartTiles = 1;
    [SerializeField] private int maxCoinTiles = 3;
    [SerializeField] private int maxGoalTiles = 1;

    [Header("Save Settings")]
    [SerializeField] private string levelName = "Level_01";
    [SerializeField] private string fileName = "level_01_tiles.json";
    [SerializeField] private string outputFolder = "LevelData";

    [Header("Editor State")]
    [SerializeField] private TileType selectedTileType = TileType.Wall;
    [SerializeField] private bool isEraserSelected;

    private readonly Dictionary<TileType, TileBase> tileByType = new Dictionary<TileType, TileBase>();
    private readonly Dictionary<TileBase, TileType> typeByTile = new Dictionary<TileBase, TileType>();

    private int startTileCount;
    private int coinTileCount;
    private int goalTileCount;

    public event Action<int, int, int> PlacementRemainingChanged;

    public TileType SelectedTileType => selectedTileType;
    public bool IsEraserSelected => isEraserSelected;

    public int RemainingStart => Mathf.Max(0, maxStartTiles - startTileCount);
    public int RemainingCoin => Mathf.Max(0, maxCoinTiles - coinTileCount);
    public int RemainingGoal => Mathf.Max(0, maxGoalTiles - goalTileCount);

    public bool CanPlaceStart => RemainingStart > 0;
    public bool CanPlaceCoin => RemainingCoin > 0;
    public bool CanPlaceGoal => RemainingGoal > 0;

    public int StartTileCount => startTileCount;
    public int GoalTileCount => goalTileCount;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (gridLayout == null)
            gridLayout = FindFirstObjectByType<GridLayout>(FindObjectsInactive.Include);

        if (tilemap == null)
            tilemap = FindFirstObjectByType<Tilemap>(FindObjectsInactive.Include);

        RebuildTileLookups();
        RefreshPlacementCountsAndNotify();
    }

    private void OnValidate()
    {
        RebuildTileLookups();
        maxStartTiles = Mathf.Max(0, maxStartTiles);
        maxCoinTiles = Mathf.Max(0, maxCoinTiles);
        maxGoalTiles = Mathf.Max(0, maxGoalTiles);
    }

    private void Update()
    {
        if (tilemap == null || worldCamera == null || Mouse.current == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        bool leftHeld = Mouse.current.leftButton.isPressed;
        bool rightHeld = Mouse.current.rightButton.isPressed;

        if (!leftHeld && !rightHeld)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Right hold = continuous erase drag
        if (rightHeld)
        {
            EraseFromScreen(mousePosition);
            return;
        }

        // Left hold = continuous paint drag (or erase if eraser tool is selected)
        if (isEraserSelected)
            EraseFromScreen(mousePosition);
        else
            PaintFromScreen(mousePosition);
    }

    public void SetSelectedTileType(int typeIndex)
    {
        if (!Enum.IsDefined(typeof(TileType), typeIndex))
            return;

        selectedTileType = (TileType)typeIndex;
        isEraserSelected = false;
    }

    public void SetSelectedTileType(TileType type)
    {
        selectedTileType = type;
        isEraserSelected = false;
    }

    public void SelectEraser()
    {
        isEraserSelected = true;
    }

    [ContextMenu("Save Map To JSON")]
    public void SaveMapToJson()
    {
        if (tilemap == null)
            return;

        LevelTileData data = new LevelTileData
        {
            levelName = levelName
        };

        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);
            if (tile == null)
                continue;

            if (!typeByTile.TryGetValue(tile, out TileType type))
                continue;

            TileRecord record = new TileRecord
            {
                x = cell.x,
                y = cell.y,
                type = (int)type
            };

            data.tiles.Add(record);
        }

        string json = JsonUtility.ToJson(data, true);
        string path = GetFilePath();

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);

        Debug.Log("[MapTileEditor] Saved map JSON: " + path, this);
    }

    [ContextMenu("Load Map From JSON")]
    public void LoadMapFromJson()
    {
        if (tilemap == null)
            return;

        string path = GetFilePath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("[MapTileEditor] JSON file not found: " + path, this);
            return;
        }

        string json = File.ReadAllText(path);
        LevelTileData data = JsonUtility.FromJson<LevelTileData>(json);
        if (data == null)
        {
            Debug.LogError("[MapTileEditor] Failed to parse map JSON.", this);
            return;
        }

        tilemap.ClearAllTiles();

        for (int i = 0; i < data.tiles.Count; i++)
        {
            TileRecord record = data.tiles[i];
            TileType type = (TileType)record.type;

            if (!tileByType.TryGetValue(type, out TileBase tile) || tile == null)
                continue;

            tilemap.SetTile(new Vector3Int(record.x, record.y, 0), tile);
        }

        RefreshPlacementCountsAndNotify();
        Debug.Log("[MapTileEditor] Loaded map JSON: " + path + " | Tile Count: " + data.tiles.Count, this);
    }

    private void PaintFromScreen(Vector2 screenPosition)
    {
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
        Vector3Int cell = gridLayout.WorldToCell(world);

        if (!tileByType.TryGetValue(selectedTileType, out TileBase tile) || tile == null)
            return;

        TileBase currentTile = tilemap.GetTile(cell);
        if (currentTile == tile)
            return;

        if (selectedTileType == TileType.Start && !CanPlaceStart && currentTile != startTile)
            return;

        if (selectedTileType == TileType.Coin && !CanPlaceCoin && currentTile != coinTile)
            return;

        if (selectedTileType == TileType.Goal && !CanPlaceGoal && currentTile != goalTile)
            return;

        tilemap.SetTile(cell, tile);
        RefreshPlacementCountsAndNotify();
    }

    private void EraseFromScreen(Vector2 screenPosition)
    {
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
        Vector3Int cell = gridLayout.WorldToCell(world);

        if (tilemap.GetTile(cell) == null)
            return;

        tilemap.SetTile(cell, null);
        RefreshPlacementCountsAndNotify();
    }

    public string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, outputFolder, fileName);
    }

    private void RebuildTileLookups()
    {
        tileByType.Clear();
        typeByTile.Clear();

        AddTileMapping(TileType.Wall, wallTile);
        AddTileMapping(TileType.Enemy, enemyTile);
        AddTileMapping(TileType.Coin, coinTile);
        AddTileMapping(TileType.Start, startTile);
        AddTileMapping(TileType.Goal, goalTile);
    }

    private void AddTileMapping(TileType type, TileBase tile)
    {
        tileByType[type] = tile;

        if (tile != null && !typeByTile.ContainsKey(tile))
            typeByTile.Add(tile, type);
    }

    private void RefreshPlacementCountsAndNotify()
    {
        startTileCount = 0;
        coinTileCount = 0;
        goalTileCount = 0;

        if (tilemap != null)
        {
            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                TileBase tile = tilemap.GetTile(cell);
                if (tile == null)
                    continue;

                if (tile == startTile)
                    startTileCount++;
                else if (tile == coinTile)
                    coinTileCount++;
                else if (tile == goalTile)
                    goalTileCount++;
            }
        }

        PlacementRemainingChanged?.Invoke(RemainingStart, RemainingCoin, RemainingGoal);
    }
}
