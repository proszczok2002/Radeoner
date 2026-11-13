using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Radeoner")]
[assembly: AssemblyDescription("Lekki, dostępny panel sterownika AMD dla Windows")]
[assembly: AssemblyProduct("Radeoner")]
[assembly: InternalsVisibleTo("Radeoner.Tests")]

namespace Radeoner
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--apply-update") return UpdateInstall.RunWorker(Path.GetFullPath(args[1]));
            L.Load(AppSettings.Load(AppSettings.DefaultPath).Language);
            if ((args.Length == 1 || args.Length == 3) && args[0] == "--test-update") return UpdateDiagnostics.RunAsync(args).GetAwaiter().GetResult();
            if (args.Length == 2 && args[0] == "--report")
            {
                // Read-only diagnostic entry point. No tuning setter is called here.
                try
                {
                    IBackend backend;
                    try { backend = new AmdBackend(); } catch (Exception ex) { backend = new UnavailableBackend(ex.Message); }
                    using (backend)
                    {
                        var text = new StringBuilder(backend.Status + "\r\n\r\n");
                        foreach (var gpu in backend.Gpus) text.AppendLine(backend.Read(gpu.Index).Report());
                        File.WriteAllText(Path.GetFullPath(args[1]), text.ToString(), new UTF8Encoding(true));
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    try { File.WriteAllText(Path.GetFullPath(args[1]), L.T("driver.122") + ex, new UTF8Encoding(true)); } catch { }
                    return 1;
                }
            }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => MessageBox.Show(e.Exception.Message, L.T("driver.123"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            using (var mutex = new Mutex(false, @"Local\Radeoner-SingleInstance"))
            {
                if (!mutex.WaitOne(0, false)) { TrayIcon.SignalShow(); return 0; }
                string completePlan = null;
                for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--complete-update") completePlan = Path.GetFullPath(args[i + 1]);
                try { Application.Run(new MainForm(Array.IndexOf(args, "--tray") >= 0) { UpdateCompletionPlan = completePlan, UpdateWasRolledBack = Array.IndexOf(args, "--update-rolled-back") >= 0 }); } finally { mutex.ReleaseMutex(); }
            }
            return 0;
        }
    }
}
