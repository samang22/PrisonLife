#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// 명령줄: Unity.exe -batchmode -quit -executeMethod PrisonLifeAndroidBuild.BuildReleaseApk
/// 출력: Builds/Android/PrisonLife.apk
public static class PrisonLifeAndroidBuild
{
    public static void BuildReleaseApk()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("[PrisonLifeAndroidBuild] EditorBuildSettings에 활성 씬이 없습니다.");
            EditorApplication.Exit(1);
            return;
        }

        string dir  = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
        string path = Path.Combine(dir, "PrisonLife.apk");
        Directory.CreateDirectory(dir);

        var options = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = path,
            target           = BuildTarget.Android,
            options          = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[PrisonLifeAndroidBuild] 빌드 실패: {report.summary.result}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[PrisonLifeAndroidBuild] 완료: {path}");
        EditorApplication.Exit(0);
    }
}
#endif
