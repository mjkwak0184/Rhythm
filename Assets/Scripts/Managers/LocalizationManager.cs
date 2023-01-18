using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedText
{
    public static LocalizedText Error = new LocalizedText(ko: "오류", en: "Error");
    public static LocalizedText Notice = new LocalizedText("안내", "Notice");
    public static LocalizedText Retry = new LocalizedText("재시도", "Retry");
    public static LocalizedText Confirm = new LocalizedText("확인", "OK");

    private string text_en;
    private string text_ko;

    public string text {
        get {
            if(LocalizationManager.Instance != null){
                if(LocalizationManager.Instance.currentLocaleCode == "en"){
                    return this.text_en;
                }else{
                    return this.text_ko;
                }
            }else{
                return this.text_ko;
            }
        }
    }

    public LocalizedText(string ko = "", string en = ""){
        this.text_ko = ko;
        this.text_en = en;
    }

    public LocalizedText(string text){
        this.text_ko = text;
        this.text_en = text;
    }
}

public class LocalizationManager: MonoBehaviour
{
    public enum SupportedLanguages { English, Korean }
    public static LocalizationManager Instance;
    public string currentLocaleCode;

    public string defaultLocaleCode {
        get {
            return this._defaultLocaleCode;
        }
    }
    private string _defaultLocaleCode = "ko";

    void Awake()
    {
        // runs at game launch
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        _defaultLocaleCode = LocalizationSettings.Instance.GetSelectedLocale().Identifier.Code;
        setLocale(_defaultLocaleCode);

        currentLocaleCode = this.getCurrentLocaleCode();
    }
    
    public void setLocale(string code)
    {
        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
        if(locale == null){
            locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("ko"));
        }
        LocalizationSettings.SelectedLocale = locale;
        currentLocaleCode = locale.Identifier.Code;
    }

    public void resetLocaleToDefault()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(_defaultLocaleCode));
        currentLocaleCode = getCurrentLocaleCode();
    }

    private string getCurrentLocaleCode()
    {
        string lang = LocalizationSettings.Instance.GetSelectedLocale().Identifier.Code;
        if(lang != "ko" && lang != "en"){
            lang = "ko";
        }
        return lang;
    }
}