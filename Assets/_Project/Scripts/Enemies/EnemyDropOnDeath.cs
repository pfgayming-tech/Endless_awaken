using UnityEngine;

namespace VSL
{
    public class EnemyDropOnDeath : MonoBehaviour
    {
        [Header("Drop Prefab")]
        public GameObject spiritOrbPrefab;   // 드랍할 프리팹(줍는 오브)

        [Header("Settings")]
        public int dropCount = 1;
        public float scatterRadius = 0.25f;

        private Health _health;

        private void Awake()
        {
            // Health가 루트/자식 어디 있어도 찾기
            _health = GetComponent<Health>();
            if (_health == null) _health = GetComponentInChildren<Health>(true);
            if (_health == null) _health = GetComponentInParent<Health>();

            if (_health != null)
            {
                _health.OnDied += Drop;
            }
            else
            {
                Debug.LogError($"[EnemyDropOnDeath] Health not found on {name}. Drop won't work.");
            }
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDied -= Drop;
        }

        private void Drop()
        {
            if (spiritOrbPrefab == null)
            {
                Debug.LogError($"[EnemyDropOnDeath] spiritOrbPrefab is NULL on {name}");
                return;
            }

            for (int i = 0; i < dropCount; i++)
            {
                Vector2 off = Random.insideUnitCircle * scatterRadius;
                Instantiate(spiritOrbPrefab, transform.position + (Vector3)off, Quaternion.identity);
            }
        }
    }
}
