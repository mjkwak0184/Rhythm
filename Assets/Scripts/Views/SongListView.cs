using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.AddressableAssets;

public class SongListView: MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{

    public Action<int> onSongSelect;

    [SerializeField]
    private LoopVerticalScrollRect songScrollRect;
    [SerializeField]
    private ScrollRect albumScrollRect;
    [SerializeField]
    private GameObject songButtonPrefab, clearAlbumButton;
    [SerializeField]
    private Transform albumFilterParentTransform;
    [SerializeField]
    private bool isIZONE = true;

    private Canvas albumScrollViewCanvas;
    private RectTransform albumScrollViewTransform;

    private List<Song> songList = new List<Song>();
    private List<Song> filteredList;
    private List<Album> albumList = new List<Album>();
    private List<Animation> starAnimations = new List<Animation>();
    private Stack<Transform> songButtonPool = new Stack<Transform>();
    private int filteredId;
    private bool albumFilterOpen;
    private bool didStartRun;
    

    private void initializeList()
    {
        // Initialize List
        for(int i = 0; i < DataStore.Songs.Count; i++){
            if(DataStore.Songs[i].isIZONE == this.isIZONE) songList.Add(DataStore.Songs[i]);
        }
        for(int i = 0; i < DataStore.Albums.Count; i++){
            if(DataStore.Albums[i].isIZONE == this.isIZONE) albumList.Add(DataStore.Albums[i]);
        }
        filteredList = songList;
    }

    void OnEnable()
    {
        // OnEnable gets called before Start(), so skip if Start() has never run
        if(!didStartRun) return;

        // Re-enabled; reset song filter and scroll if applicable
        filterSongs(-1, false);
        scrollToSong();
        StartCoroutine(playStarAnimation());
    }

    private void scrollToSong()
    {
        if(songScrollRect.totalCount != -1) return;
        
        for(int i = 0; i < filteredList.Count; i++){
            if(filteredList[i].id == Data.saveData.lastSelectedSong){
                songScrollRect.SrollToCell(i - 5, 0);
                return;
            }
        }
    }

    void Start()
    {
        didStartRun = true;
        initializeList();
        

        songScrollRect.prefabSource = this;
        songScrollRect.dataSource = this;

        #if UNITY_IOS
        songScrollRect.onValueChanged.AddListener(songSmoothScroll);
        albumScrollRect.onValueChanged.AddListener(albumSmoothScroll);
        #endif

        int selectedSongIndex = -1;
        bool songFilterApplied = false;
        // if last selected album exists within the list, filter it
        for(int i = 0; i < songList.Count; i++){
            if(Data.saveData.lastSelectedSong == songList[i].id) selectedSongIndex = i;
            if(songList[i].album.id == Data.saveData.lastSelectedSongFilter){
                filterSongs(Data.saveData.lastSelectedSongFilter, false);
                songFilterApplied = true;
                break;
            }
        }

        if(!songFilterApplied){
            songScrollRect.totalCount = songList.Count > 12 ? -1 : songList.Count;

            // adjust padding and scroll to last selected song
            VerticalLayoutGroup group = songScrollRect.transform.GetChild(0).GetComponent<VerticalLayoutGroup>();
            // Debug.Log("Start - isiz: " + isIZONE + ", TC: " + songScrollRect.totalCount + ", selectedindex: " + selectedSongIndex);
            if(songScrollRect.totalCount == -1){
                group.padding.top = 0;
                group.padding.bottom = 0;
            }else{
                group.padding.top = 150;
                group.padding.bottom = 90;
            }
            songScrollRect.RefillCells();
        }


        // finally, scroll
        if(selectedSongIndex != -1 && songScrollRect.totalCount == -1) songScrollRect.SrollToCell(selectedSongIndex - 5, 0);

        albumScrollViewCanvas = albumScrollRect.gameObject.GetComponent<Canvas>();
        albumScrollViewTransform = albumScrollRect.gameObject.GetComponent<RectTransform>();

        
        StartCoroutine(playStarAnimation());

        // Generate Album Filter List
        for(var i = 0; i < albumList.Count; i++){
            GameObject albumBtn = new GameObject("Album Button", typeof(RectTransform), typeof(Image), typeof(Button));
            albumBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(156, 156);
            albumBtn.GetComponent<Image>().sprite = albumList[i].albumCover;
            albumBtn.transform.SetParent(albumFilterParentTransform);
            albumBtn.transform.localScale = Vector3.one;
            int albumId = albumList[i].id;
            albumBtn.GetComponent<Button>().onClick.AddListener(delegate{ filterSongs(albumId); });
        }

    }

    void OnDisable()
    {
        UIManager.Instance.ScrollRectSmoothScrollClear(songScrollRect);
        UIManager.Instance.ScrollRectSmoothScrollClear(albumScrollRect);
    }



    public void filterSongs(int albumId){
        // Save file
        Data.saveData.lastSelectedSongFilter = albumId;
        Data.saveSave();
        filterSongs(albumId, true);
    }
    public void filterSongs(int albumId, bool playTapAudio){
        if(playTapAudio) AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        filteredId = albumId;
        
        
        if(albumId == -1){
            filteredList = songList;
            clearAlbumButton.SetActive(false);
            songScrollRect.totalCount = songList.Count > 12 ? -1 : songList.Count;
        }else{
            filteredList = songList.Where(song => song.album.id == albumId).ToList();
            clearAlbumButton.SetActive(true);
            songScrollRect.totalCount = filteredList.Count;
        }

        // adjust padding
        VerticalLayoutGroup group = songScrollRect.transform.GetChild(0).GetComponent<VerticalLayoutGroup>();
        if(songScrollRect.totalCount == -1){
            group.padding.top = 0;
            group.padding.bottom = 0;
        }else{

            group.padding.top = 150;
            group.padding.bottom = 90;
        }

        songScrollRect.RefillCells();

        if(albumId == -1){
            // cancelled album sort, scroll to last selected song
            scrollToSong();
        }
    }
    

    public void toggleAlbumFilter()
    {
        bool shouldAlterRenderFrameRate = !DOTween.IsTweening(albumScrollViewTransform);
        if(shouldAlterRenderFrameRate) UIManager.Instance.IncreaseRenderFrame();
        if(!albumFilterOpen){
            albumScrollViewCanvas.enabled = true;
            albumScrollViewTransform.DOSizeDelta(new Vector2(860, 156), 0.1f).OnComplete(() => {
                if(shouldAlterRenderFrameRate) UIManager.Instance.DecreaseRenderFrame();
            });
        }else{
            albumScrollViewTransform.DOSizeDelta(new Vector2(0, 156), 0.1f).OnComplete(() => {
                if(shouldAlterRenderFrameRate) UIManager.Instance.DecreaseRenderFrame();
                albumScrollViewCanvas.enabled = false;
            });
        }
        
        albumFilterOpen = !albumFilterOpen;
    }


    private WaitForSeconds starAnimationInterval = new WaitForSeconds(2.59f);
    IEnumerator playStarAnimation()
    {
        while(true){
            yield return starAnimationInterval;
            for(int i = 0; i < starAnimations.Count; i++){
                starAnimations[i].Play();
            }
        }
    }

    #if UNITY_IOS
    private void songSmoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(songScrollRect);
    }
    private void albumSmoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(albumScrollRect);
    }
    #endif


    #region LoopScrollPrefabSource

    public GameObject GetObject(int index)
    {
        if(songButtonPool.Count == 0)
        {
            GameObject newItem = Instantiate(songButtonPrefab);
            starAnimations.Add(newItem.GetComponent<Animation>());
            return newItem;
        }
        // otherwise activate cell from pool
        Transform candidate = songButtonPool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // return cell to pool
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        // trans.SendMessage("ScrollCellReturn");
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        songButtonPool.Push(trans);
    }

    #endregion


    #region LoopScrollDataSource
    public void ProvideData(Transform trans, int index)
    {
        SongButton button = trans.gameObject.GetComponent<SongButton>();
        if(index >= 0){
            button.Init(filteredList[index % filteredList.Count].id);
        }else{
            if(index % filteredList.Count == 0) button.Init(filteredList[0].id);
            else button.Init(filteredList[index % filteredList.Count + filteredList.Count].id);

            // 50
            // 0 ~ 49
            // -1 ~ -50
            // -50 % 50 0 + 50 = 50
            // -50 % 

            // -1 -> 49
            // -2 -> 48
            // -49 -> 1
            // -50 -> 0
            // -51 -> 49 // -51 % 50 = -1 + 50 = 49
            
        }
        // button.Init(index);
        button.gameObject.SetActive(true);
        Button btn = button.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        if(onSongSelect != null) btn.onClick.AddListener(delegate { onSongSelect(button.song.id); });
        if(button.song.id == Data.saveData.lastSelectedSong) button.setSelected(true);
        else button.setSelected(false);
    }
    #endregion
}