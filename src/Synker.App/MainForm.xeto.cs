using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Synker.Infrastructure.Bundles;
using Synker.Infrastructure.ProfileLoaders;
using Synker.Domain;
using Synker.Infrastructure.Targets;
using Synker.UseCases.StartMonitor;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Synker.App
{
    /// <summary>
    /// Main form. It is invisible - only tray icon appears.
    /// </summary>
    public class MainForm : Form
    {
        private static string appUrl = @"https://github.com/krasninja/settings-synker";

        private readonly TrayIndicator trayIndicator = new TrayIndicator();
        private static readonly Icon icon = Icon.FromResource("Synker.App.Images.Wrench.png");

        private IList<Profile> profiles = new List<Profile>();

        private IBundleFactory bundleFactory;

        private DelayActionRunner<Profile> delayActionRunner;

        public MainForm()
        {
            XamlReader.Load(this);
            InitControls();
        }

        private void InitControls()
        {
            trayIndicator.Image = icon;

            // Tray icon context menu init.
            InitTrayIconMenu();

            this.LoadComplete += OnLoad;
            trayIndicator.Show();

            // Hide main form.
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.Minimize();
            this.Enabled = false;
            this.CanFocus = false;
            this.WindowStyle = WindowStyle.None;
        }

        private void InitTrayIconMenu()
        {
            trayIndicator.Menu = new ContextMenu();
            trayIndicator.Menu.Items.Clear();

            trayIndicator.Menu.Items.Add(new ButtonMenuItem(StatusHandler)
            {
                Text = "Status"
            });
            trayIndicator.Menu.Items.AddSeparator();
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(ShowLogHandler)
            {
                Text = "Open Log File"
            });
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(ShowBundlesDirectoryHandler)
            {
                Text = "Open Bundles Directory"
            });
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(ShowBundlesDirectoryHandler)
            {
                Text = "Open Profiles Directory"
            });
            trayIndicator.Menu.Items.AddSeparator();
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(AboutHandler)
            {
                Text = "About"
            });
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(QuitHandler)
            {
                Text = "Quit"
            });
        }

        #region Handlers

        private void QuitHandler(object sender, EventArgs args)
        {
            trayIndicator.Visible = false;
            Application.Instance.Quit();
        }

        private void StatusHandler(object sender, EventArgs args)
        {
            var viewModel = new StatusFormViewModel(profiles);
            var form = new StatusForm(viewModel);

            void OnRun(object o, EventArgs onRunArgs)
            {
                viewModel.LoadAsync().GetAwaiter().GetResult();
            }
            form.LoadComplete += (o, eventArgs) =>
            {
                delayActionRunner.BeforeRun += OnRun;
            };
            form.UnLoad += (o, eventArgs) =>
            {
                delayActionRunner.BeforeRun -= OnRun;
            };
            form.Show();
        }

        private void ShowLogHandler(object sender, EventArgs args)
        {
            var configData = UserConfiguration.LoadFromFile();
            if (!string.IsNullOrEmpty(configData.LogFile))
            {
                Process.Start(configData.LogFile);
            }
        }

        private void ShowBundlesDirectoryHandler(object sender, EventArgs args)
        {
            var configData = UserConfiguration.LoadFromFile();
            Process.Start(configData.BundlesDirectory);
        }

        private void ShowProfilesDirectoryHandler(object sender, EventArgs args)
        {
            var configData = UserConfiguration.LoadFromFile();
            Process.Start(configData.ProfilesSource);
        }

        private void AboutHandler(object sender, EventArgs args)
        {
            using var aboutDialog = new AboutDialog(Assembly.GetEntryAssembly())
            {
                Copyright = "2019-2020 (C) AntiSoft",
                Developers = new[]
                {
                    "Ivan Kozhin"
                },
                ProgramName = "Settings Synker",
                License = "Free",
                Website = new Uri(appUrl),
                Logo = icon
            };

            aboutDialog.ShowDialog(null);
        }

        private async void OnLoad(object sender, EventArgs args)
        {
            var configData = UserConfiguration.LoadFromFile();

            // Setup logging.
            AppLogger.LoggerFactory = !string.IsNullOrEmpty(configData.LogFile) ?
                ConfigureFileLogging(configData.LogFile) :
                ConfigureConsoleLogging();

            // Setup profiles and start monitoring.
            var filesProfileLoader = new FilesProfileLoader(configData.ProfilesSource);
            var profileYamlReader = new ProfileYamlReader(filesProfileLoader,
                ProfileYamlReader.GetProfileElementsTypesFromAssembly(typeof(NullSettingsTarget).Assembly));
            bundleFactory = new ZipBundleFactory(configData.BundlesDirectory);
            profiles = await profileYamlReader.LoadAsync();
            var startMonitorCommand = new StartMonitorCommand(profiles, bundleFactory)
            {
                DisableExport = configData.DisableExport,
                DisableImport = configData.DisableImport
            };
            delayActionRunner = await startMonitorCommand.ExecuteAsync();
        }

        #endregion

        private const string NLogLayout =
            @"${date:format=yyyy-MM-dd HH\:mm\:ss} [${level:format=FirstCharacter}] ${logger:shortName=true}: ${message} ${exception}";

        private static ILoggerFactory ConfigureFileLogging(string fileName)
        {
            // Config NLog.
            var config = new LoggingConfiguration();
            var consoleTarget = new FileTarget("file")
            {
                FileName = fileName,
                Layout = NLogLayout
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;

            return CreateLoggerFactory();
        }

        private static ILoggerFactory ConfigureConsoleLogging()
        {
            // Config NLog.
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = NLogLayout
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;

            return CreateLoggerFactory();
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            // Setup integration with Extensions.Logging .
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                })
                .BuildServiceProvider();
            return serviceProvider.GetRequiredService<ILoggerFactory>();
        }
    }
}
