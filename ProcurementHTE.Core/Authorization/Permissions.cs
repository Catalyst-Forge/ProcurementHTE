namespace ProcurementHTE.Core.Authorization
{
    public static class Permissions
    {
        public static class WO
        {
            public const string Read = "WO.Read";
            public const string Create = "WO.Create";
            public const string Edit = "WO.Edit";
            public const string Delete = "WO.Delete";
        }

        public static class Vendor
        {
            public const string Read = "Vendor.Read";
            public const string Create = "Vendor.Create";
            public const string Edit = "Vendor.Edit";
            public const string Delete = "Vendor.Delete";
        }

        public static class Doc
        {
            public const string Read = "Doc.Read";
            public const string Upload = "Doc.Upload";
            public const string Approve = "Doc.Approve";
        }
    }
}
