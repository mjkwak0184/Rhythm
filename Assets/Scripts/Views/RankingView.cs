using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;

public class RankingView : MonoBehaviour
{
    [SerializeField]
    private PopupAnimation popupAnimation;
    [SerializeField]
    private RankingViewListCell cellPrefab;
    [SerializeField]
    private GameObject contentObject;
    [SerializeField]
    private Image albumArt;
    [SerializeField]
    private TextMeshProUGUI songTitle, albumTitle, rankViewModeBtnText;
    [SerializeField]
    private ScrollRect rankScrollRect;

    private Song song;
    private RectTransform contentRectTransform;

    private string myRank;
    private bool isLoading = false;
    private int rankLoadedBefore = -1, rankLoadedAfter = -1;

    private bool viewingMyRanking = true;

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(Data.accountData == null) Data.accountData = new AccountData("", "");
        #endif

        popupAnimation.Present();
        contentRectTransform = contentObject.GetComponent<RectTransform>();

        LoadRank(firstLoad: true);
        rankScrollRect.onValueChanged.AddListener(smoothScroll);
    }

    void Update()
    {
        if(!isLoading){
            if(rankScrollRect.verticalNormalizedPosition > 1.001){
                // reached top
                if(rankLoadedBefore > 0) LoadRank(rankLoadedBefore - 100, rankLoadedBefore - 1, addToTop: true);
            }else if(rankScrollRect.verticalNormalizedPosition < -0.001){
                // reached bottom
                if(rankLoadedAfter != System.Int32.MaxValue) LoadRank(rankLoadedAfter + 1, rankLoadedAfter + 100);
            }
        }
    }

    public void toggleRankView()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(viewingMyRanking){
            // my rank -> top rank
            rankViewModeBtnText.text = new LocalizedText("내 랭킹 보기", "My Rank").text;
            if(rankLoadedBefore > 0){
                // clear and load from top
                clearRankView();
                rankLoadedBefore = -1;
                rankLoadedAfter = -1;
                LoadRank(0, 99);
            }else{
                Vector2 pos = contentRectTransform.anchoredPosition;
                pos.y = 0;
                contentRectTransform.anchoredPosition = pos;
            }
        }else{
            rankViewModeBtnText.text = new LocalizedText("1위 보기", "View 1st").text;
            // top rank -> my rank
            if(myRank != null){
                if(rankLoadedBefore <= 0 && rankLoadedAfter == System.Int32.MaxValue){
                    int _myrank = int.Parse(myRank);
                    Vector2 pos = contentRectTransform.anchoredPosition;
                    pos.y = (_myrank - 3) * 96;
                    if(pos.y < 300) pos.y = 0;
                    contentRectTransform.anchoredPosition = pos;
                }else{
                    // reset
                    clearRankView();
                    rankLoadedBefore = -1;
                    rankLoadedAfter = -1;
                    LoadRank(firstLoad: true);
                }
                
            }
        }
        viewingMyRanking = !viewingMyRanking;
    }

    private void clearRankView()
    {
        int i = 0;
        GameObject[] children = new GameObject[contentRectTransform.childCount];
        foreach(Transform child in contentRectTransform){
            children[i] = child.gameObject;
            i++;
        }
        foreach(GameObject child in children){
            DestroyImmediate(child.gameObject);
        }
    }

    void OnDestroy()
    {
        UIManager.Instance.ScrollRectSmoothScrollClear(rankScrollRect);
    }

    private void smoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(rankScrollRect);
    }

    public void Init(Song song)
    {
        this.song = song;
        songTitle.text = song.songName;
        albumTitle.text = song.album.albumName;
        albumArt.sprite = song.album.albumCover;
    }

    public void LoadRank(int start = 0, int end = 0, bool firstLoad = false, bool addToTop = false)
    {
        isLoading = true;
        WWWForm form = new WWWForm();
        form.AddField("songid", Data.saveData.lastSelectedSong);
        form.AddField("userid", Data.accountData.userid);
        if(!firstLoad){
            if(start < 0) start = 0;
            if(end < 0) end = 0;
            form.AddField("startrank", start);
            form.AddField("endrank", end);
        }
        UIManager.Instance.toggleLoadingScreen(true);
        WebRequests.Instance.PostJSONRequest(Data.serverURL + "/getscoreboard", form, 
            delegate(Dictionary<string, object> result){
                isLoading = false;
                UIManager.Instance.toggleLoadingScreen(false);

                if(result.ContainsKey("myrank")){
                    myRank = result["myrank"].ToString();
                }

                int res_start = int.Parse(result["start"].ToString());
                int res_end = int.Parse(result["end"].ToString());
                if(res_start < rankLoadedBefore || rankLoadedBefore == -1){
                    rankLoadedBefore = res_start;
                }
                if(result.ContainsKey("reachedBottom")){
                    rankLoadedAfter = System.Int32.MaxValue;
                }else if(res_end > rankLoadedAfter || rankLoadedAfter == -1){
                    rankLoadedAfter = res_end;
                }
                
                Dictionary<string, object> ranks = result["rank"] as Dictionary<string, object>;
                int count = 0;
                Vector2 scrollPosition = contentRectTransform.anchoredPosition;
                for(int i = res_start + 1; i <= res_end + 1; i++){
                    string key = i.ToString();
                    // string value = ranks[key].ToString();
                    // int colonIndex = value.LastIndexOf(":");
                    bool isMyRank = myRank == key;
                    if(isMyRank) scrollPosition.y = (count - 2) * 96;
                    Dictionary<string, object> data = ranks[key] as Dictionary<string, object>;
                    // instantiate cell
                    RankingViewListCell cell = Instantiate(cellPrefab);
                    // fill data
                   
                    // Parse received data
                    string privateid = (data.ContainsKey("privateid") && data["privateid"] != null) ? data["privateid"].ToString() : "";
                    string score = (((Int64) data["score"])).ToString("N0");
                    string username = (data.ContainsKey("username") && data["username"] != null) ? data["username"].ToString() : "???";
                    cell.Init(key, username, score, privateid, isMyRank);

                    // place cell under content scroll box
                    cell.transform.SetParent(contentObject.transform);
                    // place the cell at the top, if needd
                    if(addToTop) cell.transform.SetSiblingIndex(count);
                    // rescale to resize
                    cell.transform.localScale = Vector2.one;
                    // increment count to be used in SetSiblingIndex and setting scroll position
                    count += 1;
                }
                if(addToTop) scrollPosition.y = count * 96;
                if(scrollPosition.y < 300) scrollPosition.y = 0;

                // update canvas first and then set scroll position
                Canvas.ForceUpdateCanvases();
                if(viewingMyRanking) contentRectTransform.anchoredPosition = scrollPosition;
            }, delegate(string error){
                isLoading = false;
                UIManager.Instance.toggleLoadingScreen(false);
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 오류가 발생하였습니다.\n다시 시도해 주세요.", "A server error has occurred.\nPlease try again later."), confirmAction: delegate{ closeButtonTapped(); }));
            });
    }

    public void closeButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        popupAnimation.Dismiss();
    }
}
