using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;

namespace CHUUT2B_OPTI;

public static class Diagnostics
{
    public static async Task<List<CheckResult>> RunAsync(IProgress<int>? progress = null)
    {
        var r = new List<CheckResult>();
        void Add(string c,string t,Severity s,string d,string rec="",bool fix=false) =>
            r.Add(new(c,t,s,d,rec,fix));

        progress?.Report(5);
        var os = Wmi("Win32_OperatingSystem", "Caption,Version,BuildNumber,OSArchitecture");
        Add("Windows","Système",Severity.Info,
            $"{Val(os,"Caption")} • build {Val(os,"BuildNumber")} • {Val(os,"OSArchitecture")}");

        progress?.Report(15);
        var cpu = Wmi("Win32_Processor","Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed");
        Add("CPU","Processeur",Severity.Info,
            $"{Val(cpu,"Name")} • {Val(cpu,"NumberOfCores")}C/{Val(cpu,"NumberOfLogicalProcessors")}T • boost nominal {Val(cpu,"MaxClockSpeed")} MHz");

        var board = Wmi("Win32_BaseBoard","Manufacturer,Product,Version");
        var bios = Wmi("Win32_BIOS","Manufacturer,SMBIOSBIOSVersion,ReleaseDate");
        Add("Carte mère","Plateforme",Severity.Info,
            $"{Val(board,"Manufacturer")} {Val(board,"Product")} • BIOS {Val(bios,"SMBIOSBIOSVersion")}");

        progress?.Report(25);
        var mem = WmiAll("Win32_PhysicalMemory","Capacity,ConfiguredClockSpeed,Manufacturer,PartNumber");
        var total = mem.Sum(x => ToLong(Val(x,"Capacity")));
        var speeds = string.Join(", ", mem.Select(x => Val(x,"ConfiguredClockSpeed")).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        Add("RAM","Mémoire",Severity.Info,
            $"{Math.Round(total/1073741824d,1)} GB • fréquence configurée: {speeds} MHz • {mem.Count} module(s)");

        progress?.Report(35);
        var gpu = WmiAll("Win32_VideoController","Name,DriverVersion,CurrentRefreshRate,CurrentHorizontalResolution,CurrentVerticalResolution");
        foreach(var g in gpu)
            Add("GPU","Carte graphique",Severity.Info,
                $"{Val(g,"Name")} • pilote {Val(g,"DriverVersion")} • {Val(g,"CurrentHorizontalResolution")}x{Val(g,"CurrentVerticalResolution")} @ {Val(g,"CurrentRefreshRate")} Hz");

        progress?.Report(45);
        var pnp = WmiAll("Win32_PnPEntity","Name,ConfigManagerErrorCode,PNPDeviceID");
        var bad = pnp.Where(x => ToInt(Val(x,"ConfigManagerErrorCode")) != 0).ToList();
        if (bad.Count == 0)
            Add("Pilotes","Périphériques",Severity.Ok,"Aucun périphérique présent avec un code d'erreur PnP.","");
        else
            foreach(var d in bad)
                Add("Pilotes","Erreur périphérique",Severity.Warning,
                    $"{Val(d,"Name")} • code {Val(d,"ConfigManagerErrorCode")}",
                    "Identifier le pilote/chipset exact avant toute modification.");

        progress?.Report(55);
        var disks = WmiAll("Win32_LogicalDisk","DeviceID,Size,FreeSpace,DriveType");
        foreach(var d in disks.Where(x=>Val(x,"DriveType")=="3"))
        {
            var size=ToLong(Val(d,"Size")); var free=ToLong(Val(d,"FreeSpace"));
            if(size<=0) continue;
            var pct=free*100.0/size;
            Add("Stockage","Espace libre",pct<10?Severity.Warning:Severity.Ok,
                $"{Val(d,"DeviceID")} • {Math.Round(free/1073741824d,1)} GB libres / {Math.Round(size/1073741824d,1)} GB ({pct:F1}%)",
                pct<10?"Libérer de l'espace sur ce volume.":"");
        }

        progress?.Report(65);
        var scheme = Run("powercfg", "/getactivescheme").Trim();
        Add("Alimentation","Plan actif",Severity.Info,scheme,
            "Ne pas forcer un plan sans vérifier les besoins du PC.");

        progress?.Report(72);
        foreach(var n in NetworkInterface.GetAllNetworkInterfaces()
            .Where(x=>x.OperationalStatus==OperationalStatus.Up))
            Add("Réseau","Interface active",Severity.Info,
                $"{n.Name} • {n.NetworkInterfaceType} • {n.Description}");

        progress?.Report(80);
        var startup = WmiAll("Win32_StartupCommand","Name,Command,Location");
        Add("Démarrage","Programmes au démarrage",startup.Count>12?Severity.Warning:Severity.Info,
            $"{startup.Count} entrée(s) détectée(s).",
            startup.Count>12?"Revoir les applications réellement nécessaires au démarrage.":"");

        progress?.Report(88);
        var procs = Process.GetProcesses()
            .Select(p => { try { return (Name: p.ProcessName, Mem: p.WorkingSet64); } catch { return (Name: p.ProcessName, Mem: 0L); }})
            .OrderByDescending(x=>x.Mem).Take(8).ToList();
        Add("Arrière-plan","Processus mémoire",Severity.Info,
            string.Join(" • ",procs.Select(x=>$"{x.Name}: {Math.Round(x.Mem/1048576d)} MB")));

        progress?.Report(95);
        var hags = ReadDword(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers","HwSchMode");
        Add("Windows Gaming","HAGS",Severity.Info,
            hags is null ? "Non détecté" : $"HwSchMode={hags}");

        var gameDvr = ReadDword(@"HKEY_CURRENT_USER\System\GameConfigStore","GameDVR_Enabled");
        Add("Windows Gaming","Game DVR",Severity.Info,
            gameDvr is null ? "Non détecté" : $"GameDVR_Enabled={gameDvr}");

        progress?.Report(100);
        return r;
    }

    public static int Score(IEnumerable<CheckResult> results)
    {
        var list=results.ToList();
        int score=100;
        foreach(var x in list)
            if(x.Severity==Severity.Critical) score-=15;
            else if(x.Severity==Severity.Warning) score-=6;
        return Math.Clamp(score,0,100);
    }

    static ManagementObject? Wmi(string cls,string props)
    {
        try { using var s=new ManagementObjectSearcher($"SELECT {props} FROM {cls}");
              return s.Get().Cast<ManagementObject>().FirstOrDefault(); } catch { return null; }
    }
    static List<ManagementObject> WmiAll(string cls,string props)
    {
        try { using var s=new ManagementObjectSearcher($"SELECT {props} FROM {cls}");
              return s.Get().Cast<ManagementObject>().ToList(); } catch { return []; }
    }
    static string Val(ManagementBaseObject? o,string p) => o?[p]?.ToString() ?? "N/D";
    static int ToInt(string s)=>int.TryParse(s,out var v)?v:0;
    static long ToLong(string s)=>long.TryParse(s,out var v)?v:0;
    static string Run(string file,string args)
    {
        try { var p=Process.Start(new ProcessStartInfo(file,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true});
              return p?.StandardOutput.ReadToEnd() ?? ""; } catch { return ""; }
    }
    static uint? ReadDword(string path,string name)
    {
        try { var k=Registry.GetValue(path,name,null); return k is null?null:unchecked((uint)Convert.ToInt64(k)); } catch { return null; }
    }
}
