using UnityEngine;

namespace VSL
{
    public class EnemySpawner : MonoBehaviour
    {
        public EnemyController enemyPrefab;
        public float spawnInterval = 0.7f;
        public float spawnRadiusMin = 7f;
        public float spawnRadiusMax = 11f;

        private Transform _player;
        private float _t;
        private bool _running;

        public void Begin(Transform player)
        {
            _player = player;
            _t = 0f;
            _running = true;
        }

        public void Stop()
        {
            _running = false;
        }

        private void Update()
        {
            if (!_running) return;
            if (_player == null) return;
            if (enemyPrefab == null) return;

            _t -= Time.deltaTime;
            if (_t > 0f) return;

            _t = spawnInterval;

            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(spawnRadiusMin, spawnRadiusMax);
            Vector3 pos = _player.position + (Vector3)(dir * dist);

            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }
}
