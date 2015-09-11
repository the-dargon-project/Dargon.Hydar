using System;
using CommandLine;
using System.Threading;
using FluentValidation;
using ItzWarty;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Dargon.Platform.Webend {
   public class WebendOptions {
      [Option("PlatformServiceEndpoints", DefaultValue = "localhost:31337")]
      public string PlatformServiceEndpoints { get; set; }
   }

   public static class Program {
      public static void Main(string[] args) {
         InitializeLogging();
         var webendOptions = new WebendOptions();
         if (!Parser.Default.ParseArguments(args, webendOptions)) {
            throw new InvalidOperationException("Args parsing failed.");
         } else {
            new WebendApplicationEgg().Start("http://localhost:1234", webendOptions);
            new AutoResetEvent(false).WaitOne();
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
