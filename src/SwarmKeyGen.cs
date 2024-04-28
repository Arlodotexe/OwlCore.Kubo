using System.Security.Cryptography;
using OwlCore.Storage;
using OwlCore.Storage.Memory;

namespace OwlCore.Kubo
{
    /// <summary>
    /// A static helpers for generating swarm keys.
    /// </summary>
    public static class SwarmKeyGen
    {
        /// <summary>
        /// Generates a new swarm key.
        /// </summary>
        /// <remarks>Ported from <see href="https://github.com/Kubuxu/go-ipfs-swarm-key-gen/tree/master/ipfs-swarm-key-gen"/>.</remarks>
        /// <returns>An in-memory file containing the private key.</returns>
        public static async Task<IFile> CreateAsync(CancellationToken cancellationToken = default)
        {
            // Create in-memory file
            var file = new MemoryFile(new MemoryStream());

            // Write key to file
            await CreateAsync(file, cancellationToken);

            // Return the written file
            return file;
        }
        /// <summary>
        /// Generates a new swarm key.
        /// </summary>
        /// <remarks>Ported from <see href="https://github.com/Kubuxu/go-ipfs-swarm-key-gen/tree/master/ipfs-swarm-key-gen"/>.</remarks>
        /// <returns>An in-memory file containing the private key.</returns>
        public static async Task CreateAsync(IFile file, CancellationToken cancellationToken = default)
        {
            byte[] key = new byte[32]; // 32 bytes for the key

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key); // Fill the array with cryptographically secure random bytes
            }

            // Convert the byte array to a hexadecimal string
            string hexString = BitConverter.ToString(key).Replace("-", "").ToLower();

            var swarmKey = $"""
                            /key/swarm/psk/1.0.0/");
                            /base16/
                            {hexString}
                            """;

            // Write key to file
            await file.WriteTextAsync(swarmKey, cancellationToken);
        }
    }
}
