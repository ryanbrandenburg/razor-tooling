// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            MainAsync(args).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }

        public static async Task MainAsync(string[] args)
        {
            var trace = Trace.Messages;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("debug", StringComparison.OrdinalIgnoreCase))
                {
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(1000);
                    }

                    Debugger.Break();
                    continue;
                }

                if (args[i] == "--trace" && i + 1 < args.Length)
                {
                    var traceArg = args[++i];
                    if (!Enum.TryParse(traceArg, out trace))
                    {
                        trace = Trace.Messages;
                        Console.WriteLine($"Invalid Razor trace '{traceArg}'. Defaulting to {trace}.");
                    }
                }
            }

            throw new NotImplementedException();
            //var server = await RazorLanguageServer.CreateAsync(
            //    Console.OpenStandardInput(),
            //    Console.OpenStandardOutput(),
            //    trace,
            //    razorLspServiceProvider: null,
            //    asynchronousOperationListenerProvider: null);
            //await server.InitializedAsync(CancellationToken.None);

            //using var semaphore = new SemaphoreSlim(1);
            //await semaphore.WaitAsync();

            //server.Exit += On_Exit;

            //await semaphore.WaitAsync();

            //void On_Exit(object sender, object args)
            //{
            //    semaphore.Release();
            //}
        }
    }
}
