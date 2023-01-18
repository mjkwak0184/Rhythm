using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaAnimationView : MonoBehaviour {

    [SerializeField]
    private GameObject[] defaultObjects, grade0, grade1, grade2, grade3, grade4;
    [SerializeField]
    private GameObject mainAnimation, midShine, endShine;
    [SerializeField]
    private AnimationEvents stageCameraSwitch;
    [SerializeField]
    private AudioSource[] soundEffects;
    public System.Action animationEndCallback;
    private int grade = 0;

    void Start()
    {
        for(int i = 0; i < defaultObjects.Length; i++){
            grade0[i].SetActive(false);
            grade1[i].SetActive(false);
            grade2[i].SetActive(false);
            grade3[i].SetActive(false);
            grade4[i].SetActive(false);
        }

        for(int i = 0; i < soundEffects.Length; i++){
            soundEffects[i].volume = Data.saveData.settings_effectVolume;
        }


        stageCameraSwitch.callback = showMidShine;
    }

    public void Init(List<string> results)
    {
        int maxGrade = 0;
        for(int i = 0; i < results.Count; i++){
            // <collection_id>:<member_id>:<draw_result>:<before_level>:<after_level>
            int grade = CardData.getGradeFromRawLevel(int.Parse(results[i].Split(":")[2]));
            if(grade > maxGrade){
                maxGrade = grade;
            }
        }
        this.grade = maxGrade;
        soundEffects[0].clip = Resources.Load("Audio/se_burst_big_" + (maxGrade+1) + ".a") as AudioClip;
    }

    public void showMidShine()
    {
        stageCameraSwitch.callback = revealGrade;
        midShine.SetActive(true);
    }

    public void revealGrade()
    {
        stageCameraSwitch.callback = showEndShine;
        for(int i = 0; i < defaultObjects.Length; i++){
            if(grade == 0) grade0[i].SetActive(true);
            else if(grade == 1) grade1[i].SetActive(true);
            else if(grade == 2) grade2[i].SetActive(true);
            else if(grade == 3) grade3[i].SetActive(true);
            else if(grade == 4) grade4[i].SetActive(true);
        }
    }


    public void showEndShine()
    {
        stageCameraSwitch.callback = animationEnded;
        endShine.SetActive(true);
    }

    public void animationEnded()
    {
        if(animationEndCallback != null) animationEndCallback();
        Destroy(mainAnimation);
    }

    public void showCards()
    {
        StartCoroutine(_showCards());
    }

    IEnumerator _showCards()
    {
        // animationCamera.gameObject.SetActive(false);
        // mainCamera.gameObject.SetActive(true);
        // Destroy(gachaAnimation);
        yield return new WaitForSeconds(0.3f);
    }


}