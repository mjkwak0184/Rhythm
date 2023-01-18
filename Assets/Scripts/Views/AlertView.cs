using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;

public class Alert
{
    public enum Type { Confirm, Alert, Input, Reward }

    public Alert.Type type;
    public string title;
    public string body;
    public Sprite rewardImage;
    public string rewardText;
    public string confirmText;
    public string cancelText;
    public bool showShine;
    public Action<string> confirmAction;
    public Action cancelAction;

    public Alert(Alert.Type type = Alert.Type.Alert, string title = "", string body = "", Sprite rewardImage = null, string rewardText = "", string confirmText = null, string cancelText = null, Action<string> confirmAction = null, Action cancelAction = null, bool showShine = false)
    {
        this.type = type;
        this.title = title.Normalize(NormalizationForm.FormKC);
        this.body = body.Normalize(NormalizationForm.FormKC);
        this.rewardImage = rewardImage;
        this.rewardText = rewardText.Normalize(NormalizationForm.FormKC);
        this.confirmText = confirmText == null ? new LocalizedText("확인", "OK").text : confirmText.Normalize(NormalizationForm.FormKC);
        this.cancelText = cancelText == null ? new LocalizedText("취소", "Cancel").text : cancelText.Normalize(NormalizationForm.FormKC);
        this.confirmAction = confirmAction;
        this.cancelAction = cancelAction;
        this.showShine = showShine;
    }

    public Alert(Alert.Type type = Alert.Type.Alert, LocalizedText title = null, LocalizedText body = null, Sprite rewardImage = null, string rewardText = "", LocalizedText confirmText = null, LocalizedText cancelText = null, Action<string> confirmAction = null, Action cancelAction = null, bool showShine = false)
    {
        this.type = type;
        this.title = title == null ? "" : title.text.Normalize(NormalizationForm.FormKC);
        this.body = body == null ? "" : body.text.Normalize(NormalizationForm.FormKC);
        this.rewardImage = rewardImage;
        this.rewardText = rewardText.Normalize(NormalizationForm.FormKC);
        this.confirmText = confirmText == null ? new LocalizedText(ko: "확인", en: "OK").text : confirmText.text;
        this.cancelText = cancelText == null ? new LocalizedText(ko: "취소", en: "Cancel").text : cancelText.text;
        this.confirmAction = confirmAction;
        this.cancelAction = cancelAction;
        this.showShine = showShine;
    }


    public static Queue<Alert> alertQueue = new Queue<Alert>();

    public static void showAlert(Alert alert){
        Alert.alertQueue.Enqueue(alert);
        if(GameObject.Find("AlertView") == null){
            GameObject view = UIManager.Instance.InstantiateObj(UIManager.View.Alert);
            view.name = "AlertView";
        }
    }
}

public class AlertView: MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI titleText, bodyText, cancelText, confirmText, rewardText;
    [SerializeField]
    TMP_InputField inputField;
    [SerializeField]
    private GameObject cancelButton, inputFieldContainer, rewardEffect, rewardShineEffect;
    [SerializeField]
    private Image rewardImage;

    [SerializeField]
    private PopupAnimation popupAnimation;

    private GameObject activePopupObject;
    private Alert currentAlert;

    void Start()
    {
        // automatically show new alert if exists
        present();
    }

    private void present()
    {
        if(Alert.alertQueue.Count > 0){
            popupAnimation.Present();
            currentAlert = Alert.alertQueue.Dequeue();
            cancelButton.SetActive(currentAlert.type == Alert.Type.Confirm || currentAlert.type == Alert.Type.Input);
            inputFieldContainer.SetActive(currentAlert.type == Alert.Type.Input);
            rewardImage.transform.parent.gameObject.SetActive(currentAlert.type == Alert.Type.Reward);
            rewardText.gameObject.SetActive(currentAlert.type == Alert.Type.Reward);
            rewardEffect.SetActive(currentAlert.type == Alert.Type.Reward);
            rewardShineEffect.SetActive(currentAlert.type == Alert.Type.Reward && currentAlert.showShine);
            if(currentAlert.type == Alert.Type.Reward){
                AudioManager.Instance.playClip(SoundEffects.gift);
                rewardText.text = currentAlert.rewardText;
                rewardImage.sprite = currentAlert.rewardImage;
            }

            titleText.text = currentAlert.title;
            bodyText.text = currentAlert.body;
            cancelText.text = currentAlert.cancelText;
            confirmText.text = currentAlert.confirmText;
            inputField.text = "";
        }else{
            // no alerts in queue, hide scene
            dismiss();
        }
    }

    public void cancelTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        if(currentAlert.cancelAction != null) currentAlert.cancelAction();
        popupAnimation.Dismiss(OnOutAnimationEnd);
        
        // if(Alert.alertQueue.Count > 0) present();
        // else dismiss();
    }

    public void confirmTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(currentAlert.confirmAction != null) currentAlert.confirmAction(inputField.text);
        popupAnimation.Dismiss(OnOutAnimationEnd);
        
        // if(Alert.alertQueue.Count > 0) present();
        // else dismiss();
    }

    public void OnOutAnimationEnd()     // runs when out animation is completed
    {
        if(Alert.alertQueue.Count > 0) present();
        else dismiss();
    }


    private void dismiss()
    {
        Destroy(gameObject);
    }
}