using Game.Core;
using Game.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSfx : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (GameLauncher.Instance == null)
                return;

            GameLauncher.Instance.EventBus.Publish(new SfxRequestedEvent(ESfx.ButtonClick));
        }
    }
}
