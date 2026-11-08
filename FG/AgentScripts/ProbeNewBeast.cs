using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Показывает модал с артом новой твари первой линии.</summary>
public static class ProbeNewBeast
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        type.GetMethod("ShowMenu", Flags).Invoke(screen, null);
        type.GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "expedition" });

        // Тик похода перепишет модал ближайшей встречей — ставим паузу.
        screen.Expedition.GetType().GetMethod("TogglePause", Flags).Invoke(screen.Expedition, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var modal = root.Q<VisualElement>("monster-modal");
        var art = root.Q<VisualElement>("monster-art");

        art.ClearClassList();
        art.AddToClassList("monster-art");
        art.AddToClassList("monster-art-climber-search");

        root.Q<Label>("monster-title").text = "ВСТРЕЧА: Теневой древолаз";
        root.Q<Label>("monster-body").text = "Сидит на стволе выше головы и падает сверху, когда проходят под ним.";
        root.Q<VisualElement>("monster-options").Clear();

        modal.RemoveFromClassList("hidden");
        return "climber shown";
    }
}
