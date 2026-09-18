namespace DataGate.Permissions;

/// <summary>
/// ABP permission names. The tier a user can sign at is enforced by permission,
/// not by a string in a workflow - so authority is centrally administered.
/// </summary>
public static class DataGatePermissions
{
    public const string GroupName = "DataGate";

    public static class Promotions
    {
        public const string Default = GroupName + ".Promotions";
        public const string Evaluate = Default + ".Evaluate";
        public const string ApproveAsSteward = Default + ".Approve.Steward";
        public const string ApproveAsOwner = Default + ".Approve.Owner";
        public const string ApproveAsDirector = Default + ".Approve.Director";
        public const string ApproveAsCdo = Default + ".Approve.Cdo";
    }
}
