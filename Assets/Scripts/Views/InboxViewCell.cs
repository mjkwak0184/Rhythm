using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MiniJSON;
using UnityEngine.UI;

public class InboxViewCell: MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI itemInfo, itemDesc, itemReceivedAt;
    public Button button;
    public string itemId;       // used to send receive request in InboxView
    public bool buttonHasListener;

    public void Init(string id, string json)
    {
        itemId = id;
        Dictionary<string, object> itemData = Json.Deserialize(json) as Dictionary<string, object>;
        string type = (string)itemData["type"];
        if(type == "sscoin") itemInfo.text = new LocalizedText("SS코인 × ", "SS Coin × ").text + int.Parse(itemData["value"].ToString()).ToString("N0");
        else if(type == "diamond") itemInfo.text = new LocalizedText("다이아몬드 × ", "Diamond × ").text + int.Parse(itemData["value"].ToString()).ToString("N0");
        else if(type == "gacha"){
            if(itemData.ContainsKey("gachaDescription")){
                itemInfo.text = itemData["gachaDescription"].ToString() + " × " + int.Parse(itemData["value"].ToString()).ToString("N0");
            }else{
                itemInfo.text = new LocalizedText("카드 × ", "Card × ").text + int.Parse(itemData["value"].ToString()).ToString("N0");
            }
        }else if(type == "text"){
            if(itemData.ContainsKey("gachaDescription")){
                itemInfo.text = itemData["gachaDescription"].ToString();
            }
        }
        
        itemDesc.text = (string) itemData["description"];
        itemReceivedAt.text = (string) itemData["receivedAt"];
    }
}