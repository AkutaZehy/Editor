using UnityEngine;
using System;

public class LevelElement : MonoBehaviour
{
    [Tooltip("关卡元素的类型")]
    public ElementType type = ElementType.None;
    // TODO: 未来这里可以添加更多元素特有的标记或数据
}