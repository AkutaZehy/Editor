using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class LevelEditorManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject platformPrefab;
    public GameObject playerPrefab;
    public GameObject goalPrefab;

    [Header("Grid Settings")]
    public float gridSize = 1.0f;

    [Header("Editor State")]
    public ElementType selectedElementType = ElementType.None;
    public Transform levelElementsParent;

    [Header("Save/Load Settings")]
    public TMPro.TMP_InputField levelNameInputField;
    private string saveFolder = "Levels/";
    private string tempTestFileName = "New Level";

    [Header("Game Settings for Level")]
    public int editorMaxOptionStackSize = 5;
    public List<AvailableOption> editorAvailableOptions = new List<AvailableOption>();

    void Start()
    {
        if (levelElementsParent == null)
        {
            GameObject parentObj = GameObject.Find("_LevelElements");
            if (parentObj != null) levelElementsParent = parentObj.transform;
            else
            {
                Debug.LogError("找不到名为 '_LevelElements' 的 GameObject! 请在 Hierarchy 中创建并赋值给 LevelEditorManager.");
                // enabled = false;
            }
        }

        if (levelNameInputField != null && string.IsNullOrEmpty(levelNameInputField.text)) levelNameInputField.text = "New Level";
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) PlaceSelectedElement();
        else if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject()) DeleteElementAtMouse();

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)) SaveLevel();
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)) LoadLevel();
        if (Input.GetKeyDown(KeyCode.T)) TestLevel();
    }

    // ==================== UI 调用的包装方法 ====================

    public void SelectPlatformType() { SetSelectedElementType(ElementType.Platform); }
    public void SelectPlayerType() { SetSelectedElementType(ElementType.PlayerStart); }
    public void SelectGoalType() { SetSelectedElementType(ElementType.Goal); }

    // ==================== 内部逻辑方法 ====================

    public void SetSelectedElementType(ElementType type)
    {
        selectedElementType = type;
    }

    void PlaceSelectedElement()
    {
        if (selectedElementType == ElementType.None)
        {
            Debug.LogWarning("没有选择要放置的元素类型!");
            return;
        }
        if (Camera.main == null)
        {
            Debug.LogError("PlaceSelectedElement: Camera.main is null!");
            return;
        }

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3 snappedPos = new Vector3(
            Mathf.Round(mouseWorldPos.x / gridSize) * gridSize,
            Mathf.Round(mouseWorldPos.y / gridSize) * gridSize,
            0
        );

        GameObject prefabToInstantiate = null;
        switch (selectedElementType)
        {
            case ElementType.Platform: prefabToInstantiate = platformPrefab; break;
            case ElementType.PlayerStart: prefabToInstantiate = playerPrefab; break;
            case ElementType.Goal: prefabToInstantiate = goalPrefab; break;
        }

        if (prefabToInstantiate == null)
        {
            Debug.LogError("预制体引用丢失! 请检查 LevelEditorManager Inspector 中 " + selectedElementType + " 对应的 Prefab 槽位是否已赋值.");
            return;
        }

        GameObject newElement = Instantiate(prefabToInstantiate, snappedPos, Quaternion.identity);
        if (levelElementsParent != null)
        {
            newElement.transform.SetParent(levelElementsParent);
        }
    }

    void DeleteElementAtMouse()
    {
        if (Camera.main == null)
        {
            Debug.LogError("DeleteElementAtMouse: Camera.main is null!");
            return;
        }

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;

        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);

        if (hitCollider != null)
        {
            GameObject hitObject = hitCollider.gameObject;
            LevelElement element = hitObject.GetComponent<LevelElement>();
            if (element != null)
            {
                if (hitObject.transform.parent == levelElementsParent)
                {
                    Debug.Log("删除了 " + hitObject.name + " 在 " + hitObject.transform.position);
                    Destroy(hitObject);
                }
                else
                {
                    Debug.Log("点击了不在 '_LevelElements' 父级下的对象: " + hitObject.name);
                }
            }
        }
    }

    // ==================== 保存/加载逻辑 ====================

    // 获取保存文件的完整路径 (使用输入框的文本作为文件名)
    private string GetLevelFilePath(string levelName)
    {
        if (levelNameInputField == null)
        {
            Debug.LogError("GetLevelFilePath: Level Name Input Field 引用未设置!");
            return null;
        }

        string fileName = levelName;

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "default_level";
            Debug.LogWarning("关卡名称为空，将使用默认文件名: " + fileName);
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

    public void SaveLevel()
    {
        if (levelElementsParent == null)
        {
            Debug.LogError("无法保存关卡，找不到 _LevelElements 父级对象!");
            return;
        }
        if (levelNameInputField == null)
        {
            Debug.LogError("无法保存关卡，未设置 Level Name Input Field 引用!");
            return;
        }

        string currentLevelName = levelNameInputField.text;
        if (string.IsNullOrEmpty(currentLevelName))
        {
            Debug.LogError("无法保存关卡，关卡名称不能为空!");
            return;
        }

        LevelData levelData = new LevelData();
        levelData.levelName = currentLevelName;

        levelData.elements = new List<ElementData>();
        foreach (Transform child in levelElementsParent)
        {
            LevelElement element = child.GetComponent<LevelElement>();
            if (element != null)
            {
                ElementData elementData = new ElementData
                {
                    type = element.type,
                    position = child.position
                    // TODO: 未来在这里收集更多属性
                };
                levelData.elements.Add(elementData);
            }
        }

        levelData.maxOptionStackSize = editorMaxOptionStackSize;
        levelData.availableOptions = new List<AvailableOption>(editorAvailableOptions);

        string jsonString = JsonUtility.ToJson(levelData, true);

        string fullPath = GetLevelFilePath(currentLevelName);
        if (string.IsNullOrEmpty(fullPath)) return;

        string directoryPath = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        try
        {
            File.WriteAllText(fullPath, jsonString);
            Debug.Log("关卡保存成功到: " + fullPath);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

        }
        catch (System.Exception e)
        {
            Debug.LogError("关卡保存失败: " + e.Message);
        }
    }

    public void LoadLevel()
    {
        if (levelNameInputField == null)
        {
            Debug.LogError("无法加载关卡，未设置 Level Name Input Field 引用!");
            return;
        }

        string levelNameToLoad = levelNameInputField.text;
        if (string.IsNullOrEmpty(levelNameToLoad))
        {
            Debug.LogError("无法加载关卡，请在输入框中指定要加载的关卡名称!");
            return;
        }

        string fullPath = GetLevelFilePath(levelNameToLoad);
        if (string.IsNullOrEmpty(fullPath)) return;


        if (!File.Exists(fullPath))
        {
            Debug.LogError("加载失败: 文件不存在 - " + fullPath);
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(fullPath);

            LevelData levelData = JsonUtility.FromJson<LevelData>(jsonString);

            ClearCurrentLevel();

            if (levelElementsParent != null)
            {
                foreach (var elementData in levelData.elements)
                {
                    GameObject prefabToInstantiate = null;
                    switch (elementData.type)
                    {
                        case ElementType.Platform: prefabToInstantiate = platformPrefab; break;
                        case ElementType.PlayerStart: prefabToInstantiate = playerPrefab; break;
                        case ElementType.Goal: prefabToInstantiate = goalPrefab; break;
                            // TODO: 未来添加更多类型
                    }

                    if (prefabToInstantiate != null)
                    {
                        GameObject newElement = Instantiate(prefabToInstantiate, elementData.position, Quaternion.identity);
                        newElement.transform.SetParent(levelElementsParent);
                    }
                    else
                    {
                        Debug.LogError("加载时未找到对应 " + elementData.type + " 的预制体! 请检查 LevelEditorManager Inspector.");
                    }
                }
            }

            editorMaxOptionStackSize = levelData.maxOptionStackSize;
            editorAvailableOptions = new List<AvailableOption>(levelData.availableOptions);

            levelNameInputField.text = levelData.levelName;

            Debug.Log("关卡加载成功从: " + fullPath);
            Debug.Log("加载的关卡名称: " + levelData.levelName);
            Debug.Log("加载的指令栈大小: " + levelData.maxOptionStackSize);
            Debug.Log("加载的可用指令数量: " + levelData.availableOptions.Count);

        }
        catch (System.Exception e)
        {
            Debug.LogError("关卡加载失败: " + e.Message);
        }
    }

    void ClearCurrentLevel()
    {
        if (levelElementsParent != null)
        {
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in levelElementsParent)
            {
                childrenToDestroy.Add(child.gameObject);
            }

            foreach (GameObject child in childrenToDestroy)
            {
                if (child != null)
                {
                    Destroy(child);
                }
            }
            Debug.Log("已清空当前编辑器场景中的关卡元素 (Destroy called).");
        }
    }

    public void TestLevel()
    {
        if (levelNameInputField == null)
        {
            Debug.LogError("无法测试关卡，未设置 Level Name Input Field 引用!");
            return;
        }
        string currentLevelName = levelNameInputField.text;
        if (string.IsNullOrEmpty(currentLevelName))
        {
            Debug.LogError("无法测试关卡，关卡名称不能为空!");
            return;
        }
        string fullPathForTempSave = GetLevelFilePath(tempTestFileName);
        if (string.IsNullOrEmpty(fullPathForTempSave)) return;

        // 1. 先将当前编辑器中的关卡布局和设置数据保存到临时文件
        LevelData levelData = new LevelData();
        levelData.levelName = currentLevelName;

        levelData.elements = new List<ElementData>();
        foreach (Transform child in levelElementsParent)
        {
            LevelElement element = child.GetComponent<LevelElement>();
            if (element != null)
            {
                ElementData elementData = new ElementData
                {
                    type = element.type,
                    position = child.position
                    // TODO: 未来在这里收集更多属性
                };
                levelData.elements.Add(elementData);
            }
        }

        levelData.maxOptionStackSize = editorMaxOptionStackSize;
        levelData.availableOptions = new List<AvailableOption>(editorAvailableOptions);

        string jsonString = JsonUtility.ToJson(levelData, true);

        string directoryPath = Path.GetDirectoryName(fullPathForTempSave);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        try
        {
            File.WriteAllText(fullPathForTempSave, jsonString);
            Debug.Log("关卡临时保存成功到: " + fullPathForTempSave + "，用于测试.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("关卡临时保存失败: " + e.Message);
            return;
        }

        // 2. 加载游戏场景
        string gameSceneName = "GameScene";
        bool sceneFoundInBuildSettings = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == gameSceneName)
            {
                sceneFoundInBuildSettings = true;
                break;
            }
        }

        if (sceneFoundInBuildSettings)
        {
            Debug.Log("正在加载游戏场景: " + gameSceneName + "...");
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("游戏场景 '" + gameSceneName + "' 未添加到 Build Settings! 无法测试关卡.");
            Debug.Log("请打开 File -> Build Settings... 并将 '" + gameSceneName + ".unity' 场景文件拖拽到 'Scenes In Build' 列表中.");
        }
    }
}