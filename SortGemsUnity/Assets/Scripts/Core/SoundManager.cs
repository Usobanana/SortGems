// SoundManager.cs — アセット不要で短いクリック電子音を自動合成・再生する ＋ BGM連続クロスフェード再生
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortGems.Core
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("SE")]
        private AudioSource _seSource;
        private AudioClip _selectClip;
        private AudioClip _placeClip;

        [Header("BGM Settings")]
        [SerializeField] private List<AudioClip> _bgmClips = new();
        [SerializeField] private float _fadeDuration = 3.0f; // フェードにかける時間
        [SerializeField] private float _bgmVolume = 0.5f;

        private AudioSource _bgmSourceActive;
        private AudioSource _bgmSourceFade;
        private int _currentClipIndex = -1;
        private List<int> _playlist = new();
        private bool _isTransitioning = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _seSource = gameObject.AddComponent<AudioSource>();
            _seSource.playOnAwake = false;

            // 電子音をサンプルベースで動的に波形合成する
            _selectClip = GenerateSynthSound(frequency: 880f, duration: 0.05f, decay: 150f); // 高めの「ピッ」という選択音
            _placeClip  = GenerateSynthSound(frequency: 440f, duration: 0.03f, decay: 200f); // 低めで短い「カチッ」というハマり音

            // BGM用のAudioSourceを2つ追加
            _bgmSourceActive = gameObject.AddComponent<AudioSource>();
            _bgmSourceActive.playOnAwake = false;
            _bgmSourceActive.loop = false; // クロスフェードさせるためループはOFFにする

            _bgmSourceFade = gameObject.AddComponent<AudioSource>();
            _bgmSourceFade.playOnAwake = false;
            _bgmSourceFade.loop = false;
        }

        private void Start()
        {
            if (_bgmClips.Count > 0)
            {
                GeneratePlaylist();
                PlayNextBGM();
            }
        }

        public void PlaySelect()
        {
            if (_seSource != null && _selectClip != null)
            {
                _seSource.PlayOneShot(_selectClip, 0.35f);
            }
        }

        public void PlayPlace()
        {
            if (_seSource != null && _placeClip != null)
            {
                _seSource.PlayOneShot(_placeClip, 0.55f);
            }
        }

        // ---- BGM 再生制御 ----

        private void GeneratePlaylist()
        {
            _playlist.Clear();
            for (int i = 0; i < _bgmClips.Count; i++) _playlist.Add(i);
            // シャッフル
            for (int i = 0; i < _playlist.Count; i++)
            {
                int temp = _playlist[i];
                int randomIndex = Random.Range(i, _playlist.Count);
                _playlist[i] = _playlist[randomIndex];
                _playlist[randomIndex] = temp;
            }
            _currentClipIndex = 0;
        }

        private void PlayNextBGM()
        {
            if (_bgmClips.Count == 0) return;

            if (_currentClipIndex >= _playlist.Count)
            {
                GeneratePlaylist();
            }

            int clipIdx = _playlist[_currentClipIndex++];
            AudioClip clip = _bgmClips[clipIdx];

            // フェード中（または未使用）のSourceに次の曲をロードして再生開始
            AudioSource nextSource = (_bgmSourceActive.isPlaying) ? _bgmSourceFade : _bgmSourceActive;
            AudioSource currentSource = (nextSource == _bgmSourceActive) ? _bgmSourceFade : _bgmSourceActive;

            nextSource.clip = clip;
            nextSource.volume = 0f;
            nextSource.Play();

            // クロスフェードコルーチン開始
            StartCoroutine(CrossFadeRoutine(currentSource, nextSource, clip.length));
        }

        private IEnumerator CrossFadeRoutine(AudioSource fromSource, AudioSource toSource, float nextClipLength)
        {
            _isTransitioning = true;
            
            // ビジュアライザーが音量の大きい方にサンプリング元をスムーズに移行させるための順序切り替え
            _bgmSourceActive = toSource;
            _bgmSourceFade = fromSource;

            float elapsed = 0f;
            float startVolFrom = fromSource.isPlaying ? fromSource.volume : 0f;

            // フェードイン・アウト
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _fadeDuration;

                if (fromSource.isPlaying)
                {
                    fromSource.volume = Mathf.Lerp(startVolFrom, 0f, t);
                }
                toSource.volume = Mathf.Lerp(0f, _bgmVolume, t);
                yield return null;
            }

            if (fromSource.isPlaying)
            {
                fromSource.Stop();
            }
            toSource.volume = _bgmVolume;
            _isTransitioning = false;

            // 次の曲への遷移を監視するコルーチンを起動 (曲の残り時間が _fadeDuration 秒になったら次の曲を流す)
            float waitTime = nextClipLength - _fadeDuration;
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
                if (!_isTransitioning)
                {
                    PlayNextBGM();
                }
            }
            else
            {
                yield return new WaitForSeconds(nextClipLength);
                if (!_isTransitioning)
                {
                    PlayNextBGM();
                }
            }
        }

        /// <summary>
        /// 現在アクティブに再生中のBGM AudioSourceを返す。
        /// クロスフェード中は音量の大きい方をアクティブとみなして返すことでビジュアライザーのカクつきを防ぎます。
        /// </summary>
        public AudioSource GetActiveBGMSource()
        {
            if (_bgmSourceFade != null && _bgmSourceFade.isPlaying && _bgmSourceActive != null)
            {
                if (_bgmSourceFade.volume > _bgmSourceActive.volume)
                {
                    return _bgmSourceFade;
                }
            }
            return _bgmSourceActive;
        }

        private AudioClip GenerateSynthSound(float frequency, float duration, float decay)
        {
            int samplerate = 44100;
            int sampleCount = (int)(samplerate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / samplerate;
                // サイン波に、時間経過で急激に減衰する指数エンベロープを適用して「クリック音」にする
                float envelope = Mathf.Exp(-t * decay);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }

            AudioClip clip = AudioClip.Create("SynthSound", sampleCount, 1, samplerate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
