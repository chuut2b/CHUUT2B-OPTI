namespace CHUUT2B_OPTI;

public enum Severity { Info, Ok, Warning, Critical }

public sealed record CheckResult(
    string Category, string Title, Severity Severity,
    string Details, string Recommendation, string FixId = "", string FixArg = "")
{
    public bool CanAutoFix => FixId != "";

    public string Status => Severity switch
    {
        Severity.Ok => "OK",
        Severity.Warning => "ALERTE",
        Severity.Critical => "CRITIQUE",
        _ => "INFO"
    };
}

public sealed record SystemSnapshot(
    string ComputerName, string Os, string Cpu, string Gpu,
    string Motherboard, string Bios, string Memory,
    string PowerPlan, string Network, string Storage);
