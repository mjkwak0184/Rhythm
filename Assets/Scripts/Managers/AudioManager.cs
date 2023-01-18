using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public static class SoundEffects
    {
        public static AudioClip buttonNormal = Resources.Load("Audio/button_normal.a") as AudioClip;
        public static AudioClip buttonSmall = Resources.Load("Audio/button_small.a") as AudioClip;
        public static AudioClip buttonCancel = Resources.Load("Audio/button_cancel.a") as AudioClip;
        public static AudioClip judgeGood = Resources.Load("Audio/live_good_judgment.a") as AudioClip;
        public static AudioClip judgePerfect = Resources.Load("Audio/live_perfect_judgment.a") as AudioClip;
        public static AudioClip judgeSPerfect = Resources.Load("Audio/live_s_perfect_judgment.a") as AudioClip;
        public static AudioClip liveTap = Resources.Load("Audio/live_tap.a") as AudioClip;
        public static AudioClip liveClear = Resources.Load("Audio/live_clear.a") as AudioClip;
        public static AudioClip gift = Resources.Load("Audio/gift.a") as AudioClip;
        public static AudioClip cardEnter = Resources.Load("Audio/card_slide.a") as AudioClip;
        public static AudioClip stamp = Resources.Load("Audio/stamp_ingame.a") as AudioClip;
        public static AudioClip resumeBeep = Resources.Load("Audio/resume_beep.a") as AudioClip;
        public static AudioClip star = Resources.Load("Audio/star_ingame.a") as AudioClip;
    }

public class AudioManager: MonoBehaviour
{

    public static AudioManager Instance;

    public AudioSource backgroundAudio;
    private List<AudioSource> effectAudio = new List<AudioSource>();
    private int effectAudioIndex = 0;

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);


        GameObject bgmObj = new GameObject();
        bgmObj.name = "BackgroundMusic";
        bgmObj.transform.SetParent(this.transform);
        bgmObj.transform.position = Vector3.zero;
        backgroundAudio = bgmObj.AddComponent<AudioSource>();

        for(int i = 0; i < 10; i++){    // 10 audio sources for sfx
            GameObject sfxObj = new GameObject();
            sfxObj.name = "SoundEffect";
            sfxObj.transform.SetParent(this.transform);
            sfxObj.transform.position = Vector3.zero;
            effectAudio.Add(sfxObj.AddComponent<AudioSource>());
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if(backgroundAudio == null) return;
        if(pauseStatus){
            backgroundAudio.Pause();
        }else{
            backgroundAudio.UnPause();
        }
    }

    public void playMusic(string url, bool loop = false)
    {
        backgroundAudio.clip = Resources.Load(url) as AudioClip;
        backgroundAudio.loop = loop;
        backgroundAudio.Play();
    }

    public void playMusic(AudioClip clip, bool loop = false)
    {
        backgroundAudio.clip = clip;
        backgroundAudio.loop = loop;
        backgroundAudio.Play();
    }

    public void stopMusic()
    {
        backgroundAudio.Stop();
    }

    public void setEffectVolume(float vol){
        for(int i = 0; i < effectAudio.Count; i++){
            effectAudio[i].volume = vol;
        }
    }

    // public void playClip(AudioClip clip)
    // {
    //     effectAudio[0].PlayOneShot(clip);
    // }

    public void playClip(AudioClip clip)
    {
        if(effectAudio[effectAudioIndex].isPlaying){
            effectAudio[effectAudioIndex].Stop();
        }

        effectAudio[effectAudioIndex].clip = clip;
        effectAudio[effectAudioIndex].Play();
        effectAudioIndex++;
        if(effectAudioIndex >= effectAudio.Count) effectAudioIndex = 0;
    }
}