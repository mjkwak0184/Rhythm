using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using MiniJSON;

public class WebRequests: MonoBehaviour
{
    public static WebRequests Instance;

    void Awake()
    {
        if(Instance == null) Instance = this;
        else if(Instance == this) Destroy(gameObject);
    }

    // Called from TitleScene
    public IEnumerator LoadRemoteAssets(System.Action<string> onProgress, System.Action onSuccess, System.Action<string> onError)
    {
        // Debug.Log("WebRequests LoadRemoteAssets Called");
        // check for new updates
        onProgress(new LocalizedText("업데이트 확인 중...", "Checking for updates...").text);

        AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs();
        yield return updateHandle;
        // Check if remote download is needed
        AsyncOperationHandle<Int64> downloadSizeHandle = Addressables.GetDownloadSizeAsync("DataStore");
        yield return downloadSizeHandle;
        if(downloadSizeHandle.Status != AsyncOperationStatus.Succeeded){
            onError(new LocalizedText("업데이트 확인에 실패하였습니다.\n잠시 후 다시 시도해 주세요.", "Failed to check for updates.\nPlease try again later.").text);
            yield break;
        }
        // Debug.Log("Download handle: " + downloadSizeHandle.Result);
        if(downloadSizeHandle.Result != 0){
            // load remote data
            onProgress(new LocalizedText("게임 업데이트 중...", "Downloading updates...").text);
            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync("DataStore");
            // yield return handle;
            while(downloadHandle.PercentComplete < 1 && !downloadHandle.IsDone){
                onProgress(new LocalizedText("게임 업데이트 중... (", "Downloading updates...(").text + Mathf.RoundToInt(downloadHandle.GetDownloadStatus().Percent * 100f) + "%)");
                yield return null;
            }

            if(downloadHandle.Status != AsyncOperationStatus.Succeeded){
                // Debug.LogWarning(downloadHandle.OperationException);
                onError(new LocalizedText("업데이트 데이터 다운로드에 실패하였습니다.\n잠시 후 다시 시도해 주세요.", "Failed to download update data.\nPlease try again later.").text);
                yield break;
            }

            Addressables.Release(downloadHandle);
        }
        Addressables.Release(downloadSizeHandle);

        // Assets ready
        AsyncOperationHandle<GameObject> instantiateHandle = Addressables.InstantiateAsync("DataStore", instantiateInWorldSpace: true);
        yield return instantiateHandle;
        if(instantiateHandle.Status == AsyncOperationStatus.Succeeded){
            // Debug.Log("DataStore Instantiated: " + instantiateHandle.Result);
            onSuccess();
        }else{
            // Debug.LogWarning(instantiateHandle.OperationException);
            Addressables.Release(instantiateHandle);
            onError(new LocalizedText("업데이트 데이터 적용에 실패하였습니다.\n잠시 후 다시 시도해 주세요.", "Failed to apply update data.\nPlease try again later.").text);
        }
    }

    public void GetJSONRequest(string url, Action<Dictionary<string, object>> callback, Action<string> errorCallback = null)
    {
        StartCoroutine(CoroutineGetJSONRequest(url, callback, errorCallback));
    }

    public IEnumerator CoroutineGetJSONRequest(string url, Action<Dictionary<string, object>> callback, Action<string> errorCallback = null)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if(request.responseCode == 200){
                Dictionary<string, object> json = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                callback(json);
            }else{
                if(errorCallback != null) errorCallback(request.downloadHandler.text);
            }
        }
    }

    public void PostJSONRequest(string url, WWWForm form, Action<Dictionary<string, object>> callback, Action<string> errorCallback = null)
    {
        StartCoroutine(CoroutinePostJSONRequest(url, form, callback, errorCallback));
    }

    public IEnumerator CoroutinePostJSONRequest(string url, WWWForm form, Action<Dictionary<string, object>> callback, Action<string> errorCallback = null){
        form.AddField("version", Application.version);
        if(Application.platform == RuntimePlatform.Android) form.AddField("platform", "android");
        else if(Application.platform == RuntimePlatform.IPhonePlayer) form.AddField("platform", "ios");
        else form.AddField("platform", "other: " + SystemInfo.operatingSystem);
        
        form.AddField("lang", LocalizationManager.Instance.currentLocaleCode);
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if(request.responseCode == 200){
                Dictionary<string, object> result = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                callback(result);
            }else{
                if(errorCallback != null) errorCallback(request.downloadHandler.text);
                else {
                    Dictionary<string, object> res = new Dictionary<string, object>();
                    res["success"] = false;
                    res["message"] = new LocalizedText("서버 연결에 실패하였습니다.", "Failed to connect to the server.").text;
                    callback(res);
                }
            }
        }
    }
    
    public void GetRawRequest(string url, Action<string> callback, Action<string> errorCallback = null){
        StartCoroutine(CoroutineGetRawRequest(url, callback, errorCallback));
    }

    public IEnumerator CoroutineGetRawRequest(string url, Action<string> callback, Action<string> errorCallback = null){
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if(request.responseCode == 200){
                callback(request.downloadHandler.text);
            }else{
                if(errorCallback != null) errorCallback(request.downloadHandler.text);
            }
        }
    }

    public IEnumerator GetAudioClip(string url, Action<AudioClip> callback)
    {
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.ConnectionError){
            }else{
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                callback(clip);
            }
        }
    }

    public void DownloadFile(string url, string path, Action<float> callback)
    {
        if(File.Exists(path)){
            FileInfo fil = new FileInfo(path);
            fil.Delete();
        }
        StartCoroutine(CoroutineDownloadFile(url, path, callback));
    }

    public IEnumerator CoroutineDownloadFile(string url, string path, Action<float> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            DownloadHandlerFile dh = new DownloadHandlerFile(path);
            dh.removeFileOnAbort = true;
            request.downloadHandler = dh;
            yield return request.SendWebRequest();
            while(!request.isDone){
                callback(request.downloadProgress);
            }

            if(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError ) callback(-1f);
            else callback(1);
        }
    }

    
}