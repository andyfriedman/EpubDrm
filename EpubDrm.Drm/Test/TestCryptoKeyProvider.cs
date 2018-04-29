using System;
using System.Security.Cryptography;
using System.Text;

namespace EpubDrm.Drm.Test
{
    public class TestCryptoKeyProvider : CryptoKeyProviderBase
    {
        private readonly string _isbn;
        private readonly Guid _isbnKey;

        public TestCryptoKeyProvider(string isbn, Guid isbnKey)
        {
            _isbn = isbn;
            _isbnKey = isbnKey;
        }

        public override byte[] GetEncryptionKey()
        {
            byte[] saltBytes = Convert.FromBase64String(Salt);
            byte[] guidBytes = _isbnKey.ToByteArray();
            byte[] saltedKey = new byte[guidBytes.Length + saltBytes.Length];

            for (int i = 0; i < guidBytes.Length; i++)
                saltedKey[i] = guidBytes[i];
            for (int i = 0; i < saltBytes.Length; i++)
                saltedKey[i + guidBytes.Length] = saltBytes[i];
            
            var hasher = new SHA256Managed();
            return hasher.ComputeHash(saltedKey /*Encoding.UTF8.GetBytes(isbnKey + _isbn + Salt)*/);
        }

        public override byte[] GetInitializationVector()
        {
            return Convert.FromBase64String("ZmVkY2JhMDk4NzY1NDMyMQ==");
        }
    }
}
