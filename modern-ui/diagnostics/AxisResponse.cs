using System;
using System.Globalization;
using System.IO;

internal static class AxisResponse
{
    private static double Ratio(Profile profile, double speed, bool vertical)
    {
        using (ManagedAccel baseline = new ManagedAccel(profile))
        using (ManagedAccel sample = baseline.CreateStatelessCopy())
        {
            int x = vertical ? 0 : 1000;
            int y = vertical ? 1000 : 0;
            Tuple<double, double> output = sample.Accelerate(x, y, 1.0, 1000.0 / speed);
            return Math.Sqrt(output.Item1 * output.Item1 + output.Item2 * output.Item2) / 1000.0;
        }
    }

    public static int Main(string[] args)
    {
        string root = args.Length == 0 ? Environment.CurrentDirectory : args[0];
        string json = File.ReadAllText(Path.Combine(root, "settings.json"));
        Tuple<DriverConfig, string> conversion = DriverConfig.Convert(json);
        if (conversion == null || conversion.Item1 == null)
        {
            Console.Error.WriteLine(conversion == null ? "Conversion failed" : conversion.Item2);
            return 1;
        }
        Profile profile = conversion.Item1.profiles[0];
        bool recommended = Array.Exists(args, delegate(string arg) { return arg == "--recommended"; });
        if (recommended)
        {
            profile.domainXY.y = 1.10;
            profile.rangeXY.y = 1.05;
            Console.WriteLine("recommended vertical compensation: domainY=1.10, rangeY=1.05");
        }
        Console.WriteLine("speed\thorizontal\tvertical\tvertical/horizontal");
        double[] speeds = new double[] { 1, 5, 10, 15, 20, 30, 40 };
        foreach (double speed in speeds)
        {
            double horizontal = Ratio(profile, speed, false);
            double vertical = Ratio(profile, speed, true);
            Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0:0.0}\t{1:0.0000}\t{2:0.0000}\t{3:0.0000}", speed, horizontal, vertical, vertical / horizontal));
        }
        return 0;
    }
}
