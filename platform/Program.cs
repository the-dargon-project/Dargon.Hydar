using CommandLine;
using ItzWarty;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Diagnostics;
using System.Threading;

namespace Dargon.Hydar {
   public static class Program {
      public static void Main(string[] args) {
         ThreadPool.SetMaxThreads(64, 64);
         InitializeLogging();

         Console.Title = "PID " + Process.GetCurrentProcess().Id;
         var options = new CorePlatformOptions();
         if (Parser.Default.ParseArgumentsStrict(args, options)) {
            new CorePlatformEgg().Start(options);
            new CountdownEvent(1).Wait();
         } else {
            Console.WriteLine("Failed to parse command line args.");
         }
      }

      private static void InitializeLogging() {
         var config = new LoggingConfiguration();
         Target debuggerTarget = new DebuggerTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };
         Target consoleTarget = new ColoredConsoleTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };

#if !DEBUG
         debuggerTarget = new AsyncTargetWrapper(debuggerTarget);
         consoleTarget = new AsyncTargetWrapper(consoleTarget);
#else
         // Stops optimizing imports from breaking code.
         new AsyncTargetWrapper().Wrap();
#endif

         config.AddTarget("debugger", debuggerTarget);
         config.AddTarget("console", consoleTarget);

         var debuggerRule = new LoggingRule("*", LogLevel.Trace, debuggerTarget);
         config.LoggingRules.Add(debuggerRule);

         var consoleRule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
         config.LoggingRules.Add(consoleRule);

         LogManager.Configuration = config;
      }
   }
}
