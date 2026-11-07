using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Билдер сцены Main: PanelSettings + UIDocument + MapScreen.
/// Запуск: unity command run_script --file AgentScripts/BuildScene.cs --entry BuildScene.Run
/// </summary>
public static class BuildScene
{
    public static string Run()
    {
        const string scenePath = "Assets/Scenes/Main.unity";
        const string panelPath = "Assets/UI/MapPanelSettings.asset";

        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            Directory.CreateDirectory("Assets/UI");
            AssetDatabase.CreateAsset(panel, panelPath);
        }

        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1920, 1080);
        panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panel.match = 0.5f;

        var themePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";
        var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
        if (theme == null)
        {
            var themeGuids = AssetDatabase.FindAssets("t:ThemeStyleSheet");
            if (themeGuids.Length > 0)
                theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(themeGuids[0]));
        }

        if (theme != null) panel.themeStyleSheet = theme;

        EditorUtility.SetDirty(panel);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.08f);
        camera.orthographic = true;
        cameraObject.tag = "MainCamera";

        var uiObject = new GameObject("UI");
        var document = uiObject.AddComponent<UIDocument>();
        document.panelSettings = panel;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Map.uxml");
        uiObject.AddComponent<ForestGuild.Game.MapScreen>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);

        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (buildScenes.All(s => s.path != scenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        AssetDatabase.Refresh();
        return "scene=" + scenePath + " panel=" + panelPath + " theme=" + (panel.themeStyleSheet != null);
    }
}
