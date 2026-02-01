using UnityEngine;

namespace VSL
{
    public class UltimateVisualFeedback : MonoBehaviour
    {
        [SerializeField] private UltimateSystem ultimate;
        [SerializeField] private GameObject auraObject;   // UltAura
        [SerializeField] private ParticleSystem auraFX;   // 있으면 연결(없어도 됨)

        private bool _wasActive;

        private void Reset()
        {
            ultimate = GetComponent<UltimateSystem>();
        }

        private void Update()
        {
            if (!ultimate) return;

            // UltimateSystem에 IsActive가 없으면 아래에 "IsActive 추가" 참고
            bool active = ultimate.IsActive;

            if (active == _wasActive) return;
            _wasActive = active;

            if (auraObject) auraObject.SetActive(active);

            if (auraFX)
            {
                if (active) auraFX.Play(true);
                else auraFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
