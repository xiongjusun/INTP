using UnityEngine;
using TMPro;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 交互提示UI控制器 - 管理屏幕上的交互提示
    /// 需要放在始终 active 的 GameObject 上
    /// </summary>
    public class InteractionPromptController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup canvasGroup;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<InteractableChangedEvent>(OnInteractableChanged);
        }

        private void OnDisable()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<InteractableChangedEvent>(OnInteractableChanged);
            }
        }

        private void OnInteractableChanged(InteractableChangedEvent evt)
        {
            if (evt.HasInteractable)
            {
                ShowPrompt(evt.PromptText);
            }
            else
            {
                HidePrompt();
            }
        }

        private void ShowPrompt(string text)
        {
            if (promptText != null)
            {
                promptText.text = text;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                if (!canvasGroup.gameObject.activeSelf)
                {
                    canvasGroup.gameObject.SetActive(true);
                }
            }
        }

        private void HidePrompt()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
    }
}
