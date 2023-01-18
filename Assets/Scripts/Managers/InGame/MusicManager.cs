using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using DG.Tweening;


public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    
    private GameManager gameManager;

    public double songPosition;
    public double smoothSongPosition;
    private double songStartTime;
    public double dspTimeSong;
    public double fixedRealTimeSinceStartupAsDouble;
    public bool didAudioOutputDeviceChange = false;

    private float songStartDelay = 2;

    private double pauseDspTime = 0;
    private double pauseRealTime = 0;
    
    private bool isVideoEnabled = false;
    private bool videoStarted = false;
    private float videoPlayAt;

    public AudioSource music;

    private float syncAdjustValue = 0, additiveSync = 0;
    private Song song;
    private UnityEngine.Video.VideoPlayer video;

    public void setMusic(Song song, string videoAddress = "")
    {
        this.song = song;
        music = GetComponent<AudioSource>();
        music.ignoreListenerPause = true;
        
        // Start loading music asynchronously
        StartCoroutine(loadMusic());

        // Set sync value
        if(!gameManager.isAdjustSync){
            // Add all sync adjustments
            syncAdjustValue = additiveSync + Data.saveData.audioSyncProfiles[Data.saveData.audioSyncSelected];
            if(Data.saveData.songMusicSync.ContainsKey(this.song.id)){
                syncAdjustValue += Data.saveData.songMusicSync[this.song.id];
            }
        }

        songStartDelay = 2;
        if(videoAddress != ""){
            video = GetComponent<UnityEngine.Video.VideoPlayer>();
            video.errorReceived += delegate {
                UIManager.Instance.loadSceneAsync("SelectMusicScene", delegate {
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("배경 영상 파일이 없거나 손상되었습니다.\n배경 영상을 삭제 후 다시 불러와 주세요.", "The video background file is corrupted or missing.\nPlease delete the video file and import it again.")));
                });
            };
            videoPlayAt = song.videoPlayAt;
            video.url = videoAddress;
            isVideoEnabled = true;
            if(-videoPlayAt > 2) songStartDelay = -videoPlayAt;
            video.Prepare();
        }

        songPosition = -songStartDelay;
    }

    private IEnumerator loadMusic()
    {
        if(this.song.isCustom){
            // mp3 to AudioClip
            if(this.song.musicLoaded()){
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + Application.persistentDataPath + "/music/" + song.id + ".mp3", AudioType.MPEG))
                {
                    yield return request.SendWebRequest();
                    if(request.result == UnityWebRequest.Result.Success){
                        music.clip = DownloadHandlerAudioClip.GetContent(request);
                        music.clip.LoadAudioData();
                    }else{
                        UIManager.Instance.loadSceneAsync("SelectMusicScene", delegate {
                            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("음악 파일을 불러오지 못했습니다.\n잠시 후 다시 시도해 주세요.", "Failed to load music file.\nPlease try again later.")));
                            return;
                        });
                    }
                }
            }else{
                music.clip = Resources.Load<AudioClip>("Audio/silence");
                music.clip.LoadAudioData();
            }
        }else{
            // Built in
            AsyncOperationHandle<AudioClip> loadAudioClipHandle = Addressables.LoadAssetAsync<AudioClip>(this.song.getMusicAddress());
            yield return loadAudioClipHandle;
            if(loadAudioClipHandle.Status == AsyncOperationStatus.Succeeded){
                music.clip = loadAudioClipHandle.Result;
                music.clip.LoadAudioData();
            }else{
                UIManager.Instance.loadSceneAsync("SelectMusicScene", delegate {
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("음악 파일을 불러오지 못했습니다.\n잠시 후 다시 시도해 주세요.", "Failed to load music file.\nPlease try again later.")));
                    return;
                });
            }
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        if(Instance == null) Instance = this;
        else if(Instance == this) Destroy(gameObject);

        
    }

    void OnDestroy()
    {
        if(!this.song.isCustom) Addressables.Release(this.music.clip);
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        
        #if !UNITY_EDITOR
        AudioSettings.OnAudioConfigurationChanged += handleOutputChange;
        #endif
    }
    
    void Update()
    {
        // Debug.Log("Playing: " + music.isPlaying + ", State: " + music.clip.loadState);
        

        if(gameManager.gameState == GameState.Playing){
            // calculate position given dspTime

            songPosition = AudioSettings.dspTime - dspTimeSong - syncAdjustValue;
            fixedRealTimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
            smoothSongPosition = fixedRealTimeSinceStartupAsDouble - songStartTime - syncAdjustValue;

            if(isVideoEnabled && !videoStarted && songPosition + 0.06f > videoPlayAt - syncAdjustValue){
                video.Play();
                videoStarted = true;
            }
            // Debug.Log(Mathf.Abs((float)(smoothSongPosition - songPosition)));

            // audiosync > 0 => 늦게 침, audiosync < 0 => 빨리 침
            // 그러므로 양수일때 늦게 쳤으므로 음악이 더 빨리 나와야함 
            // songPosition = AudioSettings.dspTime - dspTimeSong - 0.5; // 음악이 더 빨리 재생
            //songPosition = AudioSettings.dspTime - dspTimeSong + 2; // 음악이 더 늦게 재생
            // songPosition = AudioSettings.dspTime - dspTimeSong - firstBeatOffset;
            // adjust for the delay between song start and music start

            if(!song.isCustom){
                if(songPosition > music.clip.length + 0.5){
                    // music over
                    music.PlayOneShot(SoundEffects.liveClear);
                    StartCoroutine(gameManager.musicEnded());
                }
            }else{
                if(songPosition > song.musicLength + 2){
                    // music over
                    gameManager.gameState = GameState.Ended;
                    fadeOutAndEnd();
                }
            }
        }
    }

    private void fadeOutAndEnd()
    {
        DOTween.To(val => music.volume = val, 1, 0, 1.2f).OnComplete(() => {
            AudioManager.Instance.playClip(SoundEffects.liveClear); // play using AudioManager since music volume is 0
            StartCoroutine(gameManager.musicEnded());
        });
    }


    void OnDisable()
    {
        AudioSettings.OnAudioConfigurationChanged -= handleOutputChange;
    }

    public void startMusic()
    {
        // videoStarted = false;
        // music.Stop();
        music.volume = Data.saveData.settings_musicVolume;
        music.PlayScheduled(AudioSettings.dspTime + songStartDelay);
        songStartTime = Time.realtimeSinceStartupAsDouble + songStartDelay;
        dspTimeSong = AudioSettings.dspTime + songStartDelay;

        if(PlayerPrefs.HasKey("music_customsync_song_" + this.song.id)){
            additiveSync = PlayerPrefs.GetFloat("music_customsync_song_" + this.song.id);
        }

        #if RHYTHMIZ_TEST
        if(Data.tempData.ContainsKey("additiveSync") && !gameManager.isAdjustSync){
            additiveSync = float.Parse(Data.tempData["additiveSync"], System.Globalization.CultureInfo.InvariantCulture);
        }else{
            additiveSync = 0;
        }
        #endif

        // video.Stop();
        // video.frame = 0;
        // video.targetTexture.Release();
        // video.Prepare();
    }

    public void pauseMusic()
    {
        pauseDspTime = AudioSettings.dspTime;
        pauseRealTime = Time.realtimeSinceStartupAsDouble;
        music.Pause();
        if(isVideoEnabled) video.Pause();
    }

    public void gameRestarted()
    {
        videoStarted = false;
        music.Stop();
        if(video != null){
            video.Stop();
            video.targetTexture.Release();
            video.Prepare();
        }
    }

    public void resumeMusic()
    {
        if(songPosition < 0){
            // song hasn't started yet, reapply song start delay
            
            music.Stop();
            songStartTime = Time.realtimeSinceStartupAsDouble - songPosition;
            dspTimeSong = AudioSettings.dspTime - songPosition;
            music.PlayScheduled(AudioSettings.dspTime - songPosition);
        }else{
            // add the difference: currentTime - pauseTime = time paused
            dspTimeSong += AudioSettings.dspTime - pauseDspTime;
            songStartTime += Time.realtimeSinceStartupAsDouble - pauseRealTime;
            music.UnPause();
        }
        if(video != null) video.Play();
    }

    private void handleOutputChange(bool deviceChanged)
    {
        if(gameManager.gameState == GameState.Playing) gameManager.pauseGame();
        didAudioOutputDeviceChange = true;
        gameManager.audioOutputChanged.SetActive(true);
        dspTimeSong = AudioSettings.dspTime - songPosition;
        songStartTime = Time.realtimeSinceStartupAsDouble - songPosition;
    }

    public bool isMusicLoaded()
    {
        if(music.clip == null) return false;
        bool musicLoaded = music.clip.loadState == AudioDataLoadState.Loaded;
        bool videoLoaded = isVideoEnabled ? video.isPrepared : true;
        return musicLoaded && videoLoaded;
    }

    public void playEffect(AudioClip effect)
    {
        music.PlayOneShot(effect);
    }
}
