// GemCellView.cs — グリッド上の1マスの表示制御（LeanTween不要版）
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SortGems.Core
{
    public class GemCellView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _gemImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _socketImage;
        [SerializeField] private GameObject _completedMark;

        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color _selectedColor = Color.white;
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0.5f, 0.8f);

        private GemColor _gemColor;
        private bool _isSelected;
        private bool _isHighlighted;
        private Vector3 _baseScale;
        private Coroutine _scaleCoroutine;

        public System.Action<GemCellView> OnTapped;
        public int Row { get; private set; }
        public int Col { get; private set; }
        public bool IsPalette { get; private set; }
        public GemColor GemColor => _gemColor;

        // goalColor 表示用（薄い下地色）
        private GemColor _goalColor;
        private Outline _outline;
        private Shadow _gemShadow;
        private Coroutine _blinkCoroutine;

        private void Awake()
        {
            _baseScale = transform.localScale;
            GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(OnPointerClick);

            // 角丸四角形のスプライトを適用し、ジェムを一回り小さく設定
            if (_gemImage != null)
            {
                _gemImage.sprite = GemColorPalette.RoundedRectSprite;
                _gemImage.transform.localRotation = Quaternion.identity;
                _gemImage.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

                // アセット参照の欠落を防ぐため、実行時にベベルシャドウスプライトを割り当てる
                var bevelTrans = _gemImage.transform.Find("BevelShadow");
                if (bevelTrans != null)
                {
                    var bevelImg = bevelTrans.GetComponent<Image>();
                    if (bevelImg != null)
                    {
                        bevelImg.sprite = GemColorPalette.BevelShadowSprite;
                        bevelImg.color = Color.white;
                    }
                }

                _gemShadow = _gemImage.GetComponent<Shadow>();
            }

            if (_socketImage != null)
            {
                // アセット参照の欠落を防ぐため、実行時に角丸スプライトを割り当てる
                _socketImage.sprite = GemColorPalette.RoundedRectSprite;
                _socketImage.transform.localRotation = Quaternion.identity;
                _socketImage.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
            }

            if (_backgroundImage != null)
            {
                // _backgroundImage.sprite は元の正方形（None）のままとする
                _outline = _backgroundImage.gameObject.AddComponent<Outline>();
                _outline.effectColor = new Color(1f, 1f, 1f, 0f);
                _outline.effectDistance = new Vector2(2f, 2f);
                _outline.enabled = false;
            }
        }

        public void Setup(int row, int col, bool isPalette = false)
        {
            Row = row;
            Col = col;
            IsPalette = isPalette;
        }

        // ---- 状態更新 ----

        /// <summary>
        /// ジェムの表示を更新する。
        /// goalColor があれば背景に薄く目標色を表示（ピクセルアートのヒント）。
        /// </summary>
        public void SetGem(GemColor color, GemColor goalColor = GemColor.None, bool isVoid = false)
        {
            _gemColor  = color;
            _goalColor = goalColor;

            var button = GetComponent<UnityEngine.UI.Button>();

            if (isVoid)
            {
                _gemImage.gameObject.SetActive(false);
                _backgroundImage.color = Color.clear;
                if (_completedMark != null) _completedMark.SetActive(false);
                if (_outline != null) _outline.enabled = false;
                if (button != null) button.enabled = false;
                return;
            }

            if (button != null) button.enabled = true;

            bool isEmpty = color == GemColor.None;
            bool isCorrect = (color != GemColor.None && color == goalColor);

            // くぼみ（ソケット）の表示制御: ジェムがない場合はくぼみを表示、ある場合は非表示
            if (_socketImage != null)
            {
                _socketImage.gameObject.SetActive(isEmpty && !isVoid);
            }

            // ジェムの表示: 空きマスでなければ、正解マスであってもジェム自体は表示する
            _gemImage.gameObject.SetActive(!isEmpty);
            if (!isEmpty)
            {
                _gemImage.color = GemColorPalette.GetColor(color);

                // 影の枠線制御: 正解マスの場合は影を無効化し、不正解マスの場合は有効化する
                if (_gemShadow != null)
                {
                    _gemShadow.enabled = !isCorrect;
                }
            }

            // 背景: ハイライト中であっても背景色は上書きしない（Outlineだけ光らせる）
            if (!_isSelected)
            {
                if (goalColor != GemColor.None)
                {
                    // 目標マスはジェムの有無や正解・不正解に関わらず、常に鮮やかな原色ベタ塗りで表示
                    _backgroundImage.color = GemColorPalette.GetColor(goalColor);
                }
                else if (isEmpty)
                {
                    // 目標色のない通常の空きマス (パレットなど)
                    _backgroundImage.color = _emptyColor;
                }
                else
                {
                    // 目標色のないマスにジェムが配置されている場合 (パレットなど)
                    _backgroundImage.color = new Color(0.18f, 0.18f, 0.22f, 1f);
                }
            }

            if (_completedMark != null)
                _completedMark.SetActive(false);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisual();

            float targetScale = selected ? 1.15f : 1f;
            ScaleTo(_baseScale * targetScale, 0.12f);
        }

        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;
            if (_outline == null) return;

            _outline.enabled = highlighted;
            if (highlighted)
            {
                if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = StartCoroutine(BlinkRoutine());
            }
            else
            {
                if (_blinkCoroutine != null) { StopCoroutine(_blinkCoroutine); _blinkCoroutine = null; }
                _outline.effectColor = new Color(1f, 1f, 1f, 0f);
            }
        }

        public void PlayCompletedEffect()
        {
            if (_completedMark != null)
                _completedMark.SetActive(true);

            StartCoroutine(CompletedScaleRoutine());
        }

        private IEnumerator CompletedScaleRoutine()
        {
            yield return ScaleRoutine(_baseScale * 1.3f, 0.1f);
            yield return ScaleRoutine(_baseScale, 0.1f);
        }

        // ---- アニメーション（Coroutine） ----

        private void ScaleTo(Vector3 target, float duration)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleRoutine(target, duration));
        }

        private IEnumerator ScaleRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack 近似
                float overshoot = 1.70158f;
                t = t - 1f;
                t = t * t * ((overshoot + 1f) * t + overshoot) + 1f;
                transform.localScale = Vector3.Lerp(start, target, t);
                yield return null;
            }
            transform.localScale = target;
        }

        private void UpdateVisual()
        {
            if (_isSelected)
                _backgroundImage.color = _selectedColor;
            else
            {
                // _isHighlighted の時は背景色は変更せず、通常の背景色を維持
                bool isCorrect = (_gemColor != GemColor.None && _gemColor == _goalColor);
                if (_goalColor != GemColor.None)
                {
                    // 目標マスは常に鮮やかな原色の背景色を維持
                    _backgroundImage.color = GemColorPalette.GetColor(_goalColor);
                }
                else
                {
                    _backgroundImage.color = _gemColor == GemColor.None
                        ? _emptyColor
                        : new Color(0.18f, 0.18f, 0.22f, 1f);
                }
            }
        }

        private IEnumerator BlinkRoutine()
        {
            float elapsed = 0f;
            while (_isHighlighted)
            {
                elapsed += Time.deltaTime;
                // 1秒間に約1.5往復ペースで明滅（アルファ0.2から1.0の間を滑らかにサイン波で往復）
                float alpha = 0.6f + Mathf.Sin(elapsed * Mathf.PI * 3f) * 0.4f;
                if (_outline != null)
                {
                    _outline.effectColor = new Color(1f, 1f, 1f, alpha);
                }
                yield return null;
            }
        }

        // ---- タップ ----

        public void OnPointerClick()
        {
            OnTapped?.Invoke(this);
        }
    }
}
