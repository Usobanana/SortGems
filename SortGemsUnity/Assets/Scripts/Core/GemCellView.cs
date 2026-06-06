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
        public System.Action<GemCellView, UnityEngine.EventSystems.PointerEventData> OnTappedWithEvent;
        public int Row { get; private set; }
        public int Col { get; private set; }
        public bool IsPalette { get; private set; }
        public GemColor GemColor => _gemColor;

        // goalColor 表示用（薄い下地色）
        private GemColor _goalColor;
        private Outline _outline;
        private Outline _selectionOutline;
        private Shadow _gemShadow;
        private Coroutine _blinkCoroutine;
        private Coroutine _selectionBlinkCoroutine;
        private Coroutine _floatCoroutine;
        private Vector3 _gemImageBasePos = Vector3.zero;

        private void Awake()
        {
            _baseScale = transform.localScale;
            GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(OnPointerClick);

            // 角丸四角形のスプライトを適用し、ジェムを一回り小さく設定
            if (_gemImage != null)
            {
                _gemImageBasePos = _gemImage.transform.localPosition;
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

                _selectionOutline = _gemImage.gameObject.AddComponent<Outline>();
                _selectionOutline.effectColor = new Color(1f, 1f, 1f, 0f);
                _selectionOutline.effectDistance = new Vector2(1.5f, 1.5f);
                _selectionOutline.enabled = false;
            }

            if (_socketImage != null)
            {
                // アセット参照の欠落を防ぐため、実行時に角丸スプライトを割り当てる
                _socketImage.sprite = GemColorPalette.RoundedRectSprite;
                _socketImage.transform.localRotation = Quaternion.identity;
                _socketImage.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                _socketImage.color = new Color(0.0f, 0.0f, 0.0f, 0.55f);
            }

            if (_backgroundImage != null)
            {
                // _backgroundImage.sprite は元の正方形（None）のままとする
                _outline = _backgroundImage.gameObject.AddComponent<Outline>();
                _outline.effectColor = new Color(1f, 1f, 1f, 0f);
                _outline.effectDistance = new Vector2(2f, 2f);
                _outline.enabled = false;
            }

            // 実行時に EventTrigger のコールバックを PointerEventData を渡せるようにラムダ式で再バインド
            var trigger = GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            trigger.triggers.Clear();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entry.callback.AddListener((eventData) => {
                OnPointerClick(eventData);
            });
            trigger.triggers.Add(entry);
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
                if (_socketImage != null) _socketImage.gameObject.SetActive(false);
                if (_completedMark != null) _completedMark.SetActive(false);
                if (_outline != null) _outline.enabled = false;
                if (button != null) button.enabled = false;
                return;
            }

            if (button != null) button.enabled = true;

            bool isEmpty = color == GemColor.None;
            bool isCorrect = (color != GemColor.None && color == goalColor);

            // くぼみ（ソケット）の表示制御: ジェムがないかつVoidでない場合はくぼみを表示、ある場合は非表示
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
                if (isCorrect)
                {
                    _backgroundImage.color = GemColorPalette.GetColor(color);
                }
                else if (goalColor != GemColor.None)
                {
                    _backgroundImage.color = GemColorPalette.GetColor(goalColor);
                }
                else if (isEmpty)
                {
                    _backgroundImage.color = _emptyColor;
                }
                else
                {
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

            // ジェムの Y 軸浮き上がり演出
            if (_gemImage != null)
            {
                if (_floatCoroutine != null) StopCoroutine(_floatCoroutine);
                float targetY = selected ? 12f : 0f;
                _floatCoroutine = StartCoroutine(FloatGemRoutine(targetY, 0.12f));
            }

            // 選択枠の細線・高速明滅演出
            if (selected)
            {
                if (_selectionBlinkCoroutine != null) StopCoroutine(_selectionBlinkCoroutine);
                _selectionBlinkCoroutine = StartCoroutine(SelectionBlinkRoutine());
            }
            else
            {
                if (_selectionBlinkCoroutine != null)
                {
                    StopCoroutine(_selectionBlinkCoroutine);
                    _selectionBlinkCoroutine = null;
                }
                if (_selectionOutline != null)
                {
                    _selectionOutline.enabled = false;
                    _selectionOutline.effectColor = new Color(1f, 1f, 1f, 0f);
                }
            }
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

            // 演出終了後に完了マークを非表示に戻す
            if (_completedMark != null)
            {
                _completedMark.SetActive(false);
            }
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
            // 選択時も背景色を白ベタ塗りにせず、元の目標色・背景色を維持する
            bool isEmpty = _gemColor == GemColor.None;
            bool isCorrect = (_gemColor != GemColor.None && _gemColor == _goalColor);

            if (isCorrect)
            {
                _backgroundImage.color = GemColorPalette.GetColor(_gemColor);
            }
            else if (_goalColor != GemColor.None)
            {
                _backgroundImage.color = GemColorPalette.GetColor(_goalColor);
            }
            else
            {
                _backgroundImage.color = isEmpty
                    ? _emptyColor
                    : new Color(0.18f, 0.18f, 0.22f, 1f);
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

        private IEnumerator FloatGemRoutine(float targetY, float duration)
        {
            Vector3 startPos = _gemImage.transform.localPosition;
            Vector3 endPos = new Vector3(_gemImageBasePos.x, _gemImageBasePos.y + targetY, _gemImageBasePos.z);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * (2f - t); // EaseOutQuad
                _gemImage.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            _gemImage.transform.localPosition = endPos;
        }

        private IEnumerator SelectionBlinkRoutine()
        {
            if (_selectionOutline == null) yield break;
            _selectionOutline.enabled = true;
            _selectionOutline.effectDistance = new Vector2(1.5f, 1.5f); // 細めの白枠

            float elapsed = 0f;
            while (_isSelected)
            {
                elapsed += Time.deltaTime;
                // 白枠を高速めで明滅（アルファ0.35〜1.0の間）
                float alpha = 0.65f + Mathf.Sin(elapsed * Mathf.PI * 4f) * 0.35f;
                _selectionOutline.effectColor = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }

        // ---- タップ ----

        public void OnPointerClick()
        {
            OnTapped?.Invoke(this);
        }

        public void OnPointerClick(UnityEngine.EventSystems.BaseEventData eventData)
        {
            var pointerData = eventData as UnityEngine.EventSystems.PointerEventData;
            if (pointerData != null)
            {
                OnTappedWithEvent?.Invoke(this, pointerData);
            }
            OnTapped?.Invoke(this);
        }
    }
}
