using UnityEngine;

namespace SortGems.Core
{
    /// <summary>
    /// 個々のビジュアライザーバーの挙動を制御するスクリプト。
    /// 割り当てられた特定の周波数帯のスペクトルデータをサンプリングして、自身の高さをリアルタイムに伸縮させます。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VisualizerBar : MonoBehaviour
    {
        private int _frequencyBin;
        private float _sensitivity;
        private float _lerpSpeed;
        private float _minHeight;
        private float _maxHeight;

        private RectTransform _rectTransform;
        private float[] _spectrum = new float[256];
        private float _currentHeight;

        /// <summary>
        /// マネージャーからパラメータをアサインする。
        /// </summary>
        public void Setup(int bin, float sensitivity, float lerpSpeed, float minHeight, float maxHeight)
        {
            _frequencyBin = bin;
            _sensitivity = sensitivity;
            _lerpSpeed = lerpSpeed;
            _minHeight = minHeight;
            _maxHeight = maxHeight;

            _rectTransform = GetComponent<RectTransform>();
            _currentHeight = _minHeight;
            
            // 初期サイズを最小の高さに設定
            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _minHeight);
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
                // 音源の周波数スペクトルデータを取得
                activeSource.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

                float rawValue = 0f;
                if (_frequencyBin >= 0 && _frequencyBin < _spectrum.Length)
                {
                    // 高音域（インデックスが大きいビン）になるほど振幅値が小さくなる物理的特性があるため、
                    // 周波数インデックスの大きさに応じて感度のスケールアップ補正をかけ、ビジュアル的に綺麗な波形を描くようにします。
                    float factor = 1.0f + Mathf.Sqrt(_frequencyBin) * 0.45f;
                    rawValue = _spectrum[_frequencyBin] * factor;
                }

                // 目標の高さ（ピクセル）を算出
                targetHeight = _minHeight + rawValue * _sensitivity;
                targetHeight = Mathf.Clamp(targetHeight, _minHeight, _maxHeight);
            }

            // Lerp を用いて、フレームレートに依存しない滑らかな伸縮アニメーションを実行
            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, Time.deltaTime * _lerpSpeed);

            // 幅（Width）は変えずに、高さ（Height）のみを適用
            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _currentHeight);
        }
    }
}
