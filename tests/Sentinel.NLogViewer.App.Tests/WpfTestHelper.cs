using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sentinel.NLogViewer.App.Tests
{
    /// <summary>
    /// Helper class to run WPF tests on STA thread
    /// </summary>
    public static class WpfTestHelper
    {
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

