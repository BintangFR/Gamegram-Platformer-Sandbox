using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JSONManager : MonoBehaviour
{
    [Header("Mock Level Settings")]
    [SerializeField] private string fileName = "mock_level_01.json";
    [SerializeField] private int levelWidth = 16;
    [SerializeField] private int levelHeight = 9;
    [SerializeField] private string outputFolder = "LevelData";

    [Header("UI Buttons")]
    [SerializeField] private Button generateJsonButton;
    [SerializeField] private Button loadJsonButton;

    [SerializeField] private TextMeshProUGUI statusText;
    [Serializable]
    public class TileData
    {
        public int x;
        public int y;
        public string tileId;
        public bool isSolid;
    }

    [Serializable]
    public class LevelData
    {
        public string levelName;
        public int width;
        public int height;
        public List<TileData> tiles = new List<TileData>();
    }

    private void Start()
    {
        if (generateJsonButton != null)
            generateJsonButton.onClick.AddListener(GenerateMockLevelJson);

        if (loadJsonButton != null)
            loadJsonButton.onClick.AddListener(LoadLevelJson);
    }

    private void OnDestroy()
    {
        if (generateJsonButton != null)
            generateJsonButton.onClick.RemoveListener(GenerateMockLevelJson);

        if (loadJsonButton != null)
            loadJsonButton.onClick.RemoveListener(LoadLevelJson);
    }

    [ContextMenu("Generate Mock Level JSON")]
    public void GenerateMockLevelJson()
    {
        LevelData levelData = BuildMockLevelData();
        string json = JsonUtility.ToJson(levelData, true);

        string path = GetFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);

        Debug.Log("Mock level JSON generated at: " + path);
    }

    [ContextMenu("Load Level JSON")]
    public void LoadLevelJson()
    {
        string path = GetFilePath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("JSON file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        LevelData levelData = JsonUtility.FromJson<LevelData>(json);

        if (levelData == null)
        {
            Debug.LogError("Failed to parse level JSON.");
            return;
        }

        Debug.Log("Loaded level: " + levelData.levelName + " | Size: " + levelData.width + "x" + levelData.height + " | Tiles: " + levelData.tiles.Count);
    }

    private LevelData BuildMockLevelData()
    {
        LevelData data = new LevelData
        {
            levelName = "Mock_Level_01",
            width = levelWidth,
            height = levelHeight
        };

        for (int y = 0; y < levelHeight; y++)
        {
            for (int x = 0; x < levelWidth; x++)
            {
                bool isGround = y == 0;
                bool isPlatform = y == 3 && x % 4 == 0;
                bool isWall = (x == 0 || x == levelWidth - 1) && y < 4;

                if (!isGround && !isPlatform && !isWall)
                    continue;

                TileData tile = new TileData
                {
                    x = x,
                    y = y,
                    tileId = isGround ? "Ground" : isWall ? "Wall" : "Platform",
                    isSolid = true
                };

                data.tiles.Add(tile);
            }
        }

        return data;
    }

    private string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, outputFolder, fileName);
    }
}
