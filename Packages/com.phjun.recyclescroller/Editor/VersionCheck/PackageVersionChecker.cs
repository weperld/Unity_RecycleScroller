using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace RecycleScroll.Editor
{
    [InitializeOnLoad]
    public static class PackageVersionChecker
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/weperld/Unity_RecycleScroller/releases/latest";
        private const string PACKAGE_NAME = "com.phjun.recyclescroller";
        private const string SESSION_STATE_KEY = "RecycleScroller_VersionChecked";
        private const string PREFS_SKIP_VERSION_KEY = "RecycleScroller_SkipVersion";

        static PackageVersionChecker()
        {
            // Check only once per editor session
            if (SessionState.GetBool(SESSION_STATE_KEY, false))
                return;

            SessionState.SetBool(SESSION_STATE_KEY, true);

            // Delay to avoid blocking editor startup
            EditorApplication.delayCall += CheckVersion;
        }

        private static void CheckVersion()
        {
            // Get current package version
            var currentVersion = GetCurrentPackageVersion();
            if (string.IsNullOrEmpty(currentVersion))
            {
                Debug.LogWarning("[RecycleScroller] Failed to read current package version.");
                return;
            }

            // Start network request
            var request = UnityWebRequest.Get(GITHUB_API_URL);
            request.timeout = 5; // 5 seconds timeout to avoid blocking

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += (op) => OnVersionCheckCompleted(request, currentVersion);
        }

        private static string GetCurrentPackageVersion()
        {
            try
            {
                var packagePath = "Packages/" + PACKAGE_NAME + "/package.json";
                var jsonText = System.IO.File.ReadAllText(packagePath);

                // Simple JSON parsing for version field
                var versionMatch = System.Text.RegularExpressions.Regex.Match(jsonText, @"""version""\s*:\s*""([^""]+)""");
                if (versionMatch.Success)
                {
                    return versionMatch.Groups[1].Value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RecycleScroller] Error reading package.json: " + e.Message);
            }

            return null;
        }

        private static void OnVersionCheckCompleted(UnityWebRequest request, string currentVersion)
        {
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var latestVersion = ParseLatestVersionFromJson(request.downloadHandler.text);
                    if (!string.IsNullOrEmpty(latestVersion))
                    {
                        CompareAndNotify(currentVersion, latestVersion);
                    }
                }
                else
                {
                    // Silently fail - don't interrupt editor startup
                    // Only log in development builds
                    #if UNITY_EDITOR && DEBUG_VERSION_CHECK
                    Debug.Log("[RecycleScroller] Version check failed: " + request.error);
                    #endif
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RecycleScroller] Error processing version check: " + e.Message);
            }
            finally
            {
                request.Dispose();
            }
        }

        private static string ParseLatestVersionFromJson(string jsonText)
        {
            try
            {
                // Simple JSON parsing for tag_name field (e.g., "v1.2.0")
                var tagMatch = System.Text.RegularExpressions.Regex.Match(jsonText, @"""tag_name""\s*:\s*""v?([^""]+)""");
                if (tagMatch.Success)
                {
                    return tagMatch.Groups[1].Value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RecycleScroller] Error parsing GitHub release data: " + e.Message);
            }

            return null;
        }

        private static void CompareAndNotify(string currentVersion, string latestVersion)
        {
            // Check if user chose to skip this version
            var skippedVersion = EditorPrefs.GetString(PREFS_SKIP_VERSION_KEY, string.Empty);
            if (skippedVersion == latestVersion)
                return;

            // Compare versions
            if (IsNewerVersion(latestVersion, currentVersion))
            {
                ShowUpdatePopup(currentVersion, latestVersion);
            }
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.');
                var currentParts = current.Split('.');

                for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
                {
                    var latestPart = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;
                    var currentPart = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;

                    if (latestPart > currentPart)
                        return true;
                    if (latestPart < currentPart)
                        return false;
                }

                return false; // Versions are equal
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RecycleScroller] Error comparing versions '{latest}' and '{current}': {e.Message}");
                return false; // If parsing fails, assume not newer
            }
        }

        private static void ShowUpdatePopup(string currentVersion, string latestVersion)
        {
            var message = $"RecycleScroller 패키지의 새로운 버전이 출시되었습니다!\n\n" +
                          $"현재 버전: {currentVersion}\n" +
                          $"최신 버전: {latestVersion}\n\n" +
                          $"업데이트하려면:\n" +
                          $"1. Package Manager를 엽니다 (Window > Package Manager)\n" +
                          $"2. RecycleScroller 패키지를 선택합니다\n" +
                          $"3. 우측 상단의 'Update' 버튼을 클릭하거나\n" +
                          $"4. 'Remove' 후 Git URL로 재설치합니다:\n" +
                          $"   https://github.com/weperld/Unity_RecycleScroller.git?path=Packages/com.phjun.recyclescroller";

            var choice = EditorUtility.DisplayDialogComplex(
                "RecycleScroller 업데이트 알림",
                message,
                "GitHub 릴리즈 페이지 열기",
                "나중에",
                "이 버전 건너뛰기"
            );

            switch (choice)
            {
                case 0: // Open GitHub releases page
                    Application.OpenURL("https://github.com/weperld/Unity_RecycleScroller/releases");
                    break;

                case 1: // Later
                    // Do nothing, will check again next session
                    break;

                case 2: // Skip this version
                    EditorPrefs.SetString(PREFS_SKIP_VERSION_KEY, latestVersion);
                    break;
            }
        }
    }
}
