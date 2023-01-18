using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;

public class SongButton: MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI albumName, songName;
    [SerializeField]
    private Image selectedImage, attribute;
    [SerializeField]
    private GameObject[] stars;
    [SerializeField]
    private Image[] starAdditive;
    [SerializeField]
    private GameObject challengeIcon;
    
    public Song song;

    public void Init(int songId)
    {
        song = DataStore.GetSong(songId);
        songName.text = song.songName;
        if(song.album != null) albumName.text = song.album.albumName;
        attribute.sprite = UIManager.Instance.localAssets.songAttributeLarge[song.attribute];
        gameObject.name = "Song" + song.id.ToString();
        int star = Data.readBitfieldData(Data.userData.songclearstar, 1, songId);
        stars[0].SetActive(star >= 1);
        stars[1].SetActive(star >= 2);
        stars[2].SetActive(star >= 3);
        challengeIcon.SetActive(Data.gameData.weeklyChallenge.ContainsKey(songId));
    }

    public void setSelected(bool selected)
    {
        if(selected){
            selectedImage.color = Color.white;
        }else{
            selectedImage.color = Color.clear;
        }
    }
}