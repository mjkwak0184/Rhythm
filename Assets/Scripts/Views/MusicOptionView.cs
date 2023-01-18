using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class MusicOptionView : MonoBehaviour
{
    [SerializeField]
    private PopupAnimation popupAnimation;
    [SerializeField]
    private GameObject deleteMusicButton;
    [SerializeField]
    private Image albumCover;
    [SerializeField]
    private TextMeshProUGUI songName, currentSync;

    private Song song;
    public System.Action onDismiss;

    // Start is called before the first frame update
    void Start()
    {
        this.song = DataStore.GetSong(Data.saveData.lastSelectedSong);

        deleteMusicButton.SetActive(this.song.isCustom);
        songName.text = this.song.songName;
        albumCover.sprite = this.song.album.albumCover;

        updateCurrentSyncLabel();
        popupAnimation.Present();
    }

    public void deleteMusic()
    {
        string targetPath = Application.persistentDataPath + "/music/" + Data.saveData.lastSelectedSong + ".mp3";
        if(File.Exists(targetPath)){
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("음원 삭제", "Delete music file"), body: new LocalizedText("불러온 음원을 삭제하겠습니까?", "Do you want to delete the music file?"), confirmAction: delegate{
                Data.saveData.songMusicSync[this.song.id] = 0;
                Data.saveSave();
                FileInfo fil = new FileInfo(targetPath);
                fil.Delete();
                if(onDismiss != null) onDismiss();
                popupAnimation.Dismiss();
            }));
        }
    }

    public void enterSyncValue()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(!this.song.isCustom){
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("음원이 제공되는 곡에서는 설정이 불가능합니다.", "Can't set this for songs with built-in music.")));
            return;
        }

        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("싱크 값 입력", "Enter sync value"), body: new LocalizedText("해당 곡에 사용할 오디오 싱크값을 입력해주세요.\n입력한 값은 설정의 오디오 싱크값과 중복 적용됩니다.\n-1 ~ 5초 범위 이내로 원하는 소숫점 자리수까지 입력 가능합니다.", "Enter a music sync value you want to use for this song.\nThis value will be applied alongside the sync value set in settings.\nYou can enter values between -1 ~ 5 seconds."), confirmAction: delegate(string s){
            try{
                float sync = Convert.ToSingle(s, System.Globalization.CultureInfo.InvariantCulture);
                if(sync < -1 || sync > 5){
                    Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("싱크 값은 -1초에서 5초 사이로 입력해 주세요.", "Sync value must be between -1 and 5 seconds.")));
                    return;
                }
                Data.saveData.songMusicSync[this.song.id] = sync;
                Data.saveSave();
                updateCurrentSyncLabel();
            }catch(FormatException){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("싱크값을 잘못 입력하셨습니다.\n다시 시도해 주세요.", "Invalid sync value.\nPlease try again.")));
                return;
            }catch(OverflowException){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("사용할 수 없는 값을 입력하셨습니다. 다시 시도해 주세요.", "Invalid sync value.\nPlease try again.")));
                return;
            }
        }));
               
    }

    private void updateCurrentSyncLabel()
    {
        if(Data.saveData.songMusicSync.ContainsKey(this.song.id)) currentSync.text = new LocalizedText("현재 싱크: ", "Current sync: ").text + Data.saveData.songMusicSync[this.song.id].ToString("0.000");
        else currentSync.text = new LocalizedText("현재 싱크: 0.000", "Current sync: 0.000").text;
    }

    public void automaticSync()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        // check if music file exists
        if(!this.song.musicLoaded()){
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("불러온 음원 파일이 없습니다. 음원을 불러온 후 다시 시도해 주세요.", "No imported music found. Please import the music and try again.")));
            return;
        }

        currentSync.text = new LocalizedText("음원 분석 중...", "Analyzing music...").text;
        UIManager.Instance.toggleLoadingScreen(true);
        StartCoroutine(analyzeMusic());
    }

    private IEnumerator analyzeMusic()
    {
        // Load music file from disk
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + Application.persistentDataPath + "/music/" + song.id + ".mp3", AudioType.MPEG);
        yield return request.SendWebRequest();

        if(request.result != UnityWebRequest.Result.Success){
            // if music file failed to load
            UIManager.Instance.toggleLoadingScreen(false);
            updateCurrentSyncLabel();
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("불러온 음원 파일을 ", "")));
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

        while(!clip.LoadAudioData()) yield return null;
        UIManager.Instance.toggleLoadingScreen(false);

        float result_75 = 0, result_85 = 0;
        int gotresult_75 = 0, gotresult_85 = 0;

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // run analysis on both 0.75 and 0.85 threshold
        for(int i = 0; i < samples.Length; i++){
            if(Mathf.Abs(samples[i]) > 0.75f && gotresult_75 == 0){
                float time = (float)(i / clip.channels) / (float) clip.frequency;
                result_75 = time - this.song.analyzeResult_0_75;
                gotresult_75 = 1;
                break;
            }
        }
        for(int i = 0; i < samples.Length; i++){
            if(Mathf.Abs(samples[i]) > 0.85f && gotresult_85 == 0){
                float time = (float)(i / clip.channels) / (float) clip.frequency;
                result_85 = time - this.song.analyzeResult_0_85;
                gotresult_85 = 1;
                break;
            }
        }

        bool bad_result_75 = result_75 > 7 || result_75 < -1.5f;
        bool bad_result_85 = result_85 > 7 || result_85 < -1.5f;

        if(bad_result_75 && bad_result_85){
            Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("음원 분석으로 정확한 결과를 도출하지 못했습니다.\n올바른 음원 파일을 불러왔는지 확인 후 다시 시도하거나, 수동으로 싱크 값을 입력해 주세요.", "Music analyzer was unable to get a reliable result for the imported music.\nYou may want to check if the imported file is correct, or enter a sync value manually.")));
            updateCurrentSyncLabel();
            yield break;
        }else if(bad_result_75){
            // Only use result 0.85
            Data.saveData.songMusicSync[this.song.id] = result_85;
        }else if(bad_result_85){
            // only use 0.75
            Data.saveData.songMusicSync[this.song.id] = result_75;
        }else{
            // average both results
            Data.saveData.songMusicSync[this.song.id] = (result_85 + result_75) / 2f;
        }

        Alert.showAlert(new Alert(title: new LocalizedText("완료", "Complete"), body: new LocalizedText("싱크를 자동으로 조절하였습니다.\n\n자동 조절 후에도 싱크가 맞지 않으면 수동으로 싱크를 조절해 주세요.", "Finished adjusting the sync value.\n\nIf sync is still off, please adjust sync manually.")));
        updateCurrentSyncLabel();
    }

    public void resetSync()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.songMusicSync[this.song.id] = 0;
        Data.saveSave();
        currentSync.text = new LocalizedText("현재 싱크: 0.000", "Current sync: 0.000").text;
    }


    public void closeButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        if(onDismiss != null) onDismiss();
        popupAnimation.Dismiss();
    }
}
