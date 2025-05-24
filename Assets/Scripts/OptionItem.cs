using UnityEngine;
using UnityEngine.UI;

public class OptionItem : MonoBehaviour
{
    public Image optionIcon;
    public Text countText;
    private GameManager gm;
    private int count;
    private string _optionType;

    public void Init(GameManager manager, string optionType, int initialCount, Sprite iconSprite)
    {
        gm = manager;
        count = initialCount;
        _optionType = optionType;

        if (optionIcon != null)
        {
            optionIcon.sprite = iconSprite;
            optionIcon.enabled = iconSprite != null;
        }
        UpdateText();

        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (count > 0)
        {
            bool success = gm.AssignOptionToSelected(_optionType);
            if (success)
            {
                count--;
                UpdateText();
            }
        }
    }

    private void UpdateText()
    {
        countText.text = "x" + count;
    }

    public void AddCount(int amount)
    {
        count += amount;
        UpdateText();
    }
}
