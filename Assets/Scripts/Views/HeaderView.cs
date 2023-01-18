using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gpm.WebView;
using UnityEngine.SceneManagement;

public class HeaderView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI levelText, usernameText, diamondText, sscoinText;
    [SerializeField]
    private GameObject dropdown;
    [SerializeField]
    private Image rankGaugeForeground;
    private bool dropdownShown;

    public static HeaderView Instance;

    void Awake()
    {

        if(Instance == null) Instance = this;
        else if(Instance == this) Destroy(gameObject);

        if(Data.userData != null) update();
    }

    public void update()
    {
        levelText.text = Data.userData.level;
        usernameText.text = Data.userData.username;
        diamondText.text = int.Parse(Data.userData.diamond).ToString("N0");
        sscoinText.text = int.Parse(Data.userData.sscoin).ToString("N0");

        int userLevel = int.Parse(Data.userData.level);
        int requiredExp = int.Parse(Data.userData.level) * 300;
        if(userLevel > 100) requiredExp = 12000 + (userLevel - 100) * 500;
        else if(userLevel > 40) requiredExp = 12000;
        rankGaugeForeground.fillAmount = float.Parse(Data.userData.exp) / (float)requiredExp;
    }

    public void toggleDropdown()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        if(dropdownShown){
            dropdown.SetActive(false);
            dropdownShown = false;
        }else{
            dropdown.SetActive(true);
            dropdownShown = true;
        }
    }

    public void dropdownItemTapped(int itemid){
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        string sceneToLoad;
        if(itemid == 0) sceneToLoad = "UserInfoScene";
        else if(itemid == 1) sceneToLoad = "CardStoreScene";
        else if(itemid == 2) sceneToLoad = "SettingsScene";
        else if(itemid == 3){
            // show guide
            #if UNITY_IOS
            Application.OpenURL("https://ssizone.notion.site/iOS-acdbdd890d8849249bca10fb817fe6af");
            #elif UNITY_ANDROID
            Application.OpenURL("https://ssizone.notion.site/Android-3eb96403b1de42799cbe70aa9f835a4f");
            #endif
            dropdown.SetActive(false);
            return;
        }
        else if(itemid == 4) sceneToLoad = "LobbyScene";
        else return;
        // if same scene, return
        if(sceneToLoad == SceneManager.GetActiveScene().name) return;
        
        UIManager.Instance.toggleLoadingScreen(true);
        dropdown.SetActive(false);
        UIManager.Instance.loadSceneAsync(sceneToLoad);
    }
}
