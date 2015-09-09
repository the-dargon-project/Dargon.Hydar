using Castle.DynamicProxy;
using CommandLine;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Networking;
using Dargon.Courier.Peering;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects.Streams;
using Dargon.Services.Messaging;
using ItzWarty;
using ItzWarty.IO;
using ItzWarty.Pooling;
using ItzWarty.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Diagnostics;
using System.Threading;
using Dargon.Hydar.Common;
using MessageSenderImpl = Dargon.Courier.Messaging.MessageSenderImpl;

namespace Dargon.Hydar {
   public static class Program {
      public class Options {
         [Option('s', "ServicePort", DefaultValue = 32000, HelpText = "Dedicated port for hydar cache service dsp.")]
         public int ServicePort { get; set; }

         [Option('m', "ManagementPort", DefaultValue = 32002, HelpText = "Dedicated port for hydar cache service dmi.")]
         public int ManagementPort { get; set; }

         [Option('c', "ConnectionString", DefaultValue = "Server=127.0.0.1;Port=5432;Database=dargon;User Id=dargon_development;Password=dargon;")]
         public string ConnectionString { get; set; }
      }

      public static void Main(string[] args) {
         ThreadPool.SetMaxThreads(64, 64);
         InitializeLogging();

         Console.Title = "PID " + Process.GetCurrentProcess().Id;
         var options = new Options();
         if (Parser.Default.ParseArgumentsStrict(args, options)) {
            new HydarEgg().Start(options.ServicePort, options.ManagementPort, options.ConnectionString);
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
         AsyncTargetWrapper a; // Placeholder for optimizing imports
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
