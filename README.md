# EpubDrm

Provides the functionality to open an EPUB3 archive and retrieve the various parts. Also applies or removes DRM to/from protected content. Using dependency injection, the encryption algorithm (IEncryptionProvider) can easily be changed as well as the method for providing the encryption key (ICryptoKeyProvider). For example, the encryption key might need to be retrieved from a license server. User permissions typically come from a Rights.xml file, but the IDrmPermissionsProvider interface provides the ability customize where the rights for a particular ISBN come from.
