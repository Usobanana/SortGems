// GameManager.cs — ゲーム状態・タイマー管理
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SortGems.Core
{
    /// <summary>
    /// ゲーム全体の状態機械とタイマーを管理するシングルトン。
    /// GridManager・UIと連携する。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ---- 状態 ----
        public enum GameState { Idle, Playing, Paused, Cleared, Failed }
        public GameState State { get; private set; } = GameState.Idle;

        // ---- タイマー ----
        public float TimeRemaining { get; private set; }
        public float TotalTime { get; private set; }
        public float TimeRatio => TotalTime > 0 ? TimeRemaining / TotalTime : 1f;

        // ---- イベント ----
        public UnityEvent OnGameCleared = new();
        public UnityEvent OnGameFailed = new();
        public UnityEvent<float> OnTimerUpdated = new();  // 残り時間（秒）

        // ---- 参照 ----
        [SerializeField] private GridManager _gridManager;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _gridManager.OnStageCleared += HandleCleared;
        }

        // ---- ゲーム開始 ----

        public void StartStage(StageData stage)
        {
            TotalTime = stage.timeLimitSeconds;
            TimeRemaining = TotalTime;
            State = GameState.Playing;
            Time.timeScale = 1f; // 安全のためTimeScaleをリセット

            _gridManager.Initialize(stage);
        }

        // ---- タイマー更新 ----

        private void Update()
        {
            // デバッグ機能：ESCキーで一時停止トグル
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
#endif

            if (State != GameState.Playing) return;

            TimeRemaining -= Time.deltaTime;
            OnTimerUpdated?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                HandleFailed();
            }
        }

        // ---- 状態遷移 ----

        private void HandleCleared()
        {
            State = GameState.Cleared;
            OnGameCleared?.Invoke();
        }

        private void HandleFailed()
        {
            // タイムオーバー：途中状態を保持したまま停止。
            // AddTime() でリワード広告 or アイテム消費により続行可能。
            State = GameState.Failed;
            OnGameFailed?.Invoke();
        }

        public void PauseGame()
        {
            if (State == GameState.Playing) State = GameState.Paused;
        }

        public void ResumeGame()
        {
            if (State == GameState.Paused) State = GameState.Playing;
        }

        /// <summary>
        /// リワード広告 or アイテム消費で時間追加して続行。
        /// タイムオーバー後（Failed）でも呼び出し可。グリッド状態はそのまま再開。
        /// </summary>
        public void AddTime(float seconds)
        {
            if (State == GameState.Failed || State == GameState.Playing)
            {
                TimeRemaining += seconds;   // 上限なし（演出上の制限はUI側で対応）
                State = GameState.Playing;  // Failed → Playing に復帰
            }
        }

        public void Undo() => _gridManager.Undo();
        public void ResetStage()
        {
            Time.timeScale = 1f; // 一時停止中にリセットされた場合はTimeScaleを戻す
            if (State == GameState.Paused) State = GameState.Playing;
            _gridManager.Reset();
        }

        private void TogglePause()
        {
            if (State == GameState.Playing)
            {
                PauseGame();
                Time.timeScale = 0f;
                Debug.Log("[GameManager] デバッグ機能：ゲームを一時停止しました");
            }
            else if (State == GameState.Paused)
            {
                ResumeGame();
                Time.timeScale = 1f;
                Debug.Log("[GameManager] デバッグ機能：ゲームを再開しました");
            }
        }
    }
}
