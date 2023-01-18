using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using TMPro;

public class CardListItem : MonoBehaviour
{
    // Start is called before the first frame update
    public CardData cardData;
    public int collectionId, memberId, index, level, grade, rawlevel, attribute;
    [SerializeField]
    private Sprite transparent;

    [SerializeField]
    private Image cardFrame, cardImage, cardAttribute, cardAttributeBoost, cardSelectFrame;
    [SerializeField]
    private TextMeshProUGUI[] cardLevelTexts = new TextMeshProUGUI[5];
    [SerializeField]
    private GameObject animation_R, animation_S, glistenDefault, glistenCustom;
    [SerializeField]
    private MeshRenderer glistenCustomMeshRenderer;

    private TextMeshProUGUI cardLevel;

    public Button button;
    private bool showingAttributeBoost = false;

    private Coroutine[] coroutines = new Coroutine[2];

    public void Init(int collectionId, int memberId)
    {
        this.collectionId = collectionId;
        this.memberId = memberId;
        this.index = collectionId * 12 + memberId;

        // if previous level text is active, disable it
        if(this.cardLevel != null) this.cardLevel.gameObject.SetActive(false);
        this.cardData = DataStore.GetCardData(collectionId);

        if(this.cardData.updateNeeded) toggleAttributeBoost(false);
        // show & calculate level
        this.rawlevel = int.Parse(Data.userData.cards.Substring(index * 2, 2), System.Globalization.NumberStyles.HexNumber);

        (int, int) gradeLevelPair = CardData.getGradeLevelFromRawLevel(this.rawlevel);
        this.grade = gradeLevelPair.Item1;
        this.level = gradeLevelPair.Item2;
        this.cardLevel = cardLevelTexts[this.grade];
        
        this.cardLevel.text = this.level.ToString();
        this.cardLevel.gameObject.SetActive(true);

        this.attribute = this.cardData.getAttribute(this.memberId);

        for(int i = 0; i < coroutines.Length; i++){
            if(coroutines[i] != null) StopCoroutine(coroutines[i]);
        }
        if(this.cardData.customCardFrame && this.grade == 4){
            // set custom frame
            coroutines[0] = StartCoroutine(loadCustomFrameImage("Assets/Texture2D/Card/CardFrame/" + this.cardData.collectionId +".png"));
            glistenCustom.SetActive(true);
            glistenDefault.SetActive(false);
        }else{
            glistenDefault.SetActive(true);
            glistenCustom.SetActive(false);
            this.cardFrame.sprite = UIManager.Instance.localAssets.cardFrames[this.grade];
        }

        // show S/R animation
        animation_S.SetActive(this.grade == 3);
        animation_R.SetActive(this.grade == 4);

        coroutines[1] = StartCoroutine(UIManager.Instance.loadImageAddressableAsync(this.cardImage, this.cardData.getCardImage(memberId)));
        this.cardAttribute.sprite = UIManager.Instance.localAssets.songAttributeSmall[this.attribute];
        if(showingAttributeBoost) this.cardAttributeBoost.sprite = UIManager.Instance.localAssets.songAttributeBoost[this.attribute];
    }

    private IEnumerator loadCustomFrameImage(string name)
    {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(name);
        yield return handle;
        if(handle.Status == AsyncOperationStatus.Succeeded){
            this.cardFrame.sprite = handle.Result;
            glistenCustomMeshRenderer.material.SetTexture("_MainTex", handle.Result.texture);
        }
    }

    public float getLevelTextWidth()
    {
        return this.cardLevel.preferredWidth;
    }

    public void reset()
    {
        // hide everything using transparent sprite
        this.cardImage.sprite = transparent;
        this.cardFrame.sprite = transparent;
        this.cardAttribute.sprite = transparent;
        animation_S.SetActive(false);
        animation_R.SetActive(false);
        if(this.cardLevel != null) this.cardLevel.gameObject.SetActive(false);
        // Set card data to empty so that other parts of the code can recognize empty slot
        this.cardData = DataStore.EmptyCard;
    }

    public void toggleAttributeBoost(bool toggle){
        this.cardAttributeBoost.gameObject.SetActive(toggle);
        showingAttributeBoost = toggle;
        
        if(showingAttributeBoost) this.cardAttributeBoost.sprite = UIManager.Instance.localAssets.songAttributeBoost[this.attribute];
    }

    public void toggleCardSelectFrame(bool toggle){
        this.cardSelectFrame.gameObject.SetActive(toggle);
    }

    public Image getCardAttributeImage()
    {
        return this.cardAttribute;
    }

    public void setLevel(string level)
    {
        this.cardLevel.text = level;
    }
}