using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StackItem : MonoBehaviour
{
    public Image background;
    public Image optionIcon;
    private GameManager gm;
    private string option = "What";

    public void Init(GameManager manager)
    {
        gm = manager;
        option = "What";
        UpdateView();

        GetComponent<Button>().onClick.AddListener(() =>
        {
            gm.OnStackSelected(this);
        });
    }

    public void SetOption(string cmd)
    {
        option = cmd;
        UpdateView();
    }

    public string GetOption()
    {
        return option;
    }

    public bool IsEmpty()
    {
        return option == "What";
    }

    public void SetHighlight(bool on)
    {
        background.color = on ? Color.yellow : Color.white;
        optionIcon.color = on ? Color.yellow : Color.white;
    }

    // StackItem.cs - UpdateView 方法最终修改
    private void UpdateView()
    {
        if (gm == null)
        {
            Debug.LogError("StackItem UpdateView 错误: GameManager 为 null!");
            if (optionIcon != null) optionIcon.enabled = false;
            return;
        }

        Sprite iconSprite = gm.GetOptionIcon(option);

        if (optionIcon != null)
        {
            optionIcon.sprite = iconSprite;
            optionIcon.enabled = iconSprite != null;
        }
        else
        {
            Debug.LogWarning("StackItem: optionIcon 引用丢失!");
        }
    }
}
