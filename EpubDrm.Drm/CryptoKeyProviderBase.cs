namespace EpubDrm.Drm
{
    public abstract class CryptoKeyProviderBase : ICryptoKeyProvider
    {
        public string Salt 
        {
            get { return "yYgfIrWeaFRNSsDdgYKbTg==" /* "zIk1Y2ak/JfL+v2B+L4GlQ=="*/; }
        }
        public abstract byte[] GetEncryptionKey();
        public abstract byte[] GetInitializationVector();
    }
}
