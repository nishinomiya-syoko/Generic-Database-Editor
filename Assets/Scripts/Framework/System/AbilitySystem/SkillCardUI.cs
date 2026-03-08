using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单个技能卡片UI
/// </summary>
public class SkillCardUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Image rarityBorder;
    public Button selectButton;

    private SkillData data;
    private System.Action<SkillData> onSelected;

    public void Setup(SkillData skillData, System.Action<SkillData> callback)
    {
        data = skillData;
        onSelected = callback;

        icon.sprite = skillData.icon;
        nameText.text = skillData.skillName;
        descText.text = skillData.GetDescription(1);

        // 根据稀有度设置边框颜色
        rarityBorder.color = GetRarityColor(skillData.rarity);

        selectButton.onClick.AddListener(() => onSelected?.Invoke(data));
    }

    private Color GetRarityColor(SkillRarity rarity)
    {
        return rarity switch
        {
            SkillRarity.Common => Color.gray,
            SkillRarity.Rare => Color.blue,
            SkillRarity.Epic => Color.magenta,
            SkillRarity.Legendary => Color.yellow,
            _ => Color.white
        };
    }
}