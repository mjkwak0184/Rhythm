using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using DG.Tweening;
using TMPro;

public class UserInfoSceneManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI usernameText, userid, statLogin, statConsecutiveLogin, statPlay, statAllCombo, statAllSP, statTop12;
    [SerializeField]
    private CardListView cardListView;
    [SerializeField]
    private CanvasGroup cardListViewCanvas;
    [SerializeField]
    private Image profileImage;
    [SerializeField]
    private GameObject profileCardObj, userInfo;
    [SerializeField]
    private Transform profileOptionsTransform, profileImageTransform;

    private List<AsyncOperationHandle<Sprite>> addressableUnloadList = new List<AsyncOperationHandle<Sprite>>();
    
    private int cardIndex = -1;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        AudioManager.Instance.playMusic("Audio/sound_my_room.a", true);
        UIManager.Instance.SetRenderFrameInterval(4);

        #if UNITY_EDITOR
        if(Data.userData == null){
            Data.userData = new UserData();
            Data.userData.inbox = new Dictionary<string, string>();
            Data.userData.profilecard = "595";
        }
        if(Data.saveData == null) Data.saveData = new SaveData();
        #endif
        userid.text = new LocalizedText("유저 ID : ", "User ID : ").text + Data.accountData.userid;
        usernameText.text = Data.userData.username;
        statLogin.text = Data.userData.totallogin;
        statConsecutiveLogin.text = Data.userData.consecutiveloginrecord;
        statPlay.text = Data.userData.playcount;
        statAllSP.text = Data.userData.allsperfectcount;
        statAllCombo.text = Data.userData.nomisscount;
        if(Data.userData.top12 != null){
            statTop12.text = Data.userData.top12 != "" ? Data.userData.top12 : "0";    
        }else{
            statTop12.text = "0";
        }
        

        setDefaultCardImage();
        
        cardListView.cardSelectAction = cardSelect;
    }

    void OnDestroy()
    {
        for(int i = 0; i < addressableUnloadList.Count; i++){
            Addressables.Release(addressableUnloadList[i]);
        }
    }

    public void changeUsername()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet."), confirmText: LocalizedText.Retry, confirmAction: delegate { changeUsername(); }));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("닉네임 변경", "Change nickname"), body: new LocalizedText("변경할 닉네임을 입력하세요. (2~12자)", "Enter a new nickname. (2~12 characters)"),
            confirmAction: delegate(string username){
                if(Application.internetReachability == NetworkReachability.NotReachable){
                    Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet."), confirmText: LocalizedText.Retry, confirmAction: delegate { changeUsername(); }));
                    return;
                }
                if(username.Length < 2 || username.Length > 12){
                    Alert.showAlert(new Alert(body: new LocalizedText("닉네임은 최소 2자에서 최대 12자까지 입력 가능합니다.", "Nickname must be between 2~12 characters.")));
                    return;
                }
                WWWForm form = new WWWForm();
                form.AddField("password", Data.accountData.password);
                form.AddField("username", username);
                form.AddField("userid", Data.accountData.userid);
                UIManager.Instance.toggleLoadingScreen(true);
                WebRequests.Instance.PostJSONRequest(Data.serverURL + "/changename", form, 
                    delegate(Dictionary<string, object> result){
                        UIManager.Instance.toggleLoadingScreen(false);
                        // success
                        if((bool) result["success"]){
                            Alert.showAlert(new Alert(title: new LocalizedText("닉네임 변경 완료", "Success"), body: new LocalizedText("닉네임을 변경하였습니다.", "Your nickname has been changed.")));
                            Data.updateUserData(result["update"] as Dictionary<string, object>);
                            usernameText.text = Data.userData.username;
                            HeaderView.Instance.update();

                            foreach(KeyValuePair<string, object> record in result["worldRecord"] as Dictionary<string, object>){
                                string[] split = record.Value.ToString().Split(":");
                                Data.gameData.worldRecords[int.Parse(record.Key)] = (split[0], int.Parse(split[1]));
                            }
                        }else{
                            Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                        }
                    }, delegate(string error){
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하였습니다.\n" + error, "An error has occurred.\n" + error)));
                    });
            }));
    }

    public void generatePassword()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("연동 코드 재발급", "Renew account login code"), body: new LocalizedText("확인 버튼을 누르면 연동 코드가 새로 생성됩니다.\n재발급시 기존 연동 코드는 사용이 불가능하며, 이 계정을 사용하는 다른 기기에서 다시 로그인해야 합니다.", "Press [OK] to renew your account login code.\nOnce you renew your login code, your previous login code will be revoked, and you will need to reconnect your account in other devices using this account."), 
            confirmAction: delegate{
                UIManager.Instance.toggleLoadingScreen(true);
                WWWForm form = new WWWForm();
                form.AddField("userid", Data.accountData.userid);
                form.AddField("password", Data.accountData.password);
                WebRequests.Instance.PostJSONRequest(Data.serverURL + "/generate_password", form, 
                    delegate(Dictionary<string, object> result){
                        UIManager.Instance.toggleLoadingScreen(false);
                        if((bool) result["success"]){
                            string newpass = (string) result["password"];
                            Alert.showAlert(new Alert(title: new LocalizedText("연동 코드 재발급", "Renew account login code"), body: new LocalizedText("연동 코드가 재발급되었습니다. 스크린샷 등으로 안전하게 보관해주세요.\n\n유저ID: " + Data.accountData.userid + "\n연동 코드: " + newpass, "Your account login code has been renewed. Please keep this information safe.\n\nUser ID: " + Data.accountData.userid + "\nAccount login code: " + newpass)));
                            Data.accountData.password = newpass;
                            Data.saveAccount();
                        }else{
                            Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                        }
                    }, delegate(string error){
                        UIManager.Instance.toggleLoadingScreen(false);
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: error));
                    });
            })); 
    }

    public void toggleProfileSelectBtn()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        toggleProfileSelect();
    }

    public void toggleProfileSelectBtnCancel()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        isSelectingProfile = true;
        toggleProfileSelect();
    }

    private bool isSelectingProfile = false;
    private void toggleProfileSelect()
    {
        isSelectingProfile = !isSelectingProfile;
        if(isSelectingProfile){
            // show card list
            cardListView.gameObject.SetActive(true);
            userInfo.SetActive(false);
            profileImageTransform.DOScale(0.85f, 0.3f);
            profileOptionsTransform.DOScale(1, 0.3f);
            UIManager.Instance.IncreaseRenderFrame();
            DOTween.To(val => cardListViewCanvas.alpha = val, 0, 1, 0.3f).OnComplete(() => {
                UIManager.Instance.DecreaseRenderFrame();
            });
            // cardListViewCanvas.alpha.
            cardListView.transform.DOLocalMoveX(380, 0.3f);
        }else{
            userInfo.SetActive(true);
            setDefaultCardImage();
            profileImageTransform.DOScale(1, 0.3f);
            profileOptionsTransform.DOScale(0, 0.3f);
            UIManager.Instance.IncreaseRenderFrame();
            DOTween.To(val => cardListViewCanvas.alpha = val, 1, 0, 0.3f);
            cardListView.transform.DOLocalMoveX(480, 0.3f).OnComplete(() => {
                UIManager.Instance.DecreaseRenderFrame();
                cardListView.gameObject.SetActive(false);
            });
        }
    }

    private void setDefaultCardImage()
    {
        if(int.TryParse(Data.userData.profilecard, out cardIndex)){
            CardData data = DataStore.GetCardData(cardIndex / 12);
            if(!data.updateNeeded){
                AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(data.getOriginalImage(cardIndex % 12));
                addressableUnloadList.Add(handle);
                profileImage.sprite = handle.WaitForCompletion();
                profileImage.gameObject.SetActive(true);
            }
        }else{
            cardIndex = -1;
            profileImage.gameObject.SetActive(false);
        }
        
    }

    private void cardSelect(int cardindex)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        cardIndex = cardindex;
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>("Assets/Texture2D/Card/Original/" + (cardindex / 12) + "/" + (cardindex % 12) + ".jpg");
        addressableUnloadList.Add(handle);
        profileImage.sprite = handle.WaitForCompletion();
        profileImage.gameObject.SetActive(true);
    }

    public void setProfileCard()
    {
        if(!isSelectingProfile) return;
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(cardIndex == -1){
            Alert.showAlert(new Alert(body: new LocalizedText("프로필 카드로 사용할 카드를 선택하세요.", "Select the card you want to set as profile card.")));
            return;
        }else if(cardIndex < 0 || DataStore.GetCardData(cardIndex / 12).updateNeeded){
            Alert.showAlert(new Alert(title: new LocalizedText("업데이트 필요", "Update required"), body: new LocalizedText("선택한 카드를 프로필 카드로 설정하려면 게임 업데이트가 필요합니다.", "You need to update your game to set this card as your profile card.")));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("프로필 카드 설정", "Profile Card").text, body: DataStore.GetCardData(cardIndex / 12).collectionName + " - " + new LocalizedText(CardData.getKoreanName(cardIndex % 12) + "\n\n선택한 카드를 프로필 카드로 설정하겠습니까?", CardData.getEnglishName(cardIndex % 12) + "\n\nDo you want to set this card as your profile card?").text, confirmAction: delegate {
            UIManager.Instance.toggleLoadingScreen(true);
            WWWForm form = new WWWForm();
            form.AddField("userid", Data.accountData.userid);
            form.AddField("password", Data.accountData.password);
            form.AddField("cardindex", cardIndex);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/setprofilecard", form, delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if((bool) result["success"]){
                    Alert.showAlert(new Alert(title: new LocalizedText("프로필 카드 변경", "Success"), body: new LocalizedText("프로필 카드를 변경하였습니다.", "Successfully updated your profile card.")));
                    Data.updateUserData(result["update"] as Dictionary<string, object>);
                    toggleProfileSelect();
                }else{
                    Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                }
            });
        }));
    }


    public void gotoScene(string sceneName)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.toggleLoadingScreen(true);
        UIManager.Instance.loadSceneAsync(sceneName);
    }

    public void backButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        UIManager.Instance.toggleLoadingScreen(true);
        UIManager.Instance.loadSceneAsync("LobbyScene");
    }
}
