// AdManager.cs — Google AdMob 広告管理（シングルトン）
// ※ AdMob SDK for Unity をインポート後に有効化してください
// Unity Package: https://github.com/googleads/googleads-mobile-unity/releases

using System;
using UnityEngine;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

namespace SortGems.Ads
{
    /// <summary>
    /// Google AdMob の初期化・広告ロード・表示を一元管理するシングルトン。
    /// モバイル以外のプラットフォームやエディタ上では、自動的にモック（ダミー広告）として機能します。
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        // ---- AdUnit ID（テスト用ID → リリース前に本番IDに差し替え） ----
        // iOS
        private const string IOS_BANNER_ID        = "ca-app-pub-3940256099942544/2934735716";
        private const string IOS_INTERSTITIAL_ID  = "ca-app-pub-3940256099942544/4411468910";
        private const string IOS_REWARDED_ID      = "ca-app-pub-3940256099942544/1712485313";

        // Android
        private const string AND_BANNER_ID        = "ca-app-pub-3940256099942544/6300978111";
        private const string AND_INTERSTITIAL_ID  = "ca-app-pub-3940256099942544/1033173712";
        private const string AND_REWARDED_ID      = "ca-app-pub-3940256099942544/5224354917";

        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;
#endif

        // ---- 状態 ----
        private bool _isRewardedReady;
        private bool _isInterstitialReady;
        private Action _onRewardEarned;
        private Action _onInterstitialClosed;
#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private GameObject _dummyBannerObj;
        private GameObject _dummyInterstitialObj;
#endif

        // ---- インタースティシャル表示間隔制御 ----
        [SerializeField] private int _interstitialEveryNStages = 3;
        private int _stagesSinceLastInterstitial;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        // ---- 初期化 ----

        private void Initialize()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdManager] AdMob initialized");
                LoadRewarded();
                LoadInterstitial();
            });
#else
            Debug.Log("[AdManager] Non-Mobile platform: AdMob initialization skipped (Mock active)");
            _isRewardedReady = true;
            _isInterstitialReady = true;
#endif
        }

        // ---- バナー広告 ----

        public void ShowBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? IOS_BANNER_ID : AND_BANNER_ID;
            
            if (_bannerView != null)
            {
                _bannerView.Destroy();
            }

            _bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
            _bannerView.LoadAd(new AdRequest());
#else
            Debug.Log("[AdManager] ShowBanner (Mocked)");
            CreateDummyBanner();
            if (_dummyBannerObj != null)
            {
                _dummyBannerObj.transform.SetAsLastSibling();
            }
#endif
        }

        public void HideBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_bannerView != null)
            {
                _bannerView.Hide();
            }
#else
            Debug.Log("[AdManager] HideBanner (Mocked)");
            if (_dummyBannerObj != null)
            {
                _dummyBannerObj.SetActive(false);
            }
#endif
        }

#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private void CreateDummyBanner()
        {
            if (_dummyBannerObj != null)
            {
                _dummyBannerObj.SetActive(true);
                _dummyBannerObj.transform.SetAsLastSibling();
                return;
            }

            // シーン内の Canvas を検索
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            // バナー用のGameObjectを作成
            _dummyBannerObj = new GameObject("DummyBanner", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _dummyBannerObj.transform.SetParent(canvas.transform, false);
            _dummyBannerObj.transform.SetAsLastSibling();

            var rect = _dummyBannerObj.GetComponent<RectTransform>();
            // 画面下部にストレッチ
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 100);

            // 背景色を設定（半透明の濃いグレー）
            var img = _dummyBannerObj.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // テキスト用のGameObjectを作成
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObj.transform.SetParent(_dummyBannerObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var txt = textObj.GetComponent<UnityEngine.UI.Text>();
            txt.text = "[ Google AdMob Banner Ad (Mocked) ]";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
#endif

        // ---- インタースティシャル広告 ----

        private void LoadInterstitial()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
            _isInterstitialReady = false;

            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? IOS_INTERSTITIAL_ID : AND_INTERSTITIAL_ID;
            
            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null) { Debug.LogWarning($"[AdManager] Interstitial load failed: {error}"); return; }
                _interstitialAd = ad;
                _interstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    _onInterstitialClosed?.Invoke();
                    _onInterstitialClosed = null;
                    LoadInterstitial();
                };
                _isInterstitialReady = true;
                Debug.Log("[AdManager] Interstitial loaded successfully");
            });
#endif
        }

        /// <summary>ステージクリア後に呼ぶ。N ステージに1回インタースティシャルを表示。</summary>
        public void OnStageCleared()
        {
            _stagesSinceLastInterstitial++;
            if (_stagesSinceLastInterstitial >= _interstitialEveryNStages)
            {
                ShowInterstitial();
                _stagesSinceLastInterstitial = 0;
            }
        }

        private void ShowInterstitial()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!_isInterstitialReady || _interstitialAd == null)
            {
                Debug.Log("[AdManager] Interstitial not ready");
                return;
            }
            
            _interstitialAd.Show();
            _isInterstitialReady = false;
#endif
            Debug.Log("[AdManager] ShowInterstitial (Mocked)");
        }

        /// <summary>
        /// Next Stage 進級時に呼び出す。
        /// ランダム＋最低5回に1回の確率でインタースティシャル広告を表示し、
        /// 完了時（または非表示時）に onComplete を実行する。
        /// </summary>
        public void ShowInterstitialWithProbability(Action onComplete)
        {
            int steps = PlayerPrefs.GetInt("StepsSinceLastInterstitial", 0);
            steps++;

            bool shouldShow = false;
            if (steps >= 5)
            {
                shouldShow = true;
            }
            else
            {
                // ランダム発生（20% の確率）
                shouldShow = UnityEngine.Random.value < 0.20f;
            }

            if (shouldShow)
            {
                PlayerPrefs.SetInt("StepsSinceLastInterstitial", 0);
                PlayerPrefs.Save();

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                if (_isInterstitialReady && _interstitialAd != null)
                {
                    _onInterstitialClosed = onComplete;
                    _interstitialAd.Show();
                    _isInterstitialReady = false;
                }
                else
                {
                    Debug.Log("[AdManager] Interstitial was selected to show, but it was not ready. Skipping.");
                    onComplete?.Invoke();
                }
#else
                Debug.Log("[AdManager] ShowInterstitial (Mocked)");
                CreateDummyInterstitial(onComplete);
#endif
            }
            else
            {
                PlayerPrefs.SetInt("StepsSinceLastInterstitial", steps);
                PlayerPrefs.Save();
                onComplete?.Invoke();
            }
        }

#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private void CreateDummyInterstitial(Action onComplete)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                onComplete?.Invoke();
                return;
            }

            // 全画面を覆うパネルの作成
            _dummyInterstitialObj = new GameObject("DummyInterstitial", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _dummyInterstitialObj.transform.SetParent(canvas.transform, false);
            _dummyInterstitialObj.transform.SetAsLastSibling();

            var rect = _dummyInterstitialObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var img = _dummyInterstitialObj.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0.95f);

            // 「広告表示中」のテキスト
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObj.transform.SetParent(_dummyInterstitialObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var txt = textObj.GetComponent<UnityEngine.UI.Text>();
            txt.text = "[ Google AdMob Interstitial Ad (Mocked) ]\n\nThis is a full-screen ad simulation.\nClick the Close button to continue.";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            // 「閉じる」ボタンの作成
            GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            closeBtnObj.transform.SetParent(_dummyInterstitialObj.transform, false);

            var btnRect = closeBtnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.3f);
            btnRect.anchorMax = new Vector2(0.5f, 0.3f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(200, 60);

            var btnImg = closeBtnObj.GetComponent<UnityEngine.UI.Image>();
            btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var button = closeBtnObj.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(() =>
            {
                Destroy(_dummyInterstitialObj);
                _dummyInterstitialObj = null;
                onComplete?.Invoke();
            });

            // 閉じるボタンのテキスト
            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            btnTextObj.transform.SetParent(closeBtnObj.transform, false);
            var btnTxtRect = btnTextObj.GetComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;
            btnTxtRect.sizeDelta = Vector2.zero;

            var btnTxt = btnTextObj.GetComponent<UnityEngine.UI.Text>();
            btnTxt.text = "CLOSE AD";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 20;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;
        }
#endif

        // ---- リワード広告 ----

        private void LoadRewarded()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
            _isRewardedReady = false;

            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? IOS_REWARDED_ID : AND_REWARDED_ID;

            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null) { Debug.LogWarning($"[AdManager] Rewarded load failed: {error}"); return; }
                _rewardedAd = ad;
                _rewardedAd.OnAdFullScreenContentClosed += () => { LoadRewarded(); };
                _isRewardedReady = true;
                Debug.Log("[AdManager] Rewarded loaded successfully");
            });
#endif
        }

        /// <summary>
        /// リワード広告を表示する。
        /// 視聴完了で onRewardEarned が呼ばれる。
        /// 広告が準備できていない場合は onFallback（省略可）を呼ぶ。
        /// </summary>
        public void ShowRewardedAd(Action onRewardEarned, Action onFallback = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!_isRewardedReady || _rewardedAd == null)
            {
                Debug.Log("[AdManager] Rewarded not ready, calling fallback");
                onFallback?.Invoke();
                return;
            }

            _onRewardEarned = onRewardEarned;

            _rewardedAd.Show(reward =>
            {
                _onRewardEarned?.Invoke();
                _isRewardedReady = false;
            });
#else
            Debug.Log("[AdManager] ShowRewardedAd (Mocked): Automatically granting reward");
            onRewardEarned?.Invoke();
#endif
        }

        public bool IsRewardedReady => _isRewardedReady;
        public bool IsInterstitialReady => _isInterstitialReady;
    }
}
