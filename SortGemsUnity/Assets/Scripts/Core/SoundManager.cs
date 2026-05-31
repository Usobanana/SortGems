// SoundManager.cs — アセット不要で短いクリック電子音を自動合成・再生する
using UnityEngine;

namespace SortGems.Core
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource _audioSource;
        private AudioClip _selectClip;
        private AudioClip _placeClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            // 電子音をサンプルベースで動的に波形合成する
            _selectClip = GenerateSynthSound(frequency: 880f, duration: 0.05f, decay: 150f); // 高めの「ピッ」という選択音
            _placeClip  = GenerateSynthSound(frequency: 440f, duration: 0.03f, decay: 200f); // 低めで短い「カチッ」というハマり音
        }

        public void PlaySelect()
        {
            if (_audioSource != null && _selectClip != null)
            {
                _audioSource.PlayOneShot(_selectClip, 0.35f);
            }
        }

        public void PlayPlace()
        {
            if (_audioSource != null && _placeClip != null)
            {
                _audioSource.PlayOneShot(_placeClip, 0.55f);
            }
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
