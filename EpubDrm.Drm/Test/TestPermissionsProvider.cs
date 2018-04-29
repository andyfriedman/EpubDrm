using System.IO;

namespace EpubDrm.Drm.Test
{
    public class TestPermissionsProvider : IDrmPermissionsProvider
    {
        private string _rightsFile;

        public TestPermissionsProvider(string rightsFile)
        {
            _rightsFile = rightsFile;
        }
        
        public string GetPermissions(string isbn)
        {
            return File.ReadAllText(_rightsFile);
        }
    }
}
