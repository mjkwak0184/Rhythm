using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniJSON;

public class InboxView : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{
    [SerializeField]
    private PopupAnimation popupAnimation;
    [SerializeField]
    private LoopVerticalScrollRect scrollRect;

    [SerializeField]
    private GameObject cellPrefab, inboxEmpty, receiveAllButtons;

    private Stack<Transform> itemPool = new Stack<Transform>();

    private List<(string, string)> itemList;

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(Data.userData == null){
            Data.userData = new UserData();
            Data.userData.inbox = new Dictionary<string, string>();
        }
        #endif

        receiveAllButtons.SetActive(false);
        popupAnimation.Present(delegate{
            fillList();
        });

        scrollRect.onValueChanged.AddListener(smoothScroll);
    }
    void OnDestroy()
    {
        UIManager.Instance.ScrollRectSmoothScrollClear(scrollRect);
    }
    private void smoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(scrollRect);
    }

    private void fillList()
    {
        Dictionary<string, string> receivedDates = new Dictionary<string, string>();
        itemList = new List<(string, string)>();
        foreach(KeyValuePair<string, string> entry in Data.userData.inbox){
            itemList.Add((entry.Key, entry.Value));
            Dictionary<string, object> data = Json.Deserialize(entry.Value) as Dictionary<string, object>;
            receivedDates[entry.Key] = (string) data["receivedAt"];
        }

        if(itemList.Count > 0){
            inboxEmpty.SetActive(false);

            itemList.Sort((x, y) => receivedDates[x.Item1].CompareTo(receivedDates[y.Item1]));

            scrollRect.totalCount = itemList.Count;
            scrollRect.prefabSource = this;
            scrollRect.dataSource = this;
            // loopScrollRect.threshold = 200;
            scrollRect.RefillCells();
            scrollRect.SrollToCell(0, 0);
            receiveAllButtons.SetActive(true);
        }else{
            scrollRect.totalCount = 0;
            receiveAllButtons.SetActive(false);
            scrollRect.gameObject.SetActive(false);
        }
    }

    public void receiveItem(string id)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet.")));
            return;
        }

        UIManager.Instance.toggleLoadingScreen(true);
        
        WWWForm form = new WWWForm();
        form.AddField("userid", Data.accountData.userid);
        form.AddField("password", Data.accountData.password);
        form.AddField("itemid", id);
        WebRequests.Instance.PostJSONRequest(Data.serverURL + "/inbox_receive", form, 
            delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if((bool) result["success"]){
                    if(result.ContainsKey("update")){
                        Data.updateUserData(result["update"] as Dictionary<string, object>);
                        HeaderView.Instance.update();
                    }

                    Dictionary<string, object> item = result["item"] as Dictionary<string, object>;
                    string itemType = (string) item["type"].ToString();

                    // Present reward alert
                    if(itemType == "sscoin") 
                        Alert.showAlert(new Alert(type: Alert.Type.Reward, title: "REWARD", showShine:true, rewardImage: UIManager.Instance.localAssets.sscoin, rewardText: "+" + int.Parse(item["value"].ToString()).ToString("N0")));
                    else if(itemType == "diamond")
                        Alert.showAlert(new Alert(type: Alert.Type.Reward, title: "REWARD", showShine:true, rewardImage: UIManager.Instance.localAssets.diamond, rewardText: "+" + int.Parse(item["value"].ToString()).ToString("N0")));
                    else if(itemType == "gacha"){
                        GameObject cardGachaObj = UIManager.Instance.InstantiateObj(UIManager.View.CardGacha);
                        CardGachaView cardGacha = cardGachaObj.GetComponent<CardGachaView>();
                        List<string> cardGachaResult = new List<string>();
                        List<object> resultRaw = result["received"] as List<object>;
                        for(int i = 0; i < resultRaw.Count; i++){
                            cardGachaResult.Add(resultRaw[i].ToString());
                        }
                        cardGacha.Init(cardGachaResult, false);
                        cardGacha.showCards();
                    }

                    // Remove received cell from list
                    Data.userData.inbox.Remove(id);
                    for(int i = 0; i < itemList.Count; i++){
                        if(itemList[i].Item1 == id){
                            itemList.RemoveAt(i);
                            break;
                        }
                    }

                    scrollRect.totalCount--;
                    scrollRect.RefillCells();
                    scrollRect.SrollToCell(0, 0);
                    if(scrollRect.totalCount <= 0){
                        inboxEmpty.SetActive(true);
                        receiveAllButtons.SetActive(false);
                        GameObject inboxNew = GameObject.Find("Inbox Button New");
                        if(inboxNew != null) inboxNew.SetActive(false);
                    }
                }else{
                    if(result.ContainsKey("logout")){
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"], confirmAction: delegate {
                            UIManager.Instance.loadScene("TitleScene");
                        }));
                    }else{
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                    }
                }
            }, delegate(string error){
                UIManager.Instance.toggleLoadingScreen(false);
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하였습니다.", "An error has occurred."), confirmAction: delegate{
                    UIManager.Instance.loadScene("TitleScene");
                }));
            });
    }

    public void receiveAllMoney()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet.")));
            return;
        }

        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("재화 모두받기", "Receive all"), body: new LocalizedText("선물함에 있는 모든 다이아몬드 및 SS코인을 받습니다.", "Receive all diamonds and SS coins in your inbox."), confirmAction: delegate{
            UIManager.Instance.toggleLoadingScreen(true);
            WWWForm form = new WWWForm();
            form.AddField("userid", Data.accountData.userid);
            form.AddField("password", Data.accountData.password);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/inbox_receiveall_money", form, 
                delegate(Dictionary<string, object> result){
                    UIManager.Instance.toggleLoadingScreen(false);
                    if((bool) result["success"]){
                        // update data
                        Data.updateUserData(result["update"] as Dictionary<string, object>);
                        HeaderView.Instance.update();

                        int received_sscoin = (int)(System.Int64)result["received_sscoin"];
                        int received_diamond = (int)(System.Int64)result["received_diamond"];

                        // Present reward alert
                        if(received_sscoin > 0) Alert.showAlert(new Alert(type: Alert.Type.Reward, title: "REWARD", showShine:true, rewardImage: UIManager.Instance.localAssets.sscoin, rewardText: "+" + received_sscoin.ToString("N0")));
                        if(received_diamond > 0) Alert.showAlert(new Alert(type: Alert.Type.Reward, title: "REWARD", showShine:true, rewardImage: UIManager.Instance.localAssets.diamond, rewardText: "+" + received_diamond.ToString("N0")));
                        if(received_diamond == 0 && received_sscoin == 0) Alert.showAlert(new Alert(body: new LocalizedText("수령할 수 있는 재화가 없습니다.", "You do not have any diamonds or SS coins in your inbox.")));

                        
                        
                        // refill inbox list
                        fillList();
                        if(scrollRect.totalCount <= 0){
                            inboxEmpty.SetActive(true);
                            GameObject inboxNew = GameObject.Find("Inbox Button New");
                            if(inboxNew != null) inboxNew.SetActive(false);
                        }
                    }else{
                        if(result.ContainsKey("logout")){
                            Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"], confirmAction: delegate {
                                UIManager.Instance.loadScene("TitleScene");
                            }));
                        }else{
                            Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                        }
                    }
                }, delegate(string error){
                    UIManager.Instance.toggleLoadingScreen(false);
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하였습니다.", "An error has occurred."), confirmAction: delegate{
                        UIManager.Instance.loadScene("TitleScene");
                    }));
                });
        }));
    }

    public void receiveAllCard()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet.")));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("카드 모두받기", "Receive all (Card)"), body: new LocalizedText("선물함에 있는 카드를 받습니다.\n(카드 모두받기는 한번에 10장까지 가능합니다)", "Receive all cards in your inbox.\n(You can receive up to 10 cards at a time)"), confirmAction: delegate{
            UIManager.Instance.toggleLoadingScreen(true);
            
            WWWForm form = new WWWForm();
            form.AddField("userid", Data.accountData.userid);
            form.AddField("password", Data.accountData.password);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/inbox_receiveall_card", form, 
                delegate(Dictionary<string, object> result){
                    UIManager.Instance.toggleLoadingScreen(false);
                    if((bool) result["success"]){
                        // update data
                        Data.updateUserData(result["update"] as Dictionary<string, object>);
                        HeaderView.Instance.update();

                        List<object> resultRaw = result["received"] as List<object>;
                        if(resultRaw.Count > 0){
                            GameObject cardGachaObj = UIManager.Instance.InstantiateObj(UIManager.View.CardGacha);
                            CardGachaView cardGacha = cardGachaObj.GetComponent<CardGachaView>();
                            List<string> cardGachaResult = new List<string>();
                            
                            for(int i = 0; i < resultRaw.Count; i++){
                                cardGachaResult.Add(resultRaw[i].ToString());
                            }
                            cardGacha.Init(cardGachaResult, false);
                            cardGacha.showCards();
                        }else{
                            Alert.showAlert(new Alert(body: new LocalizedText("수령할 수 있는 카드가 없습니다.", "You do not have any cards in your inbox.")));
                        }

                        
                        // refill inbox list
                        fillList();
                        if(scrollRect.totalCount <= 0){
                            inboxEmpty.SetActive(true);
                            GameObject inboxNew = GameObject.Find("Inbox Button New");
                            if(inboxNew != null) inboxNew.SetActive(false);
                        }
                    }else{
                        if(result.ContainsKey("logout")){
                            Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"], confirmAction: delegate {
                                UIManager.Instance.loadScene("TitleScene");
                            }));
                        }else{
                            Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                        }
                    }
                }, delegate(string error){
                    UIManager.Instance.toggleLoadingScreen(false);
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하였습니다.", "An error has occurred."), confirmAction: delegate{
                        UIManager.Instance.loadScene("TitleScene");
                    }));
                });
        }));
    }

     #region LoopScrollPrefabSource

    public GameObject GetObject(int index)
    {
        if(itemPool.Count == 0)
        {
            return Instantiate(cellPrefab);
        }
        // otherwise activate cell from pool
        Transform candidate = itemPool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // return cell to pool
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        itemPool.Push(trans);
    }

    #endregion


    #region LoopScrollDataSource
    public void ProvideData(Transform trans, int index)
    {
        InboxViewCell cell = trans.gameObject.GetComponent<InboxViewCell>();
        cell.Init(itemList[index].Item1, itemList[index].Item2);
        cell.button.onClick.RemoveAllListeners();
        cell.button.onClick.AddListener( delegate { receiveItem(cell.itemId); } );
    }
    #endregion


    public void closeButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        popupAnimation.Dismiss();
    }
}
