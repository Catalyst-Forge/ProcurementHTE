namespace ProcurementHTE.Core.Authorization
{
    public static class Permissions
    {
        public static class Procurement
        {
            public const string Read = "Procurement.Read";
            public const string Create = "Procurement.Create";
            public const string Edit = "Procurement.Edit";
            public const string Delete = "Procurement.Delete";
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
