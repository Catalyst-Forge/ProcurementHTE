namespace ProcurementHTE.Core.Options
{
    public class ObjectStorageOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public bool UseSSL { get; set; }
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;

        // 👇 inilah nama bucket yang nanti dipakai
        public string Bucket { get; set; } = "procurementhte";

        public string Region { get; set; } = "us-east-1";
        public int PresignExpirySeconds { get; set; } = 1800;
    }
}
