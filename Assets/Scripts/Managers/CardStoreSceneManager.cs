using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using Gpm.WebView;
using TMPro;
public class StoreItem
{
    public int startTime;
    public int endTime;
    public string imageAddress;
    public int itemid;
    public int unitPrice;
    public bool isDiamond;
    public string name;
    
    public static StoreItem FromObject(Dictionary<string, object> obj){
        StoreItem item = new StoreItem();
        item.itemid = int.Parse((string) obj["itemid"]);
        item.startTime = int.Parse((string) obj["startTime"]);
        item.endTime = int.Parse((string) obj["endTime"]);
        item.isDiamond = (bool) obj["isDiamond"];
        item.unitPrice = int.Parse((string) obj["unitPrice"]);
        item.imageAddress = (string) obj["imageAddress"];
        item.name = (string) obj["name"];
        return item;
    }
}

public class CardStoreSceneManager : MonoBehaviour
{
    [SerializeField]
    private StoreItemCell itemPrefab;
    [SerializeField]
    private ScrollRect scrollRect;
    [SerializeField]
    private HorizontalLayoutGroup horizontalLayoutGroup;
    [SerializeField]
    private Transform contentTransform;
    [SerializeField]
    private Sprite store_sscoin, store_collectioniz;

    private int loadImageCount = 0;
    private List<AsyncOperationHandle> addressableList = new List<AsyncOperationHandle>();

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(Data.saveData == null) Data.saveData = new SaveData();
        if(Data.accountData == null) Data.accountData = new AccountData("", "");
        if(Data.userData == null) Data.userData = new UserData();
        Data.userData.cards="F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0";
        Data.gameData.storeItemList = new List<StoreItem>();
        StoreItem item = new StoreItem();
        item.imageAddress = "SSCoinStore";
        item.isDiamond = false;
        item.unitPrice = 1000;
        item.endTime = 0;
        Data.gameData.storeItemList.Add(item);
        item = new StoreItem();
        item.imageAddress = "CollectionIZStore";
        item.isDiamond = false;
        item.itemid = 1;
        item.unitPrice = 1000;
        item.endTime = 0;
        Data.gameData.storeItemList.Add(item);
        item = new StoreItem();
        item.imageAddress = "StartService.jpg";
        item.isDiamond = false;
        item.itemid = 2;
        item.unitPrice = 1000;
        item.endTime = 1654570799;
        Data.gameData.storeItemList.Add(item);
        #endif

        AudioManager.Instance.playMusic("Audio/sound_inventory_menu.a", true);
        #if UNITY_IOS
        UIManager.Instance.SetRenderFrameInterval(4);
        #endif

        // fill in store
        Data.gameData.storeItemList.Sort((x, y) => y.itemid - x.itemid);
        for(int i = 0; i < Data.gameData.storeItemList.Count; i++){
            if((System.DateTimeOffset.Now.ToUnixTimeSeconds() < Data.gameData.storeItemList[i].endTime && System.DateTimeOffset.Now.ToUnixTimeSeconds() >= Data.gameData.storeItemList[i].startTime) || Data.gameData.storeItemList[i].endTime == 0){
                // item is being sold, show event draw
                StoreItemCell cell = Instantiate(itemPrefab, parent: contentTransform);
                cell.Init(Data.gameData.storeItemList[i]);
                cell.callback = purchase;
                // Display Image
                if(Data.gameData.storeItemList[i].imageAddress == "SSCoinStore"){
                    cell.storeItemImage.sprite = store_sscoin;
                }else if(Data.gameData.storeItemList[i].imageAddress == "CollectionIZStore"){
                    cell.storeItemImage.sprite = store_collectioniz;
                }else{
                    loadImageCount++;
                    UIManager.Instance.toggleLoadingScreen(true);
                    cell.storeItemImage.color = new Color(1, 1, 1, 0.15f);
                    StartCoroutine(loadImage(Data.gameData.storeItemList[i].imageAddress, cell.storeItemImage));
                }
            }
        }

        if(contentTransform.childCount > 2){
            // event gacha is on
            scrollRect.enabled = true;
            scrollRect.onValueChanged.AddListener(smoothScroll);
            horizontalLayoutGroup.padding.left = 200;
            horizontalLayoutGroup.padding.right = 200;
        }
    }

    private void smoothScroll(Vector2 _)
    {
        #if UNITY_IOS
        UIManager.Instance.ScrollRectSmoothScroll(scrollRect);
        #endif
    }

    IEnumerator loadImage(string address, Image targetImage)
    {
        string targetAddress = Application.persistentDataPath + "/cdn/" + address;
        UnityWebRequest request;
        if(!File.Exists(targetAddress)){
            request = UnityWebRequest.Get(Data.cdnURL + address);
            Debug.Log(Data.cdnURL + address);
            DownloadHandlerFile dh = new DownloadHandlerFile(targetAddress);
            dh.removeFileOnAbort = true;
            request.downloadHandler = dh;
            yield return request.SendWebRequest();
            if(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError ){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하여 이미지를 불러오지 못했습니다.\n잠시 후 다시 시도해 주세요.", "Failed to load store images.\nPlease try again later.")));
                if(File.Exists(targetAddress)){
                    File.Delete(targetAddress);
                }
            }
        }
        if(--loadImageCount == 0) UIManager.Instance.toggleLoadingScreen(false);
        if(File.Exists(targetAddress)){
            byte[] image = System.IO.File.ReadAllBytes(targetAddress);
            Texture2D texture = new Texture2D(0, 0, textureFormat: TextureFormat.ASTC_6x6, mipChain: false);
            texture.LoadImage(image);
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            targetImage.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
            targetImage.color = Color.white;
        }
    }

    void OnDestroy()
    {
        for(int i = 0; i < addressableList.Count; i++){
            Addressables.Release(addressableList[i]);
        }
    }

    public void showPercentageWebView()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GpmWebView.ShowUrl(Data.serverURL + "/gacha_list?lang=" + LocalizationManager.Instance.currentLocaleCode, new GpmWebViewRequest.Configuration(){
            style = GpmWebViewStyle.FULLSCREEN,
            isNavigationBarVisible = true,
            title = new LocalizedText("확률", "Item List").text,
            navigationBarColor = "#E1458B"
        });
    }

    public void purchase((int, int, string) param)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("구매", "Purchase").text, confirmText: new LocalizedText("구매", "Buy").text, body: param.Item3, 
            confirmAction: delegate{
                WWWForm form = new WWWForm();
                form.AddField("userid", Data.accountData.userid);
                form.AddField("password", Data.accountData.password);
                form.AddField("itemid", param.Item1);
                form.AddField("amount", param.Item2);
                if(Application.internetReachability == NetworkReachability.NotReachable){
                    Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet.")));
                    return;
                }
                UIManager.Instance.toggleLoadingScreen(true);
                WebRequests.Instance.PostJSONRequest(Data.serverURL + "/card_gacha", form, 
                    delegate(Dictionary<string, object> result){
                        if((bool) result["success"]){
                            // update result data
                            #if !UNITY_EDITOR
                            Data.updateUserData(result["update"] as Dictionary<string, object>);
                            HeaderView.Instance.update();
                            #endif
                            // parse result and show gacha view
                            List<string> drawResult = new List<string>();
                            foreach(object obj in result["result"] as List<object>){
                                drawResult.Add(obj.ToString());
                            }
                            // StartCoroutine(UIManager.Instance.loadSceneAdditive("GachaResultScene", delegate{
                            //     UIManager.Instance.toggleLoadingScreen(false);
                            //     GachaResultSceneManager.Instance.Init(drawResult);
                            // }));
                            
                            // return;
                            CardGachaView view = UIManager.Instance.InstantiateObj(UIManager.View.CardGacha).GetComponent<CardGachaView>();
                            UIManager.Instance.toggleLoadingScreen(false);
                            view.Init(drawResult, true);
                        }else{
                            UIManager.Instance.toggleLoadingScreen(false);
                            Alert.showAlert(new Alert(title: new LocalizedText("실패", "Failure").text, body: (string) result["message"]));
                        }
                    }, delegate(string error){
                        UIManager.Instance.toggleLoadingScreen(false);
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하였습니다.\n\n" + error, "An error occurred.\n\n" + error)));
                    });
            }));
    }

    public void backButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        UIManager.Instance.toggleLoadingScreen(true);
        UIManager.Instance.loadSceneAsync("LobbyScene");
    }
}
