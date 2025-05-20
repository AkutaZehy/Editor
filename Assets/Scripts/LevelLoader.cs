using UnityEngine;
using System.IO;

public class LevelLoader : MonoBehaviour
{
    [Header("Loading Settings")]
    private string saveFileName = "New Level";
    private string saveFolder = "Levels/";

    public LevelData loadedLevelData;
    public GameManager gameManager;

    private string GetLoadFilePath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "default_level";
            Debug.LogWarning("文件名为空，将使用默认文件名: " + fileName);
        }
        fileName = SanitizeFileName(fileName);

        return Path.Combine(Application.dataPath, saveFolder, fileName + ".json");
    }

    private string SanitizeFileName(string fileName)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("LevelLoader: 找不到场景中的 GameManager!");
            return;
        }

        LoadLevelData();

        if (loadedLevelData != null)
        {
            gameManager.InitializeGame(loadedLevelData);
        }
        else
        {
            Debug.LogError("LevelLoader: 关卡数据加载失败，无法初始化 GameManager.");
        }
    }

    void LoadLevelData()
    {
        string fullPath = GetLoadFilePath(saveFileName);

        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            Debug.LogError("加载失败: 文件不存在或路径无效 - " + fullPath);
            loadedLevelData = null;
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(fullPath);
            loadedLevelData = JsonUtility.FromJson<LevelData>(jsonString);

            string info = "";

            if (loadedLevelData != null)
            {
                info += "关卡数据加载成功从: " + fullPath + "\n";
                info += "关卡名称: " + loadedLevelData.levelName + "\n";
                info += "指令栈大小: " + loadedLevelData.maxOptionStackSize + "\n";
                info += (loadedLevelData.availableOptions != null)
                ? "可用指令数量: " + loadedLevelData.availableOptions.Count
                : "可用指令列表为空.";
            }
            Debug.Log(info);
        }
        catch (System.Exception e)
        {
            Debug.LogError("关卡数据加载失败: " + e.Message);
            loadedLevelData = null;
        }
    }
}