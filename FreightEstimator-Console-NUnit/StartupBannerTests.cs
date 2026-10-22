using System.Collections.Generic;
using FreightEstApp35;
using NUnit.Framework;

namespace FreightEstimator_Console_NUnit
{
    public class StartupBannerTests
    {
        static readonly Dictionary<string, string> Plants = new Dictionary<string, string>
        {
            { "ALP", "Alpharetta" },
            { "BUT", "Butler" },
        };

        static StartupInfo SampleInfo(string connString = "Server=dbhost01; Initial Catalog=UpsRate;uid=sa;pwd=Sup3rS3cret!; Connection Timeout=300;")
        {
            return new StartupInfo
            {
                Version = "1.2.3.4",
                StartedAt = new System.DateTime(2026, 10, 22, 14, 5, 0),
                MachineName = "DEV-LAPTOP",
                Mode = RunMode.Test,
                ModeReason = "DEV-LAPTOP is not a configured production host",
                Plant = "ALP - Alpharetta (linked server ALPERP)",
                CarrierLogDb = DbEndpoint.FromConnectionString(connString),
                RequestQueue = "ALPERP.CostPlus",
                Checks = new[]
                {
                    new ConnectivityCheck("Carrier log DB", true, "OK"),
                    new ConnectivityCheck("Request queue", false, "Login failed"),
                },
                ConnectedServers = new[] { new KeyValuePair<string, string>("TransPlace", "tms.transplace.com") },
                WindowsAccount = "WISENT\\svc-freight",
                RuntimeDescription = ".NET 10.0.1",
                OperatingSystem = "Windows 11",
                Architecture = "X64",
            };
        }

        [Test]
        public void Render_ConnectionStringWithPassword_NeverShowsPassword()
        {
            string text = StartupBanner.Render(SampleInfo());

            Assert.That(text, Does.Not.Contain("Sup3rS3cret!"));
            Assert.That(text, Does.Not.Contain("pwd"));
        }

        [Test]
        public void Render_ShowsServersAccountsPlantAndRuntime()
        {
            string text = StartupBanner.Render(SampleInfo());

            Assert.That(text, Does.Contain("dbhost01"));
            Assert.That(text, Does.Contain("UpsRate"));
            Assert.That(text, Does.Contain("ALPERP.CostPlus"));
            Assert.That(text, Does.Contain("ALP - Alpharetta (linked server ALPERP)"));
            Assert.That(text, Does.Contain("WISENT\\svc-freight"));
            Assert.That(text, Does.Contain("tms.transplace.com"));
            Assert.That(text, Does.Contain(".NET 10.0.1"));
            Assert.That(text, Does.Contain("1.2.3.4"));
        }

        [Test]
        public void Render_TestMode_ShowsModeMachineAndReason()
        {
            string text = StartupBanner.Render(SampleInfo());

            Assert.That(text, Does.Contain("TEST MODE"));
            Assert.That(text, Does.Contain("DEV-LAPTOP"));
            Assert.That(text, Does.Contain("DEV-LAPTOP is not a configured production host"));
        }

        [Test]
        public void Render_ProductionHost_ShowsProductionHost()
        {
            StartupInfo info = SampleInfo();
            info.Mode = RunMode.Production;
            info.ModeReason = "PRODHOST01 is a configured production host";

            string text = StartupBanner.Render(info);

            Assert.That(text, Does.Contain("PRODUCTION HOST"));
            Assert.That(text, Does.Not.Contain("TEST MODE"));
            Assert.That(text, Does.Contain("PRODHOST01 is a configured production host"));
        }

        [Test]
        public void Render_SaLogin_IsFlagged()
        {
            string text = StartupBanner.Render(SampleInfo());

            Assert.That(text, Does.Contain("sa  [WARN: sysadmin login]"));
        }

        [Test]
        public void Render_NonSaLogin_IsNotFlagged()
        {
            string text = StartupBanner.Render(SampleInfo("Server=s; Initial Catalog=UpsRate;uid=freight_svc;pwd=x;"));

            Assert.That(text, Does.Contain("freight_svc"));
            Assert.That(text, Does.Not.Contain("WARN"));
        }

        [Test]
        public void Render_FailedCheck_ShowsFailAndDetail()
        {
            string text = StartupBanner.Render(SampleInfo());

            Assert.That(text, Does.Contain("[ OK ] Carrier log DB"));
            Assert.That(text, Does.Contain("[FAIL] Request queue: Login failed"));
        }

        [Test]
        public void DescribePlant_LinkedServerMatchesPlantCode_ShowsCodeNameAndLinkedServer()
        {
            string text = StartupBanner.DescribePlant("ALPERP", Plants);

            Assert.That(text, Is.EqualTo("ALP - Alpharetta (linked server ALPERP)"));
        }

        [Test]
        public void DescribePlant_LinkedServerNotAPlant_ShowsUnknownAndRawName()
        {
            string text = StartupBanner.DescribePlant("XYZERP", Plants);

            Assert.That(text, Is.EqualTo("UNKNOWN (linked server XYZERP)"));
        }

        [Test]
        public void DescribePlant_NoLinkedServerConfigured_ShowsNotConfigured()
        {
            string text = StartupBanner.DescribePlant(null, Plants);

            Assert.That(text, Is.EqualTo("UNKNOWN (linked server not configured)"));
        }
    }
}
