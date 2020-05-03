using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Eto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Eto.Drawing;
using Eto.Forms;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Synker.Infrastructure.Bundles;
using Synker.Infrastructure.ProfileLoaders;
using Synker.Domain;
using Synker.Infrastructure.Targets;
using Synker.UseCases.StartMonitor;

namespace Synker.Desktop
{
    /// <summary>
    /// Main form. It is invisible - only tray icon appears.
    /// </summary>
    public class MainForm : Form, IDisposable
    {
        private static string appUrl = @"https://github.com/krasninja/settings-synker";

        private readonly TrayIndicator trayIndicator = new TrayIndicator();
        private static readonly Icon icon = Icon.FromResource("Synker.Desktop.Images.Wrench.png");

        private IList<Profile> profiles = new List<Profile>();

        private IBundleFactory bundleFactory;

        private DelayActionRunner<Profile> delayActionRunner;

        private readonly CheckCommand exportMonitorCommand = new CheckCommand();

        private readonly CheckCommand importMonitorCommand = new CheckCommand();

        private readonly UserConfiguration configData;

        public MainForm()
        {
            configData = UserConfiguration.LoadFromFile();
            InitControls();

            exportMonitorCommand.Executed += (sender, args) =>
            {
                if (delayActionRunner == null)
                {
                    return;
                }

                if (exportMonitorCommand.Checked)
                {
                    delayActionRunner.Start();
                }
                else
                {
                    delayActionRunner.Stop();
                }
            };
            importMonitorCommand.Executed += (sender, args) =>
            {
                if (bundleFactory is IBundleFactoryWithMonitor bundleFactoryWithMonitor)
                {
                    if (importMonitorCommand.Checked)
                    {
                        bundleFactoryWithMonitor.StartMonitor();
                    }
                    else
                    {
                        bundleFactoryWithMonitor.StopMonitor();
                    }
                }
            };
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
            trayIndicator.Menu.Items.Add(new CheckMenuItem
            {
                Command = exportMonitorCommand,
                Checked = !configData.DisableImport,
                Text = "Export Enabled"
            });
            trayIndicator.Menu.Items.Add(new CheckMenuItem
            {
                Command = importMonitorCommand,
                Checked = !configData.DisableImport,
                Text = "Import Enabled"
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
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(ShowProfilesDirectoryHandler)
            {
                Text = "Open Profiles Directory"
            });
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(ShowSettingsFileHandler)
            {
                Text = "Open Settings File"
            });
            trayIndicator.Menu.Items.AddSeparator();
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(AboutHandler)
            {
                Text = "About"
            });
            trayIndicator.Menu.Items.Add(new ButtonMenuItem(QuitHandler)
            {
                Text = "Quit",
                Shortcut = Application.Instance.CommonModifier | Keys.Q
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
            var viewModel = new StatusFormViewModel(profiles, bundleFactory);
            var form = new StatusForm(viewModel);

            void OnRun(object o, EventArgs onRunArgs)
            {
                Application.Instance.InvokeAsync(async () =>
                {
                    await viewModel.SetLastUpdateDateAsync(viewModel.Profiles);
                });
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
            if (!string.IsNullOrEmpty(configData.LogFile))
            {
                Process.Start(new ProcessStartInfo {
                    FileName = configData.LogFile,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }

        private void ShowBundlesDirectoryHandler(object sender, EventArgs args)
        {
            Process.Start(new ProcessStartInfo {
                FileName = configData.BundlesDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void ShowProfilesDirectoryHandler(object sender, EventArgs args)
        {
            Process.Start(new ProcessStartInfo {
                FileName = configData.ProfilesSource,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void ShowSettingsFileHandler(object sender, EventArgs args)
        {
            if (!string.IsNullOrEmpty(configData.FileName))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = configData.FileName,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
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
            // Setup logging.
            AppLogger.LoggerFactory = !string.IsNullOrEmpty(configData.LogFile) ?
                ConfigureFileLogging(configData.LogFile) :
                ConfigureConsoleLogging();

            // Setup profiles.
            var filesProfileLoader = new FilesProfileLoader(configData.ProfilesSource);
            var profileYamlReader = new ProfileYamlReader(filesProfileLoader,
                ProfileYamlReader.GetProfileElementsTypesFromAssembly(typeof(NullSettingsTarget).Assembly));
            bundleFactory = new ZipBundleFactory(configData.BundlesDirectory);
            profiles = await profileYamlReader.LoadAsync();

            await SetupExportAsync();
            SetupImport();
        }

        private async Task SetupExportAsync()
        {
            var startMonitorCommand = new StartMonitorCommand(profiles, bundleFactory)
            {
                DisableExport = configData.DisableExport,
                DisableImport = configData.DisableImport
            };
            delayActionRunner = await startMonitorCommand.ExecuteAsync();
            delayActionRunner.AfterRun += (o, eventArgs) =>
            {
                ShowNotification(eventArgs.Item, "Synker Export");
            };
            if (!configData.DisableExport)
            {
                delayActionRunner.Start();
            }
        }

        private void SetupImport()
        {
            if (bundleFactory is IBundleFactoryWithMonitor bundleFactoryWithMonitor)
            {
                bundleFactoryWithMonitor.OnSettingsUpdate += (o, s) =>
                {
                    var profile = profiles.FirstOrDefault(p => p.Id == s);
                    ShowNotification(profile, "Synker Import");
                };
                if (!configData.DisableImport)
                {
                    bundleFactoryWithMonitor.StartMonitor();
                }
            }
        }

        private void ShowNotification(Profile profile, string title)
        {
            if (profile == null)
            {
                return;
            }
            new Notification
            {
                Message = "Profile - " + profile.Name,
                Title = title
            }.Show(trayIndicator);
        }

        #endregion

        #region IDisposable

        /// <inheritdoc />
        public new void Dispose()
        {
            // TODO: Why Widget doesn't support Dispose pattern?
            ((Widget)this).Dispose();
            delayActionRunner?.Dispose();
            (bundleFactory as IDisposable)?.Dispose();
        }

        #endregion

        #region Logging

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

        #endregion
    }
}
