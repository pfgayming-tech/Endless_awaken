using UnityEngine;
using UnityEngine.SceneManagement;

namespace VSL
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Run Settings")]
        public JobType selectedJob = JobType.Knight;
        public DifficultyType selectedDifficulty = DifficultyType.Normal;
        public float stageDurationSeconds = 180f;

        [Header("Difficulty Multipliers")]
        public float hardEnemyHpMultiplier = 1.25f;

        private float _time;
        private bool _isRunning;

        private GameObject _player;
        private Health _playerHealth;
        private Experience _playerExp;
        private PlayerStats _playerStats;
        private EnemySpawner _spawner;
        private UIManager _ui;

        // ✅ 바인딩 상태
        private bool _boundThisScene;
        private int _bindTryCount;

        public float EnemyHpMultiplier
            => selectedDifficulty == DifficultyType.Hard ? hardEnemyHpMultiplier : 1f;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            // 에디터에서 Game 씬부터 바로 실행하는 경우도 있어서 안전장치
            _boundThisScene = false;
            _bindTryCount = 0;
            TryBindGameScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {


            _boundThisScene = false;
            _bindTryCount = 0;

            // 씬 이름이 달라도, Player/Spawner가 있으면 바인딩되게
            TryBindGameScene();
        }

        private void Update()
        {
            // ✅ 바인딩이 아직 안 됐으면 매 프레임 재시도 (최대 몇 번만)
            if (!_boundThisScene && _bindTryCount < 300) // 약 5초(60fps) 정도 재시도
            {
                TryBindGameScene();
            }

            if (!_isRunning) return;
            if (SceneManager.GetActiveScene().name != SceneNames.Game) return;

            _time += Time.deltaTime;

            if (_ui != null) _ui.SetTimer(stageDurationSeconds - _time);

            if (_time >= stageDurationSeconds)
            {
                ClearRun();
            }
        }

        private void TryBindGameScene()
        {
            _bindTryCount++;

            // timeScale이 0이면 스폰/코루틴이 멈춤 → 무조건 풀어주기
            if (Time.timeScale <= 0.0001f)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }

            // 1) Player
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null)
            {                return;
            }

            // 2) Spawner (비활성 포함 탐색)
            _spawner = FindInSceneEvenIfInactive<EnemySpawner>();
            if (_spawner == null)
            {
          
                return;
            }

            // 3) UI (선택)
            _ui = FindInSceneEvenIfInactive<UIManager>();

            // 이제 진짜 바인딩
            BindGameScene();
            _boundThisScene = true;

  
        }

        private void BindGameScene()
        {
            _playerHealth = _player.GetComponent<Health>();
            _playerExp = _player.GetComponent<Experience>();
            _playerStats = _player.GetComponent<PlayerStats>();


            _playerHealth.OnDied -= OnPlayerDied;
            _playerHealth.OnDied += OnPlayerDied;

            _playerStats.BuildFromSelectedJob(selectedJob);

            _time = 0f;
            _isRunning = true;

            if (_ui != null) _ui.Bind(_playerHealth, _playerExp, this);

            // ✅ 스폰 시작 로그
         

            if (_spawner != null)
            {

                _spawner.Begin(_player.transform);
            }
         
            _spawner.Begin(_player.transform);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        public void AddXP(int amount)
        {
            if (_playerExp != null) _playerExp.AddXP(amount);
        }

        private void OnPlayerDied()
        {
            if (!_isRunning) return;
            _isRunning = false;

            if (_spawner != null) _spawner.Stop();

            if (_ui != null) _ui.ShowResult(false, 0);
            Time.timeScale = 0f;
        }

        private void ClearRun()
        {
            if (!_isRunning) return;
            _isRunning = false;

            if (_spawner != null) _spawner.Stop();

            int reward = CalculateTranscendReward(selectedJob, selectedDifficulty);
            ApplyTranscendReward(selectedJob, reward);

            if (_ui != null) _ui.ShowResult(true, reward);
            Time.timeScale = 0f;
        }

        private int CalculateTranscendReward(JobType job, DifficultyType diff)
        {
            var jp = SaveService.GetJob(job);
            if (!jp.hasClearedOnce) return 5;
            return diff == DifficultyType.Hard ? 2 : 1;
        }

        private void ApplyTranscendReward(JobType job, int points)
        {
            var jp = SaveService.GetJob(job);
            jp.totalEarned += points;
            jp.hasClearedOnce = true;
            SaveService.Save();
        }

        public void StartGame(JobType job, DifficultyType diff)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            selectedJob = job;
            selectedDifficulty = diff;
            SceneManager.LoadScene(SceneNames.Game);
        }

        public void BackToMenu()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private static T FindInSceneEvenIfInactive<T>() where T : UnityEngine.Object
        {
            var all = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i] is Component c && c.gameObject.scene.IsValid())
                    return all[i];
            }
            return null;
        }
    }
}
