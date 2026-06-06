using UnityEngine;
using UnityEngine.UI;

namespace SortGems.Core
{
    /// <summary>
    /// BGMの周波数（特に低音域）に同期してUI Image（RectTransform）の高さ（Height）をリアルタイムに伸縮させるスクリプト。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AudioVisualizer : MonoBehaviour
    {
        [Header("Visualizer Settings")]
        [Tooltip("伸縮の感度（2Dピクセル単位なので大きめの値が必要です。例: 1500〜3000）")]
        [SerializeField] private float _sensitivity = 2000f;

        [Tooltip("元のサイズに戻るスムーズさ（Lerpスピード、値が大きいほど速く戻り、小さいほどゆっくり滑らかに戻ります）")]
        [SerializeField] private float _lerpSpeed = 12f;

        [Tooltip("低音域を検知する周波数ビンのインデックス（通常 0〜7 程度が低音域）")]
        [SerializeField] private int _frequencyBin = 2;

        [Tooltip("ビジュアライザーの最小高さ（ピクセル）")]
        [SerializeField] private float _minHeight = 12f;

        [Tooltip("ビジュアライザーの最大高さ（ピクセル）")]
        [SerializeField] private float _maxHeight = 200f;

        private RectTransform _rectTransform;
        private float[] _spectrum = new float[256];
        private float _currentHeight = 12f;
        private Vector2 _originalSize;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalSize = _rectTransform.sizeDelta;
            _currentHeight = _minHeight;
            _rectTransform.sizeDelta = new Vector2(_originalSize.x, _minHeight);
        }

        private void Update()
        {
            AudioSource activeSource = null;
            if (SoundManager.Instance != null)
            {
                activeSource = SoundManager.Instance.GetActiveBGMSource();
            }

            float targetHeight = _minHeight;

            if (activeSource != null && activeSource.isPlaying)
            {
                // 音声データから周波数スペクトル（振幅分布）を取得
                activeSource.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

                // 指定した特定の周波数域（低音）の振幅値を取得
                float rawValue = 0f;
                if (_frequencyBin >= 0 && _frequencyBin < _spectrum.Length)
                {
                    rawValue = _spectrum[_frequencyBin];
                }

                // 目標の高さ（ピクセル）を算出
                targetHeight = _minHeight + rawValue * _sensitivity;
                targetHeight = Mathf.Clamp(targetHeight, _minHeight, _maxHeight);
            }

            // Lerpを用いてフレームレートに依存しない滑らかな高さ変化を適用
            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, Time.deltaTime * _lerpSpeed);

            // 高さをRectTransformに適用 (幅は元のサイズを維持)
            _rectTransform.sizeDelta = new Vector2(_originalSize.x, _currentHeight);
        }
    }
}
