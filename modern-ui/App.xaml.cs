using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace RawAccelModern
{
    public partial class App : Application
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

        protected override void OnStartup(StartupEventArgs e)
        {
            // This UI is mostly static. Software rendering avoids loading the much larger
            // vendor GPU stack while keeping mouse acceleration calculations untouched.
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
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
