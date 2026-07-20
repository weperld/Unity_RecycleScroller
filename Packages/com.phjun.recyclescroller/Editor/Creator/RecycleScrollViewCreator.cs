using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RecycleScrollViewCreator : MonoBehaviour
{
    internal const string MENU_PATH = "GameObject/UI/RecycleScroll/";
    private const string FOLDER_PATH = "Packages/com.phjun.recyclescroller/Runtime/Prefabs/";

    /// <summary>2.x 의 템플릿 기능이 생성하던 폴더. 3.0.0 에서는 만들지 않는다.</summary>
    private const string LEGACY_GENERATED_ROOT = "Assets/RecycleScroller.Generated";
    private const string TEMPLATE_PACKAGE_URL = "https://github.com/weperld/Unity_PrefabTemplateMenu";

    [MenuItem(MENU_PATH + "Recycle Scroll View", false)]
    private static void CreateRecycleScrollView(MenuCommand menuCommand)
        => Create(menuCommand, "Recycle Scroll View.prefab", "Recycle Scroll View");

    [MenuItem(MENU_PATH + "Recycle Scrollbar (Vertical)", false)]
    private static void CreateRecycleScrollbarVertical(MenuCommand menuCommand)
        => Create(menuCommand, "Recycle Scrollbar - Vertical.prefab", "Recycle Scrollbar Vertical");

    [MenuItem(MENU_PATH + "Recycle Scrollbar (Horizontal)", false)]
    private static void CreateRecycleScrollbarHorizontal(MenuCommand menuCommand)
        => Create(menuCommand, "Recycle Scrollbar - Horizontal.prefab", "Recycle Scrollbar Horizontal");

    /// <summary>
    /// 2.x 의 템플릿 기능이 생성한 메뉴 코드가 호출하는 진입점.
    ///
    /// 템플릿 기능은 3.0.0 에서 Prefab Template Menu 패키지로 분리되었지만, 이 메서드를
    /// 지우면 옛 생성 코드가 남은 프로젝트가 업데이트 직후 컴파일 에러로 멈춘다. 그러면
    /// 에디터 코드가 돌지 않아 그 생성 코드를 지울 수도 없게 되므로 안내용으로 남긴다.
    /// </summary>
    public static void CreateFromTemplate(MenuCommand menuCommand, int index)
    {
        Debug.LogError(
            "[RecycleScroller] 템플릿 생성 메뉴는 3.0.0 에서 별도 패키지(Prefab Template Menu)로 분리되었습니다. " +
            $"이 메뉴는 이전 버전이 만든 코드가 '{LEGACY_GENERATED_ROOT}' 에 남아 있어 표시됩니다.");

        var choice = EditorUtility.DisplayDialogComplex(
            "RecycleScroller",
            "템플릿 생성 메뉴는 3.0.0 에서 Prefab Template Menu 패키지로 분리되었습니다.\n\n" +
            $"이 메뉴는 이전 버전이 만든 코드가 '{LEGACY_GENERATED_ROOT}' 에 남아 있어 표시됩니다.\n" +
            "폴더를 지우면 메뉴도 함께 사라집니다.",
            "생성 폴더 삭제",
            "닫기",
            "패키지 페이지 열기");

        switch (choice)
        {
            case 0:
                DeleteLegacyGenerated();
                break;

            case 2:
                Application.OpenURL(TEMPLATE_PACKAGE_URL);
                break;
        }
    }

    private static void DeleteLegacyGenerated()
    {
        if (AssetDatabase.DeleteAsset(LEGACY_GENERATED_ROOT))
        {
            AssetDatabase.Refresh();
            Debug.Log($"[RecycleScroller] '{LEGACY_GENERATED_ROOT}' 을 삭제했습니다. 템플릿 설정은 " +
                      "'ProjectSettings/RecycleScrollerTemplates.asset' 에 남아 있으니 필요 없으면 직접 지워 주세요.");
            return;
        }

        Debug.LogWarning($"[RecycleScroller] '{LEGACY_GENERATED_ROOT}' 삭제에 실패했습니다. 직접 지워 주세요.");
    }

    /// <summary>생성 마무리 — 이름 지정, 형제 중복 회피, 선택, Undo 등록.</summary>
    private static void FinalizeCreatedObject(GameObject instance, string name)
    {
        instance.name = name;

        // Unity 표준 UI 생성 메뉴와 동일하게 형제 중 이름이 겹치면 "(1)" 을 붙인다.
        // Undo 등록보다 먼저 해야 Undo 항목 표기가 실제 이름과 일치한다.
        GameObjectUtility.EnsureUniqueNameForSibling(instance);

        Selection.activeObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Create " + instance.name);
    }

    /// <summary>캔버스 생성과 오브젝트 생성을 Ctrl+Z 한 번에 되돌리기 위한 그룹 시작.</summary>
    private static int BeginUndoGroup()
    {
        Undo.IncrementCurrentGroup();

        return Undo.GetCurrentGroup();
    }

    private static void EndUndoGroup(int undoGroup, string name)
    {
        Undo.SetCurrentGroupName("Create " + name);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static GameObject FindOrCreateCanvas_ReturnParent(GameObject selectedObj)
    {
        Canvas canvas = null;
        GameObject parent = selectedObj;

        if (selectedObj != null) canvas = selectedObj.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            if (selectedObj != null) canvasObject.transform.SetParent(selectedObj.transform);
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            parent = canvasObject;

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create " + canvasObject.name);
        }

        return parent;
    }

    private static void Create(MenuCommand menuCommand, string prefabName, string instanceName)
    {
        var undoGroup = BeginUndoGroup();

        // Find Or Create Canvas
        var selectedObj = Selection.activeGameObject;
        var parent = FindOrCreateCanvas_ReturnParent(selectedObj);

        // Create Recycle Scroll View as Canvas Child
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FOLDER_PATH + prefabName);
        var instance = Instantiate(prefab, parent.transform);

        FinalizeCreatedObject(instance, instanceName);

        EndUndoGroup(undoGroup, instance.name);
    }
}
