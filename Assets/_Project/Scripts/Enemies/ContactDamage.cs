using UnityEngine;

namespace VSL
{
    public class ContactDamage : MonoBehaviour
    {
        [Header("Damage")]
        public int damageOnEnter = 10;     // 닿자마자 1회
        public int damagePerTick = 5;      // 붙어있는 동안 주기 데미지
        public float tickInterval = 0.5f;

        [Header("Target")]
        public LayerMask playerLayer;      // Player 레이어

        private float _nextTickTime;

        private bool IsPlayer(Collider2D col)
        {
            return ((1 << col.gameObject.layer) & playerLayer.value) != 0;
        }

        private void Damage(Collider2D col, int amount)
        {
            var hp = col.GetComponentInParent<Health>();
            if (hp == null || hp.IsDead) return;

            hp.TakeDamage(amount);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            Damage(other, damageOnEnter);
            _nextTickTime = Time.time + tickInterval;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            if (Time.time < _nextTickTime) return;

            _nextTickTime = Time.time + tickInterval;
            Damage(other, damagePerTick);
        }

        // Trigger를 안 쓰는 세팅도 대비
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider == null) return;
            if (!IsPlayer(collision.collider)) return;
            Damage(collision.collider, damageOnEnter);
            _nextTickTime = Time.time + tickInterval;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.collider == null) return;
            if (!IsPlayer(collision.collider)) return;
            if (Time.time < _nextTickTime) return;

            _nextTickTime = Time.time + tickInterval;
            Damage(collision.collider, damagePerTick);
        }
    }
}
