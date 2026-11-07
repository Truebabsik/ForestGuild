using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Чинит ссылку PanelSettings у UIDocument в открытой сцене и сохраняет сцену.
/// Сеттер panelSettings не всегда сериализуется при сборке сцены скриптом.
/// </summary>
public static class FixPanelSettings
{
    public static string Run()
    {
        const string panelPath = "Assets/UI/MapPanelSettings.asset";
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
        if (panel == null) return "ОШИБКА: не загрузился " + panelPath;

        var document = Object.FindAnyObjectByType<UIDocument>();
        if (document == null) return "ОШИБКА: UIDocument не найден в открытой сцене";

        var serialized = new SerializedObject(document);
        var property = serialized.FindProperty("m_PanelSettings");
        if (property == null) return "ОШИБКА: нет поля m_PanelSettings";

        property.objectReferenceValue = panel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var scene = document.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var check = new SerializedObject(document).FindProperty("m_PanelSettings").objectReferenceValue;
        return "panel=" + panel.name + " assigned=" + (check != null) + " scene=" + scene.path;
    }
}
