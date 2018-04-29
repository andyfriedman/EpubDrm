using System.IO;

namespace EpubDrm.Drm
{
    public interface IEncryptionProvider
    {
        byte[] Encrypt(byte[] data);
        void EncryptToStream(byte[] data, Stream stream);
        byte[] Decrypt(byte[] data);
        void DecryptToStream(byte[] data, Stream stream);
    }
}
