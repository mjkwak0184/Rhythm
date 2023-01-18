using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Gpm.WebView;
public class LobbySceneManager : MonoBehaviour
{
    AudioManager audioManager;

    [SerializeField]
    GameObject inboxPrefab, noticeNew, inboxNew, challengeRankingEvent, eventDraw;
    [SerializeField]
    GameObject[] backgrounds;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        // #if UNITY_IOS
        UIManager.Instance.SetRenderFrameInterval(4);
        // #endif
        audioManager = AudioManager.Instance;
        audioManager.playMusic("Audio/sound_lobby_menu.a", true);

        #if UNITY_EDITOR
        if(Data.saveData == null) Data.saveData = new SaveData();
        if(Data.userData == null){
            Data.userData = new UserData();
            Data.userData.inbox = new Dictionary<string, string>();
            Data.userData.cards="F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0";
        }
        #endif
        
        refresh();

        // set random background
        backgrounds[Random.Range(0, backgrounds.Length)].SetActive(true);
    }

    public void refresh()
    {
        inboxNew.SetActive(Data.userData.inbox.Count > 0);
        noticeNew.SetActive(Data.saveData.noticeReadUntil < Data.gameData.noticeVersion);
        challengeRankingEvent.SetActive(Data.gameData.weeklyChallengeInfo.ContainsKey("event"));
        // check store items with time limit
        Data.gameData.storeItemList.Sort((x, y) => y.itemid - x.itemid);
        for(int i = 0; i < Data.gameData.storeItemList.Count; i++){
            if(System.DateTimeOffset.Now.ToUnixTimeSeconds() < Data.gameData.storeItemList[i].endTime && System.DateTimeOffset.Now.ToUnixTimeSeconds() >= Data.gameData.storeItemList[i].startTime){
                eventDraw.SetActive(true);
                break;
            }
        }
    }

    #if UNITY_ANDROID
    // Update is called once per frame
    void Update(){
        if(UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame){
            // Do something
            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("종료", "Exit game"), body: new LocalizedText("게임을 종료하시겠습니까?", "Do you want to close the game?"), cancelText: new LocalizedText("아니오", "No"), confirmText: new LocalizedText("네", "Yes"), confirmAction: delegate { Application.Quit(); }));
        }
    }
    #endif

    public void openInbox()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.InstantiateObj(inboxPrefab);
    }

    public void openNotice()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(LocalizationManager.Instance.currentLocaleCode == "en"){
            GpmWebView.ShowUrl("https://ssizone.notion.site/Game-Notice-cede8d2bcd584826b00020cc82c35de0", new GpmWebViewRequest.Configuration(){
                style = GpmWebViewStyle.POPUP,
                isMaskViewVisible = true,
                isNavigationBarVisible = true,
                title = "Notice",
                navigationBarColor = "#E1458B"
            });
        }else{
            GpmWebView.ShowUrl("https://ssizone.notion.site/ssizone/Rhythm-IZ-ac57ff1ed00a4a53a378886087734453", new GpmWebViewRequest.Configuration(){
                style = GpmWebViewStyle.POPUP,
                isMaskViewVisible = true,
                isNavigationBarVisible = true,
                title = "공지",
                navigationBarColor = "#E1458B"
            });
        }
        Data.saveData.noticeReadUntil = Data.gameData.noticeVersion;
        Data.saveSave();
        noticeNew.SetActive(false);
    }

    public void showChallengeRankingView()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Data.gameData.gameDataWeekNumber != Data.getWeekNumber()){
            Data.loadDataFromServer(callback: delegate(Dictionary<string, object> result){
                // success
                HeaderView.Instance.update();
                refresh();

                // show challenge ranking popup
                if(Data.gameData.weeklyChallenge.Keys.Count == 0){
                    Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("현재 진행중인 챌린지 랭킹이 없습니다.", "There are currently no active challenge ranking.")));
                }else{
                    GameObject obj = UIManager.Instance.InstantiateObj(UIManager.View.ChallengeRanking);
                    obj.GetComponent<ChallengeRankingView>().Init(0);
                }

                if(result.ContainsKey("message")){
                    if(result.ContainsKey("url")){
                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, body: (string) result["message"], confirmAction: delegate{
                            Application.OpenURL((string) result["url"]);
                        }));
                    }else{
                        Alert.showAlert(new Alert(body: (string) result["message"]));
                    }
                }
            }, errorcallback: delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if(result.ContainsKey("message")){
                    if(result.ContainsKey("logout")){
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText((string)result["message"]), confirmAction: delegate{
                            UIManager.Instance.loadScene("TitleScene");
                        }));
                    }else if(result.ContainsKey("url")){
                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, body: (string) result["message"], confirmAction: delegate{ 
                            Application.OpenURL((string) result["url"]);
                        }));
                    }else{
                        Alert.showAlert(new Alert(body: (string) result["message"]));
                    }
                }else{
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 오류가 발생하였습니다.\n잠시 후 다시 시도해 주세요.", "A server error occurred.\nPlease try again later.")));
                }
            });
        }else{
            if(Data.gameData.weeklyChallenge.Keys.Count == 0){
                Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("현재 진행중인 챌린지 랭킹이 없습니다.", "There are currently no active challenge ranking.")));
            }else{
                GameObject obj = UIManager.Instance.InstantiateObj(UIManager.View.ChallengeRanking);
                obj.GetComponent<ChallengeRankingView>().Init(0);
            }
        }
    }

    public void gotoScene(string sceneName)
    {
        UIManager.Instance.toggleLoadingScreen(true);
        audioManager.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.loadSceneAsync(sceneName);
    }

    private IEnumerator loadSceneAsync(string sceneName)
    {
        AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName);
        while(!sceneLoad.isDone){
            yield return null;
        }
    }
}
