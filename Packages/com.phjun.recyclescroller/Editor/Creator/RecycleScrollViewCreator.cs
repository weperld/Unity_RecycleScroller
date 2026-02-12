using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RecycleScrollViewCreator : MonoBehaviour
{
    private const string MENU_PATH = "GameObject/UI/RecycleScroll/";
    private const string FOLDER_PATH = "Assets/ManagedAssets/AdressAssets/UIs/Prefabs/Commons/RecycleScroll/";

    [MenuItem(MENU_PATH + "Recycle Scroll View", false)]
    private static void CreateRecycleScrollView(MenuCommand menuCommand)
        => Create(menuCommand, "Recycle_Scroll_View.prefab", "Recycle Scroll View");

    [MenuItem(MENU_PATH + "Loop Scrollbar (Vertical)", false)]
    private static void CreateLoopScrollbarVertical(MenuCommand menuCommand)
        => Create(menuCommand, "Loop_Scrollbar_Vertical.prefab", "Loop Scrollbar Vertical");

    [MenuItem(MENU_PATH + "Loop Scrollbar (Horizontal)", false)]
    private static void CreateLoopScrollbarHorizontal(MenuCommand menuCommand)
        => Create(menuCommand, "Loop_Scrollbar_Horizontal.prefab", "Loop Scrollbar Horizontal");


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
        // Find Or Create Canvas
        var selectedObj = Selection.activeGameObject;
        var parent = FindOrCreateCanvas_ReturnParent(selectedObj);

        // Create Recycle Scroll View as Canvas Child
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FOLDER_PATH + prefabName);
        var instance = Instantiate(prefab, parent.transform);
        instance.name = instanceName;

        // Select Created Object
        Selection.activeObject = instance;

        // Register Undo
        Undo.RegisterCreatedObjectUndo(instance, "Create " + instance.name);
    }
}