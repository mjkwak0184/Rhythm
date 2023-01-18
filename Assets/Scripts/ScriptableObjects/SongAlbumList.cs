using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName="Song & Album List", menuName="Scriptable Object/List/Song & Album List", order = 0)]
public class SongAlbumList : ScriptableObject {
    public List<Song> songList;
    public List<Album> albumList;
}