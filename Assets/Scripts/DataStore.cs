using System.Collections.Generic;
using UnityEngine;


// Object initialized in TitleScene from WebRequests.LoadRemoteAssets
public class DataStore: MonoBehaviour
{

    public static DataStore Instance;

    [SerializeField]
    private CardList cardList;
    [SerializeField]
    private SongAlbumList songAlbumList;

    public static List<Song> Songs {
        get {
            return DataStore.Instance.songAlbumList.songList;
        }
    }

    public static List<Album> Albums {
        get {
            return DataStore.Instance.songAlbumList.albumList;
        }
    }

    public static List<CardData> Cards {
        get {
            return DataStore.Instance.cardList.cardList;
        }
    }

    private Dictionary<int, Song> songsDict = new Dictionary<int, Song>();
    private Dictionary<int, Album> albumsDict = new Dictionary<int, Album>();
    private Dictionary<int, CardData> cardDataDict = new Dictionary<int, CardData>();


    public static Song GetSong(int id)
    {
        if(Instance.songsDict.ContainsKey(id)) return Instance.songsDict[id];
        else return null;
    }

    public static Album GetAlbum(int id)
    {
        if(Instance.albumsDict.ContainsKey(id)) return Instance.albumsDict[id];
        else return null;
    }

    public static CardData GetCardData(int id)
    {
        if(Instance.cardDataDict.ContainsKey(id)) return Instance.cardDataDict[id];
        else return Instance.cardList.emptyCard;
    }

    public static CardData EmptyCard {
        get {
            return Instance.cardList.emptyCard;
        }
    }

    

    void Awake()
    {
        if(Instance != this && Instance != null) Destroy(Instance.gameObject);
        Instance = this;
        DontDestroyOnLoad(this);

        // Initialize dictionary
        for(int i = 0; i < this.songAlbumList.songList.Count; i++){
            this.songsDict[this.songAlbumList.songList[i].id] = this.songAlbumList.songList[i];
        }
        for(int i = 0; i < this.songAlbumList.albumList.Count; i++){
            this.albumsDict[this.songAlbumList.albumList[i].id] = this.songAlbumList.albumList[i];
        }
        for(int i = 0; i < this.cardList.cardList.Count; i++){
            this.cardDataDict[this.cardList.cardList[i].collectionId] = this.cardList.cardList[i];
        }
    }
}