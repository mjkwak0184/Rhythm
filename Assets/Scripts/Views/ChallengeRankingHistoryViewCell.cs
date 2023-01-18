using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MiniJSON;
using UnityEngine.UI;

public class ChallengeRankingHistoryViewCell: MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI dateText;
    [SerializeField]
    private Image selected;

    public Button button;
    public int weekNumber;       // used to send receive request in ChallengeRankingHistoryView
    public bool buttonHasListener;

    public void Init(int weekNumber)
    {
        this.gameObject.name = "ChallengeWeek_" + weekNumber.ToString();
        this.weekNumber = weekNumber;
        this.dateText.text = Data.unixToString(Data.timeFromWeekNumber(weekNumber)) + " ~ " + Data.unixToString(Data.timeFromWeekNumber(weekNumber) + 518500);
    }

    public void setSelected(bool toggle){
        if(toggle){
            selected.color = Color.white;
        }else{
            selected.color = Color.clear;
        }
    }
}