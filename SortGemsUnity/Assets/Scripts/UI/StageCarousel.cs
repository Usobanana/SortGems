using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace SortGems.UI
{
    /// <summary>
    /// ステージ選択のスクロールビューをカルーセル形式（左右スライド・スナップ・ボタン制御）にするコンポーネント。
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class StageCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private Button _leftButton;
        [SerializeField] private Button _rightButton;
        [SerializeField] private float _snapSpeed = 10f;

        private ScrollRect _scrollRect;
        private int _pageCount = 0;
        private int _currentPage = 0;
        private bool _isDragging = false;
        private float _targetNormalizedPos = 0f;
        private bool _isInitialized = false;

        public Action<int> OnPageChanged;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            if (_leftButton != null) _leftButton.onClick.AddListener(PrevPage);
            if (_rightButton != null) _rightButton.onClick.AddListener(NextPage);
        }

        public void Initialize(int pageCount, int initialPage, Button leftButton, Button rightButton)
        {
            _pageCount = pageCount;
            _leftButton = leftButton;
            _rightButton = rightButton;
            _currentPage = Mathf.Clamp(initialPage, 0, _pageCount - 1);
            _isInitialized = false;

            if (_leftButton != null)
            {
                _leftButton.onClick.RemoveListener(PrevPage);
                _leftButton.onClick.AddListener(PrevPage);
            }
            if (_rightButton != null)
            {
                _rightButton.onClick.RemoveListener(NextPage);
                _rightButton.onClick.AddListener(NextPage);
            }

            // レイアウト強制再構築（Contentのサイズを即座に計算させ、初期フォーカスのスナップを正常に行う）
            if (_scrollRect != null && _scrollRect.content != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
            }
        }

        private void Update()
        {
            if (_pageCount <= 1) return;

            // Contentの幅が計算されてから初期ページを設定する（遅延初期化）
            if (!_isInitialized && _scrollRect.content != null && _scrollRect.content.rect.width > 0)
            {
                _isInitialized = true;
                SetPage(_currentPage, true);
            }

            if (_isInitialized)
            {
                if (!_isDragging)
                {
                    // スムーズにターゲット位置へスクロール
                    float currentPos = _scrollRect.horizontalNormalizedPosition;
                    if (float.IsNaN(currentPos)) currentPos = 0f;

                    float newPos = Mathf.Lerp(currentPos, _targetNormalizedPos, Time.deltaTime * _snapSpeed);
                    if (!float.IsNaN(newPos))
                    {
                        _scrollRect.horizontalNormalizedPosition = newPos;
                    }

                    // ドラッグ中でなく、十分にターゲットに近づいたら完全に合わせる
                    if (Mathf.Abs(currentPos - _targetNormalizedPos) < 0.0001f)
                    {
                        _scrollRect.horizontalNormalizedPosition = _targetNormalizedPos;
                    }
                }
                else
                {
                    // ドラッグ中も現在の最寄りページを更新して左右ボタンの状態を制御
                    int nearestPage = GetNearestPage();
                    if (nearestPage != _currentPage)
                    {
                        _currentPage = nearestPage;
                        UpdateButtonStates();
                        OnPageChanged?.Invoke(_currentPage);
                    }
                }
            }
        }

        public void SetPage(int pageIndex, bool immediate = false)
        {
            if (_pageCount <= 0) return;
            _currentPage = Mathf.Clamp(pageIndex, 0, _pageCount - 1);
            
            _targetNormalizedPos = _pageCount > 1 ? (float)_currentPage / (_pageCount - 1) : 0f;

            if (immediate)
            {
                _scrollRect.horizontalNormalizedPosition = _targetNormalizedPos;
            }

            UpdateButtonStates();
            OnPageChanged?.Invoke(_currentPage);
        }

        public void NextPage()
        {
            if (_currentPage < _pageCount - 1)
            {
                SetPage(_currentPage + 1);
            }
        }

        public void PrevPage()
        {
            if (_currentPage > 0)
            {
                SetPage(_currentPage - 1);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            // ドラッグ終了時に最寄りのページにスナップ
            int targetPage = GetNearestPage();
            SetPage(targetPage);
        }

        private int GetNearestPage()
        {
            if (_pageCount <= 1) return 0;
            float currentPos = Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition);
            
            // 各ページ位置の中で最も近いインデックスを見つける
            float minDistance = float.MaxValue;
            int nearest = 0;
            for (int i = 0; i < _pageCount; i++)
            {
                float pagePos = (float)i / (_pageCount - 1);
                float dist = Mathf.Abs(currentPos - pagePos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = i;
                }
            }
            return nearest;
        }

        private void UpdateButtonStates()
        {
            if (_leftButton != null) _leftButton.gameObject.SetActive(_currentPage > 0);
            if (_rightButton != null) _rightButton.gameObject.SetActive(_currentPage < _pageCount - 1);
        }
    }
}
