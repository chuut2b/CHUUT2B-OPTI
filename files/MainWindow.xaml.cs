using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace CHUUT2B_OPTI;

public partial class MainWindow : Window
{
    private List<CheckResult> _results = [];

    public MainWindow() { InitializeComponent(); }

    private async void Scan_Click(object sender, RoutedEventArgs e) => await RunScanAsync();

    private async Task RunScanAsync()
    {
        ScanButton.IsEnabled=false;
        RepairButton.IsEnabled=false;
        ExportButton.IsEnabled=false;
        ScanProgress.Value=0;
        StatusText.Text="DIAGNOSTIC EN COURS";
        SummaryText.Text="Analyse de la configuration...";
        var progress=new Progress<int>(p=>ScanProgress.Value=p);
        try
        {
            _results=await Task.Run(()=>Diagnostics.RunAsync(progress));
            ResultsGrid.ItemsSource=_results;
            var score=Diagnostics.Score(_results);
            ScoreText.Text=score.ToString();
            ScoreText.Foreground=score>=90?FindResource("Good") as Brush:score>=75?FindResource("Warn") as Brush:FindResource("Bad") as Brush;
            var warnings=_results.Count(x=>x.Severity==Severity.Warning||x.Severity==Severity.Critical);
            ScoreHint.Text=$"{warnings} point(s) à examiner";
            SummaryText.Text=$"{_results.Count} contrôles effectués • {warnings} alerte(s)";
            StatusText.Text="TEST TERMINÉ";
            ExportButton.IsEnabled=true;
            RepairButton.IsEnabled=_results.Any(x=>x.CanAutoFix);
        }
        catch(Exception ex)
        {
            MessageBox.Show("Le diagnostic a rencontré une erreur :\n\n"+ex.Message,"CHUUT2B OPTI",MessageBoxButton.OK,MessageBoxImage.Error);
            StatusText.Text="ERREUR";
        }
        finally { ScanButton.IsEnabled=true; }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if(_results.Count==0)return;
        var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"CHUUT2B_OPTI_Reports");
        Directory.CreateDirectory(dir);
        var path=Path.Combine(dir,$"CHUUT2B_OPTI_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var sb=new StringBuilder();
        sb.AppendLine("CHUUT2B OPTI - RAPPORT");
        sb.AppendLine(DateTime.Now.ToString("O"));
        sb.AppendLine($"Score: {Diagnostics.Score(_results)}/100");
        sb.AppendLine(new string('=',80));
        foreach(var r in _results)
        {
            sb.AppendLine($"[{r.Severity}] [{r.Category}] {r.Title}");
            sb.AppendLine(r.Details);
            if(!string.IsNullOrWhiteSpace(r.Recommendation)) sb.AppendLine("Action: "+r.Recommendation);
            sb.AppendLine();
        }
        File.WriteAllText(path,sb.ToString(),Encoding.UTF8);
        MessageBox.Show($"Rapport enregistré :\n{path}","CHUUT2B OPTI",MessageBoxButton.OK,MessageBoxImage.Information);
    }

    private async void Repair_Click(object sender, RoutedEventArgs e)
    {
        var fixes=_results.Where(x=>x.CanAutoFix).ToList();
        if(fixes.Count==0)
        {
            MessageBox.Show("Rien à réparer automatiquement.","Réparer en sécurité",MessageBoxButton.OK,MessageBoxImage.Information);
            return;
        }

        var plan=string.Join("\n",fixes.Select(Repairs.Describe));
        var answer=MessageBox.Show(
            "Réparations proposées :\n\n"+plan+
            "\n\nAucun fichier n'est supprimé et aucun réglage BIOS, pilote ou registre n'est modifié. "+
            "Le redémarrage d'un périphérique réseau peut couper la connexion quelques secondes.\n\nLancer ?",
            "Réparer en sécurité",MessageBoxButton.YesNo,MessageBoxImage.Question);
        if(answer!=MessageBoxResult.Yes) return;

        ScanButton.IsEnabled=false;
        RepairButton.IsEnabled=false;
        ExportButton.IsEnabled=false;
        StatusText.Text="RÉPARATION EN COURS";
        try
        {
            var log=await Task.Run(()=>Repairs.Apply(fixes));
            MessageBox.Show(string.Join("\n",log)+"\n\nUn nouveau diagnostic va se lancer pour vérifier le résultat.",
                "Réparation terminée",MessageBoxButton.OK,MessageBoxImage.Information);
        }
        catch(Exception ex)
        {
            MessageBox.Show("La réparation a rencontré une erreur :\n\n"+ex.Message,"CHUUT2B OPTI",MessageBoxButton.OK,MessageBoxImage.Error);
        }
        await RunScanAsync();
    }
}
