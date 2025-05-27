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
    public Button editorButton;

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
    private StackItem _lastClickedStackItem = null;
    private string _selectedOptionTypeToAssign = "";
    private Dictionary<string, int> _availableOptionsCount = new();
    private List<OptionItem> _optionItemsUI = new();

    private OptionItem _highlightedOptionItem = null;
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
        // if (escapeButton != null) escapeButton.onClick.AddListener(LoadEditorScene);
        if (editorButton != null) editorButton.onClick.AddListener(LoadEditorScene);
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

    public void OnOptionSelected(string optionType)
    {
        if (_lastClickedStackItem != null)
        {
            _lastClickedStackItem.SetHighlight(false);
            _lastClickedStackItem = null;
        }

        OptionItem clickedOptionItem = _optionItemsUI.Find(item => item.GetOptionType() == optionType);

        if (clickedOptionItem != null)
        {
            if (_highlightedOptionItem != null && _highlightedOptionItem != clickedOptionItem)
            {
                _highlightedOptionItem.SetHighlight(false);
            }

            _highlightedOptionItem = clickedOptionItem;
            _highlightedOptionItem.SetHighlight(true);

            _selectedOptionTypeToAssign = optionType;
        }
        else
        {
            Debug.LogWarning($"GameManager: 未找到点击的 OptionItem 实例或类型 '{optionType}' 无效.");
            if (_highlightedOptionItem != null) _highlightedOptionItem.SetHighlight(false);
            _highlightedOptionItem = null;
            _selectedOptionTypeToAssign = "";
            if (_lastClickedStackItem != null) _lastClickedStackItem.SetHighlight(false);
            _lastClickedStackItem = null;
        }
    }

    public void OnStackSelected(StackItem targetStack)
    {
        if (string.IsNullOrEmpty(_selectedOptionTypeToAssign) || _selectedOptionTypeToAssign == "Empty")
        {
            if (_lastClickedStackItem != null)
                _lastClickedStackItem.SetHighlight(false);

            _lastClickedStackItem = targetStack;
            _lastClickedStackItem.SetHighlight(true);
            return;
        }

        bool success = TryAssignSelectedOptionToStack(targetStack, _selectedOptionTypeToAssign);

        if (success)
        {
            _selectedOptionTypeToAssign = "";
            // TODO: 取消之前选中的 OptionItem 的高亮（如果实现了高亮）
        }
        else
        {
            Debug.LogWarning("GameManager: 无法将选中的选项分配到该槽位。请先选择一个空槽位。");
            if (_lastClickedStackItem != null) _lastClickedStackItem.SetHighlight(false);
            _lastClickedStackItem = null;
        }
    }

    private bool TryAssignSelectedOptionToStack(StackItem targetStack, string optionTypeToAssign)
    {
        if (targetStack == null || !targetStack.IsEmpty())
        {
            Debug.LogWarning("GameManager: 目标指令槽位无效或不为空。");
            return false;
        }

        if (!_availableOptionsCount.ContainsKey(optionTypeToAssign) || _availableOptionsCount[optionTypeToAssign] <= 0)
        {
            Debug.LogWarning($"GameManager: 指令 '{optionTypeToAssign}' 数量不足。");
            return false;
        }

        targetStack.SetOption(optionTypeToAssign);
        _availableOptionsCount[optionTypeToAssign]--;
        int currentCount = _availableOptionsCount[optionTypeToAssign];

        OptionItem assignedOptionItem = _optionItemsUI.Find(item => item.GetOptionType() == optionTypeToAssign);
        if (assignedOptionItem != null)
        {
            assignedOptionItem.SetCount(currentCount);
        }

        if (_highlightedOptionItem != null)
        {
            _highlightedOptionItem.SetHighlight(false);
            _highlightedOptionItem = null;
        }
        _selectedOptionTypeToAssign = "";

        return true;
    }

    public bool TryReturnOptionToAvailable(StackItem stackItemToReturn)
    {
        if (stackItemToReturn == null || stackItemToReturn.IsEmpty())
        {
            Debug.LogWarning("GameManager: 尝试归还的指令槽位为空或无效。");
            return false;
        }

        string returnedOptionType = stackItemToReturn.GetOption();

        stackItemToReturn.SetOption("Empty");

        if (_availableOptionsCount.ContainsKey(returnedOptionType))
        {
            _availableOptionsCount[returnedOptionType]++;
            int currentCount = _availableOptionsCount[returnedOptionType];
            OptionItem returnedOptionItem = _optionItemsUI.Find(item => item.GetOptionType() == returnedOptionType);
            if (returnedOptionItem != null)
            {
                returnedOptionItem.SetCount(currentCount);
            }
            return true;
        }
        else
        {
            Debug.LogError($"GameManager: 尝试归还未知指令类型 '{returnedOptionType}'。");
            return true;
        }
    }

    void InitOption()
    {
        foreach (Transform child in optionContainer)
            Destroy(child.gameObject);

        _optionItemsUI.Clear();

        foreach (var optKVP in _availableOptionsCount)
        {
            string optionType = optKVP.Key;
            int initialCount = optKVP.Value;

            if (string.IsNullOrEmpty(optionType))
            {
                Debug.LogWarning($"GameManager: 字典中发现无效指令类型，跳过: Type='{optionType}'.");
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

            _optionItemsUI.Add(optionItem);
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

                if (elementData.type == ElementType.PlayerStart)
                {
                    playerController = newElement.GetComponent<PlayerController>();
                    if (playerController == null)
                    {
                        Debug.LogError("构建场景时找不到 PlayerController 在 PlayerPrefab 上! 请检查 Prefab.");
                    }
                    else
                    {
                        playerInitialPosition = newElement.transform.position;
                        playerInitialRotation = newElement.transform.rotation;
                    }
                }
                if (elementData.type == ElementType.Goal)
                {
                    FlagTrigger flag = newElement.GetComponent<FlagTrigger>(); // 获取 FlagTrigger 组件
                    if (flag != null)
                    {
                        flag.gm = this;
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

        stackCount = currentLevelData.maxOptionStackSize;
        InitStack();

        _availableOptionsCount.Clear();
        if (currentLevelData.availableOptions != null)
        {
            foreach (var opt in currentLevelData.availableOptions)
            {
                if (string.IsNullOrEmpty(opt.optionType) || opt.initialCount < 0) // count 可以为0
                {
                    Debug.LogWarning($"Invalid option data found in LevelData: Type='{opt.optionType}', Count={opt.initialCount}. Skipping.");
                    continue;
                }
                if (!_availableOptionsCount.ContainsKey(opt.optionType))
                {
                    _availableOptionsCount.Add(opt.optionType, opt.initialCount);
                }
                else
                {
                    Debug.LogWarning($"Duplicate option type '{opt.optionType}' found in LevelData. Overwriting or skipping based on your design. Currently skipping.");
                }
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
        if (_runningOptionSequenceCoroutine != null)
        {
            StopCoroutine(_runningOptionSequenceCoroutine);
            _runningOptionSequenceCoroutine = null;
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
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < options.Count; i++)
        {
            if (i < stackItems.Count && stackItems[i] != null)
            {
                stackItems[i].SetHighlight(true);
            }

            if (playerController != null)
            {
                Coroutine playerActionCoroutine = playerController.ExecuteSingleOption(options[i]);
                if (playerActionCoroutine != null)
                {
                    yield return playerActionCoroutine;
                }
                else
                {
                    Debug.LogWarning($"PlayerController.ExecuteSingleOption({options[i]}) 未返回有效的协程，可能指令未被处理或PlayerController已禁用。");
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                Debug.LogError("GameManager: PlayerController 引用丢失，无法执行指令!");
                yield return new WaitForSeconds(0.5f);
            }

            if (i < stackItems.Count && stackItems[i] != null)
            {
                stackItems[i].SetHighlight(false);
            }
        }

        CheckWinCondition();
        _runningOptionSequenceCoroutine = null;
    }

    void OnReplayClicked()
    {
        if (_runningOptionSequenceCoroutine != null)
        {
            StopCoroutine(_runningOptionSequenceCoroutine);
            _runningOptionSequenceCoroutine = null;
        }

        if (playerController != null)
        {
            playerController.StopAllPlayerCoroutines();
        }

        ClearLevel();
        BuildLevel();

        if (playerController != null)
        {
            playerController.ResetTo(playerInitialPosition, playerInitialRotation);
        }
        else
        {
            Debug.LogError("GameManager: 玩家控制器引用丢失，无法完成重置!");
        }

        foreach (var stack in stackItems)
        {
            stack.SetOption("Empty");
            stack.SetHighlight(false);
        }

        foreach (var optionItemUI in _optionItemsUI)
        {
            if (_availableOptionsCount.ContainsKey(optionItemUI.GetOptionType()))
            {
                optionItemUI.SetCount(_availableOptionsCount[optionItemUI.GetOptionType()]);
            }
            else
            {
                optionItemUI.SetCount(0);
            }
        }
        if (_lastClickedStackItem != null) _lastClickedStackItem.SetHighlight(false);
        _lastClickedStackItem = null;
        _selectedOptionTypeToAssign = "";

        winText.gameObject.SetActive(false);
        hasWon = false;
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