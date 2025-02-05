using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Domain.Entities;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Services
{
    public class AesEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private const string KEY_SETTING_NAME = "Encryption:Key";
        private const string IV_SETTING_NAME = "Encryption:IV";

        public AesEncryptionService(ApiDbContext dbContext)
        {
            // Récupérer ou générer les clés de chiffrement
            var keyString = GetOrCreateEncryptionKey(dbContext, KEY_SETTING_NAME);
            var ivString = GetOrCreateEncryptionKey(dbContext, IV_SETTING_NAME, 16); // AES utilise un IV de 16 bytes

            _key = Convert.FromBase64String(keyString);
            _iv = Convert.FromBase64String(ivString);
        }

        private string GetOrCreateEncryptionKey(ApiDbContext dbContext, string settingName, int keySize = 32)
        {
            var setting = dbContext.Settings.FirstOrDefault(s => s.Name == settingName);
            
            if (setting == null)
            {
                // Générer une nouvelle clé
                byte[] keyBytes = new byte[keySize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(keyBytes);
                }
                
                var keyBase64 = Convert.ToBase64String(keyBytes);
                
                setting = new Setting
                {
                    Name = settingName,
                    Value = keyBase64,
                    Type = "encrypted",
                    Description = $"Clé de chiffrement générée automatiquement ({keySize} bytes)"
                };
                
                dbContext.Settings.Add(setting);
                dbContext.SaveChanges();
            }
            
            return setting.Value;
        }

        public async Task<string> EncryptAsync(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                await swEncrypt.WriteAsync(plainText);
            }

            var encrypted = msEncrypt.ToArray();
            return Convert.ToBase64String(encrypted);
        }

        public async Task<string> DecryptAsync(string encryptedText)
        {
            var cipherBytes = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return await srDecrypt.ReadToEndAsync();
        }
    }
} 