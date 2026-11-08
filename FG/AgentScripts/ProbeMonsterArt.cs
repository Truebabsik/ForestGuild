using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Показывает модал с артом пепельного вора поверх похода.</summary>
public static class ProbeMonsterArt
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        try
        {
            type.GetMethod("ShowMenu", Flags).Invoke(screen, null);
            type.GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "expedition" });
        }
        catch (System.Exception e)
        {
            var inner = e.InnerException ?? e;
            var trace = inner.StackTrace ?? "";
            var head = trace.Length > 220 ? trace.Substring(0, 220) : trace;
            return "ошибка: " + inner.GetType().Name + ": " + inner.Message + " @ " + head.Replace("\n", " | ");
        }

        var root = screen.GetComponent<UIDocument>().rootVisualElement;

        // Останавливаем тик похода, иначе он перепишет модал ближайшей встречей.
        screen.Expedition.GetType().GetMethod("TogglePause", Flags).Invoke(screen.Expedition, null);

        var modal = root.Q<VisualElement>("monster-modal");
        var art = root.Q<VisualElement>("monster-art");

        art.ClearClassList();
        art.AddToClassList("monster-art");
        art.AddToClassList("monster-art-ash-search");

        root.Q<Label>("monster-title").text = "ВСТРЕЧА: Пепельный вор";
        root.Q<Label>("monster-body").text = "Проверка арта ящерицы.";
        root.Q<VisualElement>("monster-options").Clear();

        modal.RemoveFromClassList("hidden");
        return "shown";
    }
}
