using UnityEngine;

namespace VSL
{
    public class SpiritOrb : MonoBehaviour
    {
        private Transform _owner;
        private int _index;
        private int _count;
        private float _damage;
        private LayerMask _enemyLayer;

        private float _orbitRadius = 1.6f;
        private float _angleOffset;
        private float _angleRuntime;

        private float _hitCd = 0.2f;
        private float _hitTimer;

        public void Bind(Transform owner, int index, int count, float damage, LayerMask enemyLayer)
        {
            _owner = owner;
            _index = index;
            _count = Mathf.Max(1, count);
            _damage = damage;
            _enemyLayer = enemyLayer;
            _angleOffset = 360f * (_index / (float)_count);
        }

        public void SetOrbitAngle(float baseAngle)
        {
            _angleRuntime = baseAngle;
        }

        private void Update()
        {
            if (_owner == null) return;

            float a = (_angleRuntime + _angleOffset) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _orbitRadius;
            transform.position = _owner.position + offset;

            _hitTimer -= Time.deltaTime;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_hitTimer > 0f) return;
            if (((1 << other.gameObject.layer) & _enemyLayer) == 0) return;

            var h = other.GetComponent<Health>();
            if (h != null)
            {
                h.TakeDamage(Mathf.RoundToInt(_damage));
                _hitTimer = _hitCd;
            }
        }
    }
}
