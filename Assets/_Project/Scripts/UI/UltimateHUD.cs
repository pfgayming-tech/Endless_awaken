using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VSL.UI
{
    public class UltimateHUD : MonoBehaviour
    {
        public Slider gaugeSlider;
        public Button activateButton;
        public TMP_Text stateText;

        public VSL.UltimateSystem ultimate;

        private void Start()
        {
            if (activateButton != null)
            {
                activateButton.onClick.RemoveAllListeners();
                activateButton.onClick.AddListener(() =>
                {
                    if (ultimate != null) ultimate.TryActivate();
                });
            }
        }

        private void Update()
        {
            if (ultimate == null) return;

            if (gaugeSlider != null)
                gaugeSlider.value = ultimate.GetGauge01();

            bool ready = ultimate.IsReady;

            if (activateButton != null)
                activateButton.interactable = ready;

            if (stateText != null)
                stateText.text = ready ? "ULT READY" : "ULT CHARGING";
        }
    }
}
