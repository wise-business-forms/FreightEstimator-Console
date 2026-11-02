using System;
using System.Collections.Generic;
using System.Linq;

namespace FreightEstApp35
{
    public enum RunMode
    {
        Test,
        Production,
    }

    /// <summary>The outcome of classifying this run: a mode and why, or a refusal to start.</summary>
    public sealed class EnvironmentResolution
    {
        public RunMode Mode { get; set; }
        public string Reason { get; set; }
        public string Refusal { get; set; }
        public bool IsRefused { get { return Refusal != null; } }
    }

    /// <summary>
    /// Pure decision, no I/O: classifies the machine as a Production Host or Test Mode and checks the
    /// configured servers against that mode. Test Mode may only use SANDBOX; a Production Host may not.
    /// </summary>
    public static class EnvironmentEvaluator
    {
        public const string SandboxServer = "SANDBOX";

        /// <summary>Splits the configured production host list on commas and semicolons.</summary>
        public static IReadOnlyList<string> ParseHostList(string setting)
        {
            if (string.IsNullOrWhiteSpace(setting))
            {
                return new string[0];
            }
            return setting.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim())
                .Where(h => h.Length > 0)
                .ToList();
        }

        public static EnvironmentResolution Evaluate(string machineName, IEnumerable<string> productionHosts, string databaseServer, string requestQueueServer)
        {
            string machine = (machineName ?? string.Empty).Trim();
            bool isProductionHost = productionHosts != null
                && productionHosts.Any(h => string.Equals((h ?? string.Empty).Trim(), machine, StringComparison.OrdinalIgnoreCase));

            if (isProductionHost)
            {
                string sandboxSetting = IsSandbox(databaseServer) ? "PROD_Server" : IsSandbox(requestQueueServer) ? "RemoteServerName" : null;
                if (sandboxSetting != null)
                {
                    return Refuse(RunMode.Production, machine + " is a configured production host and may not use "
                        + SandboxServer + ", but " + sandboxSetting + " is '" + SandboxServer + "'.");
                }

                return new EnvironmentResolution
                {
                    Mode = RunMode.Production,
                    Reason = machine + " is a configured production host",
                };
            }

            string wrongServer = FirstNonSandbox(databaseServer, requestQueueServer, out string setting);
            if (wrongServer != null)
            {
                return Refuse(RunMode.Test, machine + " is not a configured production host, so it runs in Test Mode and may only use "
                    + SandboxServer + ", but " + setting + " is " + Describe(wrongServer) + ".");
            }

            return new EnvironmentResolution
            {
                Mode = RunMode.Test,
                Reason = machine + " is not a configured production host",
            };
        }

        // Returns the offending value (empty string when unset) and which setting held it, or null when both are SANDBOX.
        static string FirstNonSandbox(string databaseServer, string requestQueueServer, out string setting)
        {
            setting = null;
            if (!IsSandbox(databaseServer))
            {
                setting = "PROD_Server";
                return databaseServer ?? string.Empty;
            }
            if (!IsSandbox(requestQueueServer))
            {
                setting = "RemoteServerName";
                return requestQueueServer ?? string.Empty;
            }
            return null;
        }

        static bool IsSandbox(string server)
        {
            return string.Equals(server == null ? null : server.Trim(), SandboxServer, StringComparison.OrdinalIgnoreCase);
        }

        static string Describe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "not set" : "'" + value.Trim() + "'";
        }

        static EnvironmentResolution Refuse(RunMode mode, string message)
        {
            return new EnvironmentResolution { Mode = mode, Refusal = message };
        }
    }
}
