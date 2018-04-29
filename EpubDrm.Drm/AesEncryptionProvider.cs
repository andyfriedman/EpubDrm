using System;
using System.IO;
using System.Security.Cryptography;

namespace EpubDrm.Drm
{
    public class AesEncryptionProvider : IEncryptionProvider
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryptionProvider(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;
        }

        public AesEncryptionProvider(ICryptoKeyProvider keyProvider)
        {
            _key = keyProvider.GetEncryptionKey();
            _iv = keyProvider.GetInitializationVector();
        }

        public byte[] Encrypt(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var cipher = CreateCipher();

            using (var cryptoTransform = cipher.CreateEncryptor())
                return cryptoTransform.TransformFinalBlock(data, 0, data.Length);
        }

        public void EncryptToStream(byte[] data, Stream stream)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (stream == null)
                throw new ArgumentNullException("stream");
            
            var cipher = CreateCipher();

            using (var cryptoTransform = cipher.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(stream, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
            }
        }

        public byte[] Decrypt(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var cipher = CreateCipher();

            using (var cryptoTransform = cipher.CreateDecryptor())
                return cryptoTransform.TransformFinalBlock(data, 0, data.Length);
        }

        public void DecryptToStream(byte[] data, Stream stream)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (stream == null)
                throw new ArgumentNullException("stream");

            var cipher = CreateCipher();

            using (var cryptoTransform = cipher.CreateDecryptor())
            using (var cryptoStream = new CryptoStream(stream, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
            }
        }

        private byte[] GetStreamData(Stream stream)
        {
            byte[] data;
            using (var reader = new MemoryStream())
            {
                stream.CopyTo(reader);
                data = reader.GetBuffer();
            }
            stream.Position = 0;
            return data;
        }

        private AesManaged CreateCipher()
        {
            return new AesManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
                Key = _key,
                IV = _iv
            };
        }
    }
}
