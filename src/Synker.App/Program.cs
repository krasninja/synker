using System;
using System.Reflection;
using Eto;
using Eto.Forms;
using UnhandledExceptionEventArgs = Eto.UnhandledExceptionEventArgs;

namespace Synker.App
{
    /// <summary>
    /// Program entry point.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Startup arguments.</param>
        /// <returns>Exit code.</returns>
        [STAThread]
        private static int Main(string[] args)
        {
            var app = new Application(Platform.Detect);
            app.UnhandledException += InstanceOnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += DomainUnhandledException;
            app.Run(new MainForm());
            return 0;
        }

        private static void InstanceOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowException(e.ExceptionObject as Exception);
        }

        private static void DomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            ShowException(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// Show exception in message box.
        /// </summary>
        /// <param name="ex">Exception to show.</param>
        private static void ShowException(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            if (ex is TargetInvocationException tiex && tiex.InnerException != null)
            {
                MessageBox.Show(tiex.InnerException.Message, "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            }
            else
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxType.Error);
            }
            Application.Instance.Quit();
        }
    }
}
