using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DelayTests {
    public class Tests {
        private const int iters = 5000;
        private const int delay = 2;
        private const double mspt = 1.0 / TimeSpan.TicksPerMillisecond;

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
            Console.WriteLine($"    * Platform: {Environment.OSVersion.Platform}");
            Console.WriteLine($"    * Framework: {framework} {(Type.GetType("System.MonoType") == null ? "" : " (Mono)")}");
            Console.WriteLine($"    * {TimeSpan.TicksPerMillisecond:N0} ms/tick");
            Console.WriteLine($"    * Delay: {delay:N0} ms");
            Console.WriteLine($"    * Loop: {iters:N0} iters");
            Console.WriteLine($"    * Visual Studio is {(Process.GetProcessesByName("devenv").Length > 0 ? "" : "not ")}running\n");

            for(int i = 0; i < tests.Length; i++) {
                Console.WriteLine($"Starting test #{i + 1}: {tests[i].Name}\n");
                if(tests[i].useTimer) tmr.Change(0, delay);
                Console.WriteLine($"    1ms = {RunTest(tests[i].Method):N4} ms\n");
                if(tests[i].useTimer) tmr.Change(Timeout.Infinite, Timeout.Infinite);
            }

            tmr.Dispose();

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
    }
}