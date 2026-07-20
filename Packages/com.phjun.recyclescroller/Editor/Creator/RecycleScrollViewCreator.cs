using RecycleScroll.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RecycleScrollViewCreator : MonoBehaviour
{
    internal const string MENU_PATH = "GameObject/UI/RecycleScroll/";
    private const string FOLDER_PATH = "Packages/com.phjun.recyclescroller/Runtime/Prefabs/";

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
    /// 사용자 템플릿으로부터 생성. 생성된 메뉴 코드(RecycleScrollerTemplateMenu)에서 호출된다.
    /// 생성 코드는 Assets 아래 다른 어셈블리로 컴파일되므로 internal 이면 접근할 수 없다.
    /// </summary>
    public static void CreateFromTemplate(MenuCommand menuCommand, int index)
    {
        var templates = RecycleScrollerTemplateSettings.instance.Templates;
        if (index < 0 || index >= templates.Count)
        {
            Debug.LogError($"[RecycleScroller] 템플릿 인덱스 {index} 가 설정 범위를 벗어났습니다. {RecycleScrollerTemplateSettings.SETTINGS_MENU_PATH} 에서 다시 적용해 주세요.");
            return;
        }

        var entry = templates[index];
        if (entry == null || entry.m_prefab == null)
        {
            Debug.LogError($"[RecycleScroller] 템플릿 {index + 1} 의 프리팹이 비어 있습니다.");
            return;
        }

        var undoGroup = BeginUndoGroup();

        var parent = ResolveTemplateParent(entry.m_prefab, Selection.activeGameObject);

        var instance = InstantiateUnder(entry.m_prefab, parent) as GameObject;
        if (instance == null)
        {
            Debug.LogError($"[RecycleScroller] 프리팹 '{entry.m_prefab.name}' 인스턴스 생성에 실패했습니다.");
            return;
        }

        // Undo 등록이 언팩보다 앞서야 한다. 순서가 뒤집히면 되감을 때
        // 생성 취소로 지워진 오브젝트를 언팩 취소가 프리팹 인스턴스로 되살린다.
        FinalizeCreatedObject(instance, RecycleScrollerTemplateSettings.GetObjectName(entry));
        Unpack(instance, entry.m_unpackMode);

        EndUndoGroup(undoGroup, instance.name);
    }

    /// <summary>
    /// UI 프리팹만 Canvas 를 필요로 한다. RectTransform 이 없는 프리팹까지 Canvas 로 넣으면
    /// 3D 오브젝트가 UI 계층에 끌려 들어가므로, 그런 경우는 선택한 오브젝트 하위에 만든다.
    /// </summary>
    internal static bool NeedsCanvas(GameObject prefab)
        => prefab != null && prefab.GetComponent<RectTransform>() != null;

    private static Transform ResolveTemplateParent(GameObject prefab, GameObject selectedObj)
    {
        if (NeedsCanvas(prefab)) return FindOrCreateCanvas_ReturnParent(selectedObj).transform;

        // 선택이 없으면 씬 루트에 생성한다 (Unity 의 3D Object 생성 메뉴와 같은 동작).
        return selectedObj == null ? null : selectedObj.transform;
    }

    private static Object InstantiateUnder(GameObject prefab, Transform parent)
    {
        if (parent == null) return PrefabUtility.InstantiatePrefab(prefab);

        return PrefabUtility.InstantiatePrefab(prefab, parent);
    }

    /// <summary>생성 마무리 — 이름 지정, 형제 중복 회피, 선택, Undo 등록. 모든 생성 경로가 공유한다.</summary>
    private static void FinalizeCreatedObject(GameObject instance, string name)
    {
        instance.name = name;

        // Unity 표준 UI 생성 메뉴와 동일하게 형제 중 이름이 겹치면 "(1)" 을 붙인다.
        // Undo 등록보다 먼저 해야 Undo 항목 표기가 실제 이름과 일치한다.
        GameObjectUtility.EnsureUniqueNameForSibling(instance);

        Selection.activeObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Create " + instance.name);
    }

    /// <summary>캔버스 생성·오브젝트 생성·언팩을 Ctrl+Z 한 번에 되돌리기 위한 그룹 시작.</summary>
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

    private static void Unpack(GameObject instance, TemplateUnpackMode mode)
    {
        var unpackMode = ToPrefabUnpackMode(mode);
        if (unpackMode.HasValue is false) return;

        PrefabUtility.UnpackPrefabInstance(instance, unpackMode.Value, InteractionMode.UserAction);
    }

    /// <summary>설정 값 → Unity 언팩 모드. null 이면 언팩하지 않고 Nested 를 유지한다.</summary>
    internal static PrefabUnpackMode? ToPrefabUnpackMode(TemplateUnpackMode mode)
    {
        switch (mode)
        {
            case TemplateUnpackMode.Completely: return PrefabUnpackMode.Completely;
            case TemplateUnpackMode.OutermostRoot: return PrefabUnpackMode.OutermostRoot;
            default: return null;
        }
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