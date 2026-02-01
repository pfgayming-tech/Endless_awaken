using UnityEngine;

namespace VSL
{
    /// <summary>
    /// 적이 플레이어와 접촉했을 때만 데미지 주는 스크립트
    /// ✅ 적-적 충돌/트리거에는 절대 데미지 안 줌
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyContactDamage : MonoBehaviour
    {
        [Header("Damage")]
        public int contactDamage = 5;
        public float contactInterval = 0.5f;

        [Header("Target Filter")]
        public LayerMask playerLayer;     // ✅ Player 레이어만 포함시키기
        public string playerTag = "Player"; // (선택) 태그로도 한 번 더 안전장치

        private float _timer;

        private void Reset()
        {
            // 편의: 붙였을 때 Trigger로 권장(서로 밀치기 싫으면)
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_timer > 0f) return;

            // ✅ 레이어 필터: 플레이어가 아니면 즉시 리턴 (적끼리 절대 안 때림)
            if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

            // ✅ 태그도 같이 쓰면 더 안전(레이어 실수해도 방어)
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
                return;

            var hp = other.GetComponentInParent<Health>();
            if (hp == null || hp.IsDead) return;

            hp.TakeDamage(contactDamage);
            _timer = contactInterval;
        }

        // 만약 너가 Trigger가 아니라 Collision을 쓰고 있다면 아래도 추가로 켜줄 수 있음
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_timer > 0f) return;

            var other = collision.collider;

            if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
                return;

            var hp = other.GetComponentInParent<Health>();
            if (hp == null || hp.IsDead) return;

            hp.TakeDamage(contactDamage);
            _timer = contactInterval;
        }
    }
}
