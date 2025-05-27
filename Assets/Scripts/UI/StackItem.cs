using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StackItem : MonoBehaviour, IPointerClickHandler
{
    public Image background;
    public Image optionIcon;

    private GameManager gm;
    private string option = "Empty";

    public void Init(GameManager manager)
    {
        gm = manager;
        option = "Empty";
        UpdateView();
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
        return option == "Empty";
    }

    public void SetHighlight(bool on)
    {
        if (background != null)
        {
            background.color = on ? Color.yellow : Color.white;
        }
        if (optionIcon != null)
        {
            optionIcon.color = on ? Color.yellow : Color.white;
        }
    }

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

        // TODO: 可以根据指令类型改变 StackItem 的背景样式
        // if (option == "Empty") { /* 设置为空槽位的样式 */ }
        // else { /* 设置为有指令的样式 */ }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gm == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gm.OnStackSelected(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            bool success = gm.TryReturnOptionToAvailable(this);
            if (success)
            {
                SetHighlight(false);
            }
        }
    }
}