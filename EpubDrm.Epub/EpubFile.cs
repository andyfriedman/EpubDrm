using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

using EpubDrm.Drm;

namespace EpubDrm.Epub
{
    /// <summary>
    /// Provides the functionality to open an EPUB3 archive and retrieve the various parts. Also applies or removes
    /// DRM to/from protected content. Using dependency injection, the encryption algorithm (IEncryptionProvider) can 
    /// easily be changed as well as the method for providing the encryption key (ICryptoKeyProvider). For example,
    /// the encryption key might need to be retrieved from a license server. User permissions typically come from
    /// a Rights.xml file, but the IDrmPermissionsProvider interface provides the ability customize where the rights
    /// for a particular ISBN come from.
    /// </summary>
    public class EpubFile : IEpub, IDisposable
    {
        public const string MetaRightsPath = @"META-INF/rights.xml";
        public const string MetaContainerPath = @"META-INF/container.xml";

        private const string IdpfNamespace = "http://www.idpf.org/2007/opf";
        private const string OasisContainerNamespace = "urn:oasis:names:tc:opendocument:xmlns:container";

        private readonly List<string> _drmExemptParts = new List<string>() 
        {
            MetaContainerPath
        };

        private readonly string _epubPath;
        private string _packagePath;
        private string _tocPath;
        private string _coverImagePath;
        private string _coverPagePath;
        private string _oebpsPath;
        private bool _encrypted;
        private bool _hasRights;
        private EpubRights _rights;
        private ZipArchive _archive;
        private bool _disposed;

        public IDrmPermissionsProvider DrmPermissionsProvider { get; set; }
        public IEncryptionProvider EncryptionProvider { get; set; }

        public string EpubPath
        {
            get { return _epubPath; }
        }

        public string Isbn { get; set; }

        // A collection of EPUB parts that are excluded from encryption/decryption. The
        // client can extend this list at runtime with the path(s) to other EPUB part(s).
        public List<string> DrmExemptParts
        {
            get { return _drmExemptParts; }
        }

        public string PackagePath
        {
            get
            {
                if (string.IsNullOrEmpty(_packagePath))
                {
                    var containerXml = GetArchiveEntryString(MetaContainerPath);
                    var container = XDocument.Parse(containerXml);
                    var rootFile = container.Descendants(XName.Get("rootfile", OasisContainerNamespace)).FirstOrDefault();
                    _packagePath = (rootFile != null ? rootFile.Attribute("full-path").Value : null);
                    _oebpsPath = (_packagePath != null ? _packagePath.Split('/').First() + "/" : null);
                }
                return _packagePath;
            }
        }

        public string TocPath
        {
            get
            {
                if (string.IsNullOrEmpty(_tocPath))
                {
                    var packageXml = GetArchiveEntryString(PackagePath);
                    var package = XDocument.Parse(packageXml);
                    var ncx = package.Descendants(XName.Get("item", IdpfNamespace))
                        .ByAttributeValue("id", "ncx").FirstOrDefault();
                    _tocPath = _oebpsPath + (ncx != null ? ncx.Attribute("href").Value : null);
                }
                return _tocPath;
            }
        }

        public string CoverImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(_coverImagePath))
                {
                    var packageXml = GetArchiveEntryString(PackagePath);
                    var package = XDocument.Parse(packageXml);
                    var metaCover = package.Descendants(XName.Get("meta", IdpfNamespace))
                        .ByAttributeValue("name", "cover").FirstOrDefault();
                    var coverImageItemName = (metaCover != null ? metaCover.Attribute("content").Value : null);
                    var coverImageItem = package.Descendants(XName.Get("item", IdpfNamespace))
                        .ByAttributeValue("id", coverImageItemName).FirstOrDefault();
                    _coverImagePath = _oebpsPath + (coverImageItem != null ? coverImageItem.Attribute("href").Value : null);
                }
                return _coverImagePath;
            }
        }

        public string CoverPagePath
        {
            get
            {
                if (string.IsNullOrEmpty(_coverPagePath))
                {
                    var packageXml = GetArchiveEntryString(PackagePath);
                    var package = XDocument.Parse(packageXml);
                    var coverPageRef = package.Descendants(XName.Get("itemref", IdpfNamespace)).FirstOrDefault();
                    var coverPageItemName = (coverPageRef != null ? coverPageRef.Attribute("idref").Value : null);
                    var coverPageItem = package.Descendants(XName.Get("item", IdpfNamespace))
                        .ByAttributeValue("id", coverPageItemName).FirstOrDefault();
                    _coverPagePath = _oebpsPath + (coverPageItem != null ? coverPageItem.Attribute("href").Value : null);
                }
                return _coverPagePath;
            }
        }

        public bool HasRights
        {
            get
            {
                if (!_hasRights)
                    _hasRights = (_archive.GetEntry(MetaRightsPath) != null);
                return _hasRights;
            }
        }

        public EpubRights Rights
        {
            get
            {
                if (!HasRights)
                    return null;

                if (_rights == null)
                {
                    using (var stream = GetEpubPart(MetaRightsPath))
                    using (var reader = new StreamReader(stream))
                        _rights = EpubRights.Parse(reader.ReadToEnd());
                }
                return _rights;
            }
        }

        public bool Encrypted
        {
            get
            {
                if (!_encrypted)
                {
                    try
                    {
                        // TODO: come up with a better way to do this
                        // crude way of detecting encryption - if the rights file fails
                        // xml parsing, assume it's encrypted. if rights file doesn't exist,
                        // assume no encryption.
                        if (HasRights)
                            XDocument.Parse(GetArchiveEntryString(MetaRightsPath));
                    }
                    catch (Exception)
                    {
                        _encrypted = true;
                    }
                }
                return _encrypted;
            }
        }

        public IEnumerable<string> EpubPartsList
        {
            get
            {
                if (_archive != null)
                    return _archive.Entries.Select(x => x.FullName);
                return null;
            }
        }

        public EpubFile(string epubFilePath, bool readOnly = true)
        {
            if (string.IsNullOrEmpty(epubFilePath))
                throw new ArgumentNullException("epubFilePath");
            
            var fileName = Path.GetFileNameWithoutExtension(epubFilePath);

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("epubFilePath");

            // assume the file name is also the ISBN if it's a 13 digit number
            long num;
            if (fileName.Length == 13 && long.TryParse(fileName, out num))
                Isbn = fileName;

            _epubPath = epubFilePath;
            LoadArchive(epubFilePath, readOnly);
        }

        public EpubFile(string isbn, string epubFilePath, bool readOnly = true)
        {
            if (string.IsNullOrEmpty(epubFilePath))
                throw new ArgumentNullException("epubFilePath");

            Isbn = isbn;
            _epubPath = epubFilePath;
            LoadArchive(epubFilePath, readOnly);
        }

        public void AddRights()
        {
            if (DrmPermissionsProvider == null)
                throw new ArgumentNullException("DrmPermissionsProvider");

            if (HasRights)
                return;

            var rightsXml = XDocument.Parse(DrmPermissionsProvider.GetPermissions(Isbn));
            var rightsEntry = _archive.CreateEntry(MetaRightsPath, CompressionLevel.Optimal);
            using (var stream = rightsEntry.Open())
                rightsXml.Save(stream);

            _hasRights = true;
        }

        public void ApplyDrm()
        {
            if (EncryptionProvider == null)
                throw new ArgumentNullException("EncryptionProvider");

            if (Encrypted)
                throw new Exception("EPUB is already encrypted");

            if (!HasRights)
                throw new Exception("No rights specified");

            // encrypt all elements except directory names and exempt files (content.opf, toc.ncx, cover image, etc)
            var entries = _archive.Entries
                .Where(x => !string.IsNullOrEmpty(x.Name) &&
                    !DrmExemptParts.Contains(x.FullName)).ToList();
            
            foreach (var entry in entries)
            {
                var data = GetArchiveEntryBytes(entry.FullName);
                entry.Delete();

                var newEntry = _archive.CreateEntry(entry.FullName);
                using (var stream = newEntry.Open())
                    EncryptionProvider.EncryptToStream(data, stream);
            }

            _encrypted = true;
        }

        public void RemoveDrm()
        {
            if (EncryptionProvider == null)
                throw new ArgumentNullException("EncryptionProvider");

            if (!Encrypted)
                throw new Exception("EPUB is already unencrypted");

            if (!HasRights)
                throw new Exception("No rights specified");

            // decrypt all elements except directory names and exempt files (content.opf, toc.ncx, cover image, etc)
            var entries = _archive.Entries
                .Where(x => !string.IsNullOrEmpty(x.Name) &&
                    !DrmExemptParts.Contains(x.FullName)).ToList();

            foreach (var entry in entries)
            {
                var data = GetArchiveEntryBytes(entry.FullName);
                entry.Delete();

                var newEntry = _archive.CreateEntry(entry.FullName);
                using (var stream = newEntry.Open())
                    EncryptionProvider.DecryptToStream(data, stream);
            }

            _encrypted = false;
        }

        public Stream GetEpubPart(string partPath)
        {
            if (IsPartEncrypted(partPath))
            {
                if (EncryptionProvider == null)
                    throw new ArgumentNullException("EncryptionProvider");

                var part = GetArchiveEntryBytes(partPath);
                if (part == null)
                    throw new Exception(string.Format("Epub part \"{0}\" doesn't exist", partPath));

                var decrypted = EncryptionProvider.Decrypt(part);
                return new MemoryStream(decrypted);
            }
            else
            {
                return GetArchiveEntryStream(partPath);
            }
        }

        public bool IsPartEncrypted(string partPath)
        {
            return Encrypted && !DrmExemptParts.Contains(partPath);
        }

        private void LoadArchive(string epubFilePath, bool readOnly)
        {
            if (string.IsNullOrEmpty(EpubPath))
                throw new ArgumentNullException("epubFilePath");

            _archive = ZipFile.Open(epubFilePath, 
                (readOnly ? ZipArchiveMode.Read : ZipArchiveMode.Update));
        }

        private string GetArchiveEntryString(string archiveEntryPath)
        {
            var entry = _archive.GetEntry(archiveEntryPath);
            if (entry == null)
                return null;

            using (var zipStrm = entry.Open())
            using (var reader = new StreamReader(zipStrm))
                return reader.ReadToEnd();
        }

        private byte[] GetArchiveEntryBytes(string archiveEntryPath)
        {
            var entry = _archive.GetEntry(archiveEntryPath);
            if (entry == null)
                return null;

            using (var zipStrm = entry.Open())
            using (var memStrm = new MemoryStream())
            {
                zipStrm.CopyTo(memStrm);
                return memStrm.ToArray();
            }
        }

        private Stream GetArchiveEntryStream(string archiveEntryPath)
        {
            var entry = _archive.GetEntry(archiveEntryPath);
            if (entry == null)
                return null;

            var memStrm = new MemoryStream();
            using (var zipStrm = entry.Open())
                zipStrm.CopyTo(memStrm);

            memStrm.Position = 0;
            return memStrm;
        }

        #region IDisposeable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_archive != null)
                    _archive.Dispose();
            }
            _disposed = true;
        } 
        #endregion
    }
}
