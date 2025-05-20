// File: Assets/Scripts/LevelData.cs (或者你希望存放数据脚本的路径)
using System.Collections.Generic;
using UnityEngine; // 需要 UnityEngine 来使用 Vector2
using System; // 需要 System 来使用 Serializable

// 关卡元素的类型
public enum ElementType
{
    None,
    Platform,
    PlayerStart,
    Goal,
    // TODO: 未来添加更多类型
}

// 用于存储单个关卡元素的数据
[System.Serializable]
public class ElementData
{
    public ElementType type; // 元素类型
    public Vector2 position; // 元素在世界坐标系中的位置
    // TODO: 未来这里可以添加更多属性，如旋转、大小、特定机关的状态/配置等
}

// 用于存储可用指令的信息
[System.Serializable]
public class AvailableCommand
{
    public string commandType; // 指令类型字符串 (如 "Right", "Jump", "Observe")
    public int initialCount; // 初始可用次数
    // TODO: 可能需要引用对应的 Option UI Prefab 名称或类型，或者在这里存储更多配置
}

// 用于存储整个关卡的数据
[System.Serializable]
public class LevelData
{
    public string levelName = "New Level"; // 关卡名称

    [Header("Level Elements")] // 关卡元素列表
    public List<ElementData> elements = new List<ElementData>();

    [Header("Game Settings")] // 新增：关卡的游戏设置
    public int maxCommandStackSize = 5; // 指令栈最大数量
    public List<AvailableCommand> availableCommands = new List<AvailableCommand>(); // 本关可用的指令及其数量
    // TODO: 未来可以添加更多设置，如背景音乐、初始限制等
}