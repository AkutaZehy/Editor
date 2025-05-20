using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    // public PlayerController player;
    public Button playButton;
    public Button replayButton;
    public Button escapeButton;
    public Transform stackContainer;
    public GameObject stackItemPrefab;
    public Transform optionContainer;
    // 由于指令选项类型可能多种，这里暂时只留一个泛型的 GameObject 引用，或未来用列表
    public GameObject optionItemPrefab; // 通用指令选项预制体 (需要修改旧 OptionRightItem 或创建新通用脚本)

    public Text winText; // 胜利文本 (使用旧 UI Text 或 TMP_Text)

    // 用于存储从 LevelLoader 加载的关卡游戏设置
    private LevelData currentLevelData;


    void Awake()
    {
        // GameManager 可能会被 LevelLoader 在 Awake 中查找并引用，但初始化逻辑在 InitializeGame 中
    }

    void Start()
    {
        if (escapeButton != null) escapeButton.onClick.AddListener(LoadEditorScene);

        if (winText != null) winText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LoadEditorScene();
    }

    void LoadEditorScene()
    {
        Debug.Log("按下 Esc，返回编辑器场景...");
        SceneManager.LoadScene("EditorScene"); // 确保 EditorScene 已添加到 Build Settings
    }

    // 由 LevelLoader 在加载关卡数据后调用，用于初始化游戏状态
    public void InitializeGame(LevelData levelData)
    {
        currentLevelData = levelData;
        Debug.Log("GameManager 收到 LevelData，开始初始化游戏...");
        Debug.Log("关卡名称: " + currentLevelData.levelName);
        Debug.Log("指令栈大小: " + currentLevelData.maxCommandStackSize);
        Debug.Log("可用指令数量: " + currentLevelData.availableCommands.Count);
    }
}
