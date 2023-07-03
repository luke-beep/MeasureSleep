using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;
using CommandLine.Text;

partial class Program
{
//https://learn.microsoft.com/en-us/windows/win32/multimedia/obtaining-and-setting-timer-resolution
    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryTimerResolution(out uint minimum_resolution, out uint maximum_resolution, out uint current_resolution);

    static void Main(string[] args)
    {
        var parser = new Parser(with => with.EnableDashDash = true);
        var result = parser.ParseArguments<Options>(args);
         
        _ = result.WithParsed(options =>
        {
            var sleepDelays = new List<double>();

            for (int i = 1; ; i++)
            {
                if (NtQueryTimerResolution(out uint minimum_resolution, out uint maximum_resolution, out uint current_resolution) != 0)
                {
                    Console.WriteLine("NtQueryTimerResolution failed");
                    return;
                }
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                Thread.Sleep(1);
                stopwatch.Stop();
                double delta_ms = stopwatch.Elapsed.TotalMilliseconds;
                double delta_from_sleep = delta_ms - 1;
                Console.WriteLine($"Resolution: {current_resolution / 10000.0}ms, Sleep(1) slept {delta_ms}ms (delta: {delta_from_sleep})");
                if (options.Samples.HasValue)
                {
                    sleepDelays.Add(delta_from_sleep);

                    if (i == options.Samples.Value)
                        break;

                    Thread.Sleep(100);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
                if (options.Samples.HasValue)
            {
                sleepDelays.RemoveAt(0);

                sleepDelays.Sort();

                double sum = sleepDelays.Sum();
                double average = sum / sleepDelays.Count;

                double standardDeviation = sleepDelays.Select(delay => Math.Pow(delay - average, 2)).Sum();
                double stdev = Math.Sqrt(standardDeviation / sleepDelays.Count);

                Console.WriteLine($"\nMax: {sleepDelays.Max()}");
                Console.WriteLine($"Avg: {average}");
                Console.WriteLine($"Min: {sleepDelays.Min()}");
                Console.WriteLine($"STDEV: {stdev}");
            }
        });

        result.WithNotParsed(errors =>
        {
            var helpText = HelpText.AutoBuild(result, h => h, e => e);
            Console.WriteLine(helpText);
        });
    }
}

class Options
{
    [Value(0, MetaName = "samples", HelpText = "Measure the Sleep(1) deltas for a specified amount of samples then compute the maximum, average, minimum, and stdev from the collected samples")]
    public int? Samples { get; set; }
}

//https://stackoverflow.com/questions/21156944/how-to-get-the-current-windows-system-wide-timer-resolution
