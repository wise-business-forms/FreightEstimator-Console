using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Data.SqlClient;

namespace FreightEstApp35
{
    public sealed class DbEndpoint
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Login { get; set; }

        // Copies only non-secret fields, so the password cannot reach the banner.
        public static DbEndpoint FromConnectionString(string connString)
        {
            var builder = new SqlConnectionStringBuilder(connString);
            return new DbEndpoint
            {
                Server = builder.DataSource,
                Database = builder.InitialCatalog,
                Login = builder.IntegratedSecurity ? "(Windows integrated)" : builder.UserID,
            };
        }
    }

    public sealed class ConnectivityCheck
    {
        public ConnectivityCheck(string label, bool ok, string detail)
        {
            Label = label;
            Ok = ok;
            Detail = detail;
        }

        public string Label { get; }
        public bool Ok { get; }
        public string Detail { get; }
    }

    public sealed class StartupInfo
    {
        public string Version { get; set; }
        public DateTime StartedAt { get; set; }
        public string MachineName { get; set; }
        public string Plant { get; set; }
        public DbEndpoint CarrierLogDb { get; set; }
        public string RequestQueue { get; set; }
        public IReadOnlyList<ConnectivityCheck> Checks { get; set; }
        public IReadOnlyList<KeyValuePair<string, string>> ConnectedServers { get; set; }
        public string WindowsAccount { get; set; }
        public string RuntimeDescription { get; set; }
        public string OperatingSystem { get; set; }
        public string Architecture { get; set; }
    }

    public static class StartupBanner
    {
        const int LabelWidth = 18;

        /// <summary>Gathers the live configuration, checks the SQL connections, and prints the banner.</summary>
        public static void Print()
        {
            string text = Render(Gather());

            if (Console.IsOutputRedirected)
            {
                Console.Write(text);
                return;
            }

            // Interactive console only: colour the problem lines so they stand out.
            foreach (string line in text.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                bool bad = line.Contains("[FAIL]") || line.Contains("[WARN");
                if (bad) Console.ForegroundColor = line.Contains("[FAIL]") ? ConsoleColor.Red : ConsoleColor.Yellow;
                Console.WriteLine(line);
                if (bad) Console.ResetColor();
            }
        }

        static StartupInfo Gather()
        {
            string remote = Config.RemoteServerName;

            return new StartupInfo
            {
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                StartedAt = DateTime.Now,
                MachineName = Environment.MachineName,
                Plant = DescribePlant(remote, Config.PlantNames),
                CarrierLogDb = DbEndpoint.FromConnectionString(Config.ConnString),
                RequestQueue = remote + ".CostPlus",
                Checks = new[]
                {
                    CheckSql("Carrier log DB", Config.ConnString, "SELECT 1"),
                    CheckSql("Request queue", Config.ConnString, "SELECT TOP 1 1 FROM " + remote + ".CostPlus.dbo.FreightRequests"),
                },
                ConnectedServers = new[]
                {
                    Host("TransPlace", Config.TransPlaceProdEndpoint.Url, Config.TransPlaceDemoEndpoint.Url),
                    Host("TransportationInsight", Config.TransportationInsightProdUrl, Config.TransportationInsightDemoUrl),
                    Host("M33", Config.M33Url, null),
                },
                WindowsAccount = Environment.UserDomainName + "\\" + Environment.UserName,
                RuntimeDescription = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            };
        }

        // Configured host only; carriers are never probed at startup.
        static KeyValuePair<string, string> Host(string name, string prodUrl, string testUrl)
        {
            string host = new Uri(prodUrl).Host;
            string testHost = testUrl == null ? null : new Uri(testUrl).Host;
            if (testHost != null && testHost != host)
            {
                host += " (test calls: " + testHost + ")";
            }
            return new KeyValuePair<string, string>(name, host);
        }

        static ConnectivityCheck CheckSql(string label, string connString, string query)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connString) { ConnectTimeout = 5 };
                using (var conn = new SqlConnection(builder.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(query, conn) { CommandTimeout = 5 })
                    {
                        cmd.ExecuteScalar();
                    }
                }
                return new ConnectivityCheck(label, true, "OK");
            }
            catch (Exception ex)
            {
                // Startup must not fail because a check failed; the message is the first line only.
                string firstLine = ex.Message.Split('\n')[0].Trim();
                return new ConnectivityCheck(label, false, firstLine);
            }
        }

        public static string Render(StartupInfo info)
        {
            var sb = new StringBuilder();
            string rule = new string('=', 64);

            sb.AppendLine(rule);
            sb.AppendLine("  FreightEstApp35 - startup");
            sb.AppendLine(rule);

            Section(sb, "Application");
            Row(sb, "Version", info.Version);
            Row(sb, "Started", info.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            Row(sb, "Machine", info.MachineName);
            Row(sb, "Plant", info.Plant);

            Section(sb, "Databases written to");
            Row(sb, "Carrier log DB", info.CarrierLogDb.Server + " / " + info.CarrierLogDb.Database);
            Row(sb, "Request queue", info.RequestQueue);

            Section(sb, "Connectivity");
            foreach (ConnectivityCheck check in info.Checks)
            {
                string status = check.Ok ? "[ OK ]" : "[FAIL]";
                string detail = check.Ok || string.IsNullOrEmpty(check.Detail) ? string.Empty : ": " + check.Detail;
                sb.AppendLine("  " + status + " " + check.Label + detail);
            }

            Section(sb, "Connected servers");
            foreach (KeyValuePair<string, string> server in info.ConnectedServers)
            {
                Row(sb, server.Key, server.Value);
            }

            Section(sb, "Accounts");
            string login = info.CarrierLogDb.Login;
            if (string.Equals(login, "sa", StringComparison.OrdinalIgnoreCase))
            {
                login += "  [WARN: sysadmin login]";
            }
            Row(sb, "SQL login", login);
            Row(sb, "Windows account", info.WindowsAccount);

            Section(sb, "Runtime");
            Row(sb, ".NET runtime", info.RuntimeDescription);
            Row(sb, "OS", info.OperatingSystem);
            Row(sb, "Architecture", info.Architecture);

            sb.AppendLine(rule);
            return sb.ToString();
        }

        static void Section(StringBuilder sb, string title)
        {
            sb.AppendLine();
            sb.AppendLine(title);
        }

        static void Row(StringBuilder sb, string label, string value)
        {
            sb.AppendLine("  " + (label + ":").PadRight(LabelWidth) + value);
        }

        public static string DescribePlant(string remoteServerName, IDictionary<string, string> plantNames)
        {
            if (string.IsNullOrWhiteSpace(remoteServerName))
            {
                return "UNKNOWN (linked server not configured)";
            }

            // Linked servers are named <PLANTCODE>ERP (ALPERP, BUTERP, ...).
            const string suffix = "ERP";
            if (remoteServerName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                string code = remoteServerName.Substring(0, remoteServerName.Length - suffix.Length).ToUpperInvariant();
                if (plantNames.TryGetValue(code, out string name))
                {
                    return code + " - " + name + " (linked server " + remoteServerName + ")";
                }
            }

            return "UNKNOWN (linked server " + remoteServerName + ")";
        }
    }
}
