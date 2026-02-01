using UnityEngine;

namespace VSL
{
    [RequireComponent(typeof(Collider2D))]
    public class XPOrbPickup : MonoBehaviour
    {
        public int xpValue = 1;

        [Header("Optional Filter")]
        public LayerMask playerLayer; // Player 레이어만 먹게 하고 싶으면 체크

        private void Awake()
        {
            // 트리거 강제
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // playerLayer를 쓰는 경우(0이 아니면 필터 적용)
            if (playerLayer.value != 0)
            {
                if (((1 << other.gameObject.layer) & playerLayer.value) == 0) return;
            }

            // ✅ 플레이어에 있는 Experience 찾기
            var exp = other.GetComponentInParent<Experience>();
            if (exp == null) return;

            exp.AddXP(xpValue);
            Destroy(gameObject);
        }
    }
}
