using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading;
using VContainer;
using UniRx;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;

namespace Refactoring
{
    // BGM管理
    public enum BGM_Type
    {
        Title = 10,
        Result = 800,
        SILENCE = 999, // 無音状態
    }

    // SE管理
    public enum SE_Type
    {
        CursorOnButton = 10,
        ButtonClicked1 = 21,
        ButtonClicked2 = 22,
        ButtonClicked3 = 23,
        BackButtonClicked1 = 31,
        Correct = 100,
        Incorrect = 110,
    }

    /// <summary>
    /// 音源管理クラス
    /// </summary>
    public class SoundManager : LocalSingletonMonoBehaviour<SoundManager>
    {
        [System.Serializable]
        class BGMTypeToAudioClip
        {
            public BGM_Type type;
            public AudioClip clip;
        }

        [System.Serializable]
        class SETypeToAudioClip
        {
            public SE_Type type;
            public AudioClip clip;
        }

        // クロスフェード時間
        const int BGM_ARRAY_LENGTH = 2;
        const int SE_ARRAY_LENGTH = 16;

        //フェード関係
        public float BGMFadeInDuration = 0f;
        public float BGMFadeOutDuration = 3f;
        public float BGMCrossFadeDuration = 2f;
        public bool Mute = false;

        // === AudioClip ===
        [SerializeField] BGMTypeToAudioClip[] bgmClips;
        [SerializeField] SETypeToAudioClip[] seClips;

        // === AudioMixer ===
        [SerializeField] AudioMixerGroup audioMixerGroupSE;
        [SerializeField] AudioMixerGroup audioMixerGroupBGM;

        // === AudioSource ===
        AudioSource[] bgmSources = new AudioSource[BGM_ARRAY_LENGTH];
        AudioSource[] seSources = new AudioSource[SE_ARRAY_LENGTH];

        bool isCrossFading;
        CancellationTokenSource cts;

        IVolumeGetter volumeGetter;

        [Inject]
        public void Constructor(IVolumeGetter volumeGetter)
        {
            this.volumeGetter = volumeGetter;
            Bind();
        }

        private new void Awake()
        {
            // BGM用 AudioSource追加
            bgmSources[0] = gameObject.AddComponent<AudioSource>();
            bgmSources[0].outputAudioMixerGroup = audioMixerGroupBGM;
            bgmSources[1] = gameObject.AddComponent<AudioSource>();
            bgmSources[1].outputAudioMixerGroup = audioMixerGroupBGM;

            // SE用 AudioSource追加
            for (int i = 0; i < seSources.Length; i++)
            {
                seSources[i] = gameObject.AddComponent<AudioSource>();
                seSources[i].outputAudioMixerGroup = audioMixerGroupSE;
            }

        }

        void Update()
        {
            // ボリューム設定
            if (!isCrossFading)
            {
                bgmSources[0].volume = 1f;
                bgmSources[1].volume = 1f;
            }

            foreach (AudioSource source in seSources)
            {
                source.volume = 1f;
            }
        }

        private void Bind()
        {
            // bgmVolume → 
            volumeGetter?.BGMVolume
                .Subscribe(OnBGMVolumeChanged)
                .AddTo(this.gameObject);

            // seVolume → 
            volumeGetter?.SEVolume
                .Subscribe(OnSEVolumeChanged)
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// BGM再生
        /// </summary>
        /// <param name="bgmType"></param>
        /// <param name="loopFlg"></param>
        public void PlayBGM(BGM_Type bgmType, bool loopFlg = true, bool isFadeout = true)
        {
            // BGMなしの状態にする場合            
            if (bgmType == BGM_Type.SILENCE)
            {
                StopBGM(isFadeout);
                return;
            }

            PlayBGM(GetBGMClip(bgmType));
        }

        public void PlayBGM(AudioClip audioClip, bool loopFlg = true, bool isFadeout = true)
        {
            // 同じBGMの場合は何もしない
            if (bgmSources[0].clip != null && bgmSources[0].clip == audioClip) { return; }
            else if (bgmSources[1].clip != null && bgmSources[1].clip == audioClip) { return; }

            // フェードでBGM開始
            if (bgmSources[0].clip == null && bgmSources[1].clip == null)
            {
                //フェードイン処理
                cts = new CancellationTokenSource();
                FadeIn(audioClip, loopFlg, cts.Token).Forget();
            }
            else
            {
                // クロスフェード処理
                cts = new CancellationTokenSource();
                CrossFade(audioClip, loopFlg, cts.Token).Forget();
            }
        }

        /// <summary>
        /// クロスフェード
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="loopFlg"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTaskVoid CrossFade(AudioClip clip, bool loopFlg, CancellationToken token)
        {
            isCrossFading = true;
            AudioSource sourceFadeIn = bgmSources[0].clip != null ? bgmSources[1] : bgmSources[0];
            AudioSource sourceFadeOut = bgmSources[0].clip != null ? bgmSources[0] : bgmSources[1];

            //新しく再生する方の初期化
            sourceFadeIn.volume = 0;
            sourceFadeIn.clip = clip;
            sourceFadeIn.loop = loopFlg;
            sourceFadeIn.Play();
            sourceFadeIn.DOFade(1.0f, BGMCrossFadeDuration).SetEase(Ease.Linear);
            sourceFadeOut.DOFade(0, BGMCrossFadeDuration).SetEase(Ease.Linear);

            await UniTask.Delay((int)(BGMCrossFadeDuration * 1000), false, PlayerLoopTiming.Update, token);
            sourceFadeOut.Stop();
            sourceFadeOut.clip = null;
        }

        private async UniTaskVoid FadeIn(AudioClip clip, bool loopFlg, CancellationToken token)
        {
            bgmSources[0].volume = 0;
            bgmSources[0].loop = loopFlg;
            bgmSources[0].clip = clip;
            bgmSources[0].Play();
            bgmSources[0].DOFade(1.0f, BGMFadeInDuration).SetEase(Ease.Linear);
            await UniTask.Delay((int)(BGMFadeInDuration * 1000), false, PlayerLoopTiming.Update, token);
        }

        private async UniTaskVoid FadeOut(CancellationToken token)
        {
            bgmSources[0].DOFade(0f, BGMFadeOutDuration).SetEase(Ease.Linear);
            bgmSources[1].DOFade(0f, BGMFadeOutDuration).SetEase(Ease.Linear);
            await UniTask.Delay((int)(BGMFadeOutDuration * 1000), false, PlayerLoopTiming.Update, token);

            bgmSources[0].Stop();
            bgmSources[1].Stop();
            bgmSources[0].clip = null;
            bgmSources[1].clip = null;
        }

        /// <summary>
        /// BGM完全停止
        /// </summary>
        public void StopBGM(bool isFade)
        {
            if (isFade)
            {
                cts = new CancellationTokenSource();
                FadeOut(cts.Token).Forget();
            }
            else
            {
                bgmSources[0].Stop();
                bgmSources[1].Stop();
                bgmSources[0].clip = null;
                bgmSources[1].clip = null;
            }
        }

        /// <summary>
        /// SE再生
        /// </summary>
        /// <param name="seType"></param>
        public void PlaySE(SE_Type seType)
        {
            PlaySE(GetSEClip(seType));
        }

        public void PlaySE(AudioClip setClip)
        {
            // 再生中ではないAudioSourceをつかってSEを鳴らす
            foreach (AudioSource source in seSources)
            {
                // 再生中の AudioSource の場合には次のループ処理へ移る
                if (source.isPlaying) { continue; }

                // 再生中でない AudioSource に Clip をセットして SE を鳴らす
                source.clip = setClip;
                source.Play();
                break;
            }
        }

        /// <summary>
        /// SE停止
        /// </summary>
        public void StopSE()
        {
            // 全てのSE用のAudioSourceを停止する
            foreach (AudioSource source in seSources)
            {
                source.Stop();
                source.clip = null;
            }
        }

        /// <summary>
        /// BGM一時停止
        /// </summary>
        public void PauseBGM()
        {
            bgmSources[0].Pause();
            bgmSources[1].Pause();
        }

        /// <summary>
        /// 一時停止した同じBGMを再生(再開)
        /// </summary>
        public void ResumeBGM()
        {
            bgmSources[0].Play();
            bgmSources[1].Play();
        }

        private AudioClip GetBGMClip(BGM_Type type)
        {
            foreach (BGMTypeToAudioClip b in bgmClips)
            {
                if (b.type == type) { return b.clip; }
            }
            return null;
        }

        private AudioClip GetSEClip(SE_Type type)
        {
            foreach (SETypeToAudioClip s in seClips)
            {
                if (s.type == type) { return s.clip; }
            }
            return null;
        }

        /// <summary>
        /// AudioMixer設定
        /// </summary>
        /// <param name="vol"></param>
        private void OnSEVolumeChanged(float vol)
        {
            var volume = Mathf.Clamp(Mathf.Log10(vol) * 20f, -80f, 0f);
            audioMixerGroupSE.audioMixer.SetFloat("Volume_SE", volume);
        }

        private void OnBGMVolumeChanged(float vol)
        {
            var volume = Mathf.Clamp(Mathf.Log10(vol) * 20f, -80f, 0f);
            audioMixerGroupBGM.audioMixer.SetFloat("Volume_BGM", volume);
        }

        private void OnDestroy()
        {
            // キャンセルしてリソースを解放
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}