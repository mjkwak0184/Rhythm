using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChallengeRankingSongItem : MonoBehaviour
{
    [SerializeField]
    private Image albumCover;
    [SerializeField]
    private TextMeshProUGUI songName, songScore;
    
    private int songid = -1;
    
    public void Init(int songid, string score){
        Song song = DataStore.GetSong(songid);
        if(song == null){
            this.songid = -1;
            songName.text = (new LocalizedText("게임 업데이트가 필요합니다", "Update needed")).text;
            albumCover.sprite = null;
            songScore.text = "";
        }else{
            this.songid = songid;
            songName.text = song.songName;
            albumCover.sprite = song.album.albumCover;
            int score_num;
            if(int.TryParse(score, out score_num)){
                songScore.text = score_num.ToString("N0");
            }else{
                songScore.text = score;
            }
            
        }
    }

    public void gotoSong()
    {
        if(this.songid != -1){
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
            UIManager.Instance.toggleLoadingScreen(true);
            Data.saveData.lastSelectedSong = this.songid;
            Data.saveSave();
            UIManager.Instance.loadSceneAsync("SelectMusicScene");
        }
    }
}
