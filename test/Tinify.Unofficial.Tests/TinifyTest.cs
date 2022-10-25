using NUnit.Framework;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

// ReSharper disable InconsistentNaming

namespace Tinify.Unofficial.Tests
{
    [SetUpFixture]
    public class TinifyUnofficialTestsSetup
    {
        [OneTimeSetUp]
        public void InitializationForAllTests()
        {
            TinifyClient.RetryDelay = 10;
        }
    }
}
