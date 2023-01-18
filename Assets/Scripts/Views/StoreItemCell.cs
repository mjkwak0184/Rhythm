using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class StoreItemCell:MonoBehaviour
{
    [SerializeField]
    public Image storeItemImage;
    [SerializeField]
    private TextMeshProUGUI soldUntilText, price1, price10, buy1, buy10;
    [SerializeField]
    private GameObject soldUntilFrame, diamondIcon1, coinIcon1, diamondIcon10, coinIcon10;

    public System.Action<(int, int, string)> callback;
    private StoreItem item;

    public void Init(StoreItem item)
    {
        if(item.endTime != 0){
            if(LocalizationManager.Instance.currentLocaleCode == "en"){
                soldUntilText.text = System.DateTimeOffset.FromUnixTimeSeconds(item.endTime).ToOffset(new System.TimeSpan(9,0,0)).ToString("~ MM/dd hh:mm tt");
            }else{
                soldUntilText.text = System.DateTimeOffset.FromUnixTimeSeconds(item.endTime).ToOffset(new System.TimeSpan(9,0,0)).ToString("MM/dd hh:mm tt 까지");
            }
            soldUntilFrame.SetActive(true);
        }
        this.item = item;
        this.setPrice(false);
    }

    public void setPrice(bool massBuy)
    {
        if(massBuy){
            this.price1.text = (item.unitPrice * 100).ToString("N0");
            this.price10.text = (item.unitPrice * 1000).ToString("N0");
            this.buy1.text = new LocalizedText("100장 뽑기", "Draw 100").text;
            this.buy10.text = new LocalizedText("1,000장 뽑기", "Draw 1,000").text;
        }else{
            this.price1.text = item.unitPrice.ToString("N0");
            this.price10.text = (item.unitPrice * 10).ToString("N0");
            this.buy1.text = new LocalizedText("1장 뽑기", "Draw 1").text;
            this.buy10.text = new LocalizedText("10장 뽑기", "Draw 10").text;
        }
        diamondIcon1.SetActive(item.isDiamond);
        coinIcon1.SetActive(!item.isDiamond);
        diamondIcon10.SetActive(item.isDiamond);
        coinIcon10.SetActive(!item.isDiamond);
    }

    private string getMessage(int amount)
    {
        if(item.name != null && item.name != "") return new LocalizedText(item.name + " 뽑기 " + amount + "장을 구매하겠습니까?", "Buy " + amount + " cards from '" + item.name + "'?").text;
        return new LocalizedText("뽑기 " + amount + "장을 구매하시겠습니까?", "Buy " + amount + " cards?").text;
    }

    public void purchase(int amount)
    {
        callback((item.itemid, amount, getMessage(amount)));
    }
}