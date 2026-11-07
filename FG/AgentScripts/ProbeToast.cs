using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Проверка тоста: текст, класс hidden, автоскрытие.</summary>
public static class ProbeToast
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = screen.GetType();
        type.GetMethod("ShowMessage", flags).Invoke(screen, new object[] { "Тост: проверка" });

        var toast = (Label)type.GetField("_toast", flags).GetValue(screen);
        var sb = new StringBuilder();
        sb.AppendLine("toast=" + (toast != null));
        if (toast == null) return sb.ToString();

        sb.AppendLine("text=" + toast.text);
        sb.AppendLine("hidden=" + toast.ClassListContains("hidden"));
        sb.AppendLine("layout=" + toast.layout);
        return sb.ToString();
    }
}
