using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Text;

namespace LanChat.Server.Services
{
    public class KeyVaultCryptoService
    {
        private readonly CryptographyClient _cryptoClient;

        public KeyVaultCryptoService(IConfiguration configuration)
        {
            string vaultUri = configuration["KeyVaultUri"] ?? throw new InvalidOperationException("KeyVaultUri is missing.");

            string keyUrl = $"{vaultUri.TrimEnd('/')}/keys/LanChatEncryptionKey";

            _cryptoClient = new CryptographyClient(new Uri(keyUrl), new DefaultAzureCredential());
        }

        public async Task<string> EncryptTextAsync(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            EncryptResult result = await _cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plainTextBytes);

            return Convert.ToBase64String(result.Ciphertext);
        }

        public async Task<string> DecryptTextAsync(string cipherTextBase64)
        {
            if(string.IsNullOrWhiteSpace(cipherTextBase64)) return string.Empty;

            byte[] cipherTextBytes = Convert.FromBase64String(cipherTextBase64);

            DecryptResult result = await _cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, cipherTextBytes);

            return Encoding.UTF8.GetString(result.Plaintext);
        }
    }
}
