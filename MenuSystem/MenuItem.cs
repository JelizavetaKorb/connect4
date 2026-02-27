namespace MenuSystem;

public class MenuItem
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;

    public Func<string>? MethodToRun { get; set; }

    public override string ToString()
    {
        return $"{Key}) {Value}";
    }
}