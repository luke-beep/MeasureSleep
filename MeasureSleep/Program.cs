using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;
using CommandLine.Text;

partial class Program
{

    //NTQUERYTIMERRESOLUTION NtQueryTimerResolution = (NTQUERYTIMERRESOLUTION)GetProcAddress(ntdll, "NtQueryTimerResolution");
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
                //Variables can't be negative here so I went with uint.
                //ULONG minimum_resolution, maximum_resolution, current_resolution;
                /* 
                 
                 
                for (int i = 1;; i++) {
                    if (NtQueryTimerResolution(&minimum_resolution, &maximum_resolution, &current_resolution) != 0) {
                        std::cerr << "NtQueryTimerResolution failed\n";
                        return 1;
                }
                 */
                if (NtQueryTimerResolution(out uint minimum_resolution, out uint maximum_resolution, out uint current_resolution) != 0)
                {
                    Console.WriteLine("NtQueryTimerResolution failed");
                    return;
                }


                /*
                QueryPerformanceCounter(&start);
                Sleep(1);
                QueryPerformanceCounter(&end);
                */
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                Thread.Sleep(1);
                stopwatch.Stop();

                //double delta_ms = delta_s * 1000;
                //double delta_from_sleep = delta_ms - 1;
                double delta_ms = stopwatch.Elapsed.TotalMilliseconds;
                double delta_from_sleep = delta_ms - 1;

                //std::cout << std::fixed << std::setprecision(12) << "Resolution: " << (current_resolution / 10000.0) << "ms, Sleep(1) slept " << delta_ms << "ms (delta: " << delta_from_sleep << ")\n";
                Console.WriteLine($"Resolution: {current_resolution / 10000.0}ms, Sleep(1) slept {delta_ms}ms (delta: {delta_from_sleep})");


                /* if (samples) {
            sleep_delays.push_back(delta_from_sleep);

            if (i == args::get(samples)) {
                break;
            }

            Sleep(100);
        } else {
            Sleep(1000);
        }*/

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


            /*if (samples) {
        // discard first trial since it is almost always invalid
        sleep_delays.erase(sleep_delays.begin());

        sort(sleep_delays.begin(), sleep_delays.end());

        double sum = 0.0;
        for (double delay : sleep_delays) {
            sum += delay;
        }

        double average = sum / sleep_delays.size();

        // stdev
        double standard_deviation = 0.0;

        for (double delay : sleep_delays) {
            standard_deviation += pow(delay - average, 2);
        }

        double stdev = sqrt(standard_deviation / sleep_delays.size());

        std::cout << "\nMax: " << sleep_delays.back() << "\n";
        std::cout << "Avg: " << average << "\n";
        std::cout << "Min: " << sleep_delays.front() << "\n";
        std::cout << "STDEV: " << stdev << "\n";
    }*/

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