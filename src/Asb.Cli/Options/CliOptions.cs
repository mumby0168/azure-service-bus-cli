namespace Asb.Cli.Options;

public class CliOptions
{
    public record Wrapper(CliOptions CliOptions);

    public Wrapper WithWrapper() => new(this);

    public Dictionary<string, string> Instances { get; set; } = new();
}

