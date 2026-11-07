using System;
using ForestGuild.Core.Sim;

internal static class Program
{
    private static int Main(string[] args)
    {
        var checks = new CheckRunner();

        checks.Check("rng is deterministic", () =>
        {
            var a = new Rng(42);
            var b = new Rng(42);
            return a.Next(100) == b.Next(100);
        });

        return checks.Report() ? 0 : 1;
    }
}
