using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Skill
{
    public enum Type { DamageCut, RaveBonus, ScoreUp, None }
    public Type type;
    public int activateInterval;
    public int activateChance;
    public int activeDuration;
    public int primaryStat;
    public int secondaryStat;
    public Skill(Type type, int activateInterval, int activateChance, int activeDuration, int primaryStat, int secondaryStat){
        this.type = type;
        this.activateInterval = activateInterval;
        this.activateChance = activateChance;
        this.activeDuration = activeDuration;
        this.primaryStat = primaryStat;
        this.secondaryStat = secondaryStat;
    }

    public string formatDescription(){
        string template = "";

        if(LocalizationManager.Instance.currentLocaleCode == "en"){
            if(type == Type.None){
                return "";
            }else if(type == Type.ScoreUp){
                if(primaryStat == secondaryStat){
                    template = "Increases S-PERFECT and PERFECT scores by %p% for %ds (activates every %is at %c% chance)";
                }else if(primaryStat > 0 && secondaryStat == 0){
                    template = "Increases S-PERFECT scores by %p% for %ds (activates every %is at %c% chance)";
                }else if(secondaryStat > 0 && primaryStat == 0){
                    template = "Increases PERFECT scores by %s% for %ds (activates every %is at %c% chance)";
                }else{
                    template = "Increases S-PERFECT scores by %p% and PERFECT scores by %s% for %ds (activates every %is at %c% chance)";
                }
            }else if(type == Type.RaveBonus){
                if(primaryStat > 0 && secondaryStat == 0){
                    template = "Increases RAVE bonus by %p% for %ds (activates every %is at %c% chance)";
                }else if(secondaryStat > 0 && primaryStat == 0){
                    template = "Increases RAVE gauge rate by %s% for %ds (activates every %is at %c% chance)";
                }else{
                    template = "Increases RAVE bonus by %p% and RAVE gauge rate by %s% for %ds (activates every %is at %c% chance)";
                }
            }else if(type == Type.DamageCut){
                if(primaryStat > 0 && secondaryStat == 0){
                    template = "Reduces damage by %p% for %ds (activates every %is at %c% chance)";
                }else{
                    template = "Reduces damage by %p% and increases S-PERFECT judge range by %s% for %ds (activates every %is at %c% chance)";
                }
            }
        }else{
            if(type == Type.None){
                return "";
            }else if(type == Type.ScoreUp){
                if(primaryStat == secondaryStat){
                    template = "%i초마다 %c%확률로 %d초동안 S-PERFECT 및 PERFECT 점수 %p% 상승";
                }else if(primaryStat > 0 && secondaryStat == 0){
                    template = "%i초마다 %c%확률로 %d초동안 S-PERFECT 점수 %p% 상승";
                }else if(secondaryStat > 0 && primaryStat == 0){
                    template = "%i초마다 %c%확률로 %d초동안 PERFECT 점수 %s% 상승";
                }else{
                    template = "%i초마다 %c%확률로 %d초동안 S-PERFECT 점수 %p% 상승, PERFECT 점수 %s% 상승";
                }
            }else if(type == Type.RaveBonus){
                if(primaryStat > 0 && secondaryStat == 0){
                    template = "%i초마다 %c%확률로 %d초동안 RAVE 보너스 %p% 증가";
                }else if(secondaryStat > 0 && primaryStat == 0){
                    template = "%i초마다 %c%확률로 %d초동안 RAVE 게이지 상승속도 %s% 증가";
                }else{
                    template = "%i초마다 %c%확률로 %d초동안 RAVE 보너스 %p% 증가, RAVE 게이지 상승속도 %s% 증가";
                }
            }else if(type == Type.DamageCut){
                if(primaryStat > 0 && secondaryStat == 0){
                    template = "%i초마다 %c%확률로 %d초동안 데미지 %p% 감소";
                }else{
                    template = "%i초마다 %c%확률로 %d초동안 데미지 %p% 감소, S-PERFECT 판정범위 %s% 증가";
                }
            }
        }
        return template.Replace("%i", activateInterval.ToString("N0")).Replace("%c", activateChance.ToString("N0")).Replace("%d", activeDuration.ToString("N0")).Replace("%p", primaryStat.ToString("N0")).Replace("%s", secondaryStat.ToString("N0"));
    }
}

public class SkillManager: MonoBehaviour
{

    private GameManager gameManager;
    private NoteManager noteManager;
    private JudgeScoreManager judgeScoreManager;

    [SerializeField]
    private GameObject gaugeIconPrefab;
    [SerializeField]
    private SpriteRenderer skillActivateCardSprite, skillActivateCardBackground;
    [SerializeField]
    private Animation skillActivateCardAnimation;
    [SerializeField]
    private Animator skillActivate;
    [SerializeField]
    private GameObject skillRave, skillDamage, skillScore;
    private GameObject skillActivateCardAnimationObj, skillActivateObj;
    private Transform gaugeIconParent;

    public static SkillManager Instance;
    public Skill[] skills = new Skill[5];
    private Coroutine loop;
    private Coroutine updateGauge;

    // Skill
    private Queue<int> skillAnimationQueue = new Queue<int>();
    private bool animationPlaying = false;

    public GameObject[] skillGauges = new GameObject[5];
    public Sprite[] skillImages = new Sprite[5];
    public int[] skillMembers = new int[5];
    private float[] skillTimers = new float[5];
    public bool[] skillActive = new bool[5];
    private Transform[] skillGaugeImages = new Transform[5];
    private double playTime;

    // public float raveBonusPrimary, raveBonusSecondary, scoreUpPrimary, scoreUpSecondary, damageCutPrimary, damageCutSecondary;

    public float sPerfectMultiplier = 1, perfectMultiplier = 1, raveSpeedMultiplier = 1, raveScoreMultiplier = 1, damageMultiplier = 1, sPerfectRangeMultiplier = 1;

    public static Color[] memberColors = new Color[12]{
        new Color(0.733f, 0.69f, 0.862f),
        new Color(0.935f, 0.824f, 0.906f),
        new Color(0.859f, 0.439f, 0.424f),
        new Color(0.988f, 0.965f, 0.584f),
        new Color(0.655f, 0.878f, 0.882f),
        new Color(0.808f, 0.898f, 0.835f),
        new Color(0.95f, 0.95f, 0.95f),
        new Color(0.718f, 0.827f, 0.914f),
        new Color(0.945f, 0.765f, 0.666f),
        new Color(0.953f, 0.666f, 0.318f),
        new Color(0.337f, 0.478f, 0.808f),
        new Color(0.851f, 0.349f, 0.549f)
    };
    
    void OnDestroy()
    {
        for(int i = 0; i < skillImages.Length; i++){
            if(skillImages[i] != null) Addressables.Release(skillImages[i]);
        }
    }

    void Awake()
    {
        if(Instance == null) Instance = this;
        else if(Instance == this) Destroy(gameObject);
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        noteManager = NoteManager.Instance;
        judgeScoreManager = JudgeScoreManager.Instance;

        gaugeIconParent = GameObject.Find("Skill/SkillGauge").transform;

        skillActivateCardAnimationObj = skillActivateCardAnimation.gameObject;
        skillActivateObj = skillActivate.gameObject;
        if(Data.saveData.settings_ingameHideSkillActivate) skillActivateObj.SetActive(false);

        setup();
    }

    public void reset()
    {
        for(int i = 0; i < 5; i++){
            if(skills[i] != null){
                skillTimers[i] = 0;
                skillActive[i] = false;
            }
            if(skillGauges[i] != null) skillGauges[i].SetActive(false);
        }
        
        sPerfectMultiplier = 1;
        perfectMultiplier = 1;
        raveSpeedMultiplier = 1;
        raveScoreMultiplier = 1;
        damageMultiplier = 1;
        sPerfectRangeMultiplier = 1;
    }

    void setup()
    {
        if(gameManager.isAdjustSync) return;

        // Instantiate prefab
        for(int i = 0; i < 5; i++){
            if(skills[i] == null) continue;
            if(skills[i].type == Skill.Type.None) continue;
            skillGauges[i] = Instantiate(gaugeIconPrefab);
            skillGauges[i].transform.SetParent(gaugeIconParent);
            skillGaugeImages[i] = skillGauges[i].transform.Find("GaugeForeground");
            if(skills[i].type == Skill.Type.DamageCut){
                skillGauges[i].transform.Find("GaugeScoreUp").gameObject.SetActive(false);
                skillGauges[i].transform.Find("GaugeRaveBonus").gameObject.SetActive(false);
            }else if(skills[i].type == Skill.Type.ScoreUp){
                skillGauges[i].transform.Find("GaugeDamageCut").gameObject.SetActive(false);
                skillGauges[i].transform.Find("GaugeRaveBonus").gameObject.SetActive(false);
            }else if(skills[i].type == Skill.Type.RaveBonus){
                skillGauges[i].transform.Find("GaugeScoreUp").gameObject.SetActive(false);
                skillGauges[i].transform.Find("GaugeDamageCut").gameObject.SetActive(false);
            }
            skillGauges[i].SetActive(false);
        }

        loop = StartCoroutine(skillLoop());
        updateGauge = StartCoroutine(skillGaugeUpdate());
    }

    private WaitForSeconds skillEffectInterval = new WaitForSeconds(1f);
    IEnumerator skillEffect()
    {
        animationPlaying = true;
        skillActivateCardAnimationObj.SetActive(true);
        while(skillAnimationQueue.Count > 0){
            int skillIndex = skillAnimationQueue.Dequeue();
            Skill.Type type = skills[skillIndex].type;
            if(!Data.saveData.settings_ingameHideSkillActivate){
                // if skill activate animation at center is not hidden
                if(type == Skill.Type.RaveBonus){
                    skillRave.SetActive(true);
                    skillDamage.SetActive(false);
                    skillScore.SetActive(false);
                }else if(type == Skill.Type.ScoreUp){
                    skillRave.SetActive(false);
                    skillDamage.SetActive(false);
                    skillScore.SetActive(true);
                }else if(type == Skill.Type.DamageCut){
                    skillRave.SetActive(false);
                    skillDamage.SetActive(true);
                    skillScore.SetActive(false);
                }
                skillActivate.Play("In");
            }
            skillActivateCardBackground.color = memberColors[skillMembers[skillIndex]];
            skillActivateCardSprite.sprite = skillImages[skillIndex];
            skillActivateCardAnimation.Stop();
            skillActivateCardAnimation.Play();            
            
            yield return skillEffectInterval;
        }

        animationPlaying = false;
    }

    IEnumerator skillLoop()
    {
        bool shouldUpdateValues = false;
        while(true){
            while(noteManager.interpolatedTime < 0) yield return null;
            while(gameManager.gameState != GameState.Playing) yield return null;
            for(int i = 0; i < 5; i++){
                if(skills[i] == null) continue;
                if(skills[i].type == Skill.Type.None) continue;

                skillTimers[i] += Time.deltaTime;

                if(skillActive[i]){
                    if(skillTimers[i] > skills[i].activeDuration){
                        // deactivate skill
                        skillActive[i] = false;
                        skillTimers[i] = 0;
                        skillGauges[i].SetActive(false);
                        shouldUpdateValues = true;
                    }
                }else{
                    if(skillTimers[i] > skills[i].activateInterval){
                        if(Random.Range(0, 100) < skills[i].activateChance){
                            skillActive[i] = true;
                            skillAnimationQueue.Enqueue(i);
                            if(!animationPlaying) StartCoroutine(skillEffect());
                            skillGauges[i].SetActive(true);
                            skillGauges[i].transform.SetSiblingIndex(0);    // place it at the rightmost end
                            shouldUpdateValues = true;
                        }
                        skillTimers[i] = 0;
                    }
                }
                
            }

            if(shouldUpdateValues){

                sPerfectMultiplier = 1;
                perfectMultiplier = 1;
                raveSpeedMultiplier = 1;
                raveScoreMultiplier = 1;
                damageMultiplier = 1;
                sPerfectRangeMultiplier = 1;

                for(int i = 0; i < 5; i++){
                    if(skills[i] == null) continue;
                    if(skillActive[i]){
                        if(skills[i].type == Skill.Type.RaveBonus){
                            raveScoreMultiplier += (float)skills[i].primaryStat * 0.01f;
                            raveSpeedMultiplier += (float) skills[i].secondaryStat * 0.01f;
                        }else if(skills[i].type == Skill.Type.ScoreUp){
                            sPerfectMultiplier += (float)skills[i].primaryStat * 0.01f;
                            perfectMultiplier += (float)skills[i].secondaryStat * 0.01f;
                        }else if(skills[i].type == Skill.Type.DamageCut){
                            damageMultiplier *= 1 - ((float) skills[i].primaryStat * 0.01f);
                            sPerfectRangeMultiplier += (float)skills[i].secondaryStat * 0.01f;
                        }
                    }
                }

                shouldUpdateValues = false;
            }


            yield return null;
        }
    }

    private WaitForSeconds skillGaugeUpdateInterval = new WaitForSeconds(0.1f);
    IEnumerator skillGaugeUpdate()
    {
        while(true){
            yield return skillGaugeUpdateInterval;
            for(int i = 0; i < 5; i++){
                if(skills[i] == null) continue;
                if(!skillActive[i]) continue;
                float fillpercent = skillTimers[i] / (float)skills[i].activeDuration;
                skillGaugeImages[i].localScale = new Vector2(1 - fillpercent, 1);
            }
        }
    }
}