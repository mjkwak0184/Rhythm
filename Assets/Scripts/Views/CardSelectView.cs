using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using TMPro;

public class CardSelectView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public System.Action<int> onCardTap, onDeckCycle;

    [SerializeField]
    private TextMeshProUGUI deckPower, deckHealth;
    [SerializeField]
    public CardListItem[] cards = new CardListItem[5];
    [SerializeField]
    private Image[] deckIndexIcon = new Image[6];
    [SerializeField]
    private GameObject autoSelectLabel;

    public bool highlightAttribute;

    [SerializeField]
    private Sprite iconFilled, iconEmpty;

    private bool touchCycle;

    // #if UNITY_EDITOR
    // void Awake()
    // {
    //     if(Data.saveData == null) Data.saveData = new SaveData();
    //     if(Data.userData == null){
    //         Data.userData = new UserData();
    //         Data.userData.cards="10F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0";
    //     }
    //     Data.saveData.selectedCards[0, 0] = 0;
    //     Data.saveData.selectedCards[0, 1] = 1;
    //     Data.saveData.selectedCards[0, 2] = 2;
    //     Data.saveData.selectedCards[0, 3] = 3;
    //     Data.saveData.selectedCards[0, 4] = 4;
    // }
    // #endif

    // Start is called before the first frame update
    void Start()
    {
        // Highlight selected deck index
        deckIndexIcon[Data.saveData.selectedCardDeck].sprite = iconFilled;
        deckIndexIcon[Data.saveData.selectedCardDeck].transform.localScale = new Vector2(1.3f, 1.3f);
        updateCards();

        autoSelectLabel.SetActive(Data.saveData.settings_autoSelectDeck);
    }

    public void toggleAutoSelectLabel(bool toggle)
    {
        autoSelectLabel.SetActive(toggle);
    }

    public void OnBeginDrag(PointerEventData eventData){
        touchCycle = true;
    }

    public void OnDrag(PointerEventData eventData){
        if(!touchCycle) return;
        if(eventData.delta.x < -50){
            cycleSelectedDeck(1);
            touchCycle = false;
        }else if(eventData.delta.x > 50){
            cycleSelectedDeck(-1);
            touchCycle = false;
        }
    }

    public void OnEndDrag(PointerEventData eventData){
        touchCycle = false;
    }

    public void updateCards()
    {
        int totalPower = 0, totalHp = 0; 
        // loop through 5 cards
        for(int i = 0; i < 5; i++){
            int index = Data.saveData.selectedCards[Data.saveData.selectedCardDeck, i];
            if(index == -1){
                cards[i].reset();
            }else{
                cards[i].Init(index / 12, index % 12);
                totalPower += cards[i].cardData.getPower(cards[i].rawlevel);
                totalHp += cards[i].cardData.getHp(cards[i].rawlevel);
            }
        }

        bool powerLimited = false;
        if(Data.saveData.settings_maxPower > 0){
            while(totalPower > Data.saveData.settings_maxPower){
                totalPower -= 10;
                powerLimited = true;
            }
        }

        deckPower.text = totalPower.ToString("N0");
        if(powerLimited) deckPower.text += new LocalizedText(" (제한됨)", " (Capped)").text;
        deckHealth.text = totalHp.ToString("N0");

        if(highlightAttribute) updateAttributeBoost();
    }

    public void updateAttributeBoost()
    {
        int attribute = DataStore.GetSong(Data.saveData.lastSelectedSong).attribute;
        for(int i = 0; i < 5; i++){
            if(cards[i].cardData == DataStore.EmptyCard){
                cards[i].toggleAttributeBoost(false);
            }else{
                cards[i].toggleAttributeBoost(highlightAttribute && attribute == cards[i].attribute);
            }
        }
    }

    public void cycleSelectedDeck(int increment){
        // Uncheck previous deck index
        deckIndexIcon[Data.saveData.selectedCardDeck].sprite = iconEmpty;
        deckIndexIcon[Data.saveData.selectedCardDeck].transform.localScale = Vector2.one;

        // Update data
        Data.saveData.selectedCardDeck += increment;
        if(Data.saveData.selectedCardDeck > 5) Data.saveData.selectedCardDeck = 0;
        else if(Data.saveData.selectedCardDeck < 0) Data.saveData.selectedCardDeck = 5;

        // Refresh deck index image
        deckIndexIcon[Data.saveData.selectedCardDeck].sprite = iconFilled;
        deckIndexIcon[Data.saveData.selectedCardDeck].transform.localScale = new Vector2(1.3f, 1.3f);

        // Update
        Data.saveSave();    // save selection
        updateCards();      // update UI

        if(onDeckCycle != null) onDeckCycle(Data.saveData.selectedCardDeck);
    }

    public void setSelectedDeck(int index){
        if(index < 0 || index > 5) return;
        // Uncheck previous deck index
        deckIndexIcon[Data.saveData.selectedCardDeck].sprite = iconEmpty;
        deckIndexIcon[Data.saveData.selectedCardDeck].transform.localScale = Vector2.one;

        Data.saveData.selectedCardDeck = index;

        // Refresh deck index image
        deckIndexIcon[Data.saveData.selectedCardDeck].sprite = iconFilled;
        deckIndexIcon[Data.saveData.selectedCardDeck].transform.localScale = new Vector2(1.3f, 1.3f);

        // Update
        Data.saveSave();    // save selection
        updateCards();      // update UI

        if(onDeckCycle != null) onDeckCycle(Data.saveData.selectedCardDeck);
    }

    public void cardTapped(int index)
    {
        if(onCardTap != null) onCardTap(index);
    }
}
