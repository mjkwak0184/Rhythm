using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;


public class BuildScript: IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("Build");
        #if RHYTHMIZ_TEST
            // Test server build
            PlayerSettings.applicationIdentifier = "com.izone.RhythmIZdev";
            PlayerSettings.productName = "Rhythm*IZ (Test)";

            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

            PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
        #else
            PlayerSettings.productName = "Rhythm*IZ";
            #if UNITY_ANDROID
            PlayerSettings.applicationIdentifier = "com.izone.RhythmIZ";
            #elif UNITY_IOS
            PlayerSettings.applicationIdentifier = "com.iz-one.RhythmIZ";
            #endif

            PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;

            PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
        #endif
    }

    public static string AddressableOverridePlayerVersion {
        get {
            return UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.OverridePlayerVersion;
        }
    }
}
