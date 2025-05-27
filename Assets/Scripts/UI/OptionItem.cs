using UnityEngine;
using UnityEngine.UI;

public class OptionItem : MonoBehaviour
{
    public Image optionIcon;
    public Image background;
    public Text countText;

    private GameManager gm;
    private int _currentCount;
    private string _optionType;

    public void Init(GameManager manager, string optionType, int initialCount, Sprite iconSprite)
    {
        gm = manager;
        _optionType = optionType;
        _currentCount = initialCount;

        if (optionIcon != null)
        {
            optionIcon.sprite = iconSprite;
            optionIcon.enabled = iconSprite != null;
        }

        UpdateCountDisplay();

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (_currentCount > 0 && gm != null)
        {
            gm.OnOptionSelected(_optionType);
        }
        else
        {
            Debug.Log($"OptionItem: '{_optionType}' 数量不足或 GameManager 引用丢失。");
        }
    }

    public void UpdateCountDisplay()
    {
        if (countText != null)
        {
            countText.text = "x" + _currentCount;
            if (_currentCount <= 0) GetComponent<Button>().interactable = false;
            else GetComponent<Button>().interactable = true;
        }
    }

    public string GetOptionType()
    {
        return _optionType;
    }

    public void SetCount(int newCount)
    {
        _currentCount = newCount;
        UpdateCountDisplay();
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
}