using RecycleScroll;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RS_LDE_ChangeAlignment_UsingByGroupCount2))]
[CanEditMultipleObjects]
public class RS_LDE_ChangeAlignment_UsingByGroupCount2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawHelpBox();
        base.OnInspectorGUI();
    }

    private static void DrawHelpBox()
    {
        EditorDrawerHelper.DrawCustomHelpBox(
            "괄호/논리연산((), and, or) 등을 사용한 문자열 표현식으로 조건을 검사합니다.\n"
            + "표현식의 기본 형태는 \"숫자 연산자\" 순서입니다. (예: 10 >, 20 ==)\n"
            + "상세 내용은 노션을 참고해 주십시오.",
            HelpBoxMessageType.Info.ToMessageType(),
            new Color(0.8f, 0.8f, 0.9f, 1f));
    }
}
