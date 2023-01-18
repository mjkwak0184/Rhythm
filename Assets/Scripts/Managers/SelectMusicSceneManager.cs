using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using System.Linq;
using System.IO;
using DG.Tweening;
using TMPro;

public class SelectMusicSceneManager : MonoBehaviour
{
    AudioManager audioManager;
    WebRequests webRequests;
    [SerializeField]
    private Transform mainCanvasTransform;

    [SerializeField]
    private GameObject musicOptionViewPrefab, testObject, liveBackgroundVideoButton, deleteVideoButton, deleteMusicButton, importMusicButton, gameOptionView, startButton;
    [SerializeField]
    private SongListView izoneSongListView, customSongListView;

    [SerializeField]
    private TextMeshProUGUI selectedAlbumName, selectedSongName, worldRecordName, worldRecordScore, myRecordName, myRecordScore, labOptions;
    [SerializeField]
    private Image albumImage, songAttributeImage, liveBackgroundBtnImage, liveBackgroundBtnVideo;
    [SerializeField]
    private CardSelectView cardSelectView;
    // Game Option Texts
    [SerializeField]
    private TextMeshProUGUI flipNotesText, noteSpeedText, maxPowerLimit, ingameLoop, autoSelectDeck;
    
    void Awake()
    {
        #if UNITY_EDITOR
        if(Data.gameData == null) Data.loadGame();
        if(Data.saveData == null) Data.saveData = new SaveData();
        if(Data.userData == null) Data.userData = new UserData();
        if(Data.userData.songclearstar == null) Data.userData.songclearstar = "3333333333333333333333333333333333333";
        // if(Data.saveData == null) Data.loadSave();

        // Addressables.InstantiateAsync("DataStore").WaitForCompletion();
        #endif

        // setup song list view
        izoneSongListView.onSongSelect = delegate(int id){ musicSelect(id); };
        customSongListView.onSongSelect = delegate(int id){ musicSelect(id); };
    }

    // Start is called before the first frame update
    void Start()
    {
        audioManager = AudioManager.Instance;
        audioManager.playMusic("Audio/sound_music_select.a", true);
        Application.targetFrameRate = 60;

        #if RHYTHMIZ_TEST
        testObject.SetActive(true);
        #else
        testObject.SetActive(false);
        #endif

        updateStartButtonLabel();

        #if UNITY_IOS
        UIManager.Instance.SetRenderFrameInterval(4);
        #endif

        musicSelect(Data.saveData.lastSelectedSong);

        myRecordName.text = Data.userData.username;

        // set up song list view
        toggleLeftPanel(Data.saveData.lastSelectedSongListView, false);

        // Show loaded cards
        cardSelectView.onCardTap = delegate { gotoScene("CardEquipScene"); };

        // Set up game options
        if(Data.saveData.settings_flipNotes == 0){
            flipNotesText.text = new LocalizedText("사용 안함", "OFF").text;
        }else if(Data.saveData.settings_flipNotes == 1){
            flipNotesText.text = new LocalizedText("랜덤 적용", "Random").text;
        }else if(Data.saveData.settings_flipNotes == 2){
            flipNotesText.text = new LocalizedText("항상 사용", "Always").text;
        }

        noteSpeedText.text = Data.saveData.settings_noteSpeed == 5 ? new LocalizedText("기본값", "Default").text : "×" + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0");

        if(Data.saveData.settings_maxPower == 0){
            maxPowerLimit.text = new LocalizedText("사용 안함", "OFF").text;
        }else{
            maxPowerLimit.text = Data.saveData.settings_maxPower.ToString("N0");
        }

        ingameLoop.text = Data.saveData.settings_ingameLoop ? "ON" : "OFF";
        autoSelectDeck.text = Data.saveData.settings_autoSelectDeck ? "ON" : "OFF";

        // TEST
        if(Data.tempData.ContainsKey("additiveSync")){
            GameObject.Find("AdditiveSync").GetComponent<TextMeshProUGUI>().text = "추가 싱크값: " + float.Parse(Data.tempData["additiveSync"], System.Globalization.CultureInfo.InvariantCulture).ToString("0.000") + "s";
        }
    }

    public void setLeftPanel(int id)
    {
        Data.saveData.lastSelectedSongListView = id;
        Data.saveData.lastSelectedSongFilter = -1;  // reset album filter
        Data.saveSave();
        toggleLeftPanel(id, true);
    }

    private void toggleLeftPanel(int id, bool playTapAudio)
    {
        if(playTapAudio) AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        izoneSongListView.gameObject.SetActive(Data.saveData.lastSelectedSongListView == 0);
        customSongListView.gameObject.SetActive(Data.saveData.lastSelectedSongListView == 1);
        gameOptionView.SetActive(Data.saveData.lastSelectedSongListView == 2);
    }
    
    public void deleteVideo()
    {
        string targetPath = Application.persistentDataPath + "/videos1/" + Data.saveData.lastSelectedSong + ".mp4";
        if(File.Exists(targetPath)){
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("영상배경 삭제", "Delete video background"), body: new LocalizedText("불러온 영상 배경을 삭제하겠습니까?", "Do you want to delete the video background file?"), confirmAction: delegate{
                FileInfo fil = new FileInfo(targetPath);
                fil.Delete();
                if(Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] == 2){
                    Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] = 1;
                    Data.saveSave();
                }
                deleteVideoButton.SetActive(false);
                // Update button sprites
                liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
                liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
            }));
        }
    }

    public void deleteMusic()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GameObject view = Instantiate(musicOptionViewPrefab);
        view.transform.SetParent(mainCanvasTransform);
        view.transform.localScale = Vector2.one;
        view.GetComponent<MusicOptionView>().onDismiss = delegate {
            bool songLoaded = DataStore.GetSong(Data.saveData.lastSelectedSong).musicLoaded();
            importMusicButton.SetActive(!songLoaded);
            deleteMusicButton.SetActive(songLoaded);
        };        
    }

    public void setLiveBackground(int mode)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        // if mode == 2 (video), check if video has been downloaded
        Song song = DataStore.GetSong(Data.saveData.lastSelectedSong);



        if(mode == 2){
            if(!song.videoAvailable) return;
            if(!song.videoLoaded()){    // video file doesn't exist in target directory
                Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("배경영상 불러오기", "Import video background"), body: new LocalizedText("'" + song.songName + "' (곡 #" + song.id + ")의 배경영상을 불러옵니다.\n곡 배경영상으로 사용할 배경 영상을 선택해 주세요.\n\n(게임 설정에서 배경 영상을 한번에 불러올 수 있습니다.)", "Importing video background file for '" + song.songName + "' (Song #" + song.id + ")\nPlease select the video file to import.\n\n(You can import multiple video files at once in Settings.)"), 
                    confirmAction: delegate {
                        if(NativeFilePicker.IsFilePickerBusy()){
                            Alert.showAlert(new Alert(body: new LocalizedText("파일 선택창을 현재 사용할 수 없습니다.\n잠시 후 다시 시도해 주세요.", "Failed to open file select window.\nPlease try again later.")));
                            return;
                        }
                        #if !UNITY_ANDROID && !UNITY_IOS
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("지원하지 않는 플랫폼입니다.", "Unsupported platform.")));
                        return;
                        #endif

                        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) => {
                            if(path == null){
                            }else{
                                WebRequests.Instance.DownloadFile("file://" + path, Application.persistentDataPath + "/videos1/" + Data.saveData.lastSelectedSong + ".mp4", 
                                    delegate(float progress){
                                        if(progress == -1){
                                            Alert.showAlert(new Alert(title: new LocalizedText("불러오기 오류", "Import error"), body: new LocalizedText("오류가 발생했습니다.\n다시 시도해 주세요.", "An error occurred.\nPlease try again.")));
                                            UIManager.Instance.toggleLoadingScreen(false);
                                        }else if(progress == 1){
                                            Alert.showAlert(new Alert(title: new LocalizedText("불러오기 완료", "Success"), body: new LocalizedText("영상을 성공적으로 불러왔습니다.", "Successfully imported video file.")));
                                            Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] = mode;

                                            // update UI
                                            if(Data.saveData.backgroundMode[song.id] == 2){
                                                // video selected
                                                liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPink;
                                                liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
                                            }else{
                                                liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
                                                liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
                                            }
                                            deleteVideoButton.SetActive(true);

                                            Data.saveSave();
                                            
                                            UIManager.Instance.toggleLoadingScreen(false);
                                        }
                                    });
                            }
                        }, new string[]{ NativeFilePicker.ConvertExtensionToFileType("mp4") });

                        if(permission == NativeFilePicker.Permission.Denied){
                            #if UNITY_ANDROID
                            Alert.showAlert(new Alert(type:Alert.Type.Confirm, title: new LocalizedText("접근 권한 필요", "Need storage permission"), body: new LocalizedText("저장소 접근 권한이 거부되었습니다. 설정 앱에서 저장소 접근을 허용해 주세요.", "Storage permission is needed to launch the file select window.\nPlease allow storage access in phone settings."), confirmText: new LocalizedText("설정 이동", "Open settings"), confirmAction: delegate { UIManager.androidOpenAppSettings(); }));
                            #endif
                        }
                    }));
                return;
            }
        }
        Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] = mode;

        // update UI
        if(Data.saveData.backgroundMode[song.id] == 2){
            // video selected
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPink;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
        }else{
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
        }

        Data.saveSave();
    }

    public void importMusic()
    {
        Song song = DataStore.GetSong(Data.saveData.lastSelectedSong);
        if(song == null) return;
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(song.musicLoaded()){
            Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("불러온 음원 파일이 이미 존재합니다.", "You already imported music file for this song.")));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("음원 불러오기", "Import music"), body: new LocalizedText("'" + song.songName + "' 의 음원을 불러옵니다.\nmp3 음원 파일을 선택해 주세요.\n\n(곡 채보는 멜론 mp3 파일을 기준으로 작성되었습니다. 음원 구매처에 따라 플레이 시 싱크가 맞지 않을 수 있으며, 싱크 조절이 필요한 경우 불러오기를 완료한 후 이 버튼을 다시 눌러주세요.)", "Importing music file for '" + song.songName + "'.\nPlease select an mp3 file for the song.\n\n(Notes are designed using mp3 files purchased from Melon. If you obtained your mp3 file from a different vendor, notes may not be fully in sync with the music. You can adjust sync for this song by tapping this button again after importing the music.)"), 
            confirmAction: delegate {
                if(NativeFilePicker.IsFilePickerBusy()){
                    Alert.showAlert(new Alert(body: new LocalizedText("파일 선택창을 현재 사용할 수 없습니다.\n잠시 후 다시 시도해 주세요.", "Failed to open file select window.\nPlease try again later.")));
                    return;
                }
                #if !UNITY_ANDROID && !UNITY_IOS
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("지원하지 않는 플랫폼입니다.", "Unsupported platform.")));
                return;
                #endif

                NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) => {
                    if(path == null){
                    }else{
                        WebRequests.Instance.DownloadFile("file://" + path, Application.persistentDataPath + "/music/" + Data.saveData.lastSelectedSong + ".mp3", 
                            delegate(float progress){
                                UIManager.Instance.toggleLoadingScreen(false);
                                if(progress == -1){
                                    Alert.showAlert(new Alert(title: new LocalizedText("불러오기 오류", "Import error"), body: new LocalizedText("오류가 발생했습니다.\n다시 시도해 주세요.", "An error occurred.\nPlease try again.")));
                                    
                                }else if(progress == 1){
                                    Alert.showAlert(new Alert(title: new LocalizedText("불러오기 완료", "Success"), body: new LocalizedText("음원을 성공적으로 불러왔습니다.\n\n불러온 음원의 싱크가 맞지 않을 경우 시작 버튼 왼쪽의 음표 버튼을 눌러 싱크를 설정하세요.", "Successfully imported music file.\n\nIf the music is out of sync, you can adjust specific sync values for this song by pressing on the button left to the start button.")));

                                    // update UI
                                    importMusicButton.SetActive(false);
                                    deleteMusicButton.SetActive(true);
                                }
                            });
                    }
                }, new string[]{ NativeFilePicker.ConvertExtensionToFileType("mp3") });

                if(permission == NativeFilePicker.Permission.Denied){
                    #if UNITY_ANDROID
                    Alert.showAlert(new Alert(type:Alert.Type.Confirm, title: new LocalizedText("접근 권한 필요", "Need storage permission"), body: new LocalizedText("저장소 접근 권한이 거부되었습니다. 설정 앱에서 저장소 접근을 허용해 주세요.", "Storage permission is needed to launch the file select window.\nPlease allow storage access in phone settings."), confirmText: new LocalizedText("설정 이동", "Open settings"), confirmAction: delegate { UIManager.androidOpenAppSettings(); }));
                    #endif
                }
            }));
        return;
    }

    public void musicSelect(int songId)
    {
        Song song = DataStore.GetSong(songId);
        if(song == null){
            startButton.GetComponent<Button>().interactable = false;
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("곡 정보가 없습니다.", "Song information not found")));
            return;
        }

        // If deck auto select is on, select deck
        if(Data.saveData.settings_autoSelectDeck){
            cardSelectView.setSelectedDeck(song.attribute);
        }

        Album album = song.album;

        if(Data.saveData.lastSelectedSong != songId){
            // Deselect previous song
            GameObject btn = GameObject.Find("Song" + Data.saveData.lastSelectedSong);
            GameObject.Find("Song" + songId).GetComponent<SongButton>().setSelected(true);
            if(btn != null) btn.GetComponent<SongButton>().setSelected(false);
            // save selection
            Data.saveData.lastSelectedSong = songId;
        }
        // update UI
        selectedSongName.text = song.songName;
        selectedAlbumName.text = album != null ? album.albumName : "";
        if(Data.gameData.worldRecords.ContainsKey(Data.saveData.lastSelectedSong)){
            worldRecordName.text = Data.gameData.worldRecords[Data.saveData.lastSelectedSong].Item1;
            worldRecordScore.text = Data.gameData.worldRecords[Data.saveData.lastSelectedSong].Item2.ToString("N0");
        }else{
            worldRecordName.text = "";
            worldRecordScore.text = new LocalizedText("기록이 없습니다.", "No record").text;
        }
        if(Data.readBitfieldData(Data.userData.scores, 8, Data.saveData.lastSelectedSong) != 0){
            myRecordScore.text = Data.readBitfieldData(Data.userData.scores, 8, Data.saveData.lastSelectedSong).ToString("N0");
        }else myRecordScore.text = new LocalizedText("기록이 없습니다.", "No record").text;

        // album cover and song attribute images
        albumImage.sprite = album.albumCover;
        songAttributeImage.sprite = UIManager.Instance.localAssets.songAttributeLarge[song.attribute];
        // if(song.attribute == 0) songAttributeImage.sprite = Addressables.LoadAssetAsync<Sprite>(AddressableString.SongAttribute0).WaitForCompletion();
        // else if(song.attribute == 1) songAttributeImage.sprite = Addressables.LoadAssetAsync<Sprite>(AddressableString.SongAttribute1).WaitForCompletion();
        // else if(song.attribute == 2) songAttributeImage.sprite = Addressables.LoadAssetAsync<Sprite>(AddressableString.SongAttribute2).WaitForCompletion();

        // live video background option

        bool videoFileExists = File.Exists(Application.persistentDataPath + "/videos1/" + song.id + ".mp4");
        liveBackgroundVideoButton.SetActive(song.videoAvailable);
        deleteVideoButton.SetActive(song.videoAvailable && videoFileExists);

        if(!videoFileExists){
            // if file does not exist always set to image mode
            Data.saveData.backgroundMode[song.id] = 1;
        }
        
        startButton.GetComponent<Button>().interactable = true;

        // check if background mode is set; if not, set image as default
        if(!Data.saveData.backgroundMode.ContainsKey(song.id)){
            Data.saveData.backgroundMode[song.id] = 1;
        }
        if(Data.saveData.backgroundMode[song.id] == 2){
            // video selected
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPink;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
        }else{
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
        }

        cardSelectView.updateAttributeBoost();

        // Show music import button if music file is not built in
        if(song.isCustom){
            // custom music
            bool musicLoaded = song.musicLoaded();
            importMusicButton.SetActive(!musicLoaded);
            deleteMusicButton.SetActive(musicLoaded);
        }else{
            importMusicButton.SetActive(false);
            deleteMusicButton.SetActive(false);
        }

        Data.saveSave();
    }

    public void backButtonTapped()
    {
        UIManager.Instance.toggleLoadingScreen(true);
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        UIManager.Instance.loadSceneAsync("LobbyScene");
    }

    private void updateStartButtonLabel()
    {
        // lab options
        List<string> labels = new List<string>();

        // if(Data.saveData.settings_noteSpeed != 5) labels.Add(new LocalizedText("속도 ×", "Speed ×").text + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0"));
        // if(Data.saveData.settings_flipNotes != 0) labels.Add(new LocalizedText("좌우 반전", "Mirror Notes").text);

        if(Data.saveData.settings_ingameLoop) labels.Add(new LocalizedText("자동 재시작", "Auto-restart").text);
        if(Data.saveData.settings_highRefreshRate) labels.Add(new LocalizedText("60Hz 잠금해제", "60Hz Unlock").text);
        if(Data.saveData.settings_hapticFeedbackMaster) labels.Add(new LocalizedText("햅틱 피드백", "Haptic Feedback").text);

        if(labels.Count > 0) labOptions.text = string.Join(", ", labels) + " ON";
        else labOptions.text = "";
    }

    public void openRankingView()
    {
        Song song = DataStore.GetSong(Data.saveData.lastSelectedSong);
        if(song == null) return;

        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        RankingView rankingView = UIManager.Instance.InstantiateObj(UIManager.View.Ranking).GetComponent<RankingView>();
        rankingView.Init(song);
    }

    public void startButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        // check if music is loaded
        if(!DataStore.GetSong(Data.saveData.lastSelectedSong).musicLoaded()){
            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Notice, body: new LocalizedText("해당 곡에 대한 음원 mp3 파일이 없습니다.\n음원 없이 플레이 하겠습니까?\n\n(음원 mp3 파일은 게임 시작 버튼 왼쪽 음표 버튼을 눌러 불러올 수 있습니다.)", "Music mp3 file for this song has not been imported.\nDo you want to proceed without music?\n\n(Press the button on the left of start button to import mp3 file.)"), confirmText: new LocalizedText("네", "Yes"), cancelText: new LocalizedText("아니오", "No"), confirmAction: delegate{
                UIManager.Instance.toggleLoadingScreen(true);
                audioManager.stopMusic();
                UIManager.Instance.loadSceneAsync("InGameScene");
            }));
            return;
        }
        UIManager.Instance.toggleLoadingScreen(true);
        audioManager.stopMusic();
        UIManager.Instance.loadSceneAsync("InGameScene");
    }

    public void gotoScene(string sceneName){
        UIManager.Instance.toggleLoadingScreen(true);
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.loadSceneAsync(sceneName);
    }


    

    public void gameoptions_flipNotes()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(++Data.saveData.settings_flipNotes > 2){
            Data.saveData.settings_flipNotes = 0;
        }
        Data.saveSave();
        if(Data.saveData.settings_flipNotes == 0){
            flipNotesText.text = new LocalizedText("사용 안함", "OFF").text;
        }else if(Data.saveData.settings_flipNotes == 1){
            flipNotesText.text = new LocalizedText("랜덤 적용", "Random").text;
        }else if(Data.saveData.settings_flipNotes == 2){
            flipNotesText.text = new LocalizedText("항상 사용", "Always").text;
        }
    }    

    public void gameoptions_noteSpeedUp()
    {
        if(++Data.saveData.settings_noteSpeed > 15){
            Data.saveData.settings_noteSpeed = 15;
        }else{
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        }
        Data.saveSave();
        noteSpeedText.text = Data.saveData.settings_noteSpeed == 5 ? new LocalizedText("기본값", "Default").text : "×" + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0");
    }

    public void gameoptions_noteSpeedDown()
    {
        if(--Data.saveData.settings_noteSpeed < 0){
            Data.saveData.settings_noteSpeed = 0;
        }else{
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        }
        Data.saveSave();
        noteSpeedText.text = Data.saveData.settings_noteSpeed == 5 ? new LocalizedText("기본값", "Default").text : "×" + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0");
    }

    public void gameoptions_maxPower()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Data.saveData.settings_maxPower != 0){
            Data.saveData.settings_maxPower = 0;
            Data.saveSave();
            maxPowerLimit.text = new LocalizedText("사용 안함", "OFF").text;
            cardSelectView.updateCards();
        }else{
            Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("최대 파워 제한 설정", "Limit maximum power"), body: new LocalizedText("제한할 최대 파워값을 입력해주세요.\n(1,000 ~ 100,000 사이 값 입력)", "Enter the power limit value to set.\n(Enter value between 1,000 and 100,000)"), confirmAction: delegate(string val){
                int parsed;
                if(int.TryParse(val.Replace(",", ""), out parsed)){
                    if(parsed < 1000 || parsed > 100000){
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("파워 제한 값은 최소 1,000, 최대 100,000 까지 설정할 수 있습니다.\n다시 시도해 주세요.", "You must enter a number between 1,000 and 100,000.\nPlease try again.")));
                    }else{
                        Data.saveData.settings_maxPower = parsed;
                        maxPowerLimit.text = parsed.ToString("N0");
                        cardSelectView.updateCards();
                    }
                }else{
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("잘못된 값을 입력하셨습니다.\n다시 시도해 주세요.", "You must enter a number between 1,000 and 100,000.\nPlease try again.")));
                }
            }));
        }
    }

    public void gameoptions_ingameLoop()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_ingameLoop = !Data.saveData.settings_ingameLoop;
        Data.saveSave();
        ingameLoop.text = Data.saveData.settings_ingameLoop ? "ON" : "OFF";
        updateStartButtonLabel();
    }

    public void gameoptions_autoSelectDeck()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_autoSelectDeck = !Data.saveData.settings_autoSelectDeck;
        cardSelectView.toggleAutoSelectLabel(Data.saveData.settings_autoSelectDeck);
        Data.saveSave();
        autoSelectDeck.text = Data.saveData.settings_autoSelectDeck ? "ON" : "OFF";
    }



    // ====================== TEST OPTION ===========================
    #if RHYTHMIZ_TEST
    // Private Test - Apply Custom Time
    public void setAdditiveSync()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: "추가 싱크 적용", body: "추가로 적용하고 싶은 음악 싱크값을 입력해주세요.\n값이 0보다 클 수록 노트가 음악보다 늦게 내려오며, 값이 0보다 작을수록 노트가 음악보다 빨리 내려옵니다.\n(-0.5초 ~ 0.5초 사이 입력)", 
            confirmAction: delegate(string response){
                try{
                    float sync = float.Parse(response, System.Globalization.CultureInfo.InvariantCulture);
                    if(sync > 0.5 || sync < -0.5){
                        Alert.showAlert(new Alert(title: "오류", body: "값이 너무 크거나 작습니다. 다시 입력해 주세요."));
                        return;
                    }
                    Data.tempData["additiveSync"] = sync.ToString();
                    // Update UI
                    GameObject.Find("AdditiveSync").GetComponent<TextMeshProUGUI>().text = "추가 싱크값: " + sync.ToString("0.000") + "s";
                }catch{
                    Alert.showAlert(new Alert(title: "오류", body: "잘못된 값을 입력하셨습니다. 다시 시도해 주세요."));
                }
            }));
    }
    #endif
    // ==============================================================
}
