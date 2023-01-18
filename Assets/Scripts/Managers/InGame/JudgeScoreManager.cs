using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum JudgeResult{ SPerfect, Perfect, Good, Miss }

public class GameStat{
    public static float getRaveMultiplier(int level){
        if(level == 1){
            return 0.3f;
        }else if(level == 2){
            return 0.5f;
        }else if(level == 3){
            return 0.7f;
        }else if(level == 4){
            return 0.9f;
        }else{
            return 0f;
        }
    }

    public static int getNoteScore(JudgeResult judgeResult, float raveScoreMultiplier, float sPerfectMultiplier, float perfectMultiplier){
        float difficulty_coefficient = 0.06f;
        float judgeMultiplier = 0;
        if(judgeResult == JudgeResult.Good) judgeMultiplier = 0.5f;
        else if(judgeResult == JudgeResult.Perfect) judgeMultiplier = (5f/6f) * perfectMultiplier;
        else if(judgeResult == JudgeResult.SPerfect) judgeMultiplier = sPerfectMultiplier;

        return Mathf.RoundToInt(difficulty_coefficient * GameStat.power * judgeMultiplier * (1 + getRaveMultiplier(GameStat.raveLevel) * raveScoreMultiplier));
    }
    public static int power = 2000, raveLevel = 0;
    public static float health = 1, healthStat = 30;
    public static float ravePercent = 0;
    public static int totalNoteCount = 100;
    public static int countSPerfect = 0; 
    public static int countPerfect = 0;
    public static int countGood = 0;
    public static int countMiss = 0;
    public static bool usedRevive = false;
    private static int scoreOffsetValue = Random.Range(5, 1000);
    private static int scoreOffset = scoreOffsetValue;
    private static int _score;
    public static int score {
        get{ 
            if(_score + scoreOffsetValue != scoreOffset){
                return 0;
            }
            return _score;
        }
        set {
            if(_score + scoreOffsetValue == scoreOffset){
                _score = value;
                scoreOffset = _score + scoreOffsetValue;
            }else{
                _score = scoreOffset - scoreOffsetValue;
            }
        }
    }
    public static int combo = 0;        // checksum exclude
    public static void reset(){
        scoreOffsetValue = Random.Range(5, 1000);
        scoreOffset = scoreOffsetValue;
        health = 1;
        raveLevel = 0;
        ravePercent = 0;
        countSPerfect = 0;
        countPerfect = 0;
        countGood = 0;
        countMiss = 0;
        usedRevive = false;
        score = 0;
        combo = 0;
    }
}

public class JudgeScoreManager : MonoBehaviour
{
    public static JudgeScoreManager Instance;

    private AnimationManager animationManager;
    private GameManager gameManager;
    private SkillManager skillManager;

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        animationManager = AnimationManager.Instance;
        gameManager = GameManager.Instance;
        skillManager = SkillManager.Instance;
    }

    private float perfectMultiplier = 5f/6f;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void handleJudge(JudgeResult result, bool isLongNote = false)
    {        
        // add count and score
        if(result == JudgeResult.SPerfect){
            GameStat.countSPerfect++;
            if(GameStat.health < 1) GameStat.health += 0.01f;
            else GameStat.ravePercent += skillManager.raveSpeedMultiplier * 100f / GameStat.totalNoteCount;
        }else if(result == JudgeResult.Perfect){
            GameStat.countPerfect++;
            if(GameStat.health < 1) GameStat.health += perfectMultiplier * 0.01f;
            else GameStat.ravePercent += skillManager.raveSpeedMultiplier * 100f * perfectMultiplier / GameStat.totalNoteCount;
        }else if(result == JudgeResult.Good){
            GameStat.countGood++;
            if(GameStat.health < 1) GameStat.health += 0.005f;
            else GameStat.ravePercent += skillManager.raveSpeedMultiplier * 50f / GameStat.totalNoteCount;
        }else if(result == JudgeResult.Miss){
            GameStat.countMiss++;
            if(!gameManager.isAdjustSync) GameStat.health -= (5f / GameStat.healthStat) * skillManager.damageMultiplier;
            GameStat.combo = 0;
            GameStat.ravePercent = 0f;
        }

        if(GameStat.health < 0){
            gameManager.gameOver();
        }

        GameStat.score += GameStat.getNoteScore(result, skillManager.raveScoreMultiplier, skillManager.sPerfectMultiplier, skillManager.perfectMultiplier);

        // calculate rave status
        if(GameStat.ravePercent >= 52){
            GameStat.raveLevel = 4;
        }else if(GameStat.ravePercent >= 36){
            GameStat.raveLevel = 3;
        }else if(GameStat.ravePercent >= 22){
            GameStat.raveLevel = 2;
        }else if(GameStat.ravePercent >= 10){
            GameStat.raveLevel = 1;
        }else{
            GameStat.raveLevel = 0;
        }

        animationManager.shouldUpdate = true;
    }

}