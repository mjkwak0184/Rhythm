using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardEquipSceneManager : MonoBehaviour
{
    [SerializeField]
    private CardListView cardListView;
    [SerializeField]
    private CardListItem selectedCard;
    [SerializeField]
    private CardSelectView cardSelectView;
    [SerializeField]
    private GameObject cardSkinEquipViewPrefab, actionButtons;
    [SerializeField]
    private Transform mainCanvasTransform;

    [SerializeField]
    private TextMeshProUGUI collectionName, skillDescription, skillType, cardPower, cardHealth, cardLevel, nameEng, nameKor, equipCardBtnTxt;
    private bool isEquipModeOn;
    
    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(Data.userData == null){
            Data.userData = new UserData();
            Data.userData.cards="10F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0";
        }
        #endif

        AudioManager.Instance.playMusic("Audio/sound_inventory_menu.a", true);
        UIManager.Instance.SetRenderFrameInterval(4);
        cardListView.cardSelectAction = cardSelected;
        cardSelectView.onCardTap = delegate(int deckIndex){
            cardSelected(Data.saveData.selectedCards[Data.saveData.selectedCardDeck, deckIndex]);
        };

        // reset equip mode if deck cycle occurs
        cardSelectView.onDeckCycle = delegate { toggleCardEquipMode(false); };
    }

    void cardSelected(int index)
    {
        // index can be -1 if user taps on the card select view
        if(index == -1) return;

        AudioManager.Instance.playClip(SoundEffects.buttonSmall);

        selectedCard.Init(index / 12, index % 12);
        collectionName.text = selectedCard.cardData.collectionName;
        nameKor.text = CardData.getKoreanName(selectedCard.memberId);
        nameEng.text = CardData.getEnglishName(selectedCard.memberId);
        string maxLevel;
        if(selectedCard.grade == 4) maxLevel = " / 120";
        else if(selectedCard.grade == 3) maxLevel = " / 60";
        else if(selectedCard.grade == 2) maxLevel = " / 30";
        else if(selectedCard.grade == 1) maxLevel = " / 20";
        else maxLevel = " / 10";
        cardLevel.text = "Lv." + selectedCard.level + maxLevel;
        cardHealth.text = selectedCard.cardData.getHp(selectedCard.rawlevel).ToString();
        cardPower.text = selectedCard.cardData.getPower(selectedCard.rawlevel).ToString();

        switch(selectedCard.cardData.skill.type){
            case Skill.Type.ScoreUp:
                skillType.text = "SCORE UP";
                break;
            case Skill.Type.RaveBonus:
                skillType.text = "RAVE BONUS";
                break;
            case Skill.Type.DamageCut:
                skillType.text = "DAMAGE CUT";
                break;
            case Skill.Type.None:
                skillType.text = new LocalizedText("스킬 없음", "NO SKILL").text;
                break;
        }
        skillDescription.text = selectedCard.cardData.skill.formatDescription();

        actionButtons.SetActive(!DataStore.GetCardData(selectedCard.cardData.collectionId).updateNeeded);
        // reset card equip mode
        toggleCardEquipMode(false);
    }

    public void equipCard()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(selectedCard.cardData.collectionId == System.Int32.MaxValue){
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("이 카드를 사용하려면 게임 업데이트를 받아야 합니다.", "You need the latest version of the game in order to use this card.")));
            return;
        }
        if(isEquipModeOn){
            // cancel button
            toggleCardEquipMode(false);
        }else{
            // check if card is equipped        
            for(int i = 0; i < 5; i++){
                if(Data.saveData.selectedCards[Data.saveData.selectedCardDeck, i] == selectedCard.index){
                    // remove card from deck
                    Data.saveData.selectedCards[Data.saveData.selectedCardDeck, i] = -1;
                    Data.saveSave();
                    equipCardBtnTxt.text = new LocalizedText("카드 장착", "Equip card").text;
                    cardSelectView.updateCards();
                    return;
                }
            }

            // card is not equipped, show card equip area
            toggleCardEquipMode(true);
        }
    }

    void equipCardSlotSelect(int slotIndex){
        for(int i = 0; i < 5; i++){
            if(i == slotIndex) continue;
            // return if same member is already in the deck
            if(Data.saveData.selectedCards[Data.saveData.selectedCardDeck, i] % 12 == selectedCard.index % 12) return;
        }

        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.selectedCards[Data.saveData.selectedCardDeck, slotIndex] = selectedCard.index;
        Data.saveSave();
        cardSelectView.updateCards();
        toggleCardEquipMode(false);
    }

    private void toggleCardEquipMode(bool enabled){
        if(enabled){
            // check member duplicate
            int memberId = -1;
            int slotIndex = -1;
            for(int i = 0; i < 5; i++){
                if(Data.saveData.selectedCards[Data.saveData.selectedCardDeck, i] % 12 == selectedCard.index % 12){
                    memberId = selectedCard.index % 12;
                    slotIndex = i;
                }
            }
            if(memberId == -1){
                // no duplicate member, enable all fields
                for(int i = 0; i < 5; i++){
                    cardSelectView.cards[i].toggleCardSelectFrame(true);
                }
            }else{
                cardSelectView.cards[slotIndex].toggleCardSelectFrame(true);
            }

            // tapping cardselectview should call equip card
            cardSelectView.onCardTap = equipCardSlotSelect;

            isEquipModeOn = true;
            equipCardBtnTxt.text = new LocalizedText("취소", "Cancel").text;
        }else{
            // disable all selection field
            for(int i = 0; i < 5; i++){
                cardSelectView.cards[i].toggleCardSelectFrame(false);
            }
            // check if card is equipped
            bool isEquipped = false;
            for(int i = 0; i < 5; i++){
                if(Data.saveData.selectedCards[Data.saveData.selectedCardDeck, i] == selectedCard.index){
                    isEquipped = true;
                    break;
                }
            }

            // update card select view delegate
            cardSelectView.onCardTap = delegate(int deckIndex){
                cardSelected(Data.saveData.selectedCards[Data.saveData.selectedCardDeck, deckIndex]);
            };

            isEquipModeOn = false;
            equipCardBtnTxt.text = isEquipped ? new LocalizedText("카드 장착 해제", "Unequip card").text : new LocalizedText("카드 장착", "Equip card").text;
        }
    }

    public void openSkinEquipView()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GameObject view = Instantiate(cardSkinEquipViewPrefab);
        view.transform.SetParent(mainCanvasTransform);
        view.transform.localScale = Vector2.one;
        CardSkinEquipView skinview = view.GetComponent<CardSkinEquipView>();
        skinview.setCard(selectedCard.index);
    }

   

    public void backButtonTapped()
    {
        UIManager.Instance.toggleLoadingScreen(true);
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        UIManager.Instance.loadPreviousSceneAsync();
    }
}
