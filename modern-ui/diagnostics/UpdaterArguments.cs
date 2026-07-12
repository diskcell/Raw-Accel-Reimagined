using System;
using System.Runtime.InteropServices;

namespace RawAccelModern
{
    internal static class UpdaterArguments
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string commandLine, out int argumentCount);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);

        private static string[] Parse(string commandLine)
        {
            int count;
            IntPtr pointer = CommandLineToArgvW(commandLine, out count);
            if (pointer == IntPtr.Zero) throw new InvalidOperationException("CommandLineToArgvW failed.");
            try
            {
                string[] values = new string[count];
                for (int index = 0; index < count; index++)
                    values[index] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(pointer, index * IntPtr.Size));
                return values;
            }
            finally
            {
                LocalFree(pointer);
            }
        }

        private static void AssertRoundTrip(string value)
        {
            string[] parsed = Parse("RawAccelUpdater.exe --target " + WindowsCommandLine.QuoteArgument(value) + " --pid 0");
            if (parsed.Length != 5 || parsed[1] != "--target" || parsed[2] != value || parsed[3] != "--pid" || parsed[4] != "0")
                throw new InvalidOperationException("Argument round-trip failed for: " + value);
        }

        private static int Main()
        {
            AssertRoundTrip(@"C:\RawAccel\");
            AssertRoundTrip(@"C:\Folder With Spaces\Raw Accel\");
            AssertRoundTrip(@"C:\Folder With Spaces\Raw Accel");
            AssertRoundTrip("C:\\Quoted \\\"Folder\\\"\\");
            Console.WriteLine("UPDATER_ARGUMENT_TEST_PASSED");
            return 0;
        }
    }
}
