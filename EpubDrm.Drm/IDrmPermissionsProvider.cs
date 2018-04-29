namespace EpubDrm.Drm
{
    public interface IDrmPermissionsProvider
    {
        string GetPermissions(string isbn);
    }
}
