using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EpubDrm.Epub.Tests
{
    [TestClass]
    public class EpubRightsTests
    {
        [TestMethod]
        public void EpubRightsTests_Serialize()
        {
            // arrange
            var rights = new EpubRights
            {
                Audio = new EpubRights.AudioElement {TextToSpeech = true},
                CopyPaste = new EpubRights.CopyPasteElement
                {
                    Copyright = "This is copywrited",
                    HasCopyright = true,
                    Days = 365,
                    Percentage = 100
                },
                DateTime = DateTime.Now.Subtract(TimeSpan.FromDays(365)),
                Devices = new EpubRights.DevicesElement {Count = 5},
                Isbn = "9983341236786",
                Print = new EpubRights.PrintElement
                {
                    Permission = true
                }
            };

            // act
            var xml = rights.ToXml();

            //assert
            Assert.IsNotNull(XDocument.Parse(xml));
        }

        [TestMethod]
        public void EpubRightsTests_Deserialize()
        {
            // arrange
            var xml = File.ReadAllText(@"..\..\Test Files\permissions.xml");

            // act
            var rights = EpubRights.Parse(xml);

            // assert
            Assert.IsNotNull(rights);
            Assert.AreEqual(rights.Devices.Count, 5);
            Assert.AreEqual(rights.Print.Days, 30);
            Assert.IsTrue(rights.Print.Permission);
            Assert.AreEqual(rights.CopyPaste.Percentage, 20);
            Assert.IsTrue(rights.CopyPaste.Permission);
        }

        [TestMethod]
        public void EpubRightsTests_Serialize_Deserialize()
        {
            // arrange
            var rights = new EpubRights
            {
                Audio = new EpubRights.AudioElement { TextToSpeech = true },
                CopyPaste = new EpubRights.CopyPasteElement
                {
                    Copyright = "This is copywrited",
                    HasCopyright = true,
                    Days = 365,
                    Percentage = 0
                },
                DateTime = DateTime.Now.Subtract(TimeSpan.FromDays(365)),
                Devices = new EpubRights.DevicesElement { Count = 5 },
                Isbn = "9983341236786",
                Print = new EpubRights.PrintElement
                {
                    Permission = true
                }
            };

            // act
            var xml = rights.ToXml();
            var newRights = EpubRights.Parse(xml);

            //assert
            Assert.IsNotNull(newRights);
            Assert.IsTrue(newRights.Audio.TextToSpeech);
            Assert.AreEqual(newRights.CopyPaste.Percentage, 0);
            Assert.IsFalse(newRights.CopyPaste.Permission);
            Assert.AreEqual(newRights.Print.Percentage, 100);
            Assert.IsTrue(newRights.Print.Permission);
        }
    }
}
