using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using System.Linq;

public class CardListView : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{
    private class Card
    {
        public CardData cardData = DataStore.EmptyCard;
        public int index;
        public int memberId = 0;
        public int rawlevel = 0;
        public Card(int index){
            this.index = index;
            this.memberId = index % 12;
            this.bitwiseMember = Mathf.RoundToInt(Mathf.Pow(2, this.memberId));
            this.cardData = DataStore.GetCardData(index / 12);
            if(this.cardData.getAttribute(this.memberId) == 1) this.bitwiseAttribute = 2;
            else if(this.cardData.getAttribute(this.memberId) == 2) this.bitwiseAttribute = 4;
            // get bitwise skill
            if(this.cardData.skill.type == Skill.Type.RaveBonus) this.bitwiseSkill = 2;
            else if(this.cardData.skill.type == Skill.Type.DamageCut) this.bitwiseSkill = 4;
            else if(this.cardData.skill.type == Skill.Type.None) this.bitwiseSkill = 8;

            // get level
            if(Data.userData == null) return;
            if(Data.userData.cards == null) return;
            if(Data.userData.cards.Length < index * 2) return;
            this.rawlevel = int.Parse(Data.userData.cards.Substring(index * 2, 2), System.Globalization.NumberStyles.HexNumber);
        }

        public int bitwiseMember = 0;
        public int bitwiseAttribute = 1;
        public int bitwiseSkill = 1;
        // 1 : Score Up
        // 2 : Rave Bonus
        // 4 : Damage Cut
        // 8 : No Skill
    }

    [SerializeField]
    private LoopVerticalScrollRect cardScrollRect;
    [SerializeField]
    private GameObject cardListItem, filterView, sortView;
    [SerializeField]
    private TextMeshProUGUI cardOwned, sortDirectionText, sortModeText;
    [SerializeField]
    private Image filterButton;
    private Stack<Transform> itemPool = new Stack<Transform>();
    private List<Card> cardList = new List<Card>();
    private List<Card> displayList;

    public bool showUnobtained = false;
    public System.Action<int> cardSelectAction;

    
    // filter elements
    [SerializeField]
    private Toggle[] memberFilter = new Toggle[12];
    [SerializeField]
    private Toggle[] attributeFilter = new Toggle[3];
    [SerializeField]
    private Toggle[] skillFilter = new Toggle[3];
    private List<string> cardImageCatalog = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(Data.userData == null){
            Data.userData = new UserData();
            Data.userData.cards="F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0";
        }
        if(Data.saveData == null) Data.saveData = new SaveData();
        #endif

        // Add card collections where user card level > 0
        for(int i = 0; i < Data.userData.cards.Length / 2; i++){
            if(Data.userData.cards.Substring(i * 2, 2) != "00"){
                Card card = new Card(i);
                cardList.Add(card);
                // Start loading image async
                // if(CardData.List.ContainsKey(i / 12)) Addressables.LoadAssetAsync<Sprite>(CardData.List[i / 12].getCardImage(i % 12));
                // cardImageCatalog.Add(card.cardData.getCardImage(card.index));
            }
        }
        // load image
        // AsyncOperationHandle<IList<Sprite>> load = Addressables.LoadAssetsAsync<Sprite>(cardImageCatalog, sprite => {
        //     Debug.Log(sprite.name);
        // });

        displayList = cardList;
        cardScrollRect.totalCount = cardList.Count;
        cardScrollRect.prefabSource = this;
        cardScrollRect.dataSource = this;
        cardScrollRect.onValueChanged.AddListener(smoothScroll);
        
        sortDirectionText.text = Data.saveData.cardListSortDesc ? "▼" : "▲";
        // Apply sorting and refill
        if(Data.saveData.cardListSortMode == 0){
            sortModeText.text = new LocalizedText("컬렉션", "Collection").text;
        }else if(Data.saveData.cardListSortMode == 1){
            sortModeText.text = new LocalizedText("등급 / 레벨", "Level").text;
        }else if(Data.saveData.cardListSortMode == 2){
            sortModeText.text = new LocalizedText("파워", "Power").text;
        }else if(Data.saveData.cardListSortMode == 3){
            sortModeText.text = new LocalizedText("멤버", "Member").text;
        }
        
        sortCards(Data.saveData.cardListSortMode, false);

        cardOwned.text = new LocalizedText("보유중: ", "Owned: ").text + cardList.Count;
    }

    private void smoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(cardScrollRect);
    }

    public void sortCards(int mode)
    {
        // trigger from UI
        Data.saveData.cardListSortMode = mode;
        Data.saveSave();
        sortCards(mode, true);
        toggleSortView(false);
        // Apply sorting and refill
        if(Data.saveData.cardListSortMode == 0){
            sortModeText.text = new LocalizedText("컬렉션", "Collection").text;
        }else if(Data.saveData.cardListSortMode == 1){
            sortModeText.text = new LocalizedText("등급 / 레벨", "Level").text;
        }else if(Data.saveData.cardListSortMode == 2){
            sortModeText.text = new LocalizedText("파워", "Power").text;
        }else if(Data.saveData.cardListSortMode == 3){
            sortModeText.text = new LocalizedText("멤버", "Member").text;
        }
    }

    public void sortCards(int mode, bool playButtonSound = false)
    {
        if(playButtonSound) AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        int reverse = Data.saveData.cardListSortDesc ? -1 : 1;
        if(mode == 0) displayList.Sort((x, y) => {
            int collectionCompare = x.cardData.collectionId - y.cardData.collectionId;
            return collectionCompare != 0 ? reverse * collectionCompare : reverse * (x.memberId - y.memberId);
        });
        else if(mode == 1) displayList.Sort((x, y) => {
            int levelCompare = x.rawlevel - y.rawlevel;
            if(levelCompare != 0) return reverse * (-1) * levelCompare;
            int collectionCompare = x.cardData.collectionId - y.cardData.collectionId;
            return collectionCompare != 0 ? collectionCompare : (x.memberId - y.memberId);
        });
        else if(mode == 2) displayList.Sort((x, y) => {
            int powerCompare = x.cardData.getPower(x.rawlevel) - y.cardData.getPower(y.rawlevel);
            if(powerCompare != 0) return reverse * (-1) * powerCompare;
            int collectionCompare = x.cardData.collectionId - y.cardData.collectionId;
            return collectionCompare != 0 ? collectionCompare : x.memberId - y.memberId;
        });
        else if(mode == 3) displayList.Sort((x, y) => {
            int memberCompare = x.memberId - y.memberId;
            return memberCompare != 0 ? reverse * memberCompare : reverse * (x.cardData.collectionId - y.cardData.collectionId);
        });
        // displayList.Sort((x, y) =>)
        cardScrollRect.ClearCells();
        cardScrollRect.totalCount = displayList.Count;
        cardScrollRect.RefillCells();
    }

    public void reverseSort()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        Data.saveData.cardListSortDesc = !Data.saveData.cardListSortDesc;
        Data.saveSave();
        sortDirectionText.text = Data.saveData.cardListSortDesc ? "▼" : "▲";
        sortCards(Data.saveData.cardListSortMode, false);
    }

    public void toggleFilterView(bool toggle)
    {
        filterView.SetActive(toggle);
    }

    public void toggleSortView(bool toggle)
    {
        sortView.SetActive(toggle);
    }

    public void filterCards()
    {
        int memberFilterBit = 0, attributeFilterBit = 0, skillFilterBit = 0, multiplier = 1;
        // List<Card> filtered = cardList;
        for(int i = 0; i < memberFilter.Length; i++){
            if(memberFilter[i].isOn) memberFilterBit += multiplier;
            multiplier *= 2;
        }
        multiplier = 1;
        for(int i = 0; i < attributeFilter.Length; i++){
            if(attributeFilter[i].isOn) attributeFilterBit += multiplier;
            multiplier *= 2;
        }
        multiplier = 1;
        for(int i = 0; i < skillFilter.Length; i++){
            if(skillFilter[i].isOn) skillFilterBit += multiplier;
            multiplier *= 2;
        }

        if(memberFilterBit == 0) memberFilterBit = 4095;
        if(attributeFilterBit == 0) attributeFilterBit = 7;
        if(skillFilterBit == 0) skillFilterBit = 7;

        IEnumerable<Card> filtered = from card in cardList where (card.bitwiseMember & memberFilterBit) > 0 && (card.bitwiseAttribute & attributeFilterBit) > 0 && (card.bitwiseSkill & skillFilterBit) > 0 select card;
        displayList = filtered.ToList();
        sortCards(Data.saveData.cardListSortMode, false);

        if(memberFilterBit == 4095 && attributeFilterBit == 7 && skillFilterBit == 7){
            // no filter used
            this.filterButton.sprite = UIManager.Instance.localAssets.buttonBlackFilled;
        }else{
            this.filterButton.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
        }
    }

    public void filterToggleAll(bool enable)
    {
        for(int i = 0; i < memberFilter.Length; i++) memberFilter[i].isOn = enable;
        for(int i = 0; i < attributeFilter.Length; i++) attributeFilter[i].isOn = enable;
        for(int i = 0; i < skillFilter.Length; i++)skillFilter[i].isOn = enable;
        filterCards();
    }


    #region LoopScrollPrefabSource
    public GameObject GetObject(int index)
    {
        if(itemPool.Count == 0)
        {
            GameObject obj = Instantiate(cardListItem);
            obj.transform.localScale = new Vector2(0.83f, 0.83f);
            return obj;
        }
        // otherwise activate cell from pool
        Transform candidate = itemPool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // return cell to pool
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        itemPool.Push(trans);
    }
    #endregion


    #region LoopScrollDataSource
    public void ProvideData(Transform trans, int index)
    {
        CardListItem cell = trans.gameObject.GetComponent<CardListItem>();
        cell.reset();
        cell.Init(displayList[index].index / 12, displayList[index].index % 12);
        if(cardSelectAction != null){
            cell.button.onClick.RemoveAllListeners();
            cell.button.onClick.AddListener( delegate { cardSelectAction(displayList[index].index); } );
        }
    }
    #endregion
}