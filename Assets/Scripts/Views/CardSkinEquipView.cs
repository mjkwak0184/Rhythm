using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;

public class CardSkinEquipView : MonoBehaviour
{
    [SerializeField]
    private PopupAnimation popupAnimation;
    [SerializeField]
    private Animation skillAnimation, showArtistImage;
    [SerializeField]
    private SpriteRenderer artistImage, artistColor, afterArtistImage, afterArtistColor;
    [SerializeField]
    private CardListItem[] cardSlots = new CardListItem[5];
    [SerializeField]
    private CardListItem selectedCard;
    [SerializeField]
    private GameObject[] unequipButtons = new GameObject[5];
    [SerializeField]
    private GameObject equipButton, cantEquipText;
    [SerializeField]
    private TextMeshProUGUI equipButtonText;
    private bool isEquipModeOn = false;
    private int equippingCardIndex = -1, selectedCardIndex;

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(Data.userData == null){
            Data.userData = new UserData();
            Data.userData.inbox = new Dictionary<string, string>();
        }
        #endif

        for(int i = 0; i < cardSlots.Length; i++){
            unequipButtons[i].SetActive(Data.saveData.cardSkinOverride[i] != -1);
            if(Data.saveData.cardSkinOverride[i] == -1) continue;
            cardSlots[i].Init(Data.saveData.cardSkinOverride[i] / 12, Data.saveData.cardSkinOverride[i] % 12);
            
        }

        popupAnimation.Present();
        UIManager.Instance.IncreaseRenderFrame();
    }

    public void setCard(int index)
    {
        equippingCardIndex = index;
        selectCard(-1);
        selectedCard.Init(index / 12, index % 12);
        equipButton.SetActive(selectedCard.rawlevel >= 240);
        cantEquipText.SetActive(selectedCard.rawlevel < 240);
    }

    public void handleSelect(int index)
    {
        if(!isEquipModeOn) selectCard(index);
        else if(isEquipModeOn && index >= 0) equipSkin(index);
    }

    public void equipSkin(int slotIndex)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(equippingCardIndex == -1){
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("선택된 카드 정보가 없습니다.\n다시 시도해 주세요.", "Information for the selected card cannot be found.\nPlease try again.")));
            return;
        }
        Data.saveData.cardSkinOverride[slotIndex] = equippingCardIndex;
        Data.saveSave();
        cardSlots[slotIndex].Init(equippingCardIndex / 12, equippingCardIndex % 12);
        toggleEquipMode(false);
    }

    public void unequipSkin(int index)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("스킨 장착 해제", "Unequip skin"), body: new LocalizedText("슬롯 #" + (index + 1) + "\n" + DataStore.GetCardData(Data.saveData.cardSkinOverride[index] / 12).collectionName + " - " + CardData.getKoreanName(Data.saveData.cardSkinOverride[index] % 12) + "\n\n스킨을 해제하시겠습니까?", "Slot #" + (index + 1) + "\n" + DataStore.GetCardData(Data.saveData.cardSkinOverride[index] / 12).collectionName + " - " + CardData.getEnglishName(Data.saveData.cardSkinOverride[index] % 12) + "\n\nDo you want to unequip skin for this slot?"), confirmAction: delegate{
            cardSlots[index].reset();
            unequipButtons[index].SetActive(false);
            Data.saveData.cardSkinOverride[index] = -1;
            Data.saveSave();
        }));
    }

    public void equipButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        toggleEquipMode(!isEquipModeOn);
    }

    private void toggleEquipMode(bool on)
    {
        for(int i = 0; i < 5; i++){
            cardSlots[i].toggleCardSelectFrame(on);
            if(Data.saveData.cardSkinOverride[i] != -1) unequipButtons[i].SetActive(!on);
        }
        isEquipModeOn = on;
        equipButtonText.text = isEquipModeOn ? new LocalizedText("취소", "Cancel").text : new LocalizedText("스킨 장착", "Equip").text;
    }

    IEnumerator loadImage(int cardIndex)
    {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>("Assets/Texture2D/Card/Original/" + (cardIndex / 12) + "/" + (cardIndex % 12) + ".jpg");
        yield return handle;
        if(handle.Status == AsyncOperationStatus.Succeeded){
            artistImage.sprite = handle.Result;
            afterArtistImage.sprite = handle.Result;
            artistColor.color = SkillManager.memberColors[cardIndex % 12];
            afterArtistColor.color = SkillManager.memberColors[cardIndex % 12];
            skillAnimation.gameObject.SetActive(true);
            skillAnimation.Play();
            showArtistImage.Play();
        }
        yield return null;
    }

    private void selectCard(int index)
    {
        skillAnimation.Stop();
        showArtistImage.Stop();
        if(index == -1){
            selectedCardIndex = equippingCardIndex;
        }else{
            selectedCardIndex = Data.saveData.cardSkinOverride[index];
        }
        if(selectedCardIndex < 0) return;
        StartCoroutine(loadImage(selectedCardIndex));
    }


    public void closeButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        UIManager.Instance.DecreaseRenderFrame();
        popupAnimation.Dismiss();
    }
}
