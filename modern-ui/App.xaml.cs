using System.Windows;
using System.Runtime.InteropServices;

namespace RawAccelModern
{
    public partial class App : Application
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                SetCurrentProcessExplicitAppUserModelID("RawAccel.Reimagined");
            }
            catch
            {
                // Older Windows versions may not expose this shell API.
            }

            if (e.Args != null && System.Array.Exists(e.Args, delegate(string arg) { return arg.Equals("--deactivate", System.StringComparison.OrdinalIgnoreCase); }))
            {
                try
                {
                    DriverConfig.Deactivate();
                    Shutdown(0);
                }
                catch
                {
                    Shutdown(1);
                }
                return;
            }
            base.OnStartup(e);
        }
    }
}
