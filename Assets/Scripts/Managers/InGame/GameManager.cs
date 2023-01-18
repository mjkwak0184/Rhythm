using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Networking;
using MiniJSON;
using DG.Tweening;
using TMPro;

#if UNITY_ANDROID && !UNITY_EDITOR
using Haptics;
#elif UNITY_IOS && !UNITY_EDITOR
using CoreHapticsUnity;
#endif



public enum GameState { Loading, Playing, Paused, GameOver, Ended }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private GameObject backgroundVideo, backgroundImage, ssMode, pauseButton, bezel, canvasPaused, canvasGameover, resumeTimer, ingameLoopCanvas, ingameLoopLoad;
    [SerializeField]
    private Transform skillActivateCard;
    [SerializeField]
    private GameObject[] adjustSyncHide;
    [SerializeField]
    private CanvasGroup liveClearCanvasGroup;
    [SerializeField]
    private TextMeshProUGUI ingameLoopMessage;

    public GameObject audioOutputChanged;

    public Camera mainCamera;
    public GameState gameState = GameState.Loading;
    public bool isAdjustSync = false;
    public float TEST_syncDiffSum = 0;
    public int TEST_syncTapCount = 0;
    private string[] notesData;

    private MusicManager musicManager;
    private NoteManager noteManager;
    private JudgeScoreManager judgeScoreManager;
    private SkillManager skillManager;
    private Song activeSong;

    private int screenWidth, screenHeight, ingameLoopMaxScore = 0;


    // Awake is called before Start
    void Awake()
    {
        #if UNITY_EDITOR
        Application.targetFrameRate = 60;
        if(Data.saveData == null) Data.saveData = new SaveData();
        if(Data.gameData == null) Data.loadGame();
        if(Data.userData == null){
            Data.userData = new UserData();
            Data.userData.cards="F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0";
        }
        Data.saveData.selectedCards[0, 0] = 0;
        Data.saveData.selectedCards[0, 1] = 1;
        Data.saveData.selectedCards[0, 2] = 2;
        Data.saveData.selectedCards[0, 3] = 3;
        Data.saveData.selectedCards[0, 4] = 4;
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        Input.multiTouchEnabled = true;
        #endif

        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        if(Instance == null) Instance = this;
        else if(Instance == this) Destroy(gameObject);

        screenWidth = Screen.width; screenHeight = Screen.height;

        // Adjust render resolution
        int renderResolutionVert = SettingsManager.screenResolutionTable[Data.saveData.settings_ingameScreenResolution];
        if(screenHeight >= renderResolutionVert){
            if(Screen.dpi < 300) QualitySettings.antiAliasing = 4;
            float scaledWidth = ((float)screenWidth / (float)screenHeight) * renderResolutionVert;
            Screen.SetResolution((int) scaledWidth, renderResolutionVert, true);
        }
        Application.targetFrameRate = Data.saveData.settings_highRefreshRate ? 120 : 60;
        UIManager.Instance.SetRenderFrameInterval(1);
    }

    void OnDestroy()
    {
        Application.targetFrameRate = 60;
        if(QualitySettings.antiAliasing != 0) QualitySettings.antiAliasing = 0;
        Screen.SetResolution(screenWidth, screenHeight, true);
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        #if ENABLE_LEGACY_INPUT_MANAGER
        Input.multiTouchEnabled = false;
        #endif
        RefreshPopupView.Instance.activate();
    }

    // Start is called before the first frame update
    void Start()
    {
        musicManager = MusicManager.Instance;
        noteManager = NoteManager.Instance;
        judgeScoreManager = JudgeScoreManager.Instance;
        skillManager = SkillManager.Instance;

        // adjust camera depending on aspect ratio
        float width = Screen.width, height = Screen.height, ratio = width / height;
        // w/h : 1/2 : 0.5
        if(ratio <= 1.77f){   // more square aspect ratio than 16:9
            float size = 5 * (16f/9f) * (height / width);
            mainCamera.orthographicSize = size;
            
            // Enable bezels
            bezel.SetActive(true);
        }else if(ratio >= 1.78f){
            // wider aspect ratio
            backgroundImage.GetComponent<RectTransform>().sizeDelta = new Vector2(ratio * 10f, (ratio * 10f) / (16f / 9f));
            if(Data.saveData.settings_videoBackgroundZoom){
                // resize video to zoom in
                backgroundVideo.GetComponent<RectTransform>().sizeDelta = new Vector2(ratio * 1080f, (ratio * 1080f) / (16f / 9f));
            }
            // move skillactivatecard to left
            float widthAdjusted = ratio * 1080f - 1920f;
            skillActivateCard.localPosition = new Vector2(-5.5f - widthAdjusted * 0.0047f, 0);
        }

        // Disable refresh view
        #if !UNITY_EDITOR
        RefreshPopupView.Instance.deactivate();
        #endif
        // Screen.SetResolution((int)(ratio * 1080f), 1080, true);
        
        // parse temp level data
        if(Data.tempData.ContainsKey("audioSync")){
            Data.tempData.Remove("audioSync");
            isAdjustSync = true;
            activeSong = Addressables.LoadAssetAsync<Song>("Assets/GameData/Songs/Songs/AdjustSync.asset").WaitForCompletion();
            backgroundImage.SetActive(true);    // set default image
        }else{
            int songId = Data.saveData.lastSelectedSong;
            activeSong = DataStore.GetSong(songId);
        }

        Album album = activeSong.album;


        // set music and video
        bool isVideoOn = false;
        if(!isAdjustSync && Data.saveData.backgroundMode.ContainsKey(activeSong.id)){
            if(Data.saveData.backgroundMode[activeSong.id] == 2 && activeSong.videoLoaded()){
                isVideoOn = true;
            }
        }

        if(isVideoOn){
            musicManager.setMusic(activeSong, Application.persistentDataPath + "/videos1/" + activeSong.id + ".mp4");
            backgroundVideo.SetActive(true);
        }else{
            musicManager.setMusic(activeSong);
            // image mode
            if(Data.saveData.settings_backgroundImageMode == 0 && album != null){
                // Default
                if(activeSong == null){
                    // skip
                }else if(album.imageBackgrounds != null){
                    if(album.imageBackgrounds.Length != 0){
                        backgroundImage.GetComponent<UnityEngine.UI.Image>().sprite = album.imageBackgrounds[Random.Range(0, album.imageBackgrounds.Length)];
                    }
                }
                backgroundImage.SetActive(true);
            }else if(Data.saveData.settings_backgroundImageMode == 1){
                // SSMode
                ssMode.SetActive(true);
            }else if(Data.saveData.settings_backgroundImageMode == 3){
                string path = Application.persistentDataPath + "/customBackgroundImage.bytes";
                if(System.IO.File.Exists(path)){
                    byte[] image = System.IO.File.ReadAllBytes(Application.persistentDataPath + "/customBackgroundImage.bytes");
                    Texture2D texture = new Texture2D(0, 0, textureFormat: TextureFormat.ASTC_5x5, mipChain: false);
                    texture.LoadImage(image);
                    Rect rect = new Rect(0, 0, texture.width, texture.height);
                    backgroundImage.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
                }
                backgroundImage.SetActive(true);
            }
        }


        if(!isAdjustSync){
            loadCards();
        }else{
            // Hide adjustsync
            for(int i = 0; i < adjustSyncHide.Length; i++){
                adjustSyncHide[i].SetActive(false);
            }
        }
        

        // Start CoreHaptic Engine (iOS)
        #if UNITY_IOS && !UNITY_EDITOR
        CoreHapticsUnityProxy.LoadEngine();
        #endif

        StartCoroutine(loadGame());
    }

    private IEnumerator loadGame()
    {

        // load notes
        AsyncOperationHandle<TextAsset> loadNoteHandle = Addressables.LoadAssetAsync<TextAsset>(activeSong.getNoteAddress());
        yield return loadNoteHandle;

        if(loadNoteHandle.Status == AsyncOperationStatus.Succeeded){
            notesData = loadNoteHandle.Result.text.Replace(" ", "").Split("\n");
            if(Data.tempData.ContainsKey("customNotes")) notesData = Data.tempData["customNotes"].Replace(" ", "").Split("\n");
            noteManager.loadNotes(notesData);
            Addressables.Release(loadNoteHandle);
            noteManager.poolNotes();
        }else{
            UIManager.Instance.loadSceneAsync("SelectMusicScene", delegate {
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("채보를 불러오는데 실패하였습니다.\n채보를 확인해 주세요.", "Failed to load the note file.")));
            });
            yield break;
        }
        
        // Done loading
        StartCoroutine(startGame());
    }

    // Update is called once per frame
    void OnApplicationPause(bool pauseStatus)
    {
        if(pauseStatus && gameState == GameState.Playing) pauseGame();
    }

    private void loadCards()
    {
        // Load selected card information
        int totalPower = 0, totalHp = 0; 
        HashSet<int> members = new HashSet<int>();
        // loop through 5 cards
        for(int i = 0; i < 5; i++){
            int index = Data.saveData.selectedCards[Data.saveData.selectedCardDeck, i];
            if(index == -1) continue;       // empty slot
            CardData data = DataStore.GetCardData(index / 12);
            if(data.updateNeeded){
                // Exit
                UIManager.Instance.loadSceneAsync("CardEquipScene", delegate {
                    UIManager.Instance.previousSceneName = "SelectMusicScene";
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("게임 데이터가 없는 카드는 사용이 불가능합니다. 다른 카드를 사용해 주세요.", "You need to update the game in order to use this card.")));
                });
                return;
            }
            if(members.Add(index % 12)){
                // no duplicate member yet
                int rawlevel = Data.readBitfieldData(Data.userData.cards, 2, index);
                if(rawlevel <= 0){
                    UIManager.Instance.loadSceneAsync("CardEquipScene", delegate {
                        UIManager.Instance.previousSceneName = "SelectMusicScene";
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("유닛에 보유하지 않은 카드가 설정되어 있습니다.\n유닛 변경 후 플레이 해 주세요.", "You have equipped cards you do not own.\nPlease unequip them.")));
                    });
                    return;
                }
                int power = data.getPower(rawlevel);
                // 50% bonus if same attribute
                if(data.getAttribute(index % 12) == activeSong.attribute) power = (int)((float)power * 1.5f);
                totalPower += power;
                totalHp += data.getHp(rawlevel);
                skillManager.skills[i] = new Skill(data.skill.type, data.skill.activateInterval, data.skill.activateChance, data.skill.activeDuration, data.skill.primaryStat, data.skill.secondaryStat);
                int cardSkinIndex = Data.saveData.cardSkinOverride[i];
                if(cardSkinIndex != -1){
                    if(Data.readBitfieldData(Data.userData.cards, 2, cardSkinIndex) != 240){
                        UIManager.Instance.loadSceneAsync("CardEquipScene", delegate {
                            UIManager.Instance.previousSceneName = "SelectMusicScene";
                            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("카드 스킨에 사용할 수 없는 카드가 장착되어 있습니다.\n스킨 장착 카드를 변경 후 플레이 해 주세요.", "You have equipped a card skin for a card that is not at max level.\nPlease check your equipped skin cards.")));
                        });
                return;
                    }
                    skillManager.skillImages[i] = Addressables.LoadAssetAsync<Sprite>(DataStore.GetCardData(cardSkinIndex / 12).getOriginalImage(cardSkinIndex % 12)).WaitForCompletion();
                    skillManager.skillMembers[i] = cardSkinIndex % 12;
                }else{
                    skillManager.skillImages[i] = Addressables.LoadAssetAsync<Sprite>(data.getOriginalImage(index % 12)).WaitForCompletion();
                    skillManager.skillMembers[i] = index % 12;
                }
            }else{
                // duplicate member found
                UIManager.Instance.loadSceneAsync("CardEquipScene", delegate {
                    UIManager.Instance.previousSceneName = "SelectMusicScene";
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("유닛에 멤버 카드가 중복으로 설정되어 있습니다.\n유닛 카드 조합을 변경 후 플레이 해 주세요.", "You have a duplicate member on your slot.\nPlease update your equipped cards.")));
                });
                return;
            }
        }

        if(Data.saveData.settings_maxPower > 0){
            while(totalPower > Data.saveData.settings_maxPower){
                totalPower -= 10;
            }
        }

        GameStat.power = totalPower != 0 ? totalPower : 2000;
        GameStat.healthStat = totalHp != 0 ? Mathf.Sqrt(totalHp) : 30;
    }

    public IEnumerator musicEnded()
    {
        #if RHYTHMIZ_TEST
        Data.tempData["judge_before"] = noteManager.test_judge_before.ToString();
        Data.tempData["judge_after"] = noteManager.test_judge_after.ToString();
        Data.tempData["test_diff_avg"] = (TEST_syncDiffSum / (float) TEST_syncTapCount).ToString();
        noteManager.judgeDiffs.Sort();
        float testmedian = 0;
        if(noteManager.judgeDiffs.Count > 0){
            if(noteManager.judgeDiffs.Count % 2 == 0){
                // even number, get average
                testmedian = (noteManager.judgeDiffs[(noteManager.judgeDiffs.Count / 2) - 1] + noteManager.judgeDiffs[(noteManager.judgeDiffs.Count / 2)])/2;
            }else{
                testmedian = noteManager.judgeDiffs[(noteManager.judgeDiffs.Count - 1) / 2];
            }
        }
        Data.tempData["judge_median"] = testmedian.ToString();
        #endif
        
        Application.targetFrameRate = 60;
        gameState = GameState.Ended;

        pauseButton.SetActive(false);

        if(isAdjustSync){
            // save sync data
            if(noteManager.judgeDiffs.Count > 0){
                noteManager.judgeDiffs.Sort();
                float median = 0;
                if(noteManager.judgeDiffs.Count % 2 == 0){
                    // even number, get average
                    median = (noteManager.judgeDiffs[(noteManager.judgeDiffs.Count / 2) - 1] + noteManager.judgeDiffs[(noteManager.judgeDiffs.Count / 2)])/2;
                }else{
                    median = noteManager.judgeDiffs[(noteManager.judgeDiffs.Count - 1) / 2];
                }
                Data.saveData.audioSyncProfiles[Data.saveData.audioSyncSelected] = median;
                Data.saveSave();
            }
            // Screen.SetResolution(screenWidth, screenHeight, true);
            UIManager.Instance.loadScene("SettingsScene");
        }else{
            // Play animation
            AnimationManager.Instance.playLiveClear();

            if(Data.saveData.settings_ingameLoop){
                // If ingame loop is on:
                if(GameStat.score > ingameLoopMaxScore){
                    ingameLoopMaxScore = GameStat.score;
                }
                ingameLoopMessage.text = new LocalizedText("결과 전송중...", "Uploading results...").text;
                ingameLoopLoad.SetActive(true);
                ingameLoopCanvas.SetActive(true);
                WWWForm form = GameResultSceneManager.getResultForm();

                form.AddField("version", Application.version);
                form.AddField("lang", LocalizationManager.Instance.currentLocaleCode);
                using (UnityWebRequest request = UnityWebRequest.Post(Data.serverURL + "/gameclear", form))
                {
                    float time = Time.time;
                    yield return request.SendWebRequest();
                    float timeTaken = Time.time - time;
                    if(request.responseCode == 200){
                        Dictionary<string, object> result = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;

                        if((bool) result["success"]){

                            // update data
                            if(result.ContainsKey("update")) Data.updateUserData(result["update"] as Dictionary<string, object>);
                            if(result.ContainsKey("gameData")) Data.updateGameData(result["gameData"] as Dictionary<string, object>);
                            
                            if(result.ContainsKey("worldRecord")){
                                string[] wr = result["worldRecord"].ToString().Split(":");
                                // update world record
                                Data.gameData.worldRecords[Data.saveData.lastSelectedSong] = (wr[0], int.Parse(wr[1]));
                            }

                            if(result.ContainsKey("message")){
                                // Result contains message, exit...
                                // Success, wait for animation to complete
                                if(timeTaken < 4){
                                    yield return new WaitForSeconds(4f - timeTaken);
                                }
                                yield return new WaitForSeconds(3.8f);
                                if(result.ContainsKey("url")){
                                    UIManager.Instance.loadScene("SelectMusicScene", delegate{
                                        UIManager.Instance.previousSceneName = "LobbyScene";
                                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Notice.text, body: (string) result["message"], confirmAction: delegate{
                                            Application.OpenURL((string) result["url"]);
                                        }));
                                    });
                                }else{
                                    UIManager.Instance.loadScene("SelectMusicScene", delegate{
                                        UIManager.Instance.previousSceneName = "LobbyScene";
                                        Alert.showAlert(new Alert(title: LocalizedText.Notice.text, body: (string) result["message"]));
                                    });
                                }
                            }

                            ingameLoopLoad.SetActive(false);

                            if(result.ContainsKey("isNewRecord")){
                                if(result.ContainsKey("isWorldRecord")){
                                    ingameLoopMessage.text = new LocalizedText(
                                        "1위 달성!\n점수 : " + GameStat.score.ToString("N0") + "\n누적 최고 : " + ingameLoopMaxScore.ToString("N0"),
                                        "New world record!\nScore : " + GameStat.score.ToString("N0") + "\nHighest : " + ingameLoopMaxScore.ToString("N0")
                                        ).text;
                                }else{
                                    ingameLoopMessage.text = new LocalizedText(
                                        "개인 신기록 달성!\n점수 : " + GameStat.score.ToString("N0") + "\n누적 최고 : " + ingameLoopMaxScore.ToString("N0"),
                                        "New personal record!\nScore : " + GameStat.score.ToString("N0") + "\nHighest : " + ingameLoopMaxScore.ToString("N0")
                                        ).text;
                                }
                            }else{
                                ingameLoopMessage.text = new LocalizedText(
                                    "점수 : " + GameStat.score.ToString("N0") + "\n누적 최고 : " + ingameLoopMaxScore.ToString("N0"),
                                    "Score : " + GameStat.score.ToString("N0") + "\nHighest : " + ingameLoopMaxScore.ToString("N0")
                                    ).text;
                            }
                            // Success, wait for animation to complete
                            if(timeTaken < 4){
                                yield return new WaitForSeconds(4f - timeTaken);
                            }

                            // Will not exit, prepare to restart game
                            ingameLoopCanvas.SetActive(false);

                            // Calculate power again
                            loadCards();
                            pauseButton.SetActive(true);
                            restartGame(playTapAudio: false);

                            DOTween.To(val => liveClearCanvasGroup.alpha = val, 1, 0, 0.3f).OnComplete(() => {
                                AnimationManager.Instance.hideLiveClear();
                                liveClearCanvasGroup.alpha = 1;
                            });
                        }else{
                            // Result not successful
                            UIManager.Instance.loadScene("GameResultScene");
                        }

                    }else{
                        // Error
                        ingameLoopMessage.text = new LocalizedText("서버와의 통신에 실패하여 게임 결과창으로 이동합니다.", "Connection to server failed. Exiting...").text;
                        yield return new WaitForSeconds(2);
                        UIManager.Instance.loadScene("GameResultScene");
                    }
                }
            }else{
                // wait for live clear animation to be over
                yield return new WaitForSeconds(3.8f);
                // Screen.SetResolution(screenWidth, screenHeight, true);
                UIManager.Instance.loadScene("GameResultScene");
            }
        }
    }



    IEnumerator startGame()
    {
        GameStat.reset();
        while(!musicManager.isMusicLoaded()) yield return null;
        // Debug.Log("Music Loaded at frame " + Time.frameCount);
        musicManager.startMusic();
        
        gameState = GameState.Playing;
    }

    public void pauseGame()
    {
        NoteManager.LongNoteTouch.List.Clear();
        musicManager.pauseMusic();
        gameState = GameState.Paused;
        Application.targetFrameRate = 30;
        canvas.gameObject.SetActive(true);
        canvasPaused.SetActive(true);
        if(Data.saveData.settings_hapticFeedbackMaster){
            #if UNITY_IOS && !UNITY_EDITOR
            Task.Run(delegate{CoreHapticsUnityProxy.StopKeepEngine();});
            #endif
        }
    }

    public void gameOver()
    {
        NoteManager.LongNoteTouch.List.Clear();
        musicManager.pauseMusic();
        gameState = GameState.GameOver;
        canvas.gameObject.SetActive(true);
        canvasGameover.SetActive(true);
        if(Data.saveData.settings_hapticFeedbackMaster){
            #if UNITY_IOS && !UNITY_EDITOR
            Task.Run(delegate{CoreHapticsUnityProxy.StopKeepEngine();});
            #endif
        }
    }

    public void revive()
    {
        // Make network request
        GameStat.health = 1;
        GameStat.usedRevive = true;
        GameObject.Find("RaveEffect/RaveEffect0/gauge_health").transform.localScale = Vector2.one;
        resumeGame();
    }


    private static WaitForSeconds resumeDelay = new WaitForSeconds(3);
    public void resumeGame()
    {
        canvasPaused.SetActive(false);
        canvasGameover.SetActive(false);
        canvas.gameObject.SetActive(false);
        AudioManager.Instance.playClip(SoundEffects.resumeBeep);
        Application.targetFrameRate = Data.saveData.settings_highRefreshRate ? 120 : 60;
        resumeTimer.SetActive(true);

        IEnumerator resume(){
            yield return resumeDelay;
            resumeTimer.SetActive(false);
            musicManager.resumeMusic();
            gameState = GameState.Playing;
        }
        StartCoroutine(resume());
    }

    public void restartGame()
    {
        restartGame(playTapAudio: true);
    }

    public void restartGame(bool playTapAudio = true)
    {
        if(playTapAudio) AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        noteManager.reset();
        AnimationManager.Instance.reset();
        skillManager.reset();
        noteManager.loadNotes(notesData);
        musicManager.gameRestarted();
        audioOutputChanged.SetActive(false);
        Application.targetFrameRate = Data.saveData.settings_highRefreshRate ? 120 : 60;
        canvasPaused.SetActive(false);
        canvasGameover.SetActive(false);
        canvas.gameObject.SetActive(false);
        if(musicManager.didAudioOutputDeviceChange){
            UIManager.Instance.loadScene("InGameScene");
        }else{
            StartCoroutine(startGame());
        }
    }

    public void exitGame()
    {
        // Screen.SetResolution(screenWidth, screenHeight, true);
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        // disable all UI buttons
        // Button[] buttons = Canvas.FindObjectsOfType<Button>();
        // foreach(Button button in buttons){
        //     button.interactable = false;
        // }
        if(isAdjustSync) UIManager.Instance.loadScene("SettingsScene");
        else UIManager.Instance.loadScene("SelectMusicScene");
    }
}