public sealed class ObjectStorageOptions
{
    public string Endpoint { get; set; } = default!;
    public bool UseSSL { get; set; }
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Bucket { get; set; } = default!;
    public string Region { get; set; } = "us-east-1";
    public int PresignExpirySeconds { get; set; } = 1800;
}
