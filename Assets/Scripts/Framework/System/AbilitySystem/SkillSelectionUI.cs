using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// ==================== UI 系统 ====================

public class SkillSelectionUI : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject skillCardPrefab;
    public Transform cardContainer;
    public TextMeshProUGUI rerollText;

    [Header("配置")]
    public int choicesCount = 3;
    public int maxRerolls = 3;

    private int currentRerolls;
    private List<SkillData> currentChoices;

    public void ShowSkillSelection()
    {
        gameObject.SetActive(true);
        currentRerolls = maxRerolls;
        RefreshChoices();
    }

    private void RefreshChoices()
    {
        // 清除旧卡片
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        // 获取新选择
        currentChoices = SkillManager.Instance.GetRandomSkillChoices(choicesCount, SkillRarity.Common);

        // 创建卡片
        foreach (var skill in currentChoices)
        {
            var card = Instantiate(skillCardPrefab, cardContainer);
            var ui = card.GetComponent<SkillCardUI>();
            ui.Setup(skill, OnSkillSelected);
        }

        UpdateRerollText();
    }

    private void OnSkillSelected(SkillData skill)
    {
        SkillManager.Instance.AcquireSkill(skill);
        gameObject.SetActive(false);
    }

    public void OnRerollClicked()
    {
        if (currentRerolls > 0)
        {
            currentRerolls--;
            RefreshChoices();
        }
    }

    private void UpdateRerollText()
    {
        rerollText.text = $"重Roll ({currentRerolls}/{maxRerolls})";
    }
}
