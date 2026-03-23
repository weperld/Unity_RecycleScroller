using UnityEditor;
using UnityEngine;

namespace RecycleScroll.Editor
{
    public class UpdateNotificationWindow : EditorWindow
    {
        private string m_currentVersion;
        private string m_latestVersion;

        public static void Show(string currentVersion, string latestVersion)
        {
            var window = GetWindow<UpdateNotificationWindow>(true, "RecycleScroller 업데이트 알림");
            window.m_currentVersion = currentVersion;
            window.m_latestVersion = latestVersion;
            window.minSize = new Vector2(400, 220);
            window.maxSize = new Vector2(400, 220);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("새로운 버전이 출시되었습니다!", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("현재 버전: " + m_currentVersion);
            EditorGUILayout.LabelField("최신 버전: " + m_latestVersion);

            EditorGUILayout.Space(15);

            if (GUILayout.Button("GitHub 릴리즈 페이지 열기", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/weperld/Unity_RecycleScroller/releases");
                Close();
            }

            if (GUILayout.Button("패키지 매니저 열기", GUILayout.Height(30)))
            {
                UnityEditor.PackageManager.UI.Window.Open("com.phjun.recyclescroller");
                Close();
            }

            if (GUILayout.Button("나중에", GUILayout.Height(30)))
            {
                ShowPostponeConfirmation();
            }
        }

        private void ShowPostponeConfirmation()
        {
            var choice = EditorUtility.DisplayDialogComplex(
                "알림 연기",
                "업데이트 알림을 어떻게 처리할까요?",
                "확인",
                "이 버전 건너뛰기",
                "일주일 뒤에 다시 확인"
            );

            switch (choice)
            {
                case 0: // 확인 - 창만 닫기
                    Close();
                    break;
                case 1: // 이 버전 건너뛰기 - 100년 연기 (≒ 영구)
                    PackageVersionChecker.SetPostpone(
                        m_currentVersion,
                        System.DateTime.UtcNow.AddYears(100));
                    Close();
                    break;
                case 2: // 일주일 뒤에 다시 확인
                    PackageVersionChecker.SetPostpone(
                        m_currentVersion,
                        System.DateTime.UtcNow.AddDays(7));
                    Close();
                    break;
            }
        }
    }
}
