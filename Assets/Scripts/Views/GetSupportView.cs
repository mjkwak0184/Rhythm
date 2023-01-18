using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using Gpm.WebView;

public class GetSupportView: MonoBehaviour
{
    [SerializeField]
    private PopupAnimation popupAnimation;
    [SerializeField]
    private GameObject kakaoBtn, emailBtn;
    [SerializeField]
    private TextMeshProUGUI kakaoText, emailText, infoText;

    private string kakao, email;

    void Start()
    {
        popupAnimation.Present();

        infoText.text = "Version " + Application.version + " | " + Data.ccdEnvironment + " | " + Data.ccdBadge;
        #if RHYTHMIZ_TEST
        infoText.text += " (테스트 빌드)";
        #endif

        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.\n인터넷 연결 후 다시 시도해 주세요.", "You are not connected to the internet."), confirmAction: delegate { dismiss(); }));
            return;
        }
        WWWForm form = new WWWForm();
        UIManager.Instance.toggleLoadingScreen(true);
        WebRequests.Instance.GetJSONRequest(Data.serverURL + "/getsupport", infoLoaded, delegate(string error){
            UIManager.Instance.toggleLoadingScreen(false);
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 오류가 발생하였습니다.\n잠시 후 다시 시도해 주세요.", "A server error has occurred.\nPlease try again later."), confirmAction: delegate{dismiss();}));
        });

    }

    private void infoLoaded(Dictionary<string, object> result)
    {
        UIManager.Instance.toggleLoadingScreen(false);
        if(result.ContainsKey("kakao")){
            kakaoText.text = new LocalizedText("카카오 문의 : ", "KakaoTalk Support : ").text + (string) result["kakao"];
            kakao = (string) result["kakao"];
            kakaoBtn.SetActive(true);
        }else{
            kakaoBtn.SetActive(false);
        }

        if(result.ContainsKey("email")){
            emailText.text = new LocalizedText("이메일 문의 : ", "Email Support : ").text + (string) result["email"];
            email = (string) result["email"];
            emailBtn.SetActive(true);
        }else{
            emailBtn.SetActive(false);
        }

        if(!result.ContainsKey("kakao") && !result.ContainsKey("email")){
            Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("현재 운영중인 문의 창구가 없습니다.\n지원드리지 못해 죄송합니다.", "Support is currently unavailable.\nSorry for your inconvenience."), confirmAction: delegate{dismiss();}));
        }
    }

    public void kakaoSupport()
    {
        Application.OpenURL(kakao);
    }

    public void emailSupport()
    {
        #if UNITY_ANDROID
        if(Data.accountData != null){
            if(LocalizationManager.Instance.currentLocaleCode == "en"){
                Application.OpenURL("mailto:" + email + "?subject=Rhythm*IZ Support Ticket&body=User ID: " + Data.accountData.userid + "%0D%0A%0D%0A(Enter your inquiry)%0D%0A");
            }else{
                Application.OpenURL("mailto:" + email + "?subject=Rhythm*IZ 문의&body=유저 ID: " + Data.accountData.userid + "%0D%0A%0D%0A(문의내용을 적어주세요)%0D%0A");
            }
        }else{
            if(LocalizationManager.Instance.currentLocaleCode == "en"){
                Application.OpenURL("mailto:" + email + "?subject=Rhythm*IZ Support Ticket&body=(Enter your inquiry)%0D%0A");
            }else{
                Application.OpenURL("mailto:" + email + "?subject=Rhythm*IZ 문의&body=(문의내용을 적어주세요)%0D%0A");
            }
        }
        #elif UNITY_IOS
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GUIUtility.systemCopyBuffer = email;
        Alert.showAlert(new Alert(title: new LocalizedText("이메일 문의"), body: new LocalizedText("문의 이메일 주소가 복사되었습니다.\n계정 관련 문의의 경우 유저ID를 메일 본문에 적어주세요.", "Support email address has been copied.\nIf you need support regarding your user account, please include your user ID (email) in your inquiry.")));
        #endif
    }

    public void viewNotice()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(LocalizationManager.Instance.currentLocaleCode == "en"){
            GpmWebView.ShowUrl("https://ssizone.notion.site/Game-Notice-cede8d2bcd584826b00020cc82c35de0", new GpmWebViewRequest.Configuration(){
                style = GpmWebViewStyle.POPUP,
                isMaskViewVisible = true,
                isNavigationBarVisible = true,
                title = "Notice",
                navigationBarColor = "#E1458B"
            });
        }else{
            GpmWebView.ShowUrl("https://ssizone.notion.site/ssizone/Rhythm-IZ-ac57ff1ed00a4a53a378886087734453", new GpmWebViewRequest.Configuration(){
                style = GpmWebViewStyle.POPUP,
                isMaskViewVisible = true,
                isNavigationBarVisible = true,
                title = "공지",
                navigationBarColor = "#E1458B"
            });
        }
    }

    public void dismiss()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        popupAnimation.Dismiss();
    }
}