namespace QpEnhance.Tests;

/// <summary>Minimal assertion harness so tests need no external packages.</summary>
public sealed class TestRunner
{
    public int Passed { get; private set; }
    public int Failed { get; private set; }

    public void True(string name, bool condition)
    {
        if (condition)
        {
            Passed++;
        }
        else
        {
            Failed++;
            Console.WriteLine($"  FAIL: {name}");
        }
    }

    public void Near(string name, double actual, double expected, double tol = 1e-4)
        => True($"{name} (got {actual:0.#####}, want {expected:0.#####})", Math.Abs(actual - expected) <= tol);

    public void Less(string name, double a, double b)
        => True($"{name} (expected {a:0.#####} < {b:0.#####})", a < b);

    public void Greater(string name, double a, double b)
        => True($"{name} (expected {a:0.#####} > {b:0.#####})", a > b);

    public void Section(string title) => Console.WriteLine($"[{title}]");
}
