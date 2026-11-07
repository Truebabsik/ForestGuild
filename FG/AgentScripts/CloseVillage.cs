using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Закрывает окно деревни и снимает планирование.</summary>
public static class CloseVillage
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.Village.Close();
        return "open=" + screen.Village.IsOpen;
    }
}
