using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using EpubDrm.Drm;
using EpubDrm.Drm.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EpubDrm.Drm.Tests
{
    [TestClass]
    public class EncryptionProviderTests
    {
        [TestMethod]
        public void AesEncryptionProvider_Encrypt_Decrypt_PlainText()
        {
            // arrange
            const string expected = "This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text. This is some plain text.";
            var aes = new AesEncryptionProvider(new TestCryptoKeyProvider("9781908006028", 
                new Guid("F588C946-386F-4A49-A333-64A189D07DD4")));
            
            // act
            var encrypted = aes.Encrypt(Encoding.UTF8.GetBytes(expected));
            var decrypted = aes.Decrypt(encrypted);
            var actual = Encoding.ASCII.GetString(decrypted);

            // assert
            Assert.AreEqual(actual, expected);
        }
        
        [TestMethod]
        public void AesEncryptionProvider_Encrypt_Decrypt_Image()
        {
            // arrange
            byte[] encryptedData, decryptedData;
            Image decryptedImage;

            var imageData = File.ReadAllBytes(@"..\..\Test Files\Tulips.jpg");
            var aes = new AesEncryptionProvider(new TestCryptoKeyProvider("9781908006028",
                new Guid("F588C946-386F-4A49-A333-64A189D07DD4")));

            // act
            using (var ms = new MemoryStream())
            {
                aes.EncryptToStream(imageData, ms);
                encryptedData = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                aes.DecryptToStream(encryptedData, ms);
                decryptedData = ms.ToArray();
            }

            using (var ms = new MemoryStream(decryptedData))
                decryptedImage = Image.FromStream(ms);
            
            // assert
            Assert.IsNotNull(decryptedImage);
        }
    }
}
