using System.Net;
using System.Security.Cryptography;
using System.Text;

// S3-compatible provider using raw HTTP + AWS Signature Version 4. No third-party packages.
// Connection string format (semicolon-separated key=value):
//   endpoint=play.min.io;accesskey=KEY;secretkey=SECRET;secure=true;region=us-east-1

namespace FileUpload.Providers;

public class S3MinioProvider : IFileProvider
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _bucket;
    private readonly string _region;
    private readonly Uri _baseUri;
    private readonly HttpClient _http;
    private bool _bucketEnsured;
    private bool _disposed;

    public S3MinioProvider(string connectionString, string bucket)
    {
        var cfg = ParseConnectionString(connectionString);
        cfg.TryGetValue("endpoint", out var endpoint);
        cfg.TryGetValue("accesskey", out var accessKey);
        cfg.TryGetValue("secretkey", out var secretKey);
        cfg.TryGetValue("region", out var region);

        bool secure = true;
        if (cfg.TryGetValue("secure", out var secureStr) && bool.TryParse(secureStr, out var s))
            secure = s;

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Invalid S3 connection string. Requires endpoint, accesskey, and secretkey.");

        _accessKey = accessKey!;
        _secretKey = secretKey!;
        _bucket = bucket;
        _region = string.IsNullOrWhiteSpace(region) ? "us-east-1" : region!;
        _baseUri = new Uri($"{(secure ? "https" : "http")}://{endpoint!.TrimEnd('/')}");
        _http = new HttpClient();
    }

    public async Task DownloadFileAsync(string blobName, Stream output)
    {
        await EnsureBucketAsync();

        var request = BuildSignedRequest(HttpMethod.Get, blobName, bodyBytes: null);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await response.Content.CopyToAsync(output);
    }

    public async Task UploadFileAsync(string blobName, Stream input)
    {
        await EnsureBucketAsync();

        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        var bodyBytes = buffer.ToArray();

        var request = BuildSignedRequest(HttpMethod.Put, blobName, bodyBytes);
        request.Content = new ByteArrayContent(bodyBytes);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task EnsureBucketAsync()
    {
        if (_bucketEnsured) return;

        var headRequest = BuildSignedRequest(HttpMethod.Head, key: null, bodyBytes: null);
        var headResponse = await _http.SendAsync(headRequest);

        if (headResponse.StatusCode == HttpStatusCode.NotFound)
        {
            var createRequest = BuildSignedRequest(HttpMethod.Put, key: null, bodyBytes: null);
            createRequest.Content = new ByteArrayContent(Array.Empty<byte>());
            var createResponse = await _http.SendAsync(createRequest);
            createResponse.EnsureSuccessStatusCode();
        }
        else if (headResponse.StatusCode != HttpStatusCode.OK && headResponse.StatusCode != HttpStatusCode.Forbidden)
        {
            headResponse.EnsureSuccessStatusCode();
        }

        _bucketEnsured = true;
    }

    private HttpRequestMessage BuildSignedRequest(HttpMethod method, string? key, byte[]? bodyBytes)
    {
        var now = DateTime.UtcNow;
        var path = BuildPath(key);
        var uriStr = _baseUri.AbsoluteUri.TrimEnd('/') + path;
        var uri = new Uri(uriStr);

        var payloadHash = bodyBytes is { Length: > 0 }
            ? HexEncode(SHA256.HashData(bodyBytes))
            : "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // SHA256("")

        var host = uri.Authority;
        var dateTime = now.ToString("yyyyMMddTHHmmssZ");
        var date = now.ToString("yyyyMMdd");

        var canonicalHeaders =
            $"host:{host}\n" +
            $"x-amz-content-sha256:{payloadHash}\n" +
            $"x-amz-date:{dateTime}\n";
        const string signedHeaders = "host;x-amz-content-sha256;x-amz-date";

        var canonicalRequest = string.Join("\n",
            method.Method,
            path,
            string.Empty, // query string
            canonicalHeaders,
            signedHeaders,
            payloadHash);

        var credentialScope = $"{date}/{_region}/s3/aws4_request";
        var stringToSign = string.Join("\n",
            "AWS4-HMAC-SHA256",
            dateTime,
            credentialScope,
            HexEncode(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));

        var signingKey = DeriveSigningKey(date, _region, "s3");
        var signature = HexEncode(HmacSha256(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        var request = new HttpRequestMessage(method, uri);
        request.Headers.TryAddWithoutValidation("x-amz-date", dateTime);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.TryAddWithoutValidation("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_accessKey}/{credentialScope},SignedHeaders={signedHeaders},Signature={signature}");

        return request;
    }

    private string BuildPath(string? key)
    {
        if (key == null) return $"/{_bucket}";
        // Encode each segment individually so '/' within the key is preserved as a path separator
        var encodedKey = string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
        return $"/{_bucket}/{encodedKey}";
    }

    private byte[] DeriveSigningKey(string date, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{_secretKey}"), Encoding.UTF8.GetBytes(date));
        var kRegion = HmacSha256(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HmacSha256(kRegion, Encoding.UTF8.GetBytes(service));
        return HmacSha256(kService, Encoding.UTF8.GetBytes("aws4_request"));
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
        => HMACSHA256.HashData(key, data);

    private static string HexEncode(byte[] bytes)
        => Convert.ToHexString(bytes).ToLower();

    private static Dictionary<string, string> ParseConnectionString(string cs)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(cs)) return dict;
        foreach (var part in cs.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            dict[part[..idx].Trim()] = part[(idx + 1)..].Trim();
        }
        return dict;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _http.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
