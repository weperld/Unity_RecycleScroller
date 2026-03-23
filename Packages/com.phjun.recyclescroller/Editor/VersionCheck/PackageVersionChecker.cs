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
        private const string PREFS_POSTPONE_UNTIL_KEY = "RecycleScroller_PostponeUntil";
        private const string PREFS_POSTPONE_VERSION_KEY = "RecycleScroller_PostponeVersion";

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
            var postponeVersion = EditorPrefs.GetString(PREFS_POSTPONE_VERSION_KEY, string.Empty);

            if (!string.IsNullOrEmpty(postponeVersion))
            {
                if (postponeVersion != currentVersion)
                {
                    // 설치 버전이 변경됨 → 사용자가 직접 업데이트 → postpone 해제
                    EditorPrefs.DeleteKey(PREFS_POSTPONE_UNTIL_KEY);
                    EditorPrefs.DeleteKey(PREFS_POSTPONE_VERSION_KEY);
                }
                else
                {
                    // postpone 기간 내인지 확인
                    var postponeUntilStr = EditorPrefs.GetString(PREFS_POSTPONE_UNTIL_KEY, string.Empty);
                    if (!string.IsNullOrEmpty(postponeUntilStr)
                        && long.TryParse(postponeUntilStr, out var ticks))
                    {
                        var postponeUntil = new DateTime(ticks, DateTimeKind.Utc);
                        if (DateTime.UtcNow < postponeUntil)
                            return;
                    }

                    // postpone 만료 → 키 정리
                    EditorPrefs.DeleteKey(PREFS_POSTPONE_UNTIL_KEY);
                    EditorPrefs.DeleteKey(PREFS_POSTPONE_VERSION_KEY);
                }
            }

            // Compare versions
            if (IsNewerVersion(latestVersion, currentVersion))
            {
                UpdateNotificationWindow.Show(currentVersion, latestVersion);
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

        internal static void SetPostpone(string currentVersion, DateTime until)
        {
            EditorPrefs.SetString(PREFS_POSTPONE_VERSION_KEY, currentVersion);
            EditorPrefs.SetString(PREFS_POSTPONE_UNTIL_KEY, until.Ticks.ToString());
        }
    }
}
