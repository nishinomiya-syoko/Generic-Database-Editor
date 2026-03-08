using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace Top
{
    // UI管理器 - Stack模式
    public partial class UIManager : MonoBehaviour
    {
       
        [Header("通知系统")]
        public GameObject notificationPrefab;
        public Transform notificationContainer;

        // 使用栈管理面板历史，支持多级返回
        private Stack<UIPanel> panelStack = new Stack<UIPanel>();

        // 明确记录当前面板，避免每次用 activeSelf 去猜
        private UIPanel currentPanel;

        void Start()
        {
            // 初始隐藏所有面板并清空栈
            HideAllPanels();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Back();
            }
        }

        void OnDestroy()
        {
            

            panelStack.Clear();
            currentPanel = null;
        }

        #region 面板控制

        // 隐藏所有面板并清空栈
        private void HideAllPanels()
        {
            panelStack.Clear();
            currentPanel = null;
        }

        // 显示新面板（将当前面板压入栈）
        public void ShowPanel(UIPanel panel)
        {
            if (panel == null)
                return;

            // 如果要显示的就是当前面板，直接返回
            if (panel == currentPanel)
                return;

            // 如果当前有激活面板且不是主菜单，将其压栈
            if (currentPanel != null && currentPanel.panelType != PanelType.MainPanel)
            {
                panelStack.Push(currentPanel);
            }

            // 隐藏当前面板
            if (currentPanel != null)
            {
                currentPanel.Hide();
            }

            // 显示新面板
            panel.SetUIManager(this);
            currentPanel = panel;
            currentPanel.Show();
        }

        // 返回上一级面板
        public void Back()
        {
            if (panelStack.Count > 0)
            {
                // 关闭当前面板
                if (currentPanel != null)
                {
                    currentPanel.Hide();
                }

                // 弹出并显示上一个面板
                var previousPanel = panelStack.Pop();
                previousPanel.SetUIManager(this);
                currentPanel = previousPanel;
                currentPanel.Show();
            }
            else
            {
                // 栈已空，返回基地视图
                // ShowBaseView();
            }
        }

        // 关闭指定面板（支持返回）
        public void ClosePanel(UIPanel panel)
        {
            if (panel == null)
                return;

            // 如果要关闭的是当前面板，则执行返回逻辑
            if (currentPanel == panel)
            {
                Back();
                return;
            }

            // 如果不是当前面板，需要从栈中移除该面板（否则未来 Back 会跳到已经关闭的面板）
            if (panelStack.Count > 0)
            {
                var tempStack = new Stack<UIPanel>();

                bool removed = false;
                while (panelStack.Count > 0)
                {
                    var top = panelStack.Pop();
                    if (!removed && top == panel)
                    {
                        removed = true; // 丢弃一次
                        continue;
                    }

                    tempStack.Push(top);
                }

                // 还原顺序
                while (tempStack.Count > 0)
                {
                    panelStack.Push(tempStack.Pop());
                }
            }

            panel.Hide();
        }

        // 获取当前激活的面板
        public UIPanel GetCurrentPanel()
        {
            return currentPanel;
        }

        public void ShowUpgradePanel()
        {
            Debug.Log("ShowUpgradePanel");
            ShowNotification("升级面板功能开发中...");
        }

        public void ShowSettings()
        {
            ShowNotification("设置功能开发中...");
        }

        // 清空栈（用于特殊场景）
        public void ClearStack()
        {
            panelStack.Clear();
        }

        #endregion

       

        #region 通知系统

        public void ShowNotification(string message, float duration = 3f)
        {
            if (notificationPrefab != null && notificationContainer != null)
            {
                GameObject notification = Instantiate(notificationPrefab, notificationContainer);
                Text notificationText = notification.GetComponentInChildren<Text>();

                if (notificationText != null)
                    notificationText.text = message;

                // 自动销毁
                Destroy(notification, duration);

                // 动画效果
                RectTransform rect = notification.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.localScale = Vector3.zero;
                    rect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

                    rect.anchoredPosition = new Vector2(0, -100);
                    rect.DOAnchorPosY(0, 0.3f).SetEase(Ease.OutBack);
                }
            }
        }

        #endregion
    }
}
