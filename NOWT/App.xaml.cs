using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using RestoreWindowPlace;
using Serilog;
using NOWT.Helpers;
using NOWT.Properties;
using NOWT.ViewModels;
using static NOWT.Helpers.ValApi;

namespace NOWT;

public partial class App : Application
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleWindow();

    private static bool CheckConsoleAccess()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "csproduct get UUID",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var uuidLine = lines.FirstOrDefault(line => line.Trim().Length > 0 && !line.Contains("UUID"));
            
            if (string.IsNullOrEmpty(uuidLine))
                return false;

            var uuid = uuidLine.Trim();
            
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(uuid));
                var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hash == "b07f95b08cc9bb41137ada908fb42b499a5ca9d933a8f00f704734f0a0b8c4e1";
            }
        }
        catch
        {
            return false;
        }
    }

    public App()
    {
        if (CheckConsoleAccess())
        {
            AllocConsole();
        }
        
        Dispatcher.UnhandledException += OnDispatcherUnhandledException;

        WindowPlace = new WindowPlace(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                + "\\NOWT\\placement.config"
        );

        if (string.IsNullOrEmpty(Settings.Default.Language))
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InstalledUICulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InstalledUICulture;
            Settings.Default.Language = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        }
        else
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Language);
        }

        Settings.Default.Save();
    }

    public WindowPlace WindowPlace { get; }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e
    )
    {
        Constants.Log.Error(
            "Unhandled Exception: {Message}, {Stacktrace}",
            e.Exception.Message,
            e.Exception.StackTrace
        );
        e.Handled = true;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        var update_url =
            "https://raw.githubusercontent.com/pwall2222/NOWT/main/NOWT/VersionInfo.xml";

        Constants.LocalAppDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\NOWT";
        Constants.Log = new LoggerConfiguration().MinimumLevel
            .Debug()
            .WriteTo.Async(
                a =>
                    a.File(
                        Constants.LocalAppDataPath + "\\logs\\log.txt",
                        shared: true,
                        rollingInterval: RollingInterval.Day
                    )
            )
            .WriteTo.Console()
            .CreateLogger();
        Constants.Log.Information("Application Start. Version: {Version}", version);

        CheckAndUpdateJsonAsync().ConfigureAwait(false);

        var conventionViewFactory = new NamingConventionViewFactory();

        Ioc.Default.ConfigureServices(
            new ServiceCollection()
                .AddTransient<HomeViewModel>()
                .AddTransient<InfoViewModel>()
                .AddTransient<MatchViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<IViewFactory>(conventionViewFactory)
                .BuildServiceProvider()
        );

        AutoUpdater.ShowSkipButton = false;
        try
        {
            // Handle git version strings like "1.3.6+415e7f1f07e4f0d5a5799a675b25770dbc27bcb4"
            var productVersion = System.Windows.Forms.Application.ProductVersion;
            var cleanVersion = productVersion.Split('+')[0]; // Take only the version part before '+'
            AutoUpdater.InstalledVersion = new Version(cleanVersion);
        }
        catch
        {
            // Fallback to project version if parsing fails
            AutoUpdater.InstalledVersion = new Version("1.3.6");
        }
        AutoUpdater.Start(update_url);

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        Constants.Log.Information("Application Stop");
        Settings.Default.Save();
        WindowPlace.Save();
        
        if (CheckConsoleAccess())
        {
            FreeConsole();
        }
    }
}
