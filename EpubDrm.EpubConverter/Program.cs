using System;
using System.IO;
using System.Reflection;

using EpubDrm.Drm;
using EpubDrm.Drm.Test;
using EpubDrm.Epub;

using CommandLine;

namespace EpubDrm.EpubConverter
{
    class Options
    {
        [Option('e', "encrypt")]
        public bool Encrypt { get; set; }

        [Option('d', "decrypt")]
        public bool Decrypt { get; set; }

        [Option('f', "file")]
        public string File { get; set; }

        [Option('p', "part")]
        public string Part { get; set; }

        [Option('r', "rights")]
        public string RightsFile { get; set; }
    }

    /// <summary>
    /// Utility to encrypt or decrypt EPUB3 archives, or retrieve and display specific parts from an EPUB3 archive.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            Parser.Default.ParseArguments(args, options);

            Func<string, string, EpubFile> epubCreatorFunc = CreateEpub;
          // use the creatorFunc below to use dynamic loading at runtime with no strong typing (handy
          // for projects with older versions of the .NET runtime that don't support the newer
          // System.IO.Compression library and won't compile otherwise).
          // Func<string, string, dynamic> epubCreatorFunc = CreateEpubFromReflection;

            if (options.Encrypt)
            {
                File.Copy(options.File + ".original", options.File, true);
                EncryptEpub(options.File, options.RightsFile, epubCreatorFunc);
                Console.WriteLine();
                Console.WriteLine(Path.GetFileName(options.File) + " encrypted.");
            }
            else if (options.Decrypt)
            {
                DecryptEpub(options.File, epubCreatorFunc);
                Console.WriteLine();
                Console.WriteLine(Path.GetFileName(options.File) + " decrypted.");
            }
            else if (options.Part != null)
            {
                var content = GetBookPart(options.File, options.Part, epubCreatorFunc);
                Console.WriteLine();
                Console.WriteLine(content);
            }
        }

        static void EncryptEpub(string epubPath, string rightsFile, Func<string, string, EpubFile> epubCreator)
        {
            using (var epub = epubCreator(epubPath, rightsFile))
            {
                epub.AddRights();
                epub.ApplyDrm();
            }
        }

        static void DecryptEpub(string epubPath, Func<string, string, EpubFile> epubCreator)
        {
            using (var epub = epubCreator(epubPath, null))
                epub.RemoveDrm();
        }

        static string GetBookPart(string epubPath, string partPath, Func<string, string, EpubFile> epubCreator)
        {
            using (var epub = epubCreator(epubPath, null))
            using (var stream = epub.GetEpubPart(partPath))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private static EpubFile CreateEpub(string epubPath, string rightsFile = null)
        {
            var epub = new EpubFile(epubPath, false);
            epub.EncryptionProvider = new AesEncryptionProvider(new TestCryptoKeyProvider(epub.Isbn,
                new Guid("F588C946-386F-4A49-A333-64A189D07DD4")));

            if (!string.IsNullOrEmpty(rightsFile))
                epub.DrmPermissionsProvider = new TestPermissionsProvider(rightsFile);

            // add these EPUB parts to the encryption exempt list
            epub.DrmExemptParts.Add(epub.PackagePath);
            epub.DrmExemptParts.Add(epub.TocPath);
            epub.DrmExemptParts.Add(epub.CoverImagePath);
            epub.DrmExemptParts.Add(epub.CoverPagePath);
            return epub;
        }

        private static dynamic CreateEpubFromReflection(string epubPath, string rightsFile = null)
        {
            var epubAssembly = Assembly.LoadFrom("EpubDrm.Epub.dll");
            dynamic epub = Activator.CreateInstance(epubAssembly.GetType("EpubDrm.Epub.EpubFile"), epubPath, false);

            var drmAssembly = Assembly.LoadFrom("EpubDrm.Drm.dll");
            if (!string.IsNullOrEmpty(rightsFile))
                epub.DrmPermissionsProvider = (dynamic)Activator.CreateInstance(drmAssembly.GetType("EpubDrm.Drm.Test.TestPermissionsProvider"), rightsFile);
            
            dynamic cryptoKeyProvider = Activator.CreateInstance(drmAssembly.GetType("EpubDrm.Drm.Test.TestCryptoKeyProvider"), epub.Isbn);
            epub.EncryptionProvider = (dynamic)Activator.CreateInstance(drmAssembly.GetType("EpubDrm.Drm.AesEncryptionProvider"), cryptoKeyProvider);

            // add these EPUB parts to the encryption exempt list
            epub.DrmExemptParts.Add(epub.PackagePath);
            epub.DrmExemptParts.Add(epub.TocPath);
            epub.DrmExemptParts.Add(epub.CoverImagePath);
            epub.DrmExemptParts.Add(epub.CoverPagePath);
            return epub;
        }
    }
}
