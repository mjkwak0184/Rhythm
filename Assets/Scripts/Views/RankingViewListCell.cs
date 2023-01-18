using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;


public class RankingViewListCell : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI rankText, nameText, scoreText;
    [SerializeField]
    private Image cellBackground;

    private string userid;

    // Start is called before the first frame update
    public void Init(string rankText, string nameText, string scoreText, string userid, bool isMe)
    {
        this.rankText.text = rankText;
        this.nameText.text = nameText;
        this.scoreText.text = scoreText;
        this.userid = userid;

        if(isMe){
            cellBackground.sprite = UIManager.Instance.localAssets.rankingBoxMy;
        }else if(rankText == "1"){
            cellBackground.sprite = UIManager.Instance.localAssets.rankingBox1;
            this.scoreText.color = Color.white;
        }else if(rankText == "2"){
            cellBackground.sprite = UIManager.Instance.localAssets.rankingBox2;
            this.scoreText.color = Color.white;
        }else if(rankText == "3"){
            cellBackground.sprite = UIManager.Instance.localAssets.rankingBox3;
            this.scoreText.color = Color.white;
        }
    }

    public void showProfile()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GameObject view = UIManager.Instance.InstantiateObj(UIManager.View.UserProfile);
        UserProfileView profileView = view.GetComponent<UserProfileView>();
        profileView.Init(userid);
    }
}
