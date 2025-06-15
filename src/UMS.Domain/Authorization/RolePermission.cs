namespace UMS.Domain.Authorization
{
    /// <summary>
    /// Join entity representing the many-to-many relationship between Role and Permission.
    /// </summary>
    public class RolePermission
    {
        public byte RoleId { get; set; }

        public short PermissionId { get; set; }
    }
}
