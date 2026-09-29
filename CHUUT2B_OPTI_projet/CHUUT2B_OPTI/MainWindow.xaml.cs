using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace CHUUT2B_OPTI;

public partial class MainWindow : Window
{
    private List<CheckResult> _results = [];

    public MainWindow() { InitializeComponent(); }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled=false;
        OptimizeButton.IsEnabled=false;
        ExportButton.IsEnabled=false;
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
            OptimizeButton.IsEnabled=warnings>0;
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

    private void Optimize_Click(object sender, RoutedEventArgs e)
    {
        var safe=_results.Where(x=>x.CanAutoFix).ToList();
        MessageBox.Show(
            "CHUUT2B OPTI est volontairement prudent.\n\n"+
            "Cette version n'applique automatiquement que des réparations explicitement marquées comme sûres et réversibles. "+
            "Aucune modification BIOS, firmware, pilote, overclocking, audio ou contrôleur n'est appliquée automatiquement.\n\n"+
            "Les recommandations détectées sont affichées dans la colonne ACTION afin de valider chaque changement.",
            "Optimisation sécurisée",MessageBoxButton.OK,MessageBoxImage.Information);
    }
}
