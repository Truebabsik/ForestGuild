using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Принудительный импорт изменённых ассетов UI и отчёт о фоне.</summary>
public static class RefreshUi
{
    public static string Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Art/map_bg.jpg");
        var styleSheet = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Map.uxml");
        return "texture=" + (texture != null ? texture.width + "x" + texture.height : "null")
            + " uxml=" + (styleSheet != null);
    }
}
