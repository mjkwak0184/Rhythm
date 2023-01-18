using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
#if UNITY_IOS
using CoreHapticsUnity;
#elif UNITY_ANDROID && !UNITY_EDITOR
using Haptics;
#endif

public class SettingsManager : MonoBehaviour
{
    [SerializeField]
    private GameObject debugMenu;
    [SerializeField]
    private GameObject[] settingsChildViews, settingsListItems;
    [SerializeField]
    private TextMeshProUGUI versionText;

    // DISPLAY
    [SerializeField]
    private TextMeshProUGUI language, noteSize, hideSkillAnimation, screenResolution;

    // AUDIO 
    [SerializeField]
    private Slider[] volumeSliders;
    [SerializeField]
    private TextMeshProUGUI[] volumePercents;
    [SerializeField]
    private TextMeshProUGUI ingameSoundEffect;
    

    // BACKGROUND
    [SerializeField]
    private TextMeshProUGUI zoomBackgroundVideo, backgroundImageMode;


    // AUDIO / VIDEO SYNC
    [SerializeField]
    private TextMeshProUGUI audioSyncProfileText, audioSyncStatusText, videoSyncStatusText;

    // MISC
    [SerializeField]
    private GameObject appIconChangeOption, getSupportViewPrefab;
    [SerializeField]
    private Transform appIconChangeGridParent;

    // LABS
    [SerializeField]
    private TextMeshProUGUI highRefreshRate, hapticFeedback;


    // DEBUG
    [SerializeField]
    private GameObject ingameProfilerPrefab;

    [SerializeField]
    private Sprite[] appIconOptions = new Sprite[10];

    public static Dictionary<int, int> screenResolutionTable = new Dictionary<int, int>{
        {0, 480}, {1, 600}, {2, 720}, {3, 900}
    };

    public static int getRecommendedResolution()
    {
        int screenHeight = Screen.height;
        if(screenHeight > 1440){
            return 3;
        }else if(screenHeight >= 1080){
            return 2;
        }else if(screenHeight > 720){
            return 1;
        }else{
            return 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(Data.saveData == null) Data.saveData = new SaveData();
        #endif

        #if RHYTHMIZ_TEST
        debugMenu.SetActive(true);
        versionText.text = "Ver: " + Application.version + " " + Data.ccdEnvironment + " (테스트)";
        #else
        debugMenu.SetActive(false);
        versionText.text = "Ver: " + Application.version;
        #endif
        
        if(UIManager.Instance.previousSceneName != "SettingsScene") AudioManager.Instance.playMusic("Audio/sound_my_room.a", true);
        
        Application.targetFrameRate = 60;
        UIManager.Instance.SetRenderFrameInterval(4);
        switchView(-1, false);

        // iOS - Show Dynamic Icons
        #if UNITY_IOS
        if(AppIconChanger.iOS.SupportsAlternateIcons){
            appIconChangeOption.SetActive(true);
            
            // Add button to revert back to original icon
            GameObject albumBtn = new GameObject("AlternateIconButton", typeof(RectTransform), typeof(Image), typeof(Button));
            albumBtn.GetComponent<Image>().sprite = appIconOptions[0];
            Button btn = albumBtn.GetComponent<Button>();
            btn.onClick.AddListener(delegate { AppIconChanger.iOS.SetAlternateIconName(null); });
            btn.transition = Selectable.Transition.None;
            albumBtn.transform.SetParent(appIconChangeGridParent);
            albumBtn.transform.localScale = Vector2.one;

            for(int i = 1; i <= 9; i++){
                int iconId = i;
                albumBtn = new GameObject("AlternateIconButton", typeof(RectTransform), typeof(Image), typeof(Button));
                albumBtn.GetComponent<Image>().sprite = appIconOptions[i];
                btn = albumBtn.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(delegate { misc_changeAppIcon(iconId); });
                albumBtn.transform.SetParent(appIconChangeGridParent);
                albumBtn.transform.localScale = Vector2.one;
            }
        }
        #endif
    }
    

    public void backButtonTapped()
    {
        UIManager.Instance.toggleLoadingScreen(true);
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        UIManager.Instance.loadSceneAsync("LobbyScene");
    }

    /*
    0:  Display
    1:  Sound
    2:  Background
    3:  Sync
    4:  Miscellaneous
    5:  Labs
    6:  Debug
    7:  Notes

    */

    public void switchView(int id){ switchView(id, true); }
    public void switchView(int id, bool playTapAudio)
    {
        if(playTapAudio) AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Data.saveData.lastSelectedSettingsView == id) return;
        #if !RHYTHMIZ_TEST
        if(id == 6) id = 0; // if ID is display, exit
        #endif
        if(id == -1) id = Data.saveData.lastSelectedSettingsView;
        if(id < settingsChildViews.Length) settingsChildViews[Data.saveData.lastSelectedSettingsView].SetActive(false);
        settingsListItems[Data.saveData.lastSelectedSettingsView].GetComponent<Image>().color = Color.clear;
        
        switch (id)
        {
            case 0: // DISPLAY
                // Note size
                if(Data.saveData.settings_ingameNoteSize == 0) noteSize.text = new LocalizedText("작게", "Small").text;
                else if(Data.saveData.settings_ingameNoteSize == 1) noteSize.text = new LocalizedText("보통", "Normal").text;
                else if(Data.saveData.settings_ingameNoteSize == 2) noteSize.text = new LocalizedText("크게", "Large").text;
                hideSkillAnimation.text = Data.saveData.settings_ingameHideSkillActivate ? "ON" : "OFF";
                screenResolution.text = screenResolutionTable[Data.saveData.settings_ingameScreenResolution] + "p" + (Data.saveData.settings_ingameScreenResolution == getRecommendedResolution() ? new LocalizedText(" (추천)", " (Recomm.)").text : "");

                if(Data.saveData.settings_language == 0) language.text = new LocalizedText("기본값", "System Default").text;
                else if(Data.saveData.settings_language == 1) language.text = "한국어";
                else if(Data.saveData.settings_language == 2) language.text = "English";
                break;
            case 1: // AUDIO
                
                //  Music/Effect volume slider
                volumeSliders[0].value = Data.saveData.settings_backgroundVolume;
                volumeSliders[1].value = Data.saveData.settings_musicVolume;
                volumeSliders[2].value = Data.saveData.settings_effectVolume;
                audio_sliderMoved(0); audio_sliderMoved(1); audio_sliderMoved(2);         // Update percent UI
                // Ingame Judge sound effect
                ingameSoundEffect.text = Data.saveData.settings_ingameSoundEffect ? "ON" : "OFF";
                break;
            case 2:     // BACKGROUND

                // zoom background video
                zoomBackgroundVideo.text = Data.saveData.settings_videoBackgroundZoom ? "ON" : "OFF";
                // Image background mode
                switch(Data.saveData.settings_backgroundImageMode){
                    case 0:
                        backgroundImageMode.text = new LocalizedText("기본", "Default").text;
                        break;
                    case 1:
                        backgroundImageMode.text = new LocalizedText("SS모드", "SS Mode").text;
                        break;
                    case 2:
                        backgroundImageMode.text = new LocalizedText("없음", "None").text;
                        break;
                    case 3:
                        backgroundImageMode.text = new LocalizedText("커스텀", "Custom").text;
                        break;
                }
                // Recommended image background size
                break;
            case 3:     // AUDIO/VIDEO SYNC
                sync_updateAudioSyncText();
                break;
            case 4:     // MISCELLANEOUS
                
                break;
            case 5:     // LABS
                // High Refresh Rate
                highRefreshRate.text = Data.saveData.settings_highRefreshRate ? "ON" : "OFF";
                // Haptic Feedback
                hapticFeedback.text = Data.saveData.settings_hapticFeedbackMaster ? "ON" : "OFF";
                #if UNITY_IOS
                if(!CoreHapticsUnityProxy.IsSupported) hapticFeedback.text = new LocalizedText("미지원", "Not supported").text;
                #elif UNITY_ANDROID && !UNITY_EDITOR
                if(HapticsAndroid.GetApiLevel() < 29) hapticFeedback.text = new LocalizedText("미지원", "Not supported").text;
                #endif
                break;
            case 6:     // DEBUG
                break;
            default:
                id = 0;
                break;
        }

        settingsChildViews[id].SetActive(true);
        settingsListItems[id].GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
        Data.saveData.lastSelectedSettingsView = id;
        Data.saveSave();
    }

    public void display_language()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(++Data.saveData.settings_language > 2){
            Data.saveData.settings_language = 0;
        }
        Data.saveSave();

        if(Data.saveData.settings_language == 0){
            LocalizationManager.Instance.resetLocaleToDefault();
            language.text = new LocalizedText("기본값", "System Default").text;
        }else if(Data.saveData.settings_language == 1){
            language.text = "한국어";
            LocalizationManager.Instance.setLocale("ko");
        }else if(Data.saveData.settings_language == 2){
            language.text = "English";
            LocalizationManager.Instance.setLocale("en");
        }
        
        // Update texts in same screen
        UIManager.Instance.loadScene("SettingsScene");
    }

    public void display_resolutionUp()
    {
        int screenHeight = Screen.height;
        if(Data.saveData.settings_ingameScreenResolution >= 3) return;  // Max resolution
        if(screenResolutionTable[Data.saveData.settings_ingameScreenResolution + 1] > screenHeight) return;
        Data.saveData.settings_ingameScreenResolution++;
        Data.saveSave();
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        screenResolution.text = screenResolutionTable[Data.saveData.settings_ingameScreenResolution] + "p" + (Data.saveData.settings_ingameScreenResolution == getRecommendedResolution() ? new LocalizedText(" (추천)", " (Recomm.)").text : "");
    }

    public void display_resolutionDown()
    {
        if(--Data.saveData.settings_ingameScreenResolution < 0){
            Data.saveData.settings_ingameScreenResolution = 0;
        }else{
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        }
        Data.saveSave();
        screenResolution.text = screenResolutionTable[Data.saveData.settings_ingameScreenResolution] + "p" + (Data.saveData.settings_ingameScreenResolution == getRecommendedResolution() ? new LocalizedText(" (추천)", " (Recomm.)").text : "");
    }

    public void display_noteSize()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        // add and cycle
        if(++Data.saveData.settings_ingameNoteSize > 2) Data.saveData.settings_ingameNoteSize = 0;
        // Update text
        if(Data.saveData.settings_ingameNoteSize == 0) noteSize.text = new LocalizedText("작게", "Small").text;
        else if(Data.saveData.settings_ingameNoteSize == 1) noteSize.text = new LocalizedText("보통", "Normal").text;
        else if(Data.saveData.settings_ingameNoteSize == 2) noteSize.text = new LocalizedText("크게", "Large").text;
        Data.saveSave();
    }

    public void display_hideSkillAnimation()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_ingameHideSkillActivate = !Data.saveData.settings_ingameHideSkillActivate;
        Data.saveSave();
        hideSkillAnimation.text = Data.saveData.settings_ingameHideSkillActivate ? "ON" : "OFF";
    }

    #region AUDIO

    public void audio_sliderMoved(int id)
    {
        volumePercents[id].text = Mathf.RoundToInt(volumeSliders[id].value * 100) + "%";
        if(id == 0){
            // background volume
            AudioManager.Instance.backgroundAudio.volume = volumeSliders[id].value;
        }
    }

    public void audio_saveSliderValue(int id)
    {
        if(id == 0){
            Data.saveData.settings_backgroundVolume = volumeSliders[id].value;
        }else if(id == 1){
            Data.saveData.settings_musicVolume = volumeSliders[id].value;
        }else if(id == 2){
            Data.saveData.settings_effectVolume = volumeSliders[id].value;
            AudioManager.Instance.setEffectVolume(volumeSliders[id].value);
        }
        Data.saveSave();
    }

    #endregion



    public void audio_ingameSoundEffectTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_ingameSoundEffect = !Data.saveData.settings_ingameSoundEffect;
        Data.saveSave();
        ingameSoundEffect.text = Data.saveData.settings_ingameSoundEffect ? "ON" : "OFF";
    }

    

    public void background_importVideos()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(NativeFilePicker.IsFilePickerBusy()){
            return;
        }
        #if !UNITY_ANDROID && !UNITY_IOS
        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("지원하지 않는 플랫폼입니다.", "Unsupported platform.")));
        return;
        #endif
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("배경영상 불러오기", "Import background video"), body: new LocalizedText("배경 영상을 다운로드 받은 위치로 이동 한 후 영상을 선택하고 [선택] 버튼을 눌러주세요.\n파일 갯수에 따라 불러오는데 시간이 걸릴 수 있습니다.\n\n<확인 버튼을 누르면 파일 선택창이 표시됩니다>", "Select all video files you want to import and tap the 'Select' button.\nIt may take a while depending on the number of video being imported.\n\n<Press OK to select files>"), confirmAction: delegate{
            NativeFilePicker.Permission permission = NativeFilePicker.PickMultipleFiles((path) => {
                if(path != null){
                    List<string> songNames = new List<string>();
                    IEnumerator loadVideos(){
                        for(int i = 0; i < path.Length; i++){
                            // skip frame to load balance
                            yield return null;
                            string fileName = Path.GetFileNameWithoutExtension(path[i]);
                            int songId;
                            if(int.TryParse(fileName, out songId)){
                                string targetPath = Application.persistentDataPath + "/videos1/" + songId + ".mp4";
                                Song song = DataStore.GetSong(songId);
                                if(song == null){
                                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("[" + fileName + ".mp4]\n\n파일명이 곡 번호로 되어있어야 영상을 불러올 수 있습니다.\n파일명이 곡 번호가 아닐 경우 곡 선택창에서 영상을 불러올 수 있습니다.", "[" + fileName + ".mp4]\n\nOnly files with names corresponding to song number can be imported.\nIf the name of this file is not a number, import this video from Song Select screen.")));
                                    continue;
                                }
                                if(File.Exists(targetPath)){
                                    FileInfo fil = new FileInfo(targetPath);
                                    fil.Delete();
                                }
                                using (UnityWebRequest request = UnityWebRequest.Get("file://" + path[i])){
                                    DownloadHandlerFile dh = new DownloadHandlerFile(targetPath);
                                    dh.removeFileOnAbort = true;
                                    request.downloadHandler = dh;
                                    yield return request.SendWebRequest();
                                    // while(!request.isDone) yield return null;
                                    if(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError ){
                                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("[" + songId + ".mp4]\n\n오류가 발생하여 위 영상 파일을 불러오지 못했습니다.\n다시 시도해 주세요.", "[" + songId + ".mp4]\n\nFailed to import this video file.\nPlease try again.")));
                                        continue;
                                    }
                                    songNames.Add(song.songName);
                                    Data.saveData.backgroundMode[song.id] = 2;
                                }
                            }else{
                                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("[" + fileName + ".mp4]\n\n파일명이 곡 번호로 되어있어야 영상을 불러올 수 있습니다.\n파일명이 곡 번호가 아닐 경우 곡 선택창에서 영상을 불러올 수 있습니다.", "[" + fileName + ".mp4]\n\nOnly files with names corresponding to song number can be imported.\nIf the name of this file is not a number, import this video from Song Select screen.")));
                                continue;
                            }
                        }
                        UIManager.Instance.toggleLoadingScreen(false);
                        Data.saveSave();
                        if(songNames.Count > 0){
                            int iterationCount = (int)Math.Ceiling(songNames.Count / 20f);
                            for(int i = 0; i < iterationCount; i++){
                                int startIndex = i * 20;
                                int endIndex = songNames.Count - startIndex >= 20 ? 20 : songNames.Count - startIndex;
                                Alert.showAlert(new Alert(title: new LocalizedText("완료", "Success").text, body: new LocalizedText("배경 영상을 성공적으로 불러왔습니다. (" + (i+1) + "/" + iterationCount + ")\n\n곡 목록\n", "Successfully imported video files. (" + (i+1) + "/" + iterationCount + ")\n\nSong List\n").text + String.Join(", ", songNames.GetRange(startIndex, endIndex))));
                            }
                        }
                    }
                    StartCoroutine(loadVideos());
                    UIManager.Instance.toggleLoadingScreen(true);
                }
            }, new string[]{ NativeFilePicker.ConvertExtensionToFileType("mp4") });

            if(permission == NativeFilePicker.Permission.Denied){
                #if UNITY_ANDROID
                Alert.showAlert(new Alert(type:Alert.Type.Confirm, title: new LocalizedText("접근 권한 필요", "Need storage permission"), body: new LocalizedText("저장소 접근 권한이 거부되었습니다. 설정 앱에서 저장소 접근을 허용해 주세요.", "Storage permission is needed to launch the file select window.\nPlease allow storage access in phone settings."), confirmText: new LocalizedText("설정 이동", "Open settings"), confirmAction: delegate { UIManager.androidOpenAppSettings(); }));
                #endif
            }
        }));
        
    }

    public void background_zoomBackgroundVideoTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_videoBackgroundZoom = !Data.saveData.settings_videoBackgroundZoom;
        Data.saveSave();
        zoomBackgroundVideo.text = Data.saveData.settings_videoBackgroundZoom ? "ON" : "OFF";
    }



    public void background_cycleImageMode()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        // 0:일반, 1:SS모드, 2:없음, 3:커스텀
        if(++Data.saveData.settings_backgroundImageMode > 3) Data.saveData.settings_backgroundImageMode = 0;
        switch(Data.saveData.settings_backgroundImageMode){
            case 0:
                backgroundImageMode.text = new LocalizedText("기본", "Default").text;
                break;
            case 1:
                backgroundImageMode.text = new LocalizedText("SS모드", "SS Mode").text;
                break;
            case 2:
                backgroundImageMode.text = new LocalizedText("없음", "None").text;
                break;
            case 3:
                backgroundImageMode.text = new LocalizedText("커스텀", "Custom").text;
                break;
        }
        Data.saveSave();
    }

    public void background_importCustomImage()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(NativeFilePicker.IsFilePickerBusy()){
            return;
        }
        #if !UNITY_ANDROID && !UNITY_IOS
        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("지원하지 않는 플랫폼입니다.", "Unsupported platform.")));
        return;
        #endif
        string message;
        #if UNITY_ANDROID
        message = new LocalizedText("게임 내 배경으로 사용할 이미지 파일을 선택하세요.\n\n<확인 버튼을 누르면 파일 선택창이 표시됩니다>", "Select the image file you want to use as background.\n\n<Press OK to proceed>").text;
        #elif UNITY_IOS
        message = new LocalizedText("게임 내 배경으로 사용할 이미지 파일을 선택하세요.\n\n사진 앱에 있는 사진을 불러오려면 사진 앱에서 사진을 선택 후 [파일에 저장]을 눌러 파일 앱으로 이미지 파일을 먼저 가져오세요.\n\n<확인 버튼을 누르면 파일 선택창이 표시됩니다>", "Select the image file you want to use as background.\n\nIf you want to import images from Photos app, press [Save to Files] to first save the image to the Files app.\n\n<Press OK to proceed>").text;
        #endif
        Alert.showAlert(new Alert(Alert.Type.Confirm, title: LocalizedText.Notice.text, body: message, confirmAction: delegate{
            NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) => {
                if(path == null){
                }else{
                    string targetPath = Application.persistentDataPath + "/customBackgroundImage.bytes";
                    if(File.Exists(targetPath)){
                        FileInfo fil = new FileInfo(targetPath);
                        fil.Delete();
                    }
                    IEnumerator loadImage(){
                    using (UnityWebRequest request = UnityWebRequest.Get("file://" + path)){
                            // DownloadHandlerFile dh = new DownloadHandlerFile(targetPath);
                            // dh.removeFileOnAbort = true;
                            // request.downloadHandler = dh;
                            yield return request.SendWebRequest();
                            // while(!request.isDone) yield return null;
                            if(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError ){
                                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하여 이미지를 불러오지 못했습니다.\n잠시 후 다시 시도해 주세요.", "An error occurred while loading the image.\nPlease try again.")));
                            }else{
                                File.WriteAllBytes(targetPath, request.downloadHandler.data);
                                Alert.showAlert(new Alert(title: new LocalizedText("불러오기 완료", "Success"), body: new LocalizedText("이미지를 성공적으로 불러왔습니다.", "Successfully loaded image.")));
                            }
                        }
                    }
                    StartCoroutine(loadImage());
                }
            }, new string[]{ NativeFilePicker.ConvertExtensionToFileType("jpg"), NativeFilePicker.ConvertExtensionToFileType("png") });

            if(permission == NativeFilePicker.Permission.Denied){
                #if UNITY_ANDROID
                Alert.showAlert(new Alert(type:Alert.Type.Confirm, title: new LocalizedText("접근 권한 필요", "Need storage permission"), body: new LocalizedText("저장소 접근 권한이 거부되었습니다. 설정 앱에서 저장소 접근을 허용해 주세요.", "Storage permission is needed to launch the file select window.\nPlease allow storage access in phone settings."), confirmText: new LocalizedText("설정 이동", "Open settings"), confirmAction: delegate { UIManager.androidOpenAppSettings(); }));
                #endif
            }
        }));
    }

    public void background_getVideo()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        #if UNITY_ANDROID
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Notice, body: new LocalizedText("영상을 다운로드 받은 후 [배경 영상 불러오기]를 눌러 불러와 주세요.\n\n<확인 버튼을 누르면 다운로드 링크로 이동합니다>", "Download video files and import them using [Load video background].\n\n<Press OK to proceed>"), confirmAction: delegate{
            Application.OpenURL("https://drive.google.com/drive/folders/17L8p-jTgkX4ftNM0WNfZXES5w7AmDwek?usp=sharing");
        }));
        #elif UNITY_IOS
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, LocalizedText.Notice, body: new LocalizedText("영상을 '나의 iPhone (iPad)' 에 저장한 후\n[배경 영상 불러오기]를 눌러 불러와 주세요.\n\n<확인 버튼을 누르면 다운로드 링크로 이동합니다>", "Download video files to 'On my iPhone (iPad)' and import them using [Load video background].\n\n<Press OK to proceed>"), confirmAction: delegate{
            Application.OpenURL("https://drive.google.com/drive/folders/17L8p-jTgkX4ftNM0WNfZXES5w7AmDwek?usp=sharing");
        }));
        #endif
    }

    public void sync_startAudioSync(string songId)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.toggleLoadingScreen(true);
        AudioManager.Instance.stopMusic();
        Data.tempData["audioSync"] = songId;
        UIManager.Instance.loadSceneAsync("InGameScene");
    }

    public void sync_resetAudioSync()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.audioSyncProfiles[Data.saveData.audioSyncSelected] = 0;
        Data.saveSave();
        sync_updateAudioSyncText();
    }
    
    public void sync_cycleAudioSyncProfile(int add)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        Data.saveData.audioSyncSelected += add;
        if(Data.saveData.audioSyncSelected > 2) Data.saveData.audioSyncSelected = 0;
        if(Data.saveData.audioSyncSelected < 0) Data.saveData.audioSyncSelected = 2;
        Data.saveSave();
        sync_updateAudioSyncText();
    }

    public void sync_updateAudioSyncText()
    {
        audioSyncProfileText.text = new LocalizedText("프로파일 ", "Profile ").text + (Data.saveData.audioSyncSelected+1);
        audioSyncStatusText.text = new LocalizedText("현재 오디오 싱크 : ", "Current music sync : ").text + Data.saveData.audioSyncProfiles[Data.saveData.audioSyncSelected].ToString("0.000") + "s";
        videoSyncStatusText.text = Data.saveData.videoSyncProfiles[Data.saveData.audioSyncSelected].ToString("0.00") + "s";
    }

    public void sync_setAudioSync()
    {
        void updateSync(string s){
            try{
                float sync = Convert.ToSingle(s, System.Globalization.CultureInfo.InvariantCulture);
                if(sync < -1 || sync > 1){
                    Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("싱크 값은 -1초에서 1초 사이로 입력해 주세요.", "Sync value must be between -1 and 1.")));
                    return;
                }
                Data.saveData.audioSyncProfiles[Data.saveData.audioSyncSelected] = sync;
                Data.saveSave();
                audioSyncStatusText.text = new LocalizedText("현재 오디오 싱크 : ", "Current music sync : ").text + Data.saveData.audioSyncProfiles[Data.saveData.audioSyncSelected].ToString("0.000") + "s";
            }catch(FormatException){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("싱크값을 잘못 입력하셨습니다.\n다시 시도해 주세요.", "Invalid sync value.\nPlease try again.")));
                return;
            }catch(OverflowException){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("사용할 수 없는 값을 입력하셨습니다. 다시 시도해 주세요.", "Invalid sync value.\nPlease try again.")));
                return;
            }
        }

        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Input, body: new LocalizedText("오디오 싱크값을 입력해주세요.\n원하는 소숫점 자리수까지 입력 가능합니다.", "Enter a music sync value you want to use."), confirmAction: updateSync));
    }

    public void sync_adjustVideoSync(int add)
    {
        if(add >= 0){
            if(Data.saveData.videoSyncProfiles[Data.saveData.audioSyncSelected] >= 0.09){
                return;
            }
            Data.saveData.videoSyncProfiles[Data.saveData.audioSyncSelected] += 0.01f;
        }else{
            if(Data.saveData.videoSyncProfiles[Data.saveData.audioSyncSelected] <= -0.09){
                return;
            }
            Data.saveData.videoSyncProfiles[Data.saveData.audioSyncSelected] -= 0.01f;
        }
        AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        Data.saveSave();
        videoSyncStatusText.text = Data.saveData.videoSyncProfiles[Data.saveData.audioSyncSelected].ToString("0.00") + "s";
    }

    void misc_changeAppIcon(int id){
        #if UNITY_IOS
        if(!AppIconChanger.iOS.SupportsAlternateIcons) return;
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        AppIconChanger.iOS.SetAlternateIconName("Alt" + id);
        #endif
    }

    public void misc_backupSettings()
    {
        // backup settings to the server
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("데이터 백업", "Data Backup"), body: new LocalizedText("게임 설정 (오디오 싱크, 장착 카드 등)을 서버에 백업하시겠습니까?\n복원하기 버튼을 눌러서 백업한 데이터를 복원할 수 있습니다.", "Do you want to backup your settings data to the server?\nYou can restore your data using the restore button in settings."),
        confirmAction: delegate{
            WWWForm form = new WWWForm();
            form.AddField("userid", Data.accountData.userid);
            form.AddField("password", Data.accountData.password);
            form.AddField("payload", SaveData.toJSON(Data.saveData));

            UIManager.Instance.toggleLoadingScreen(true);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/backup_settings", form, delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if((bool) result["success"]){
                    Alert.showAlert(new Alert(title: new LocalizedText("백업 완료", "Backup Completed"), body: new LocalizedText("설정을 서버에 백업하였습니다.", "Successfully backed up settings data.")));
                }else{
                    if(result.ContainsKey("message")){
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: ((string) result["message"])));
                    }else{
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("데이터 백업에 실패하였습니다. 잠시 후 다시 시도해 주세요.", "Failed to backup data to the server.\nPlease try again later.")));
                    }
                }
            }, 
            delegate(string message){
                UIManager.Instance.toggleLoadingScreen(false);
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 연결에 실패하였습니다. 잠시 후 다시 시도해 주세요.", "Failed to connect to the server. Please try again later.")));
            });
        }));
    }

    public void misc_restoreSettings()
    {
        // restore settings from the server
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("데이터 복원", "Restore Data"), body: new LocalizedText("서버에 백업한 설정를 기기로 복원하겠습니까?\n복원 시 현재 사용중인 설정은 삭제되며 게임이 재실행됩니다.", "Do you want to restore your settings from the server?\nYour current settings will be overwritten with the data from the server.\nThe game will restart after restoration."),
        confirmAction: delegate{
            WWWForm form = new WWWForm();
            form.AddField("userid", Data.accountData.userid);
            form.AddField("password", Data.accountData.password);

            UIManager.Instance.toggleLoadingScreen(true);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/restore_settings", form, delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if((bool) result["success"] && result.ContainsKey("payload")){
                    try
                    {
                        SaveData newSave = SaveData.fromJSON((string) result["payload"]);
                        Data.saveData = newSave;
                        Data.saveSave();
                        Alert.showAlert(new Alert(title: new LocalizedText("복원 완료", "Restore Complete"), body: new LocalizedText("설정이 복원되었습니다.\n게임을 재시작합니다.", "Successfully restored settings data.\nThe game will be restarted."), 
                            confirmAction: delegate{
                                AudioManager.Instance.stopMusic();
                                UIManager.Instance.loadScene("TitleScene");
                            }));
                    }
                    catch (System.Exception)
                    {
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("백업 데이터가 손상되었습니다. 설정을 새로 백업해 주세요.", "Your backup data is corrupted. Please backup again.")));
                    }
                }else{
                    if(result.ContainsKey("message")){
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: ((string) result["message"])));
                    }else{
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("데이터 복원에 실패하였습니다. 잠시 후 다시 시도해 주세요.", "Failed to restore backup data from the server.\nPlease try again later.")));
                    }
                }
            }, 
            delegate(string message){
                UIManager.Instance.toggleLoadingScreen(false);
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 연결에 실패하였습니다. 잠시 후 다시 시도해 주세요.", "Failed to connect to the server. Please try again later.")));
            });
        }));
    }

    public void misc_resetSettings()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("초기화", "Reset"), body: new LocalizedText("게임 설정을 초기 상태로 되돌립니다.\n계정 데이터는 유지됩니다.", "Reset all settings to default.\nYour account data will not be affected."), 
        confirmAction: delegate{
            Data.saveData = new SaveData();
            Data.saveSave();
            LocalizationManager.Instance.resetLocaleToDefault();
            UIManager.Instance.loadSceneAsync("SettingsScene");
        }));
    }

    public void misc_resetGame()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("초기화", "Reset"), body: new LocalizedText("게임 설정 및 불러온 배경 영상과 이미지를 삭제합니다.\n계정 데이터는 그대로 유지됩니다.", "Delete all imported files and settings.\nYour account data will not be affected."), 
        confirmAction: delegate{
            UIManager.Instance.toggleLoadingScreen(true);
            Data.resetAllFile();
            AudioManager.Instance.stopMusic();
            UIManager.Instance.loadScene("TitleScene");
        }));
    }

    public void misc_getSupport()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.InstantiateObj(getSupportViewPrefab);
    }


    public void labs_highRefreshRate()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_highRefreshRate = !Data.saveData.settings_highRefreshRate;
        Data.saveSave();
        highRefreshRate.text = Data.saveData.settings_highRefreshRate ? "ON" : "OFF";
    }


    public void labs_hapticFeedback()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        #if UNITY_IOS
        if(!CoreHapticsUnityProxy.IsSupported){
            Alert.showAlert(new Alert(title: new LocalizedText("미지원 기능", "Unsupported feature"), body: new LocalizedText("햅틱 피드백 기능은 아이폰8 또는 이후 기기에서 지원합니다.", "Haptic feedback is only available on iPhone 8 or later.")));
            return;
        }
        #elif UNITY_ANDROID && !UNITY_EDITOR
        if(HapticsAndroid.GetApiLevel() < 29){
            Alert.showAlert(new Alert(title: new LocalizedText("미지원 기능", "Unsupported feature"), body: new LocalizedText("햅틱 피드백 기능은 안드로이드 10 또는 이후 버전 탑재 기기에서 사용 가능합니다.", "Haptic feedback is only available on Android 10 or later.")));
            return;
        }
        #endif
        Data.saveData.settings_hapticFeedbackMaster = !Data.saveData.settings_hapticFeedbackMaster;
        Data.saveSave();
        hapticFeedback.text = Data.saveData.settings_hapticFeedbackMaster ? "ON" : "OFF";
    }


    // ================================ DEBUG TEST OPTIONS ====================================
    #if RHYTHMIZ_TEST

    public void debug_customServer()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Input, body: "확인코드를 입력해주세요.", 
            confirmAction: delegate(string pw){
                if(pw == "8844"){
                    Alert.showAlert(new Alert(type: Alert.Type.Input, body: "연결할 서버 주소를 입력해주세요.", 
                        confirmAction: delegate(string address){
                            Data.serverURL = address;
                        }));
                }
                
            }));
        
    }

    public void debug_importNotefile()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(NativeFilePicker.IsFilePickerBusy()){
            return;
        }
        #if !UNITY_ANDROID && !UNITY_IOS
        Alert.showAlert(new Alert(title: "오류", body: "지원하지 않는 플랫폼입니다."));
        return;
        #endif

        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) => {
            if(path == null){
            }else{
                WebRequests.Instance.GetRawRequest("file://" + path, delegate(string text){
                    Data.tempData["customNotes"] = text;
                    Alert.showAlert(new Alert(title: "완료", body: "커스텀 채보를 불러왔습니다.\n플레이 할때 불러온 채보가 우선적으로 사용되며, 커스텀 채보 사용을 중단하려면 게임을 재시작해야 합니다."));
                }, delegate(string error){
                    Alert.showAlert(new Alert(title: "오류", body: "파일을 불러오지 못했습니다. 다시 시도해 주세요."));
                });
            }
        }, new string[]{ NativeFilePicker.ConvertExtensionToFileType("txt") });

        if(permission == NativeFilePicker.Permission.Denied){
            #if UNITY_ANDROID
            Alert.showAlert(new Alert(title: "접근 권한 필요", body: "저장소 접근 권한이 거부되었습니다. 설정 앱에서 저장소 접근을 허용해 주세요.", confirmText: "설정 이동", confirmAction: delegate { UIManager.androidOpenAppSettings(); }));
            #endif
        }
    }

    public void debug_clearCustomNotes()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.tempData.Remove("customNotes");
        Alert.showAlert(new Alert(body: "커스텀 노트 사용이 해제되었습니다."));
    }

    public void debug_toggleIngameProfiler()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GameObject profiler = GameObject.Find("GpmProfiler");
        if(profiler == null){
            profiler = Instantiate(ingameProfilerPrefab);
            profiler.name = "GpmProfiler";
        }else Destroy(profiler);
    }

    public void debug_toggleResolution()
    {
        int width = Screen.width, height = Screen.height;
        if(Data.tempData.ContainsKey("LowRes")){
            // high res
            float scaledWidth = ((float)Screen.width / (float)Screen.height) * 1080f;
            Screen.SetResolution((int) scaledWidth, 1080, true);
            Data.tempData.Remove("LowRes");
            Alert.showAlert(new Alert(body: "1080p"));
        }else{
            // 
            float scaledWidth = ((float)Screen.width / (float)Screen.height) * 720f;
            Screen.SetResolution((int) scaledWidth, 720, true);
            Data.tempData["LowRes"] = "Yes";
            Alert.showAlert(new Alert(body: "720p"));
        }
    }

    public void debug_autoplay()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Data.tempData.ContainsKey("FUN")){
            Data.tempData.Remove("FUN");
        }else{
            Data.tempData["FUN"] = "FUN";
        }
        Alert.showAlert(new Alert(body: "자동 플레이 : " + (Data.tempData.ContainsKey("FUN") ? "ON" : "OFF")));
    }

    public void debug_logout()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: "로그아웃", body: "로그아웃 하시겠습니까?", confirmAction: delegate {
            Data.deleteAccount();
            UIManager.Instance.loadScene("TitleScene");
        }));
    }

    public void debug_randomize()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Data.tempData.ContainsKey("random")){
            Data.tempData.Remove("random");
        }else{
            Data.tempData["random"] = "YES";
        }
        Alert.showAlert(new Alert(body: "노트 랜덤화 : " + (Data.tempData.ContainsKey("random") ? "ON" : "OFF")));
    }

    #endif
    // =============================================================
}
