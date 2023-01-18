using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class RefreshPopupView : MonoBehaviour
{
    public static RefreshPopupView Instance;
    [SerializeField]
    private GameObject canvasObj;

    [SerializeField]
    private Transform popup;
    [SerializeField]
    private GameObject[] notices;

    private Coroutine updateCoroutine;
    private long lastUpdateTime;

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        activate();
    }
    public void refresh()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        lastUpdateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        popup.DOMoveX(-800, 0.3f).OnComplete(() => {
            canvasObj.SetActive(false);
        });
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet.")));
            return;
        }
        
        UIManager.Instance.toggleLoadingScreen(true);
            Data.loadDataFromServer(callback: delegate(Dictionary<string, object> result){
                // success
                HeaderView.Instance.update();
                if(result.ContainsKey("message")){
                    if(result.ContainsKey("url")){
                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, body: (string) result["message"], confirmAction: delegate{
                            Application.OpenURL((string) result["url"]);
                        }));
                    }else{
                        Alert.showAlert(new Alert(body: (string) result["message"]));
                    }
                }

                UIManager.Instance.loadSceneAsync("LobbyScene");
            }, errorcallback: delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if(result.ContainsKey("message")){
                    if(result.ContainsKey("logout")){
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string)result["message"], confirmAction: delegate{
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
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 오류가 발생하였습니다.\n잠시 후 다시 시도해 주세요.", "A server error has occurred.\nPlease try again later.")));
                }
            });
    }

    public void activate()
    {
        lastUpdateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        updateCoroutine = StartCoroutine(update());
    }

    public void deactivate()
    {
        canvasObj.SetActive(false);
        StopCoroutine(updateCoroutine);
    }

    private void showPopup(int type)
    {
        // 
        if(type > notices.Length) return;
        for(int i = 0; i < notices.Length; i++){
            notices[i].SetActive(i == type);
        }
        canvasObj.SetActive(true);
        popup.DOMoveX(0, 0.3f);
    }

    public void hidePopup()
    {
        lastUpdateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        popup.DOMoveX(-800, 0.3f).OnComplete(() => {
            canvasObj.SetActive(false);
        });
    }

    private WaitForSeconds checkInterval = new WaitForSeconds(6f);
    IEnumerator update()
    {
        while(true){
            yield return checkInterval;
            long now = DateTimeOffset.Now.ToUnixTimeSeconds();
            
            if(now - lastUpdateTime > 28800){ // 8 hr
                if(SceneManager.GetActiveScene().name != "InGameScene"){
                    showPopup(0);
                }
            }else if((lastUpdateTime + 32400) / 86400 != (now + 32400) / 86400){    // Add 54000 to account for time zone difference
                // check if day passed
                if(SceneManager.GetActiveScene().name != "InGameScene"){
                    showPopup(1);
                }
            }

            // update time
            lastUpdateTime = now;
        }
    }

}
