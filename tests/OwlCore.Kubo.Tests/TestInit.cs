using System.Diagnostics;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class TestInit
    {
        [AssemblyCleanup]
        public static void Cleanup()
        {
            KuboAccess.Bootstrapper?.Dispose();
        }

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            OwlCore.Diagnostics.Logger.MessageReceived += (sender, args) => Debug.WriteLine(args.Message);
        }
    }
}
