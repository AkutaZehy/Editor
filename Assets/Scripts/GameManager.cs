using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // 初始化与引用部分
    [Header("Runtime Prefabs")]
    public GameObject runtimePlatformPrefab;
    public GameObject runtimePlayerPrefab;
    public GameObject runtimeGoalPrefab;

    [Header("Scene References")]
    public Transform runtimeLevelParent;

    [Header("UI References")]
    public Button playButton;
    public Button replayButton;
    public Button escapeButton;

    [Header("Option References")]
    public OptionManager optionManager;
    public Transform stackContainer;
    public GameObject stackItemPrefab;
    public Transform optionContainer;
    public GameObject optionItemPrefab;
    public Text winText;

    // 初始化数据

    private LevelData currentLevelData;
    private bool isGameInitialized = false;
    private int stackCount;

    private List<StackItem> stackItems = new();
    private StackItem selectedStack = null;
    private Dictionary<string, int> availableOptions = new Dictionary<string, int>();
    private List<OptionItem> optionItems = new();

    private Coroutine _runningOptionSequenceCoroutine;

    private PlayerController playerController;

    private Vector3 playerInitialPosition;
    private Quaternion playerInitialRotation;

    private bool hasWon = false;

    void Awake()
    {
        if (runtimeLevelParent == null)
        {
            GameObject parentObj = GameObject.Find("_RuntimeLevel");
            if (parentObj != null) runtimeLevelParent = parentObj.transform;
            if (runtimeLevelParent == null) runtimeLevelParent = new GameObject("_RuntimeLevel").transform;
        }
    }

    void Start()
    {
        if (escapeButton != null) escapeButton.onClick.AddListener(LoadEditorScene);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (replayButton != null) replayButton.onClick.AddListener(OnReplayClicked);

        if (winText != null) winText.gameObject.SetActive(false);
    }

    public Sprite GetOptionIcon(string optionType)
    {
        if (optionManager != null) return optionManager.GetIcon(optionType);
        Debug.LogError("GameManager: OptionManager 引用丢失，无法获取图标!");
        return null;
    }

    void InitStack()
    {
        foreach (Transform child in stackContainer)
            Destroy(child.gameObject);

        stackItems.Clear();

        for (int i = 0; i < stackCount; i++)
        {
            var obj = Instantiate(stackItemPrefab, stackContainer);
            var item = obj.GetComponent<StackItem>();
            item.Init(this);
            stackItems.Add(item);
        }
    }

    public void OnStackSelected(StackItem item)
    {
        if (selectedStack != null)
            selectedStack.SetHighlight(false);

        selectedStack = item;
        selectedStack.SetHighlight(true);
    }

    public bool AssignOptionToSelected(string option)
    {
        if (selectedStack != null && selectedStack.IsEmpty())
        {
            selectedStack.SetOption(option);
            selectedStack.SetHighlight(false);
            selectedStack = null;
            return true;
        }
        return false;
    }

    void InitOption()
    {
        foreach (Transform child in optionContainer)
            Destroy(child.gameObject);

        optionItems.Clear();

        foreach (var opt in availableOptions)
        {
            string optionType = opt.Key;
            int initialCount = opt.Value;

            if (string.IsNullOrEmpty(optionType) || initialCount <= 0)
            {
                Debug.LogWarning("GameManager: 字典中发现无效数据，跳过: Type=" + optionType + ", Count=" + initialCount);
                continue;
            }

            Sprite iconSprite = GetOptionIcon(optionType);

            GameObject obj = Instantiate(optionItemPrefab, optionContainer);

            var optionItem = obj.GetComponent<OptionItem>();

            if (optionItem == null)
            {
                Debug.LogError("GameManager: 实例化的 optionItemPrefab 没有找到 OptionItem 脚本!");
                Destroy(obj);
                continue;
            }

            optionItem.Init(this, optionType, initialCount, iconSprite);

            optionItems.Add(optionItem);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadEditorScene();
        }
    }

    void LoadEditorScene()
    {
        Debug.Log("返回编辑器场景...");
        SceneManager.LoadScene("EditorScene");
    }

    public void InitializeGame(LevelData levelData)
    {
        if (isGameInitialized)
        {
            Debug.LogWarning("GameManager 已经初始化过了!");
            return;
        }

        currentLevelData = levelData;
        Debug.Log("GameManager 收到 LevelData，开始初始化游戏和构建场景...");

        OnReplayClicked();

        isGameInitialized = true;
    }

    void ClearLevel()
    {
        if (runtimeLevelParent != null)
        {
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in runtimeLevelParent)
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
        }
    }

    void BuildLevel()
    {
        if (currentLevelData == null)
        {
            Debug.LogError("GameManager: 没有加载的关卡数据，无法构建场景!");
            return;
        }

        if (runtimeLevelParent == null)
        {
            Debug.LogError("GameManager: 没有 RuntimeLevelParent，无法构建场景!");
            return;
        }

        Debug.Log("GameManager: 开始构建关卡场景...");

        // Player初始化
        playerController = null;
        playerInitialPosition = Vector3.zero;
        playerInitialRotation = Quaternion.identity;

        // Map 初始化
        foreach (var elementData in currentLevelData.elements)
        {
            GameObject prefabToInstantiate = null;
            // 根据元素类型选择对应的运行时预制体
            switch (elementData.type)
            {
                case ElementType.Platform: prefabToInstantiate = runtimePlatformPrefab; break;
                case ElementType.PlayerStart: prefabToInstantiate = runtimePlayerPrefab; break;
                case ElementType.Goal: prefabToInstantiate = runtimeGoalPrefab; break;
                    // TODO: 未来添加更多类型
            }

            if (prefabToInstantiate != null)
            {
                GameObject newElement = Instantiate(prefabToInstantiate, elementData.position, Quaternion.identity);
                newElement.transform.SetParent(runtimeLevelParent);

                // 获取 PlayerController 引用并保存初始状态
                if (elementData.type == ElementType.PlayerStart)
                {
                    playerController = newElement.GetComponent<PlayerController>();
                    if (playerController == null)
                    {
                        Debug.LogError("构建场景时找不到 PlayerController 在 PlayerPrefab 上! 请检查 Prefab.");
                    }
                    else
                    {
                        // 保存玩家的初始位置和旋转
                        playerInitialPosition = newElement.transform.position;
                        playerInitialRotation = newElement.transform.rotation;
                    }
                }
                if (elementData.type == ElementType.Goal)
                {
                    FlagTrigger flag = newElement.GetComponent<FlagTrigger>(); // 获取 FlagTrigger 组件
                    if (flag != null)
                    {
                        flag.gm = this; // 将当前的 GameManager 实例赋值给 FlagTrigger 的 gm 变量
                        Debug.Log("构建场景时，已将 GameManager 引用赋值给 Goal 上的 FlagTrigger.");
                    }
                    else
                    {
                        Debug.LogError("构建场景时，Goal 对象没有找到 FlagTrigger 脚本! 请检查 GoalPrefab.");
                    }
                }
            }
            else
            {
                Debug.LogError("GameManager: 构建场景时未找到对应 " + elementData.type + " 的运行时预制体! 请检查 GameManager Inspector.");
            }
        }
        Debug.Log("GameManager: 关卡场景构建完成.");

        // Stack初始化
        stackCount = currentLevelData.maxOptionStackSize;
        InitStack();

        // Option初始化
        if (availableOptions.Count == 0 || currentLevelData.availableOptions == null)
        {
            foreach (var opt in currentLevelData.availableOptions)
            {
                if (string.IsNullOrEmpty(opt.optionType))
                {
                    Debug.LogWarning($"Invalid option data found in LevelData: Type='{opt.optionType}', Count={opt.initialCount}. Skipping.");
                    continue;
                }

                if (opt.initialCount <= 0)
                {
                    Debug.LogWarning($"Invalid option data found in LevelData: Type='{opt.optionType}', Count={opt.initialCount}. Skipping.");
                    continue;
                }

                if (!availableOptions.ContainsKey(opt.optionType)) availableOptions.Add(opt.optionType, opt.initialCount);

                else Debug.LogWarning($"Duplicate option type '{opt.optionType}' found in LevelData. Skipping duplicate.");
            }
        }
        InitOption();

        if (playerController == null)
        {
            Debug.LogError("GameManager: 关卡构建完成，但没有找到玩家对象 (PlayerController)! 游戏可能无法进行.");
        }
    }

    void OnPlayClicked()
    {
        Debug.Log("Play Button Clicked");

        if (_runningOptionSequenceCoroutine != null)
        {
            StopCoroutine(_runningOptionSequenceCoroutine);
            _runningOptionSequenceCoroutine = null; // 清空引用
            Debug.Log("已停止 GameManager 上正在运行的指令序列协程.");
        }

        hasWon = false;
        if (winText.text == "BRUHHHHHHHHHHHHHHHHHHHH") winText.gameObject.SetActive(false);

        if (playerController != null)
        {
            playerController.StopAllPlayerCoroutines();
            playerController.ResetTo(playerInitialPosition, playerInitialRotation);

            List<string> options = new();
            foreach (var stack in stackItems) options.Add(stack.GetOption());

            _runningOptionSequenceCoroutine = StartCoroutine(ExecuteWithHighlight(options));
        }
        else
        {
            Debug.LogError("GameManager: 玩家控制器引用丢失，无法启动游戏!");
        }

    }

    IEnumerator ExecuteWithHighlight(List<string> options)
    {
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < options.Count; i++)
        {
            if (i < stackItems.Count && stackItems[i] != null)
            {
                stackItems[i].SetHighlight(true); // 高亮
            }

            if (playerController != null)
            {
                Coroutine playerActionCoroutine = playerController.ExecuteSingleOption(options[i]);
                yield return playerActionCoroutine;
            }
            else
            {
                Debug.LogError("GameManager: PlayerController 引用丢失，无法执行指令!");

                yield return null;
            }

            if (i < stackItems.Count && stackItems[i] != null)
            {
                stackItems[i].SetHighlight(false);
            }
        }

        // 指令序列执行完毕后，检查胜利条件
        CheckWinCondition();
    }

    void OnReplayClicked()
    {
        ClearLevel();
        BuildLevel();
        winText.gameObject.SetActive(false);
        hasWon = false;

        if (playerController != null)
        {
            playerController.ResetTo(playerInitialPosition, playerInitialRotation);

            playerController.isControlledByPlayerInput = false;
        }
        else
        {
            Debug.LogError("GameManager: 玩家控制器引用丢失，无法完成重置!");
        }
    }

    public void OnPlayerWin()
    {
        if (!hasWon)
        {
            hasWon = true;
        }
    }

    public void OnOhWait()
    {
        if (hasWon)
        {
            hasWon = false;
        }
    }

    void CheckWinCondition()
    {
        if (hasWon)
        {
            winText.text = "okfine";
            winText.gameObject.SetActive(true);
        }
        else
        {
            winText.text = "BRUHHHHHHHHHHHHHHHHHHHH";
            winText.gameObject.SetActive(true);
        }
    }
}