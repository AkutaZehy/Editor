using UnityEngine;
using System.IO;

public class LevelLoader : MonoBehaviour
{
    [Header("Runtime Prefabs")]
    public GameObject runtimePlatformPrefab;
    public GameObject runtimePlayerPrefab;
    public GameObject runtimeGoalPrefab;

    [Header("Loading Settings")]
    private string saveFileName = "New Level";
    private string saveFolder = "Levels/";

    public Transform runtimeLevelParent;

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


    void Awake() // 使用 Awake 确保加载在 Start 之前完成
    {
        // 在 Awake 中查找 _RuntimeLevel 父级
        if (runtimeLevelParent == null)
        {
            GameObject parentObj = GameObject.Find("_RuntimeLevel");
            if (parentObj != null)
            {
                runtimeLevelParent = parentObj.transform;
            }
            if (runtimeLevelParent == null)
            {
                runtimeLevelParent = new GameObject("_RuntimeLevel").transform;
            }
        }

        LoadLevelForRuntime();
    }

    void LoadLevelForRuntime()
    {
        string fullPath = GetLoadFilePath(saveFileName);

        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            Debug.LogError("运行时加载失败: 文件不存在或路径无效 - " + fullPath);
            loadedLevelData = null;
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(fullPath);
            loadedLevelData = JsonUtility.FromJson<LevelData>(jsonString);

            // 实例化加载的元素到游戏场景
            if (runtimeLevelParent != null && loadedLevelData != null)
            {
                // ClearRuntimeLevel(); // TODO: 未来实现此方法

                foreach (var elementData in loadedLevelData.elements)
                {
                    GameObject prefabToInstantiate = null;
                    switch (elementData.type)
                    {
                        case ElementType.Platform: prefabToInstantiate = runtimePlatformPrefab; break;
                        case ElementType.PlayerStart: prefabToInstantiate = runtimePlayerPrefab; break;
                        case ElementType.Goal: prefabToInstantiate = runtimeGoalPrefab; break;
                    }

                    if (prefabToInstantiate != null)
                    {
                        GameObject newElement = Instantiate(prefabToInstantiate, elementData.position, Quaternion.identity);
                        newElement.transform.SetParent(runtimeLevelParent);
                    }
                    else
                    {
                        Debug.LogError("运行时加载时未找到对应 " + elementData.type + " 的运行时预制体! 请检查 LevelLoader Inspector.");
                    }
                }
            }

            Debug.Log("运行时关卡加载成功从: " + fullPath);
            if (loadedLevelData != null)
            {
                Debug.Log("关卡名称: " + loadedLevelData.levelName);
                Debug.Log("指令栈大小: " + loadedLevelData.maxCommandStackSize);
                // Debug.Log("可用指令数量: " + loadedLevelData.availableCommands.Count);
                foreach (var cmd in loadedLevelData.availableCommands) { Debug.Log("- " + cmd.commandType + " x" + cmd.initialCount); }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("运行时关卡加载失败: " + e.Message);
            loadedLevelData = null;
        }
    }
}