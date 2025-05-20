using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class OptionIcon
{
    public string optionType;
    public Sprite icon;
}

public class OptionManager : MonoBehaviour
{
    [Header("Option Icon Mapping")]
    public List<OptionIcon> optionIcons = new List<OptionIcon>();

    private Dictionary<string, Sprite> _iconMap = new Dictionary<string, Sprite>();
    private bool _isInitialized = false;

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        if (optionIcons != null)
        {
            foreach (var item in optionIcons)
            {
                if (!string.IsNullOrEmpty(item.optionType) && item.icon != null)
                {
                    if (!_iconMap.ContainsKey(item.optionType))
                    {
                        _iconMap.Add(item.optionType, item.icon);
                    }
                    else
                    {
                        Debug.LogWarning("OptionManager: 存在重复的指令类型配置: " + item.optionType);
                    }
                }
            }
        }

        _isInitialized = true;
        Debug.Log("OptionManager: Initialization complete. Icon map built.");
    }

    public Sprite GetIcon(string optionType)
    {
        EnsureInitialized();

        if (_iconMap.ContainsKey(optionType)) return _iconMap[optionType];

        Debug.LogWarning("OptionManager: 未找到指令类型 '" + optionType + "' 的图标配置!");
        return null;
    }
}