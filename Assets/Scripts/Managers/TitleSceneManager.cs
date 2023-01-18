using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using UnityEngine.Rendering;
using TMPro;

using Newtonsoft.Json;

// using Unity.Services.Ccd.Management;
using UnityEngine.AddressableAssets;

using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class TitleSceneManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI version, message;
    [SerializeField]

    private GameObject touchToStart, loggedIn, notLoggedIn, getSupportPrefab;
    [SerializeField]
    private ShaderVariantCollection shadersToWarm;
    [SerializeField]
    private UnityEngine.Video.VideoPlayer video;

    AudioSource music;
    bool musicLoopStarted = false;
    AudioClip titleMusic;
    AudioClip titleMusicLoop;
    
    private bool startReady = false;

    
    // // Firebase Message Handler
    // public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token) {
    //     UnityEngine.Debug.Log("Received Registration Token: " + token.Token);
    // }

    // public void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e) {
    //     UnityEngine.Debug.Log("Received a new message from: " + e.Message.From);
    // }

    void Awake()
    {
        #if ENABLE_LEGACY_INPUT_MANAGER
        Input.multiTouchEnabled = false;
        #endif

        // // Initialize Firebase
        // Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        //     var dependencyStatus = task.Result;
        //     if (dependencyStatus == Firebase.DependencyStatus.Available) {
        //         // Create and hold a reference to your FirebaseApp,
        //         // where app is a Firebase.FirebaseApp property of your application class.
        //         Data.firebase = Firebase.FirebaseApp.DefaultInstance;
        //         // Set a flag here to indicate whether Firebase is ready to use by your app.
        //         Debug.Log("[TitleSceneManager] Firebase Intialized");
        //         Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        //         Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
        //     } else {
        //         UnityEngine.Debug.LogError(System.String.Format(
        //         "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
        //         // Firebase Unity SDK is not safe to use here.
        //     }
        // });

        // Fetch CCD environment status
        if(Data.ccdURL == null){
            string getLocation(IResourceLocation location){
                if(location.InternalId.StartsWith("http")){
                    #if UNITY_EDITOR
                    Debug.Log("Addressable: " + location.InternalId);
                    #endif
                    if(location.InternalId.Contains("ee138faf-9fea-4f1f-8a06-fd84d9bf17d7")) Data.ccdEnvironment = "Development";
                    else if(location.InternalId.Contains("b0688700-90da-4085-80ee-23908acdd016")) Data.ccdEnvironment = "Staging";
                    else if(location.InternalId.Contains("887fc65f-d755-43ae-9ee7-fc758f482e4a")) Data.ccdEnvironment = "Release";
                    // fetch badge
                    string[] url = location.InternalId.Split("/");
                    for(int i = 0; i < url.Length; i++){
                        if(url[i].Contains("release_by_badge") && i + 1 < url.Length){
                            Data.ccdBadge = url[i + 1];
                            break;
                        }
                    }
                    #if !UNITY_EDITOR
                    Addressables.InternalIdTransformFunc = null;
                    #endif
                }
                return location.InternalId;
            }
            Addressables.InternalIdTransformFunc = getLocation;
        }

        // UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        // InputSystem.pollingFrequency = 1;
        music = GetComponent<AudioSource>();
        titleMusic = Resources.Load("Audio/bgm_sound_title_movie_intro.a") as AudioClip;
        Application.targetFrameRate = 60;
        touchToStart.SetActive(false);
        
        #if RHYTHMIZ_TEST
        version.text = "Ver: " + Application.version + " (테스트 빌드)";
        #else
        version.text = "Ver: " + Application.version;
        #endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if(pauseStatus) video.Pause();
        else video.Play();
    }

    // Start is called before the first frame update
    void Start()
    {

        UIManager.Instance.SetRenderFrameInterval(2);
        // Initialize Game
        music.clip = titleMusic;
        music.Play();
        // If redirected to title manager, there might already be audio playing, so stop the audiomanager music
        if(AudioManager.Instance != null) AudioManager.Instance.stopMusic();
        titleMusicLoop = Resources.Load("Audio/bgm_sound_title_movie_loop.a") as AudioClip;
        Screen.sleepTimeout = 90;  // 1.5 minutes of inactivity to turn off screen
        message.text = "";

        // Warm up shaders
        shadersToWarm.WarmUp();

        // Load save data
        try
        {
            if(!Data.loadSave()){
                // False returned, save doesn't exist
                Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("Rhythm*IZ는 SUPERSTAR IZ*ONE을 기반으로 만들어진 팬메이드 게임입니다.\n\n게임의 모든 서비스는 무료로 제공되며, 추후 여건에 따라 서비스 제공이 중단될 수 있습니다.", "Rhythm*IZ is a fanmade game based on SUPERSTAR IZ*ONE.\n\nAll game services are provided free of charge, and provision of services may stop in the future.")));
                Data.newSave();
            }
        }
        catch (System.Exception)
        {
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("게임 세이브 파일을 불러오는데 실패하였습니다.\n오류 방지를 위해 세이브 파일을 새로 생성합니다.", "Failed to load game save file.\nTo prevent errors, a new save file has been created.")));
            Data.newSave();
        }
        

        // Save loaded, apply settings
        // Set background audio volume
        music.volume = Data.saveData.settings_backgroundVolume;
        // Set music volume for AudioManager
        AudioManager.Instance.backgroundAudio.volume = Data.saveData.settings_backgroundVolume;
        AudioManager.Instance.setEffectVolume(Data.saveData.settings_effectVolume);

        // Set language
        if(Data.saveData.settings_language == 1){ // Korean
            LocalizationManager.Instance.setLocale("ko");
        }else if(Data.saveData.settings_language == 2){ // English
            LocalizationManager.Instance.setLocale("en");
        }

        // Load remote assets


        #if UNITY_EDITOR
            #if RHYTHMIZ_TEST
            Data.accountData = new AccountData("mjkwak0184@gmail.com", "1234123456785678");
            #endif
        #endif

        // Load account
        if(Data.loadAccount()){
            // Account exists and is loaded
            notLoggedIn.SetActive(false);

            // loadUser();
            loadUser();
        }else{
            // display account settings, wait until save created
            loggedIn.SetActive(false);

            // Check for version updates
            WebRequests.Instance.GetJSONRequest(Data.serverURL + "/version", delegate(Dictionary<string, object> response){
                if(response.ContainsKey("latest")){
                    if(Application.version.CompareTo((string) response["latest"]) < 0){
                        // new version available
                        if(Application.version.CompareTo((string) response["minimum"]) < 0){
                            Alert.showAlert(new Alert(title: new LocalizedText("업데이트 안내", "Update available"), body: new LocalizedText("새로운 버전이 있습니다. 게임을 플레이하려면 앱을 최신 버전으로 업데이트 해 주세요.", "You must update the game in order to play. Please update the game."), confirmAction: delegate{
                                if(response.ContainsKey("updateURL")) Application.OpenURL((string) response["updateURL"]);
                            }));
                        }else{
                            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("업데이트 안내", "Update available"), body: new LocalizedText("새로운 버전이 있습니다. 앱을 최신 버전으로 업데이트 해 주세요.", "An update to the game is available. Please update the game."), confirmAction: delegate{
                                if(response.ContainsKey("updateURL")) Application.OpenURL((string) response["updateURL"]);
                            }));
                        }
                    }
                }
            }, delegate(string error){
                // error
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("버전 확인에 실패하였습니다. 인터넷 상태를 확인해주세요.", "Failed to check for updates. Please check your internet status.")));
            });
        }

    }

    public void forgotPassword()
    {
        Application.OpenURL("https://wiz-one.space/rhythmiz_forgot");
    }

    public void setLocale(string code)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        if(code == "ko"){
            LocalizationManager.Instance.setLocale("ko");
            Data.saveData.settings_language = 1;
        }else if(code == "en"){
            LocalizationManager.Instance.setLocale("en");
            Data.saveData.settings_language = 2;
        }
        Data.saveSave();
    }

    public void createAccount_emailCheck()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet."), confirmAction: delegate{
                createAccount_emailCheck();
            }));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("새 계졍 생성", "Create new account"), body: new LocalizedText("계정에 사용할 이메일을 입력하세요.\n이메일 등록 웹사이트에서 이메일을 미리 등록하지 않았다면 취소 선택 후 이메일 등록을 먼저 진행해주세요.", "Enter your email.\nIf you have not registered your email on registration website, please register your email first."),
        confirmAction: delegate(string email){
            string stripped = email.Replace(" ", "").Replace("\n", "");
            if(!stripped.Contains("@") || stripped.Split("@")[0].Length <= 2 || stripped.Split("@")[1].Length <= 2 || !stripped.Split("@")[1].Contains(".")){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("이메일 입력을 잘못하셨습니다.\n다시 시도해 주세요.", "The email you entered is not valid. Please try again.")));
                return;
            }
            WWWForm form = new WWWForm();
            form.AddField("email", stripped);
            UIManager.Instance.toggleLoadingScreen(true);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/newuser_checkemail", form, delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if((bool) result["success"]){
                    createAccount(stripped);
                }else{
                    if(result.ContainsKey("url")){
                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Notice.text, body: (string) result["message"], confirmAction: delegate{
                            Application.OpenURL((string) result["url"]);
                        }));
                    }else{
                        Alert.showAlert(new Alert(title: LocalizedText.Notice.text, body: (string) result["message"]));
                    }
                    
                }
            }, delegate(string error){
                UIManager.Instance.toggleLoadingScreen(false);
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 오류가 발생하였습니다.\n잠시 후 다시 시도해 주세요.", "A server error has occurred.\nPlease try again later.")));
            });

        }));
    }

    public void createAccount(string email)
    {
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet."), confirmAction: delegate{
                createAccount(email);
            }));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("새 계정 생성", "Create new account"), body: new LocalizedText("계정 생성을 위한 닉네임을 입력해주세요.", "Enter a nickname you want to use."), 
            confirmAction: delegate(string username){
            if(username.Length < 2 || username.Length > 12){
                Alert.showAlert(new Alert(title: new LocalizedText("닉네임 길이 확인", "Length limit"), body: new LocalizedText("닉네임은 최소 2자, 최대 12자까지 입력 가능합니다.\n다시 입력해 주세요.", "Nickname must be between 2 to 12 characters long.\nPlease try again."), confirmAction: delegate{ createAccount(email); }));
                return;
            }
            message.text = new LocalizedText("계정 생성 중...", "Creating new account...").text;
            WWWForm form = new WWWForm();
            form.AddField("username", username);
            form.AddField("email", email);
            notLoggedIn.SetActive(false);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/newuser", form, 
                delegate (Dictionary<string, object> result) {
                    if((bool) result["success"]){
                        // login succeeded
                        Data.accountData = new AccountData((string) result["userid"], (string) result["password"]);
                        Data.saveAccount();
                        Alert.showAlert(new Alert(title: new LocalizedText("계정 연동 코드", "Account login code"), body: new LocalizedText("계정 생성이 완료되었습니다.\n게임 초기화 또는 기기 변경 등으로 계정을 잃어버렸을 경우 아래 정보를 통해 기존 계정 연결이 가능하니 스크린샷 등으로 안전하게 보관해 주세요.\n\n유저ID: " + Data.accountData.userid + "\n연동 코드: " + Data.accountData.password + "\n\n새로운 연동 코드가 필요한 경우 게임 내 [내 정보]에서 재발급이 가능합니다.", "Your account has been created successfully.\nIn cases when you need to reconnect to this account (e.g. on a new device), you can use the information below to log back in.\n\nUser ID: " + Data.accountData.userid + "\nAccount login code: " + Data.accountData.password + "\n\nIf you need a new login code, you can renew it in [Profile] screen.")));
                        
                        // Change UI
                        loggedIn.SetActive(true);
                        message.text = new LocalizedText("로그인 중...", "Logging in...").text;
                        loadUser();
                    }else{
                        notLoggedIn.SetActive(true);
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string)result["message"]));
                        message.text = "";
                    }
                },
                delegate (string error){
                    // error
                    notLoggedIn.SetActive(true);
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("오류가 발생하였습니다.\n" + error, "An error has occurred.\n"+ error)));
                    message.text = "";
                });
            }
        ));
    }



    private void loadRemoteAssets()
    {
        StartCoroutine(WebRequests.Instance.LoadRemoteAssets(
                delegate(string msg){
                    // on progress
                    message.text = msg;
                },
                delegate {
                    // On Success
                    // Load game data
                    Data.loadGame();

                    // Allow enter game
                    message.text = "";
                    startReady = true;
                    touchToStart.SetActive(true);
                    notLoggedIn.SetActive(false);
                    loggedIn.SetActive(true);
                }, 
                delegate (string error){
                    Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: error, confirmText: LocalizedText.Retry.text, confirmAction: delegate{
                        loadRemoteAssets();
                    }));
                }
            ));
    }


    void loadUser()
    {
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet."), confirmText: LocalizedText.Retry, confirmAction: delegate { loadUser(); }));
            return;
        }

        message.text = new LocalizedText("로그인 중...", "Logging in...").text;

        Data.loadDataFromServer(callback: delegate(Dictionary<string, object> result){
            if(result.ContainsKey("message")){
                if(result.ContainsKey("url")){
                    Alert.showAlert(new Alert(type: Alert.Type.Confirm, body: (string) result["message"], confirmAction: delegate{ 
                        Application.OpenURL((string) result["url"]);
                    }));
                }else{
                    Alert.showAlert(new Alert(body: (string) result["message"]));
                }
            }

            loadRemoteAssets();
        }, errorcallback: delegate(Dictionary<string, object> result){
            if(result.ContainsKey("reset")){    // password error
                Data.deleteAccount();
                Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                message.text = "";
                notLoggedIn.SetActive(true);
                loggedIn.SetActive(false);
            }else{
                
                if(result.ContainsKey("message")){
                    if(result.ContainsKey("url")){
                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, body: (string) result["message"], confirmAction: delegate{ 
                            Application.OpenURL((string) result["url"]);
                            loadUser();
                        }, cancelAction: delegate{
                            loadUser();
                        }));
                    }else{
                        Alert.showAlert(new Alert(body: (string) result["message"], confirmText: LocalizedText.Retry.text, confirmAction: delegate{ loadUser(); }));
                    }
                }else{
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("서버 오류가 발생하였습니다.\n잠시 후 다시 시도해 주세요.", "A server error has occurred.\nPlease try again later."), confirmText: LocalizedText.Retry, confirmAction: delegate{ loadUser(); }));
                }
            }
        }, isNewLogin: true);
    }

    public void connectAccount1()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("인터넷 연결 오류", "Internet error"), body: new LocalizedText("인터넷에 연결되어 있지 않습니다.", "You are not connected to the internet.")));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("이메일 입력", "Enter email"), body: new LocalizedText("연결할 계정의 유저 ID (이메일)을 입력해주세요.", "Enter the User ID (email) for your existing account."), 
        confirmAction: delegate(string userid){
            string stripped = userid.Replace(" ", "");
            if(!stripped.Contains("@")){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("유저 ID를 잘못 입력하셨습니다.\n다시 시도해 주세요.", "Invalid User ID.\nPlease try again.")));
            }else{
                connectAccount2(stripped);
            }
        }));
    }

    public void connectAccount2(string userid)
    {
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("연동 코드 입력", "Enter login code"), body: new LocalizedText("User ID : " + userid + "\n\n연동 코드를 입력해주세요.", "User ID : " + userid + "\n\nEnter your account login code."),
            confirmAction: delegate(string password){
                string stripped = password.Replace(" ", "");
                if(stripped.Length != 16) Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("연동 코드를 잘못 입력하였습니다.\n연동 코드는 숫자 16자리로 이루어져 있습니다.", "Entered login code is incorrect.\nLogin codes are 16-digits in length."), confirmAction:delegate{ connectAccount2(userid); }));
                else {
                    notLoggedIn.SetActive(false);
                    WWWForm form = new WWWForm();
                    form.AddField("userid", userid);
                    form.AddField("password", stripped);
                    WebRequests.Instance.PostJSONRequest(Data.serverURL + "/link_account", form, 
                    delegate(Dictionary<string, object> result){
                        if((bool) result["success"]){
                            Data.accountData = new AccountData(userid, stripped);
                            Data.saveAccount();
                            Alert.showAlert(new Alert(title: new LocalizedText("연결 성공", "Success"), body: new LocalizedText("계정이 연결되었습니다.\n기존 기기에서의 계정 연결을 해제하려면 [내 정보] 에서 연동 코드를 새로 발급받아주세요.", "Successfully logged into your account.\nIf you want to disconnect your login credentials in your previous device, renew your login code in [Profile] screen.")));
                            
                            // Change UI
                            loggedIn.SetActive(true);
                            message.text = new LocalizedText("로그인 중...", "Logging in...").text;
                            loadUser();
                        }else{
                            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Error.text, body: (string) result["message"], confirmText: LocalizedText.Retry.text, confirmAction:delegate{ connectAccount2(userid); }));
                            notLoggedIn.SetActive(true);
                        }
                    }, delegate(string error){
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: error));
                        notLoggedIn.SetActive(true);
                    });
                }
            }));
    }

    // Update is called once per frame
    void Update()
    {
        // Play loop version of title music after first play
        if(!musicLoopStarted && !music.isPlaying){
            music.clip = titleMusicLoop;
            music.loop = true;
            music.Play();
            musicLoopStarted = true;
        }
    }

    public void getSupportTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.InstantiateObj(getSupportPrefab);
    }

    public void startGameTapped()
    {
        if(!startReady) return;
        startReady = false;
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        UIManager.Instance.loadSceneAsync("LobbyScene");
    }
}
