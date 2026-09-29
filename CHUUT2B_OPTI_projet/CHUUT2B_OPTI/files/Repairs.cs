using System.Diagnostics;

namespace CHUUT2B_OPTI;

/// <summary>
/// Réparations volontairement limitées : aucune suppression de fichier,
/// aucune modification du registre, du BIOS ou des pilotes.
/// </summary>
public static class Repairs
{
    static string DeviceName(CheckResult r) => r.Details.Split(" • ")[0];

    public static string Describe(CheckResult r) => r.FixId switch
    {
        "pnp"      => $"• Redémarrer le périphérique : {DeviceName(r)}",
        "cleanmgr" => $"• Ouvrir le Nettoyage de disque sur {r.FixArg} (tu choisis ce qui est supprimé)",
        "startup"  => "• Ouvrir la liste des applications au démarrage (tu désactives celles dont tu n'as pas besoin)",
        _          => "• " + r.Title
    };

    public static List<string> Apply(IEnumerable<CheckResult> fixes)
    {
        var log = new List<string>();
        var needScan = false;

        foreach (var f in fixes)
        {
            try
            {
                switch (f.FixId)
                {
                    case "pnp":
                    {
                        needScan = true;
                        var (code, _) = Tool("pnputil.exe", "/restart-device", f.FixArg);
                        log.Add(code is 0 or 3010
                            ? $"OK : {DeviceName(f)} redémarré" + (code == 3010 ? " (redémarrage du PC requis)" : "")
                            : $"ÉCHEC : {DeviceName(f)} (code {code})");
                        break;
                    }
                    case "cleanmgr":
                    {
                        var letter = f.FixArg.TrimEnd(':');
                        Process.Start(new ProcessStartInfo("cleanmgr.exe") { ArgumentList = { "/d", letter }, UseShellExecute = false });
                        log.Add($"OK : Nettoyage de disque ouvert sur {f.FixArg}");
                        break;
                    }
                    case "startup":
                    {
                        Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });
                        log.Add("OK : liste des applications au démarrage ouverte");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Add($"ÉCHEC : {f.Title} ({ex.Message})");
            }
        }

        if (needScan)
        {
            try
            {
                var (code, _) = Tool("pnputil.exe", "/scan-devices");
                log.Add(code == 0 ? "OK : recherche de périphériques relancée" : $"ÉCHEC : recherche de périphériques (code {code})");
            }
            catch (Exception ex) { log.Add("ÉCHEC : recherche de périphériques (" + ex.Message + ")"); }
        }
        return log;
    }

    static (int Code, string Output) Tool(string file, params string[] args)
    {
        var psi = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Diagnostics.OemEncoding()
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Impossible de lancer " + file);
        var output = p.StandardOutput.ReadToEnd();
        if (!p.WaitForExit(30000)) { try { p.Kill(); } catch { } return (-1, output); }
        return (p.ExitCode, output);
    }
}
