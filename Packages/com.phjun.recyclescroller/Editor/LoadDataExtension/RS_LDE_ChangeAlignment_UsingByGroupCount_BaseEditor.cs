using RecycleScroll;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RS_LDE_ChangeAlignment_UsingByGroupCount_Base<>), true)]
[CanEditMultipleObjects]
public class RS_LDE_ChangeAlignment_UsingByGroupCount_BaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawHelpBox();
        base.OnInspectorGUI();
    }

    private static void DrawHelpBox()
    {
        EditorDrawerHelper.DrawCustomHelpBox(
            "생성된 그룹데이터의 개수를 이용하여 조건에 만족하는 정렬 기준으로 덮어씌웁니다.",
            eHelpBoxMessageType.Info.ToMessageType(),
            new Color(0.8f, 0.8f, 0.9f, 1f));
    }
}
