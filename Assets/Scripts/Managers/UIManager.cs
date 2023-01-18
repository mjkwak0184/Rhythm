using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

public class UIManager: MonoBehaviour
{
    [SerializeField]
    public LocalAssets localAssets;
    [SerializeField]
    private GameObject alertView, loadingScreenView, rankingView, cardGachaView, userProfileView, challengeRankingView;
    public enum View { Alert, LoadingScreen, Ranking, CardGacha, UserProfile, ChallengeRanking }

    public static UIManager Instance;
    public string previousSceneName;
    private GameObject additive_eventSystem;
    private int renderFrameInterval = 1;
    private int renderFrameIncreaseCalls = 0;
    private HashSet<int> renderFrameIncreaseRequested = new HashSet<int>();

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void IncreaseRenderFrame()
    {
        OnDemandRendering.renderFrameInterval = 1;
        renderFrameIncreaseCalls++;
    }

    public void DecreaseRenderFrame()
    {
        renderFrameIncreaseCalls--;
        if(renderFrameIncreaseCalls <= 0){
            renderFrameIncreaseCalls = 0;
            OnDemandRendering.renderFrameInterval = renderFrameInterval;
        }
        
    }

    public void SetRenderFrameInterval(int val)
    {
        renderFrameIncreaseCalls = 0;
        #if UNITY_ANDROID
        int newval = val > 2 ? 2 : val;
        renderFrameInterval = newval;
        OnDemandRendering.renderFrameInterval = newval;
        #else
        renderFrameInterval = val;
        OnDemandRendering.renderFrameInterval = val;
        #endif
    }

    public void ScrollRectSmoothScrollClear(ScrollRect rect)
    {
        int id = rect.GetInstanceID();
        if(renderFrameIncreaseRequested.Contains(id)){
            renderFrameIncreaseRequested.Remove(id);
            DecreaseRenderFrame();
        }
    }
    public void ScrollRectSmoothScrollClear(LoopScrollRect rect)
    {
        int id = rect.GetInstanceID();
        if(renderFrameIncreaseRequested.Contains(id)){
            renderFrameIncreaseRequested.Remove(id);
            DecreaseRenderFrame();
        }
    }

    public void ScrollRectSmoothScroll(ScrollRect rect)
    {
        float velocity = 0;
        if(rect.horizontal && rect.vertical) velocity = Mathf.Abs(rect.velocity.x > rect.velocity.y ? rect.velocity.x : rect.velocity.y);
        else if(rect.horizontal) velocity = Mathf.Abs(rect.velocity.x);
        else if(rect.vertical) velocity = Mathf.Abs(rect.velocity.y);
        int id = rect.GetInstanceID();
        if(velocity > 8 && OnDemandRendering.renderFrameInterval != 1){
            renderFrameIncreaseRequested.Add(id);
            IncreaseRenderFrame();
        }else if(velocity <= 8 && OnDemandRendering.renderFrameInterval == 1 && renderFrameIncreaseRequested.Contains(id)){
            renderFrameIncreaseRequested.Remove(id);
            DecreaseRenderFrame();
        }
    }

    public void ScrollRectSmoothScroll(LoopScrollRect rect)
    {
        float velocity = 0;
        if(rect.horizontal && rect.vertical) velocity = Mathf.Abs(rect.velocity.x > rect.velocity.y ? rect.velocity.x : rect.velocity.y);
        else if(rect.horizontal) velocity = Mathf.Abs(rect.velocity.x);
        else if(rect.vertical) velocity = Mathf.Abs(rect.velocity.y);
        int id = rect.GetInstanceID();
        if(velocity == 0) return;
        if(velocity > 8 && OnDemandRendering.renderFrameInterval != 1){
            renderFrameIncreaseRequested.Add(id);
            IncreaseRenderFrame();
        }else if(velocity <= 8 && OnDemandRendering.renderFrameInterval == 1 && renderFrameIncreaseRequested.Contains(id)){
            renderFrameIncreaseRequested.Remove(id);
            DecreaseRenderFrame();
        }
    }

    public IEnumerator loadImageAddressableAsync(Image image, string address){
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        yield return handle;
        if(handle.Status == AsyncOperationStatus.Succeeded) image.sprite = handle.Result;
        yield return null;
    }

    public void loadScene(string sceneName)
    {
        previousSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void loadScene(string sceneName, Action callback){
        previousSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
        callback();
    }

    public void loadSceneAsync(string sceneName){
        StartCoroutine(CoroutineLoadSceneAsync(sceneName, null));
    }

    public void loadSceneAsync(string sceneName, Action callback = null){
        StartCoroutine(CoroutineLoadSceneAsync(sceneName, callback));
    }

    public void loadPreviousSceneAsync()
    {
        StartCoroutine(CoroutineLoadPreviousSceneAsync());
    }

    public IEnumerator CoroutineLoadPreviousSceneAsync()
    {
        string current = SceneManager.GetActiveScene().name;
        if(previousSceneName == "GameResultScene") previousSceneName = "LobbyScene";
        AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(previousSceneName);
        previousSceneName = current;
        while(!sceneLoad.isDone) yield return null;
    }

    public IEnumerator CoroutineLoadSceneAsync(string sceneName, Action callback = null)
    {
        previousSceneName = SceneManager.GetActiveScene().name;
        AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName);
        while(!sceneLoad.isDone) yield return null;
        if(callback != null) callback();
    }

    public void toggleSceneAdditive(string sceneName, Action callback = null)
    {
        if(SceneManager.GetSceneByName(sceneName).isLoaded){
            StartCoroutine(unloadSceneAdditive(sceneName, callback));
        }else{
            StartCoroutine(loadSceneAdditive(sceneName, callback));
        }
    }

    public IEnumerator loadSceneAdditive(string sceneName, Action callback = null)
    {
        if(!SceneManager.GetSceneByName(sceneName).isLoaded){
            additive_eventSystem = GameObject.Find("EventSystem");
            if(additive_eventSystem != null) additive_eventSystem.SetActive(false);
            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while(!sceneLoad.isDone) yield return null;
        }
        if(callback != null) callback();
    }

    public IEnumerator unloadSceneAdditive(string sceneName, Action callback = null)
    {
        if(SceneManager.GetSceneByName(sceneName).isLoaded){
            AsyncOperation sceneLoad = SceneManager.UnloadSceneAsync(sceneName);
            while(!sceneLoad.isDone) yield return null;
            if(additive_eventSystem != null) additive_eventSystem.SetActive(true);
            additive_eventSystem = null;
        }
        if(callback != null) callback();
    }

    public void toggleLoadingScreen(bool on = true){
        GameObject loadingScreen = GameObject.Find("LoadingScreenView");
        if(loadingScreen != null && !on){
            DecreaseRenderFrame();
            Destroy(loadingScreen);
        }else if(loadingScreen == null && on){
            IncreaseRenderFrame();
            loadingScreen = InstantiateObj(View.LoadingScreen);
            loadingScreen.name = "LoadingScreenView";
        }
    }

    public GameObject InstantiateObj(View view)
    {
        GameObject obj;
        if(view == View.Alert){
            obj = Instantiate(alertView);
        }else if(view == View.LoadingScreen){
            obj = Instantiate(loadingScreenView);
        }else if(view == View.Ranking){
            obj = Instantiate(rankingView);
        }else if(view == View.CardGacha){
            obj = Instantiate(cardGachaView);
        }else if(view == View.UserProfile){
            obj = Instantiate(userProfileView);
        }else if(view == View.ChallengeRanking){
            obj = Instantiate(challengeRankingView);
        }else{
            return null;
        }
        obj.transform.SetParent(GameObject.Find("Canvas").transform);
        obj.transform.localScale = Vector2.one;
        return obj;
    }

    public GameObject InstantiateObj(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.transform.SetParent(GameObject.Find("Canvas").transform);
        obj.transform.localScale = Vector2.one;
        return obj;
    }

    public static void androidOpenAppSettings()
    {
        using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            string packageName = currentActivityObject.Call<string>("getPackageName");
    
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            using (AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null))
            using (var intentObject = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uriObject))
            {
                intentObject.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                intentObject.Call<AndroidJavaObject>("setFlags", 0x10000000);
                currentActivityObject.Call("startActivity", intentObject);
            }
        }
    }
}