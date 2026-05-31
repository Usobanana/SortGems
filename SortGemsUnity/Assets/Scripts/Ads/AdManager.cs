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
#endif
            Debug.Log("[AdManager] ShowBanner (Mocked)");
        }

        public void HideBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_bannerView != null)
            {
                _bannerView.Hide();
            }
#endif
            Debug.Log("[AdManager] HideBanner (Mocked)");
        }

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
            LoadInterstitial();
#endif
            Debug.Log("[AdManager] ShowInterstitial (Mocked)");
        }

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
    }
}
