using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Проверяет, что игра стартовала и данные прочитались без ошибки.</summary>
public static class ProbeBoot
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;

        var hud = root.Q<Label>("hud");
        var error = root.Q<Label>(className: null);

        var belt = screen.World.Zones["belt_stone"];
        var beasts = 0;
        foreach (var pair in belt.Beasts) beasts += pair.Value;

        return "hud=[" + (hud != null ? hud.text : "?") + "]"
            + " beltBeasts=" + beasts
            + " xpToNest=" + screen.Tables.Tuning.GetInt("xp_to_nest")
            + " forageBelt=" + belt.Forage
            + " zones=" + screen.Tables.Zones.All.Count;
    }
}
