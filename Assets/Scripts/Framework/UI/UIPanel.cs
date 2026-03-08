using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Top
{
    public enum PanelType
    {
        MainPanel,
        Normal,
        Tip,
    }
    // UI面板基类
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("UI设置")]
        public PanelType panelType = PanelType.Normal;
        public bool startHidden = true;
        public float animationDuration = 0.3f;
        public Ease showEase = Ease.OutBack;
        public Ease hideEase = Ease.InBack;

        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;
        protected bool isShowing = false;

        public Button exitButton;

        // 记住 UIManager，避免每次 SetUIManager 都叠加监听
        protected UIManager uiManager;

        public virtual void Init()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            rectTransform = GetComponent<RectTransform>();

            if (startHidden)
            {
                // 初始就完全隐藏
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
                isShowing = false;
            }
            else
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                isShowing = gameObject.activeSelf;
            }
            OnInit();
        }

        // 由 UIManager 注入
        public void SetUIManager(UIManager manager)
        {
            uiManager = manager;

            if (exitButton != null)
            {
                // 避免重复添加监听导致 Back 被调用多次
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(() =>
                {
                    uiManager?.Back();
                });
            }
        }

        public virtual void Show()
        {
            if (isShowing)
                return;

            isShowing = true;
            gameObject.SetActive(true);

            // 动画显示
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animationDuration).SetEase(showEase);

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 缩放动画
            rectTransform.localScale = Vector3.one * 0.8f;
            rectTransform.DOScale(Vector3.one, animationDuration).SetEase(showEase);

            OnShow();
        }

        public virtual void Hide()
        {
            if (!isShowing)
                return;

            isShowing = false;

            // 动画隐藏
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, animationDuration).SetEase(hideEase);

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 缩放动画
            rectTransform.DOScale(Vector3.one * 0.8f, animationDuration).SetEase(hideEase)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    OnHide();
                });
        }

        /// <summary>
        /// 无动画立即隐藏，用于初始化或强制重置
        /// </summary>
        public virtual void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            rectTransform.localScale = Vector3.one * 0.8f;

            gameObject.SetActive(false);
            isShowing = false;

            OnHide();
        }
        protected virtual void OnInit(){}
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        public virtual void Toggle()
        {
            if (isShowing)
                Hide();
            else
                Show();
        }
    }
}
