using System.Collections.Generic;
using UnityEngine;

public enum ElementType
{
    None, Platform,
    PlayerStart, Goal,
}

[System.Serializable]
public class ElementData
{
    public ElementType type;
    public Vector2 position;
    // TODO: 未来这里可以添加更多属性，如旋转、大小、特定机关的状态/配置等
}

[System.Serializable]
public class AvailableOption
{
    public string optionType; // 指令类型字符串 (如 "Right", "Jump", "Observe")
    public int initialCount; // 初始可用次数
    // TODO: 可能需要引用对应的 Option UI Prefab 名称或类型，或者在这里存储更多配置
}

[System.Serializable]
public class LevelData
{
    public string levelName = "New Level";

    [Header("Level Elements")]
    public List<ElementData> elements = new List<ElementData>();

    [Header("Game Settings")]
    public int maxOptionStackSize = 5;
    public List<AvailableOption> availableOptions = new List<AvailableOption>();
    // TODO: 未来可以添加更多设置，如背景音乐、初始限制等
}