using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

public class UserProfileView: MonoBehaviour
{
    [SerializeField]
    private PopupAnimation popupAnimation;
    [SerializeField]
    private GameObject backdrop, noProfilePhoto, profileMusicObj;
    [SerializeField]
    private TextMeshProUGUI username, userlevel, cardNumber, maxCardNumber, top12Number, profileMusic;
    [SerializeField]
    private Image profileMusicAlbumImage;
    [SerializeField]
    private SpriteRenderer profileImage;
    private int profileMusicId = -1;

    public void Init(string userid)
    {
        popupAnimation.Present();
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.\n인터넷 연결 후 다시 시도해 주세요.", "You are not connected to the internet."), confirmAction: delegate { dismiss(); }));
            return;
        }
        WWWForm form = new WWWForm();
        form.AddField("userid", userid);
        UIManager.Instance.toggleLoadingScreen(true);
        WebRequests.Instance.PostJSONRequest(Data.serverURL + "/getuserprofile", form, infoLoaded, delegate(string error){
            UIManager.Instance.toggleLoadingScreen(false);
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("유저 정보를 불러오지 못했습니다.\n잠시 후 다시 시도해 주세요.", "Failed to load user information.\nPlease try again later."), confirmAction: delegate{dismiss();}));
        });

    }

    private void infoLoaded(Dictionary<string, object> result)
    {
        UIManager.Instance.toggleLoadingScreen(false);
        if( !((bool) result["success"])){
            Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: result["message"].ToString(), confirmAction: delegate{dismiss();}));
            return;
        }
        backdrop.SetActive(true);

        string name = result["username"].ToString();
        username.text = name;
        userlevel.text = "Rank " + ((Int64) result["level"]).ToString("N0");
        int cardIndex;
        if(int.TryParse(result["profilecard"].ToString(), out cardIndex)){
            profileImage.sprite = Addressables.LoadAssetAsync<Sprite>(DataStore.GetCardData(cardIndex / 12).getOriginalImage(cardIndex % 12)).WaitForCompletion();
        }else{
            noProfilePhoto.SetActive(true);
        }

        int allcards = 0, maxcards = 0;
        string cards = result["cards"].ToString(), substr;
        for(int i = 0; i < cards.Length / 2; i++){
            substr = cards.Substring(i*2, 2);
            if(substr == "f0"){
                maxcards++;
                allcards++;
            }else if(substr != "00"){
                allcards++;
            }
        }
        cardNumber.text = allcards.ToString("N0");
        maxCardNumber.text = maxcards.ToString("N0");

        top12Number.text = ((Int64) result["top12"]).ToString("N0");

        // Set up profile music
        if(result.ContainsKey("profilemusic") && result["profilemusic"] != null){
            string music = result["profilemusic"].ToString();
            int music_id;
            if(music.Length > 0 && int.TryParse(music, out music_id)){
                // User has set profile music
                Song song = DataStore.GetSong(music_id);
                if(song != null){
                    this.profileMusicId = song.id;
                    profileMusicAlbumImage.sprite = song.album.albumCover;
                    profileMusic.text = song.songName;
                }
            }
        }
        if(this.profileMusicId == -1) profileMusicObj.SetActive(false);
        else{
            Canvas.ForceUpdateCanvases();
            // set sorting for profile music button
            RectTransform profileMusicBox = profileMusic.GetComponent<RectTransform>();
            if(profileMusicBox.sizeDelta.x > 240){
                profileMusic.GetComponent<ContentSizeFitter>().enabled = false;
                profileMusicBox.sizeDelta = new Vector2(240, 40);
            }
        }

    }

    public void gotoProfileMusic(){
        if(this.profileMusicId != -1){
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
            UIManager.Instance.toggleLoadingScreen(true);
            Data.saveData.lastSelectedSong = this.profileMusicId;
            Data.saveSave();
            UIManager.Instance.loadSceneAsync("SelectMusicScene");
        }
        
    }

    public void dismiss()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        popupAnimation.Dismiss(() => {
            if(profileImage.sprite != null) Addressables.Release(profileImage.sprite);
            Destroy(gameObject);
        });
        // if(profileImage.sprite == null) popupAnimation.Dismiss();
        // else popupAnimation.Dismiss(() => Addressables.Release(profileImage.sprite));
    }
}