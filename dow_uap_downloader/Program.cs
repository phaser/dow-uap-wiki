using System.Net;
using System.Net.Security;
using System.Text.RegularExpressions;

// Usage: dotnet run -- [concurrency] [outputDir]
//   concurrency: max parallel downloads (default 4, clamped to 1..5)
//   outputDir:   where to save PDFs        (default ../raw)

const string ManifestUrl = "https://www.war.gov/Portals/1/Interactive/2026/UFO/uap-csv.csv";
const string Referer     = "https://www.war.gov/UFO/";
const string UserAgent   = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

int concurrency = args.Length >= 1 && int.TryParse(args[0], out var c) ? c : 4;
concurrency = Math.Clamp(concurrency, 1, 5);
string outputDir = args.Length >= 2 ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "raw");
outputDir = Path.GetFullPath(outputDir);
Directory.CreateDirectory(outputDir);

var http = BuildHttpClient();

Console.WriteLine($"Fetching manifest: {ManifestUrl}");
string csv = await GetStringAsync(http, ManifestUrl, asDocument: false);
Console.WriteLine($"Manifest size: {csv.Length:N0} bytes");

var urls = Regex.Matches(csv, @"https://www\.war\.gov/medialink/ufo/[^\s"",]+?\.pdf", RegexOptions.IgnoreCase)
    .Select(m => m.Value)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine($"Discovered {urls.Count} unique PDF URLs");
Console.WriteLine($"Output: {outputDir}");
Console.WriteLine($"Concurrency: {concurrency}");
Console.WriteLine();

var gate = new SemaphoreSlim(concurrency);
int done = 0, skipped = 0, failed = 0;
var jitter = new Random();
var startedAt = DateTime.UtcNow;

var tasks = urls.Select(async url =>
{
    string fileName = Path.GetFileName(new Uri(url).LocalPath);
    string dest = Path.Combine(outputDir, fileName);

    if (File.Exists(dest) && new FileInfo(dest).Length > 0)
    {
        Interlocked.Increment(ref skipped);
        Log($"skip      {fileName}  (already on disk)");
        return;
    }

    await gate.WaitAsync();
    try
    {
        await Task.Delay(jitter.Next(150, 500));
        var (ok, bytes, err) = await DownloadWithRetry(http, url, dest, maxAttempts: 3);
        if (ok)
        {
            Interlocked.Increment(ref done);
            Log($"ok        {fileName}  ({Human(bytes)})");
        }
        else
        {
            Interlocked.Increment(ref failed);
            Log($"FAIL      {fileName}  -- {err}");
        }
    }
    finally
    {
        gate.Release();
    }
}).ToArray();

await Task.WhenAll(tasks);

var elapsed = DateTime.UtcNow - startedAt;
Console.WriteLine();
Console.WriteLine($"Done in {elapsed.TotalSeconds:F1}s — downloaded:{done}  skipped:{skipped}  failed:{failed}  total:{urls.Count}");
return failed == 0 ? 0 : 1;


static HttpClient BuildHttpClient()
{
    var handler = new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectTimeout = TimeSpan.FromSeconds(30),
        SslOptions = new SslClientAuthenticationOptions
        {
            ApplicationProtocols = new() { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 },
        },
    };
    return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };
}

static HttpRequestMessage BuildRequest(string url, bool asDocument)
{
    var req = new HttpRequestMessage(HttpMethod.Get, url)
    {
        Version = HttpVersion.Version20,
        VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
    };
    req.Headers.UserAgent.ParseAdd(UserAgent);
    req.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    req.Headers.Referrer = new Uri(Referer);
    req.Headers.TryAddWithoutValidation("Accept",
        asDocument
            ? "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"
            : "*/*");
    req.Headers.TryAddWithoutValidation("Sec-Ch-Ua", "\"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
    req.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", "?0");
    req.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Platform", "\"macOS\"");
    req.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", asDocument ? "document" : "empty");
    req.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", asDocument ? "navigate" : "cors");
    req.Headers.TryAddWithoutValidation("Sec-Fetch-Site", asDocument ? "none" : "same-origin");
    req.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
    return req;
}

static async Task<string> GetStringAsync(HttpClient http, string url, bool asDocument)
{
    using var req = BuildRequest(url, asDocument);
    using var resp = await http.SendAsync(req);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadAsStringAsync();
}

static async Task<(bool ok, long bytes, string? error)> DownloadWithRetry(HttpClient http, string url, string dest, int maxAttempts)
{
    string tmp = dest + ".part";
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var req = BuildRequest(url, asDocument: false);
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
            {
                if (attempt < maxAttempts && IsTransient(resp.StatusCode))
                {
                    await Task.Delay(BackoffMs(attempt));
                    continue;
                }
                return (false, 0, $"HTTP {(int)resp.StatusCode} {resp.StatusCode}");
            }
            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            await using (var src = await resp.Content.ReadAsStreamAsync())
            {
                await src.CopyToAsync(fs);
            }
            var len = new FileInfo(tmp).Length;
            File.Move(tmp, dest, overwrite: true);
            return (true, len, null);
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            await Task.Delay(BackoffMs(attempt));
            _ = ex;
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            return (false, 0, ex.Message);
        }
    }
    return (false, 0, "exhausted retries");
}

static bool IsTransient(HttpStatusCode s) =>
    s == HttpStatusCode.RequestTimeout
    || s == HttpStatusCode.TooManyRequests
    || (int)s >= 500;

static int BackoffMs(int attempt) => 1000 * (int)Math.Pow(2, attempt - 1);

static string Human(long bytes)
{
    string[] u = { "B", "KB", "MB", "GB" };
    double v = bytes; int i = 0;
    while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
    return $"{v:F1} {u[i]}";
}

static void Log(string msg)
{
    var ts = DateTime.Now.ToString("HH:mm:ss");
    Console.WriteLine($"[{ts}] {msg}");
}
