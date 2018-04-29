using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace EpubDrm.Epub
{
    [XmlRoot("DRMPermissions")]
    public class EpubRights
    {
        public enum PreviewType
        {
            WordCount,
            Percentage
        }

        public class AudioElement
        {
            [XmlAttribute("ttsread")]
            public bool TextToSpeech { get; set; }
        }

        public class DevicesElement
        {
            [XmlAttribute("count")]
            public int Count { get; set; }
        }

        public class CopyPasteElement
        {
            private ushort _percentage;
            private bool _permission;

            [XmlAttribute("days")]
            public int Days { get; set; }

            [XmlAttribute("percentage")]
            public ushort Percentage
            {
                get { return (_percentage > 0 ? _percentage : (_permission ? (ushort)100 : (ushort)0)); }
                set
                {
                    _percentage = value;
                    if (_percentage > 0)
                        _permission = true;
                }
            }

            [XmlAttribute("permission")]
            public bool Permission
            {
                get { return (_permission || _percentage > 0); }
                set
                {
                    _permission = value;
                    if (_permission && _percentage == 0)
                        _percentage = 100;
                }
            }

            [XmlAttribute("watermark")]
            public bool Watermark { get; set; }

            [XmlAttribute("copyright")]
            public bool HasCopyright { get; set; }

            [XmlAttribute("notice")]
            public string Copyright { get; set; }
        }

        public class PrintElement
        {
            private ushort _percentage;
            private bool _permission;

            [XmlAttribute("days")]
            public int Days { get; set; }

            [XmlAttribute("percentage")]
            public ushort Percentage
            {
                get { return (_percentage > 0 ? _percentage : (_permission ? (ushort)100 : (ushort)0)); }
                set
                {
                    _percentage = value;
                    if (_percentage > 0)
                        _permission = true;
                }
            }

            [XmlAttribute("permission")]
            public bool Permission
            {
                get { return (_permission || _percentage > 0); }
                set
                {
                    _permission = value;
                    if (_permission && _percentage == 0)
                        _percentage = 100;
                }
            }

            [XmlAttribute("watermark")]
            public bool Watermark { get; set; }

            [XmlAttribute("copyright")]
            public bool HasCopyright { get; set; }

            [XmlAttribute("notice")]
            public string Copyright { get; set; }
        }

        public class ReflowElement
        {
            [XmlAttribute("permission")]
            public bool Permission { get; set; }

            [XmlAttribute("smallscreen")]
            public bool SmallScreen { get; set; }
        }

        public class PreviewElement
        {
            [XmlAttribute("measurement")]
            public string Measurement
            {
                get { return null; }
                set { PreviewType = (value == "percentage" ? PreviewType.Percentage : PreviewType.WordCount); }
            }

            [XmlAttribute("quantity")]
            public int Quantity { get; set; }

            public PreviewType PreviewType { get; set; }
        }

        private string _isbn;

        [XmlAttribute("Status")]
        public int Status { get; set; }

        [XmlAttribute("DateTime")]
        public DateTime DateTime { get; set; }

        [XmlAttribute("ISBN")]
        public string Isbn
        {
            get { return _isbn; }
            set { _isbn = value.TrimStart('{').TrimEnd('}'); }
        }

        [XmlAttribute("Format")]
        public string Format { get; set; }

        [XmlAttribute("Market")]
        public string Market { get; set; }

        [XmlAttribute("ESupplier")]
        public string ESupplier { get; set; }

        [XmlElement("devices")]
        public DevicesElement Devices { get; set; }

        [XmlElement("copy")]
        public CopyPasteElement CopyPaste { get; set; }

        [XmlElement("print")]
        public PrintElement Print { get; set; }

        [XmlElement("audio")]
        public AudioElement Audio { get; set; }

        [XmlElement("reflow")]
        public ReflowElement Reflow { get; set; }

        [XmlElement("preview")]
        public PreviewElement Preview { get; set; }

        public static EpubRights Parse(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(EpubRights));
                var reader = new StringReader(xml);
                return serializer.Deserialize(reader) as EpubRights;
            }
            catch (Exception ex)
            {
                throw new Exception("Error deserializing XML", ex);
            }
        }

        public string ToXml()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(EpubRights));
                using (var ms = new MemoryStream())
                {
                    serializer.Serialize(ms, this);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error serializing to XML", ex);
            }
        }
    }
}
