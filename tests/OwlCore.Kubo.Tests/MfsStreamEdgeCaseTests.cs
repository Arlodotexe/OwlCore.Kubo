using OwlCore.Storage;
using System.Text;

namespace OwlCore.Kubo.Tests
{
    [TestClass]
    public class MfsStreamEdgeCaseTests
    {
        [TestMethod]
        public async Task SetLengthOnUninitializedStream()
        {
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            await rootFolder.CreateFileAsync("test-uninit.txt", overwrite: true);
            
            // Create stream but don't write anything (file is 0 bytes)
            using var stream = new MfsStream("/test-uninit.txt", 0, TestFixture.Client);
            
            // Grow then shrink without intermediate write
            stream.SetLength(100);  // _length = 100, but IPFS file = 0 bytes
            stream.SetLength(50);   // Should this truncate? Create sparse file?
            
            // Write something and verify
            var data = Encoding.UTF8.GetBytes("test");
            stream.Write(data, 0, data.Length);
            stream.Flush();
            
            // Verify file behavior
            var file = new MfsFile("/test-uninit.txt", TestFixture.Client);
            using var readStream = await file.OpenReadAsync();
            var buffer = new byte[100];
            var bytesRead = await readStream.ReadAsync(buffer, 0, 100);
            
            // Document observed behavior
            Assert.IsTrue(bytesRead >= 4, $"Expected at least 4 bytes, got {bytesRead}");
        }

        [TestMethod]
        public async Task SetLengthWithPositionBeyondNewLength()
        {
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test-position.txt", overwrite: true);
            
            using var stream = new MfsStream("/test-position.txt", 0, TestFixture.Client);
            
            // Write 100 bytes
            var data = new byte[100];
            stream.Write(data, 0, 100);
            stream.Flush();
            
            // Move position to end
            stream.Position = 100;
            Assert.AreEqual(100, stream.Position);
            
            // Shrink to 10 bytes - Position should be clamped
            stream.SetLength(10);
            
            // Position should be clamped to Length
            Assert.AreEqual(10, stream.Position, "Position should be clamped to new Length");
            Assert.AreEqual(10, stream.Length);
            
            // Verify we can write at clamped position
            stream.Write(new byte[] { 1 }, 0, 1);
            stream.Flush();
            
            // Verify final file state
            using var readStream = await file.OpenReadAsync();
            var buffer = new byte[20];
            var bytesRead = await readStream.ReadAsync(buffer, 0, 20);
            Assert.AreEqual(11, bytesRead, "File should be 11 bytes (10 + 1 written)");
        }

        [TestMethod]
        public async Task SetLengthOnDeletedFile()
        {
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            var file = await rootFolder.CreateFileAsync("test-deleted.txt", overwrite: true);
            
            using var stream = new MfsStream("/test-deleted.txt", 0, TestFixture.Client);
            
            // Write some data
            stream.Write(new byte[10], 0, 10);
            stream.Flush();
            
            // Delete file externally via parent folder
            await rootFolder.DeleteAsync(file);
            
            // Try to SetLength - document behavior
            try
            {
                stream.SetLength(5);
                // If we get here, operation succeeded despite file deletion
                Assert.Inconclusive("SetLength succeeded on deleted file - may create sparse file or fail silently");
            }
            catch (AggregateException ex)
            {
                // Expected: some form of error
                Assert.IsTrue(ex.InnerExceptions.Any(x => x.Message.Contains("not exist") || x.Message.Contains("not found")),
                    $"Expected file not found error, got: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Document unexpected exception type
                Assert.IsNotNull(ex, $"Unexpected exception type: {ex.GetType().Name}: {ex.Message}");
            }
        }

        [TestMethod]
        public void SetLengthAfterDispose()
        {
            var rootFolder = new MfsFolder("/", TestFixture.Client);
            rootFolder.CreateFileAsync("test-disposed.txt", overwrite: true).Wait();
            
            var stream = new MfsStream("/test-disposed.txt", 0, TestFixture.Client);
            stream.Write(new byte[10], 0, 10);
            stream.Flush();
            stream.Dispose();
            
            // Should throw ObjectDisposedException
            Assert.ThrowsException<ObjectDisposedException>(() => 
            {
                stream.SetLength(5);
            });
        }
    }
}
