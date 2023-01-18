using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CardGachaView : MonoBehaviour
{
    [SerializeField]
    private CardGachaItem cardGachaPrefab;
    [SerializeField]
    private Transform grid;
    [SerializeField]
    private GameObject gachaAnimationPrefab, skipButtonObj, animationSkipButtonObj;
    [SerializeField]
    private TextMeshProUGUI showChangesBtnTxt;

    private AudioClip previousClip;
    private GameObject gachaAnimation;

    private List<CardGachaItem> items = new List<CardGachaItem>();

    private bool allLoaded = false, gachaAnimationEnabled;

    #if UNITY_EDITOR
    void Awake()
    {
        if(Data.saveData == null) Data.saveData = new SaveData();
    }
    #endif

    void Start()
    {
        showChangesBtnTxt.text = Data.saveData.gachaShowChanges ? new LocalizedText("뽑기 결과 보기", "View results").text : new LocalizedText("보유 카드 변경사항 보기", "View changes").text;
    }

    public void Init(List<string> result, bool showAnimation)
    {
        UIManager.Instance.IncreaseRenderFrame();
        gachaAnimationEnabled = showAnimation;

        if(showAnimation){
            previousClip = AudioManager.Instance.backgroundAudio.clip;
            AudioManager.Instance.playMusic("Audio/bgm_02Violeta.a", true);
            gachaAnimation = Instantiate(gachaAnimationPrefab);
            GachaAnimationView animationView = gachaAnimation.GetComponent<GachaAnimationView>();
            animationView.Init(result);
            animationView.animationEndCallback = showCards;
        }

        for(int i = 0; i < result.Count; i++){
            CardGachaItem item = Instantiate(cardGachaPrefab);
            item.Init(result[i]);
            item.transform.SetParent(grid);
            item.button.onClick.AddListener(delegate{
                item.uncover();
                checkAllUncovered();
            });
            if(result.Count == 1) item.transform.localScale = new Vector2(1.7f, 1.7f);
            else item.transform.localScale = Vector2.one;
            items.Add(item);
        }

        if(!showAnimation) showCards();

        allLoaded = true;
    }

    public void showCards()
    {
        animationSkipButtonObj.SetActive(false);
        StartCoroutine(_showCards());
    }
    IEnumerator _showCards()
    {
        for(int i = 0; i < items.Count; i++){
            items[i].animator.SetTrigger("Enter");
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void checkAllUncovered()
    {
        for(int i = 0; i < items.Count; i++){
            if(!items[i].isUncovered){
                // more cards left to uncover
                return;
            }
        }
        
        // all uncovered
        skipButtonObj.SetActive(false);
    }

    public void toggleShowChanges()
    {
        // update save file
        Data.saveData.gachaShowChanges = !Data.saveData.gachaShowChanges;
        Data.saveSave();
        showChangesBtnTxt.text = Data.saveData.gachaShowChanges ? new LocalizedText("뽑기 결과 보기", "View results").text : new LocalizedText("보유 카드 변경사항 보기", "View changes").text;

        // toggle animator
        for(int i = 0; i < items.Count; i++){
            if(items[i].isUncovered && !items[i].isNew){
                items[i].toggleShowChanges(Data.saveData.gachaShowChanges);
            }
        }
    }

    public void skipAnimation()
    {
        Destroy(gachaAnimation);
        showCards();
    }

    public void skip()
    {
        if(!allLoaded) return;
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        for(int i = 0; i < items.Count; i++){
            if(!items[i].isUncovered){
                items[i].uncover();
            }
        }
        skipButtonObj.SetActive(false);
    }

    public void exit()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(gachaAnimationEnabled){
            Destroy(gachaAnimation);
            AudioManager.Instance.playMusic(previousClip, true);
        }
        UIManager.Instance.DecreaseRenderFrame();
        Destroy(gameObject);
    }
}
