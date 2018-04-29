using System.Collections.Generic;
using System.IO;

using EpubDrm.Drm;

namespace EpubDrm.Epub
{
    public interface IEpub
    {
        string EpubPath { get; }
        string Isbn { get; set; }
        List<string> DrmExemptParts { get; }
        string PackagePath { get; }
        string TocPath { get; }
        string CoverImagePath { get; }
        string CoverPagePath { get; }
        bool HasRights { get; }
        EpubRights Rights { get; }
        bool Encrypted { get; }
        IEnumerable<string> EpubPartsList { get; }

        IDrmPermissionsProvider DrmPermissionsProvider { get; set; }
        IEncryptionProvider EncryptionProvider { get; set; }
        
        Stream GetEpubPart(string partPath);
        bool IsPartEncrypted(string partPath);
        void AddRights();
        void ApplyDrm();
        void RemoveDrm();
    }
}
