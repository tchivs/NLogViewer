using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using NLog;
using Sentinel.NLogViewer.Wpf;

namespace Sentinel.NLogViewer.App.Tests
{
    /// <summary>
    /// Helper class to run WPF tests on STA thread
    /// </summary>
    public static class WpfTestHelper
    {
        /// <summary>
        /// Creates an NLogViewer, feeds it with the given log events via a TestCacheTarget,
        /// and waits for the viewer to process them (async pipeline + dispatcher). Call from within RunOnStaThread.
        /// </summary>
        /// <param name="testData">Log events to feed into the viewer.</param>
        /// <returns>The viewer with CacheTarget set and events processed.</returns>
        public static Wpf.NLogViewer CreateViewerWithTestData(IEnumerable<LogEventInfo> testData)
        {
            var list = testData?.ToList() ?? new List<LogEventInfo>();
            var viewer = new Wpf.NLogViewer();
            var cache = new TestCacheTarget();
            cache.AddRange(list);
            viewer.CacheTarget = cache;
            WaitForViewerEvents(viewer, list.Count);
            return viewer;
        }

        /// <summary>
        /// Waits until the viewer's LogEvents.View contains at least expectedCount items (or timeout).
        /// Pumps the current dispatcher so that async subscription callbacks run.
        /// NLogViewer uses SubscribeOn(Scheduler.Default) and Buffer(100ms), so an initial delay is needed.
        /// </summary>
        public static void WaitForViewerEvents(Wpf.NLogViewer viewer, int expectedCount, int timeoutMs = 2000)
        {
            // Give the async pipeline time: subscription on thread pool + Buffer(100ms) + dispatch
            Thread.Sleep(200);
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, () => { });
                var count = viewer.LogEvents?.View?.Cast<LogEventInfo>().Count() ?? 0;
                if (count >= expectedCount) return;
                Thread.Sleep(50);
            }
        }
        /// <summary>
        /// Runs an action on an STA thread
        /// </summary>
        public static void RunOnStaThread(Action action)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                // Already on STA thread, just execute
                action();
                return;
            }

            // Create STA thread and run action
            Exception exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Initialize WPF application context if not already done
                    if (Application.Current == null)
                    {
                        var app = new Application();
                        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    }

                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Runs a function on an STA thread and returns the result
        /// </summary>
        public static T RunOnStaThread<T>(Func<T> func)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                // Already on STA thread, just execute
                return func();
            }

            // Create STA thread and run function
            T result = default(T);
            Exception exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Initialize WPF application context if not already done
                    if (Application.Current == null)
                    {
                        var app = new Application();
                        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    }

                    result = func();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
            {
                throw exception;
            }

            return result;
        }
    }
}

