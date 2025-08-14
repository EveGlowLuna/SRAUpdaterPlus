using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using Spectre.Console;
using System.Threading;
using SRAUpdaterPlus.Tool;
using System.IO;

namespace SRAUpdaterPlus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        bool _attachedToConsole = false;

        const int ATTACH_PARENT_PROCESS = -1;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!System.IO.Directory.Exists(Parameter.SAVE_PATH))
            {
                Directory.CreateDirectory(Parameter.SAVE_PATH);
            }
            // Attach to the parent console if available
            _attachedToConsole = AttachConsole(ATTACH_PARENT_PROCESS);
            if (_attachedToConsole)
            {
                Console.Clear();
                AnsiConsole.MarkupLine("[green]Attached to parent console successfully.[/]");
                AnsiConsole.MarkupLine("Welcome to [Red]SRA[/][Yellow]Updater[/][Blue]PLus[/]!");
            }

            string downUrl = null;
            string proxy = null;
            int timeout = 10; // seconds
            bool disableProxy = false;
            bool forceUpdate = false;
            bool disableSSL = false;
            bool fileIntegrityCheck = false;
            bool useOfficialSoftware = false;
            string LPath = null;


            for (int i = 0; i < e.Args.Length; i++)
            {
                // config
                if ((e.Args[i] == "-url" || e.Args[i] == "--url" || e.Args[i] == "-u" || e.Args[i] == "--u") && i + 1 < e.Args.Length)
                {
                    downUrl = e.Args[i + 1];
                }
                if ((e.Args[i] == "-use-proxy" || e.Args[i] == "--use-proxy" || e.Args[i] == "-p" || e.Args[i] == "--p") && i + 1 < e.Args.Length)
                {
                    proxy = e.Args[i + 1];
                }
                if (e.Args[i] == "-disable-proxy" || e.Args[i] == "--disable-proxy" || e.Args[i] == "-np" || e.Args[i] == "--np")
                {
                    disableProxy = true;
                }
                if (e.Args[i] == "-disable-SSL" || e.Args[i] == "--disable-SSL" || e.Args[i] == "-nv" || e.Args[i] == "--nv")
                {
                    disableSSL = true;
                }
                if (e.Args[i] == "-force-update" || e.Args[i] == "--force-update" || e.Args[i] == "-f" || e.Args[i] == "--f")
                {
                    forceUpdate = true;
                }
                if ((e.Args[i] == "-timeout" || e.Args[i] == "--timeout") && i + 1 < e.Args.Length)
                {
                    timeout = int.Parse(e.Args[i + 1]);
                }
                if (e.Args[i] == "-file-integrity-check" || e.Args[i] == "--file-integrity-check" || e.Args[i] == "-i" || e.Args[i] == "--i")
                {
                    fileIntegrityCheck = true;
                }
                if (e.Args[i] == "-use-sraupdater" || e.Args[i] == "--use-sraupdater" || e.Args[i] == "-ur" || e.Args[i] == "--ur")
                {
                    useOfficialSoftware = true;
                }

                if ((e.Args[i] == "-SRAL-Disable") && i + 1 < e.Args.Length)
                {
                    LPath = e.Args[i + 1];
                }


                // output
                if (e.Args[i] == "-version" || e.Args[i] == "--version" || e.Args[i] == "-v" || e.Args[i] == "--v")
                {
                    AnsiConsole.MarkupLine($"[yellow]SRAUpdaterPlus Version: {Parameter.VERSION}[/]");
                    AnsiConsole.MarkupLine($"[yellow]Author: {Parameter.AUTHOR}[/]");
                    Shutdown();
                }
                if (e.Args[i] == "-help" || e.Args[i] == "--help" || e.Args[i] == "-h" || e.Args[i] == "--h")
                {
                    AnsiConsole.MarkupLine("[yellow]Usage:[/]");
                    AnsiConsole.MarkupLine("[green]-url, -u[/] [blue]<URL>[/] [yellow]Specify the download URL.[/]");
                    AnsiConsole.MarkupLine("[green]-use-proxy, -p[/] [blue]<Proxy>[/] [yellow]Use the specified proxy.[/]");
                    AnsiConsole.MarkupLine("[green]-disable-proxy, -np[/] [yellow]Disable proxy usage.[/]");
                    AnsiConsole.MarkupLine("[green]-disable-SSL, -nv[/] [yellow]Disable SSL verification.[/]");
                    AnsiConsole.MarkupLine("[green]-force-update, -f[/] [yellow]Force update even if the latest version is already installed.[/]");
                    AnsiConsole.MarkupLine("[green]-timeout, -t[/] [blue]<Seconds>[/] [yellow]Set the timeout for download operations (default: 10 seconds).[/]");
                    AnsiConsole.MarkupLine("[green]-file-integrity-check, -i[/] [yellow]Enable file integrity check after download.[/]");
                    AnsiConsole.MarkupLine("[green]-help, -h[/] [yellow]Show this help message.[/]");
                    AnsiConsole.MarkupLine("[green]-use-sraupdater, -ur[/] [yellow]Use the official SRAUpdater software for updates.[/]");
                    Shutdown();
                }
            }   

            if (LPath != null)
            {
                Parameter.ChangeDirLocate(LPath);
                useOfficialSoftware = true;
            }

            var mainWindow = new MainWindow(
                downUrl: downUrl,
                proxy: proxy,
                timeout: timeout,
                disableProxy: disableProxy,
                forceUpdate: forceUpdate,
                disableSSL: disableSSL,
                fileIntegrityCheck: fileIntegrityCheck,
                useOfficialSoftware: useOfficialSoftware
            );
            if (proxy != null)
            {
                Parameter.ChangeProxy(proxy);

            }
            
            mainWindow.Show();
        }


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            AnsiConsole.MarkupLine("[yellow]The program has been terminated. You may need to press[/] [blue]'Ctrl+C'[/] [yellow]to detach from the console.[/]");
            if (_attachedToConsole)
            {
                FreeConsole();
            }

            Shutdown();
            
        }
    }

}
