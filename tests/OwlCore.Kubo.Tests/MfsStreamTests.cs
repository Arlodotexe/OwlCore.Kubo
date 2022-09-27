namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class MfsStreamTests
    {
        [TestMethod]
        public async Task WriteRandomData()
        {
            await KuboAccess.TryInitAsync();

            // Create test file
            var rootFolder = new MfsFolder("/", KuboAccess.Ipfs);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, KuboAccess.Ipfs);
            var randomData = GenerateRandomData(256);
            stream.Write(randomData, 0, 256);
        }

        [TestMethod]
        public async Task WriteAndReadRandomData()
        {
            await KuboAccess.TryInitAsync();

            // Create test file
            var rootFolder = new MfsFolder("/", KuboAccess.Ipfs);
            var file = await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data, manually testing MfsStream (instead of opening MfsStream from file)
            var randomData = GenerateRandomData(256);
            using (var stream = new MfsStream("/test.bin", 0, KuboAccess.Ipfs))
            {
                stream.Write(randomData, 0, 256);
            }

            // Open MfsStream from file, as standard stream.
            using var fileStream = await file.OpenStreamAsync();

            var buffer = new byte[256];
            var bytesRead = fileStream.Read(buffer, 0, 256);

            // Validate
            Assert.AreEqual(256, bytesRead);
            CollectionAssert.AreEqual(randomData, buffer);
        }

        [TestMethod]
        public async Task WriteRandomDataAsync()
        {
            await KuboAccess.TryInitAsync();

            // Create test file
            var rootFolder = new MfsFolder("/", KuboAccess.Ipfs);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, KuboAccess.Ipfs);
            var randomData = GenerateRandomData(256);
            await stream.WriteAsync(randomData, 0, 256);
        }

        [TestMethod]
        public async Task WriteAndReadRandomDataAsync()
        {
            await KuboAccess.TryInitAsync();

            // Create test file
            var rootFolder = new MfsFolder("/", KuboAccess.Ipfs);
            var file = await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, KuboAccess.Ipfs);
            var randomData = GenerateRandomData(256);
            await stream.WriteAsync(randomData, 0, 256);
            await stream.FlushAsync();

            // Read data back via file.
            using var fileStream = await file.OpenStreamAsync();

            var buffer = new byte[256];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, 256);

            // Validate
            Assert.AreEqual(256, bytesRead);
            CollectionAssert.AreEqual(randomData, buffer);
        }

        [TestMethod]
        public async Task Flush()
        {
            await KuboAccess.TryInitAsync();

            // Create test file
            var rootFolder = new MfsFolder("/", KuboAccess.Ipfs);
            var file = await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, KuboAccess.Ipfs);
            var randomData = GenerateRandomData(256);
            stream.Write(randomData, 0, 256);

            stream.Flush();
        }

        [TestMethod]
        public async Task FlushAsync()
        {
            await KuboAccess.TryInitAsync();

            // Create test file
            var rootFolder = new MfsFolder("/", KuboAccess.Ipfs);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, KuboAccess.Ipfs);
            var randomData = GenerateRandomData(256);
            stream.Write(randomData, 0, 256);

            await stream.FlushAsync();
        }

        [TestMethod]
        public async Task Seek()
        {
            await KuboAccess.TryInitAsync();

            // Create test file
            var rootFolder = new MfsFolder("/", KuboAccess.Ipfs);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, KuboAccess.Ipfs);
            var randomData = GenerateRandomData(256);
            stream.Write(randomData, 0, 256);

            // SeekOrigin.Begin
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0, stream.Position);

            stream.Seek(5, SeekOrigin.Begin);
            Assert.AreEqual(5, stream.Position);

            // SeekOrigin.End
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.End);
            Assert.AreEqual(stream.Length, stream.Position);

            stream.Seek(-5, SeekOrigin.End);
            Assert.AreEqual(stream.Length - 5, stream.Position);

            // SeekOrigin.Current
            var currentPos = stream.Position;
            stream.Seek(0, SeekOrigin.Current);
            Assert.AreEqual(currentPos, stream.Position);

            stream.Seek(5, SeekOrigin.Current);
            Assert.AreEqual(currentPos + 5, stream.Position);

            stream.Seek(-5, SeekOrigin.Current);
            Assert.AreEqual(currentPos, stream.Position);

        }

        static byte[] GenerateRandomData(int length)
        {
            var rand = new Random();
            var b = new byte[length];
            rand.NextBytes(b);

            return b;
        }
    }
}