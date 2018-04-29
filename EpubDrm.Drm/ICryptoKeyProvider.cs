namespace EpubDrm.Drm
{
    public interface ICryptoKeyProvider
    {
        string Salt { get; }
        byte[] GetEncryptionKey();
        byte[] GetInitializationVector();
    }
}
