using System;
using System.Collections.Generic;

/// Мини-раннер проверок: без xunit, без сети, код возврата для CI.
internal sealed class CheckRunner
{
    private readonly List<string> _failed = new List<string>();
    private int _total;

    public void Check(string name, Func<bool> body)
    {
        _total++;
        string error = null;
        try
        {
            if (!body()) error = "вернул false";
        }
        catch (Exception e)
        {
            error = e.GetType().Name + ": " + e.Message;
        }

        if (error == null)
        {
            Console.WriteLine("ok   " + name);
            return;
        }

        _failed.Add(name + ": " + error);
        Console.WriteLine("FAIL " + name + " — " + error);
    }

    public bool Report()
    {
        Console.WriteLine();
        if (_failed.Count == 0)
        {
            Console.WriteLine("все проверки прошли (" + _total + ")");
            return true;
        }

        Console.WriteLine("провалено " + _failed.Count + " из " + _total + ":");
        foreach (var f in _failed) Console.WriteLine("  ✗ " + f);
        return false;
    }
}
