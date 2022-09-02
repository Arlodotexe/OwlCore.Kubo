namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class TestCleanup
    {
        [AssemblyCleanup]
        public static void Cleanup()
        {
            KuboAccess.Bootstrapper?.Dispose();
        }
    }
}
