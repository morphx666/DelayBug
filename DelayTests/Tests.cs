using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DelayTests {
    public class Tests {
        private const double mspt = 1.0 / TimeSpan.TicksPerMillisecond;
        private static int iters = 5000;
        private static int delay = 2;

        private delegate void DelayMethod();

        private static AutoResetEvent evt = new AutoResetEvent(false);
        private static Timer tmr;

        private static (string Name, DelayMethod Method, bool useTimer)[] tests = {
            ($"Thread.Sleep({delay})", () => Thread.Sleep(delay), false),
            ($"Task.Delay({delay})", () => Task.Delay(delay).Wait(), false),
            ($"AutoResetEvent.WaitOne({delay})", () => evt.WaitOne(delay), false),
            ($"AutoResetEvent.WaitOne() | Triggered via Threading.Timer", () => evt.WaitOne(), true)
        };

        public static void RunTests(string framework) {
            tmr = new Timer((_) => evt.Set(), null, Timeout.Infinite, delay);

            Console.WriteLine($"Initializing for configuration:");
            Console.WriteLine($"    * Platform: {Environment.OSVersion.VersionString}");
            Console.WriteLine($"                {(Environment.Is64BitOperatingSystem ? "x64" : "x86")} / {Environment.ProcessorCount} cores");
            Console.WriteLine($"    * Framework: {framework} {(Type.GetType("System.MonoType") == null ? "" : " (Mono)")}");
            Console.WriteLine($"    * {TimeSpan.TicksPerMillisecond:N0} ms/tick");
            Console.WriteLine($"    * Delay: {delay:N0} ms");
            Console.WriteLine($"    * Loop: {iters:N0} iters");
            Console.WriteLine($"    * Visual Studio is {(Process.GetProcessesByName("devenv").Length > 0 ? "" : "not ")}running\n");

            for(int i = 0; i < tests.Length; i++) {
                Console.WriteLine($"Running test {i + 1}/{tests.Length}: {tests[i].Name}\n");
                if(tests[i].useTimer) tmr.Change(0, delay);
                Console.WriteLine($"    1 ms ~ {RunTest(tests[i].Method):N4} ms\n");
                if(tests[i].useTimer) tmr.Change(Timeout.Infinite, Timeout.Infinite);
            }

            tmr.Dispose();

            FindMinimumDelay();

            Console.WriteLine("Done. Press any key to exit.");
            Console.ReadKey(true);
        }

        private static double RunTest(DelayMethod delayMethod) {
            long accTicks = 0;

            Console.Write("    ...");

            Task.Run(() => {
                long curTick = DateTime.Now.Ticks;
                long lastTick = curTick;

                for(int i = 0; i < iters; i++) {
                    delayMethod.Invoke();

                    curTick = DateTime.Now.Ticks;
                    accTicks += (curTick - lastTick);
                    lastTick = curTick;
                }
            }).Wait();

            Console.CursorLeft = 0;
            return accTicks * mspt / (delay * iters);
        }

        private static void FindMinimumDelay() {
            Console.WriteLine("Testing minimum reliable Thread.Sleep(delay) for this platform:");

            iters = 1000;
            delay = 1;
            while(true) {
                double r = RunTest(tests[0].Method);
                double err = Math.Abs(1.0 - r) / ((1.0 + r) / 2.0) * 100.0;
                Console.WriteLine($"    For {delay,2}ms the error is {err,7:N2}% {(err < 40 ? " √\n" : "")}");
                if(err <= 40) break;
                delay++;
            }
        }
    }
}