
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[CreateAssetMenu(fileName="Song", menuName="Scriptable Object/Song", order = 0)]
public class Song : ScriptableObject {

    public int id;
    public string songName {
        get {
            if(LocalizationManager.Instance.currentLocaleCode == "en") return songName_en;
            else return songName_ko;
        }
    }

    public string songName_ko;
    public string songName_en;
    public Album album;
    public int attribute;
    public int musicLength;
    public bool videoAvailable;
    public float videoPlayAt;
    public bool isIZONE;
    public bool isCustom;
    public float analyzeResult_0_75, analyzeResult_0_85;

    public string getMusicAddress()
    {
        if(!isCustom) return "Assets/GameData/Songs/Music/" + id + ".wav";
        else return null;
    }

    public string getNoteAddress()
    {
        if(!isCustom) return "Assets/GameData/Songs/NoteData/" + id + ".txt";
        else return "Assets/RemoteAssets/Songs/NoteData/" + id + ".txt";
    }

    public Song()
    {
        isIZONE = true;
    }

    public bool videoLoaded()
    {
        return File.Exists(Application.persistentDataPath + "/videos1/" + id + ".mp4");
    }

    public bool musicLoaded()
    {
        if(!this.isCustom) return true;
        return File.Exists(Application.persistentDataPath + "/music/" + id + ".mp3");
    }

    public override string ToString()
    {
        return "Song ID #" + this.id + " <" + this.songName + "> Attribute: " + this.attribute + ", IZ*ONE: " + this.isIZONE + ", Custom: " + this.isCustom;

    }

    public static Song getRandomSong()
    {
        int index = Random.Range(0, DataStore.Songs.Count);
        return DataStore.Songs[index];
    }

    public static Song getRandomSongWithMusic()
    {
        Song song = getRandomSong();
        if(!song.musicLoaded()) return getRandomSongWithMusic();
        return song;
    }
}