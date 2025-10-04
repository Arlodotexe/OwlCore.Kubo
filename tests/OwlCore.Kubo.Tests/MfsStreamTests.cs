using OwlCore.Storage;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class MfsStreamTests
    {
        [TestMethod]
        public async Task WriteRandomData()
        {
            // Create test file
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, TestFixture.Client);
            var randomData = GenerateRandomData(256);
            stream.Write(randomData, 0, 256);
        }

        [TestMethod]
        public async Task WriteAndReadRandomData()
        {
            // Create test file
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data, manually testing MfsStream (instead of opening MfsStream from file)
            var randomData = GenerateRandomData(256);
            using (var stream = new MfsStream("/test.bin", 0, TestFixture.Client))
            {
                stream.Write(randomData, 0, 256);
            }

            // Open MfsStream from file, as standard stream.
            using var fileStream = await file.OpenReadAsync();

            var buffer = new byte[256];
            var bytesRead = fileStream.Read(buffer, 0, 256);

            // Validate
            Assert.AreEqual(256, bytesRead);
            CollectionAssert.AreEqual(randomData, buffer);
        }

        [TestMethod]
        public async Task WriteRandomDataAsync()
        {
            // Create test file
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, TestFixture.Client);
            var randomData = GenerateRandomData(256);
            await stream.WriteAsync(randomData, 0, 256);
        }

        [TestMethod]
        public async Task WriteAndReadRandomDataAsync()
        {
            // Create test file
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, TestFixture.Client);
            var randomData = GenerateRandomData(256);
            await stream.WriteAsync(randomData, 0, 256);

            // Read data back via file.
            using var fileStream = await file.OpenReadAsync();

            var buffer = new byte[256];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, 256);

            // Validate
            Assert.AreEqual(256, bytesRead);
            CollectionAssert.AreEqual(randomData, buffer);
        }

        [TestMethod]
        public async Task Flush()
        {
            // Create test file
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, TestFixture.Client);
            var randomData = GenerateRandomData(256);
            stream.Write(randomData, 0, 256);

            stream.Flush();
        }

        [TestMethod]
        public async Task FlushAsync()
        {
            // Create test file
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, TestFixture.Client);
            var randomData = GenerateRandomData(256);
            stream.Write(randomData, 0, 256);

            await stream.FlushAsync();
        }

        [TestMethod]
        public async Task Seek()
        {
            // Create test file
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            await rootFolder.CreateFileAsync("test.bin", overwrite: true);

            // Write random data
            using var stream = new MfsStream("/test.bin", 0, TestFixture.Client);
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

        [TestMethod]
        public async Task SetLengthTruncates()
        {
            // Create test file with initial content
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test-truncate.txt", overwrite: true);

            // Write initial content: "ABC\nDEF" (7 bytes)
            using (var stream = new MfsStream("/test-truncate.txt", 0, TestFixture.Client))
            {
                var initialData = System.Text.Encoding.UTF8.GetBytes("ABC\nDEF");
                await stream.WriteAsync(initialData, 0, initialData.Length);
                await stream.FlushAsync();
            }

            // Verify initial content written correctly
            using (var readStream = await file.OpenReadAsync())
            {
                var buffer = new byte[7];
                await readStream.ReadAsync(buffer, 0, 7);
                var content = System.Text.Encoding.UTF8.GetString(buffer);
                Assert.AreEqual("ABC\nDEF", content);
            }

            // Now write shorter content with truncation
            using (var stream = new MfsStream("/test-truncate.txt", 7, TestFixture.Client))
            {
                stream.SetLength(0); // Should truncate to 0 bytes
                var newData = System.Text.Encoding.UTF8.GetBytes("X");
                await stream.WriteAsync(newData, 0, newData.Length);
                await stream.FlushAsync();
            }

            // Read back and verify only "X" exists (no "XBC\nDEF" corruption)
            using (var readStream = await file.OpenReadAsync())
            {
                var buffer = new byte[20]; // Extra space to catch any garbage
                var bytesRead = await readStream.ReadAsync(buffer, 0, 20);
                var content = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Assert.AreEqual(1, bytesRead, "File should be 1 byte (just 'X')");
                Assert.AreEqual("X", content, "File should contain only 'X', not old bytes");
            }
        }

        [TestMethod]
        public async Task SetLengthTruncatesViaFileStream()
        {
            // This test matches the bug report scenario more closely by using MfsFile.OpenStreamAsync()
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test-truncate-filestream.txt", overwrite: true);

            // Write initial content: "ABC\nDEF" (7 bytes)
            using (var writeStream = await file.OpenStreamAsync(System.IO.FileAccess.Write))
            {
                var initialData = System.Text.Encoding.UTF8.GetBytes("ABC\nDEF");
                await writeStream.WriteAsync(initialData, 0, initialData.Length);
                await writeStream.FlushAsync();
            }

            // Verify initial content
            using (var readStream = await file.OpenReadAsync())
            {
                var buffer = new byte[7];
                await readStream.ReadAsync(buffer, 0, 7);
                var content = System.Text.Encoding.UTF8.GetString(buffer);
                Assert.AreEqual("ABC\nDEF", content, "Initial content should be 'ABC\\nDEF'");
            }

            // Write shorter content with SetLength(0) - this is where the bug manifests
            using (var writeStream = await file.OpenStreamAsync(System.IO.FileAccess.Write))
            {
                writeStream.SetLength(0); // Should truncate to 0 bytes
                var newData = System.Text.Encoding.UTF8.GetBytes("X");
                await writeStream.WriteAsync(newData, 0, newData.Length);
                await writeStream.FlushAsync();
            }

            // Read back and verify - this is where bug would show "XBC\nDEF" instead of "X"
            using (var readStream = await file.OpenReadAsync())
            {
                var buffer = new byte[20]; // Extra space to catch corruption
                var bytesRead = await readStream.ReadAsync(buffer, 0, 20);
                var content = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Assert.AreEqual(1, bytesRead, "File should be 1 byte after truncation");
                Assert.AreEqual("X", content, "File should contain only 'X', not 'XBC\\nDEF'");
            }
        }

        [TestMethod]
        public async Task SetLengthTruncatesWithStreamWriter()
        {
            // This test EXACTLY matches the bug report reproduction code using StreamWriter
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test-truncate-streamwriter.txt", overwrite: true);

            // Write initial content: "ABC\nDEF" (7 bytes) using StreamWriter
            using (var writeStream = await file.OpenStreamAsync(System.IO.FileAccess.Write))
            {
                using var writer = new StreamWriter(writeStream, new System.Text.UTF8Encoding(false));
                await writer.WriteAsync("ABC\nDEF");
                await writer.FlushAsync();
            }

            // Verify initial content
            using (var readStream = await file.OpenReadAsync())
            {
                var buffer = new byte[7];
                await readStream.ReadAsync(buffer, 0, 7);
                var content = System.Text.Encoding.UTF8.GetString(buffer);
                Assert.AreEqual("ABC\nDEF", content, "Initial content should be 'ABC\\nDEF'");
            }

            // Write shorter content with SetLength(0) using StreamWriter - BUG REPRODUCTION
            using (var writeStream = await file.OpenStreamAsync(System.IO.FileAccess.Write))
            {
                writeStream.SetLength(0); // Should truncate to 0 bytes
                using var writer = new StreamWriter(writeStream, new System.Text.UTF8Encoding(false));
                await writer.WriteAsync("X");
                await writer.FlushAsync();
            }

            // Read back and verify - BUG: expect "XBC\nDEF" corruption here
            using (var readStream = await file.OpenReadAsync())
            {
                var buffer = new byte[20]; // Extra space to catch corruption
                var bytesRead = await readStream.ReadAsync(buffer, 0, 20);
                var content = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Assert.AreEqual(1, bytesRead, $"File should be 1 byte after truncation, was {bytesRead}");
                Assert.AreEqual("X", content, $"File should contain only 'X', but got '{content}'");
            }
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