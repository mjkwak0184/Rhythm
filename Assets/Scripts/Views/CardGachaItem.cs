using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using TMPro;

public class CardGachaItem : MonoBehaviour
{
    // Start is called before the first frame update
    CardData cardData;
    int collectionId, memberId, grade;
    
    [SerializeField]
    public Animator animator;
    [SerializeField]
    private CardListItem cardListItem;
    [SerializeField]
    private Image cardFrame, cardImage, cardAttribute, cardCover, rankUpIcon;
    [SerializeField]
    private GameObject cardCoverObj, cardObj, newBadgeObj, rankUpObj, maxLvObj, animation_S, animation_R, glistenCustom, glistenDefault;
    [SerializeField]
    private RectTransform cardLevelNew, cardLevelBefore;
    [SerializeField]
    private TextMeshProUGUI[] cardLevelBeforeTexts = new TextMeshProUGUI[5];
    [SerializeField]
    private MeshRenderer glistenCustomMeshRenderer;

    public Button button;
    public bool isUncovered = false, isNew = false;
    private Coroutine showChangesAfterSecond;

    void Start()
    {
        cardObj.SetActive(false);
    }

    public void uncover()
    {
        isUncovered = true;
        cardCoverObj.SetActive(false);
        cardObj.SetActive(true);
        if(isNew) newBadgeObj.SetActive(true);
        else if(Data.saveData.gachaShowChanges){
            IEnumerator showChanges(){
                yield return new WaitForSeconds(1);
                animator.SetBool("showChanges", true);
                
            }
            showChangesAfterSecond = StartCoroutine(showChanges());
        }
    }

    public void toggleShowChanges()
    {
        if(showChangesAfterSecond != null) StopCoroutine(showChangesAfterSecond);
        animator.SetBool("showChanges", Data.saveData.gachaShowChanges);
    }

    public void toggleShowChanges(bool show)
    {
        if(showChangesAfterSecond != null) StopCoroutine(showChangesAfterSecond);
        animator.SetBool("showChanges", show);
    }

    public void Init(string result)
    {
        // <collection_id>:<member_id>:<draw_result>:<before_level>:<after_level>
        string[] split = result.Split(":");
        this.collectionId = int.Parse(split[0]);
        this.memberId = int.Parse(split[1]);
        if(int.Parse(split[3]) == 0) isNew = true;
        (int, int) existingGradeLevel = CardData.getGradeLevelFromRawLevel(int.Parse(split[3]));
        (int, int) newGradeLevel = CardData.getGradeLevelFromRawLevel(int.Parse(split[4]));
        this.grade = CardData.getGradeFromRawLevel(int.Parse(split[2]));

        // show changes
        cardListItem.Init(this.collectionId, this.memberId);
        // modify level (handling cases where same card has been drawn twice)
        cardListItem.setLevel(newGradeLevel.Item2.ToString());
        if(existingGradeLevel.Item2 >= 100){
            Image cardAttributeImage = cardListItem.getCardAttributeImage();
            cardAttributeImage.transform.localPosition = new Vector2(cardAttributeImage.transform.localPosition.x, -92f);
        }
        cardLevelBefore.localPosition = new Vector2(cardLevelNew.anchoredPosition.x - cardListItem.getLevelTextWidth() + 15, -119.3f);
        cardLevelBeforeTexts[existingGradeLevel.Item1].text = existingGradeLevel.Item2.ToString();
        cardLevelBeforeTexts[existingGradeLevel.Item1].gameObject.SetActive(true);

        // if new grade > old grade
        if(newGradeLevel.Item1 > existingGradeLevel.Item1){
            rankUpIcon.sprite = UIManager.Instance.localAssets.cardGradeIcons[newGradeLevel.Item1];
            rankUpObj.SetActive(true);
        }

        if(newGradeLevel.Item2 >= 120) maxLvObj.SetActive(true);

        this.cardData = DataStore.GetCardData(collectionId);
        
        // Load card image
        StartCoroutine(UIManager.Instance.loadImageAddressableAsync(this.cardImage, this.cardData.getCardImage(memberId)));
        
        // Load card frame
        if(this.cardData.customCardFrame && this.grade == 4){
            // set custom frame
            // glistenCustom.SetActive(true);
            // glistenDefault.SetActive(false);
            StartCoroutine(loadCustomFrameImage("Assets/Texture2D/Card/CardFrame/" + this.cardData.collectionId +".png"));
        }else{
            // glistenDefault.SetActive(true);
            // glistenCustom.SetActive(false);
            this.cardFrame.sprite = UIManager.Instance.localAssets.cardFrames[this.grade];
        }

        // load attribute
        // string attributeAddress;
        int cardAttribute = this.cardData.getAttribute(this.memberId);
        // if(cardAttribute == 2) attributeAddress = AddressableString.SongAttribute2S;
        // else if(cardAttribute == 1) attributeAddress = AddressableString.SongAttribute1S;
        // else attributeAddress = AddressableString.SongAttribute0S;
        // StartCoroutine(UIManager.Instance.loadImageAddressableAsync(this.cardAttribute, attributeAddress));
        this.cardAttribute.sprite = UIManager.Instance.localAssets.songAttributeSmall[cardAttribute];

        // load card cover
        this.cardCover.sprite = UIManager.Instance.localAssets.cardCovers[this.grade];
        // StartCoroutine(UIManager.Instance.loadImageAddressableAsync(this.cardCover, "Card[Cover" + this.grade +"]"));
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

    private void cardEnterSound()
    {
        AudioManager.Instance.playClip(SoundEffects.cardEnter);
    }
}