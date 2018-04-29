using System;
using System.Linq;
using EpubDrm.Drm.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EpubDrm.Drm.Tests
{
    [TestClass]
    public class CryptoKeyProviderTests
    {
        [TestMethod]
        public void TestCryptoKeyProvider_GetEncryptionKey()
        {
            // arrange
            var expected = new byte[] // expected key based on ISBN 9781908006028
            {
                220, 148, 29, 49, 218, 92, 250, 33, 
                106, 205, 87, 200, 57, 128, 171, 65, 
                106, 13, 82, 244, 219, 148, 61, 128, 
                175, 8, 194, 233, 210, 97, 227, 82
            };

            var provider = new TestCryptoKeyProvider("9781908006028",
                new Guid("F588C946-386F-4A49-A333-64A189D07DD4"));

            // act
            var actual = provider.GetEncryptionKey();

            // assert
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}
