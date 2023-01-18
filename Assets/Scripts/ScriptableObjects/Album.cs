using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName="Album", menuName="Scriptable Object/Album", order = 0)]
public class Album : ScriptableObject {
    public static Dictionary<int, Album> List = new Dictionary<int, Album>();

    public int id;
    public string albumName;
    public Sprite albumCover;
    public Sprite[] imageBackgrounds;
    public Song[] songs;
    public bool isIZONE;

    public static Album getAlbum(int id){
        if(Album.List.ContainsKey(id)) return Album.List[id];
        else return null;
    }
}