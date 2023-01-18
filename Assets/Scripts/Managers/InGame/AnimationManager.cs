using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
public class AnimationManager: MonoBehaviour
{
    public static AnimationManager Instance;

// Combo Box
    
    // Score Text, Combo Box
    [SerializeField]
    private TextMeshPro scoreText, comboText;
    [SerializeField]
    private Animation comboAnimation;
    [SerializeField]
    private GameObject comboBox;
    private bool isComboBoxActive = false;

    // Rave & Health
    [SerializeField]
    private Transform healthGauge;
    [SerializeField]
    private GameObject ultraRaveEffectObject;
    [SerializeField]
    private AnimationEvents ultraRaveEffectOutroCallbackEvent;
    [SerializeField]
    private GameObject[] raveEffects = new GameObject[5], raveTextObjects = new GameObject[5];
    [SerializeField]
    private Transform[] raveGaugeForegrounds = new Transform[4];
    [SerializeField]
    private Animator ultraRaveAnimatorL, ultraRaveAnimatorR;
    private int activeRaveLevel = 0;

    // Judgement
    [SerializeField]
    private Animator judgementAnimator;
    // Tap Effects
    [SerializeField]
    private GameObject shortTapEffectPrefab, longTapEffectPrefab;
    private List<ParticleSystem> shortTapEffectParticles = new List<ParticleSystem>();
    private List<ParticleSystem> longTapEffectParticles = new List<ParticleSystem>();
    private List<Transform> shortTapEffectTransforms = new List<Transform>();
    private List<Transform> longTapEffectTransforms = new List<Transform>();
    private int shortTapEffectIndex = 0, longTapEffectIndex = 0;
    
    // Live Clear
    [SerializeField]
    private GameObject liveClear;


    // Managers
    private AudioManager audioManager;
    private GameManager gameManager;
    private SkillManager skillManager;

    // Order
    public bool shouldUpdate = false, shouldUpdateCombo = false;
    private bool canPlayNextVideoEffect = true, canPlayNextSoundEffect = true;
    public Queue<(bool, float)> tapEffectQueue = new Queue<(bool, float)>();


    private string[] spriteTexts = { "<sprite=0>", "<sprite=1>", "<sprite=2>", "<sprite=3>", "<sprite=4>", "<sprite=5>", "<sprite=6>", "<sprite=7>", "<sprite=8>", "<sprite=9>" };

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private Vector3 tapEffectPosition = new Vector3(0f, -3.67f, 0f);
    private Vector2 tapEffectScale = new Vector2(0.026f, 0.026f);
    void Start()
    {
        audioManager = AudioManager.Instance;
        gameManager = GameManager.Instance;
        skillManager = SkillManager.Instance;
        
        Transform tapEffectParent = GameObject.Find("TapEffects").transform, tapEffectTransform;
        GameObject tapEffectObj;
        // Initialize tap effects
        for(int i = 0; i < 12; i++){
            tapEffectObj = Instantiate(shortTapEffectPrefab);
            tapEffectTransform = tapEffectObj.transform;
            tapEffectTransform.localPosition = tapEffectPosition;
            tapEffectTransform.localScale = tapEffectScale;
            tapEffectTransform.SetParent(tapEffectParent);
            shortTapEffectTransforms.Add(tapEffectTransform);
            shortTapEffectParticles.Add(tapEffectObj.GetComponent<ParticleSystem>());
        }
        for(int i = 0; i < 50; i++){
            tapEffectObj = Instantiate(longTapEffectPrefab);
            tapEffectTransform = tapEffectObj.transform;
            tapEffectTransform.localPosition = tapEffectPosition;
            tapEffectTransform.localScale = tapEffectScale;
            tapEffectTransform.SetParent(tapEffectParent);
            longTapEffectTransforms.Add(tapEffectTransform);
            longTapEffectParticles.Add(tapEffectObj.GetComponent<ParticleSystem>());
        }

        if(gameManager.isAdjustSync) scoreText.text = "";
        raveGaugeForegrounds[0].localScale = Vector2.up;


        ultraRaveEffectOutroCallbackEvent.callback = disableUltraRaveEffectObject;
        ultraRaveEffectObject.SetActive(false);
        StartCoroutine(playComboEffect());
    }

    private void disableUltraRaveEffectObject()
    {
        ultraRaveEffectObject.SetActive(false);
    }

    public void reset()
    {
        activeRaveLevel = 0;
        isComboBoxActive = false;

        scoreText.text = spriteTexts[0];
        healthGauge.localScale = Vector2.one;
        raveGaugeForegrounds[0].localScale = Vector2.up;    // 0, 1
        raveEffects[0].SetActive(true);
        raveTextObjects[0].SetActive(true);
        for(int i = 1; i < raveEffects.Length; i++){
            raveEffects[i].SetActive(false);
            raveTextObjects[i].SetActive(false);
        }

        comboBox.SetActive(false);
        isComboBoxActive = false;

        ultraRaveAnimatorL.Play("Iz_ultrarave_out");
        ultraRaveAnimatorR.Play("Iz_ultrarave_out");
        // ultraRaveEffectObject.SetActive(false);
    }

    private string comboTxt;
    IEnumerator playComboEffect()
    {
        while(true){
            while(gameManager.gameState != GameState.Playing || !shouldUpdateCombo) yield return null;
            // Show combo
            if(GameStat.combo > 0 && !gameManager.isAdjustSync){
                if(shouldUpdateCombo){
                    comboTxt = "";
                    for(int i = GameStat.combo; i > 0; i /= 10){
                        comboTxt = spriteTexts[i % 10] + comboTxt;
                    }

                    comboText.text = comboTxt;

                    if(!isComboBoxActive){
                        comboBox.SetActive(true);
                        isComboBoxActive = true;
                    }
                    
                    comboAnimation.Play();
                    shouldUpdateCombo = false;
                }
            }else{
                if(isComboBoxActive){
                    comboBox.SetActive(false);
                    isComboBoxActive = false;
                }
            }
            yield return null;
        }
    }

    public void playJudgeEffect(JudgeResult result, bool shouldPlayEffectImmediately = false)
    {
        if(shouldPlayEffectImmediately){
            judgeVideoEffect(result);
            if(Data.saveData.settings_ingameSoundEffect) judgeSoundEffect(result);
            return;
        }

        if(canPlayNextVideoEffect){
            canPlayNextVideoEffect = false;
            StartCoroutine(judgeVideoRepeat(result));
        }

        if(canPlayNextSoundEffect && Data.saveData.settings_ingameSoundEffect){
            canPlayNextSoundEffect = false;
            StartCoroutine(judgeSoundRepeat(result));
        }
    }

    private WaitForSeconds videoRepeatInterval = new WaitForSeconds(0.085f);
    IEnumerator judgeVideoRepeat(JudgeResult result)
    {
        // canPlayNextVideoEffect = false;
        yield return videoRepeatInterval;
        judgeVideoEffect(result);
        canPlayNextVideoEffect = true;
    }

    private WaitForSeconds audioRepeatInterval = new WaitForSeconds(0.06f);
    IEnumerator judgeSoundRepeat(JudgeResult result)
    {
        // canPlayNextSoundEffect = false;
        yield return audioRepeatInterval;
        judgeSoundEffect(result);
        canPlayNextSoundEffect = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void judgeVideoEffect(JudgeResult result)
    {
        // Play judge effect
        switch(result){
            case JudgeResult.SPerfect:
                judgementAnimator.SetTrigger("SPerfect");
                break;
            case JudgeResult.Perfect:
                judgementAnimator.SetTrigger("Perfect");
                break;
            case JudgeResult.Good:
                judgementAnimator.SetTrigger("Good");
                break;
            case JudgeResult.Miss:
                judgementAnimator.SetTrigger("Miss");
                break;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void judgeSoundEffect(JudgeResult result){
        if(result == JudgeResult.SPerfect) audioManager.playClip(SoundEffects.judgeSPerfect);
        else if(result == JudgeResult.Perfect) audioManager.playClip(SoundEffects.judgePerfect);
        else if(result == JudgeResult.Good) audioManager.playClip(SoundEffects.judgeGood);
        // if(result == JudgeResult.SPerfect) MusicManager.Instance.music.PlayOneShot(SoundEffects.judgeSPerfect);
        // else if(result == JudgeResult.Perfect) MusicManager.Instance.music.PlayOneShot(SoundEffects.judgePerfect);
        // else if(result == JudgeResult.Good) MusicManager.Instance.music.PlayOneShot(SoundEffects.judgeGood);
    }

    private string lateUpdate_scoreTxt;
    void LateUpdate() {
        // Show tap effects
        while(tapEffectQueue.Count > 0){
            (bool, float) tapEffectData = tapEffectQueue.Dequeue(); // (isLongNote, xPosition)

            if(tapEffectData.Item1){
                // Long Tap animation
                longTapEffectTransforms[longTapEffectIndex].localPosition = new Vector2(tapEffectData.Item2, 0);
                longTapEffectParticles[longTapEffectIndex].Play();

                if(++longTapEffectIndex >= longTapEffectParticles.Count) longTapEffectIndex = 0;
            }else{
                shortTapEffectTransforms[shortTapEffectIndex].localPosition = new Vector2(tapEffectData.Item2, 0);
                shortTapEffectParticles[shortTapEffectIndex].Play();

                if(++shortTapEffectIndex >= shortTapEffectParticles.Count) shortTapEffectIndex = 0;
            }
        }

        if(!shouldUpdate) return;       // judgement hasn't occurred

        

        // If adjust sync, show tap result and don't show rest of the animations
        if(gameManager.isAdjustSync){
            
            return;
        }

        // Show score
        if(!Data.tempData.ContainsKey("NewInput")){
        if(GameStat.score == 0) scoreText.text = "<sprite=0>";
        else {
            lateUpdate_scoreTxt = "";
            int digitCount = 0;
            for(int i = GameStat.score; i > 0; i /= 10){
                lateUpdate_scoreTxt = spriteTexts[i % 10] + lateUpdate_scoreTxt;
                if(++digitCount % 3 == 0 && i >= 10) lateUpdate_scoreTxt = "<sprite=10>" + lateUpdate_scoreTxt;
            }
            scoreText.text = lateUpdate_scoreTxt;
        }
        }

        // Show rave effect
        if(GameStat.raveLevel != activeRaveLevel){
            // Rave level has changed
            for(int i = 0; i < raveEffects.Length; i++){
                raveEffects[i].SetActive(i == GameStat.raveLevel);
            }

            if(GameStat.raveLevel == 4){
                // Entered ultra rave
                ultraRaveEffectObject.SetActive(true);
                ultraRaveAnimatorL.Play("Iz_ultrarave_in");
                ultraRaveAnimatorR.Play("Iz_ultrarave_in");
            }else if(GameStat.raveLevel == 0 && activeRaveLevel == 4){
                // Was ultra rave, but missed
                ultraRaveAnimatorL.Play("Iz_ultrarave_out");
                ultraRaveAnimatorR.Play("Iz_ultrarave_out");
            }
            
            // Update rave text
            raveTextObjects[activeRaveLevel].SetActive(false);
            raveTextObjects[GameStat.raveLevel].SetActive(true);

            activeRaveLevel = GameStat.raveLevel;
        }

        float gaugePercent = 0;
        if(GameStat.ravePercent >= 52) gaugePercent = 1f;
        else if(GameStat.ravePercent >= 36) gaugePercent = (GameStat.ravePercent - 36)/16f;
        else if(GameStat.ravePercent >= 22) gaugePercent = (GameStat.ravePercent - 22)/14f;
        else if(GameStat.ravePercent >= 10) gaugePercent = (GameStat.ravePercent - 10)/12f;
        else gaugePercent = GameStat.ravePercent / 10f;
        

        if(GameStat.raveLevel <= 3) raveGaugeForegrounds[GameStat.raveLevel].localScale = new Vector2(gaugePercent, 1);
        // if(GameStat.raveLevel <= 3) raveGaugeForegrounds[GameStat.raveLevel].localScale = new Vector2(gaugePercent <= 1 ? gaugePercent : 1, 1);
        if(GameStat.ravePercent == 0){
            // Rave zero, health < 1
            healthGauge.localScale = new Vector2(GameStat.health >= 0 ? GameStat.health : 0, 1);
        }

        shouldUpdate = false;
    }

    public void playLiveClear()
    {
        liveClear.SetActive(true);
    }

    public void hideLiveClear()
    {
        liveClear.SetActive(false);
    }

    public void playResume()
    {

    }
}