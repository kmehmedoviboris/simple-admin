using System.Net;
using System.Net.Http.Headers;

namespace SimpleAdmin.Tests.Helpers;

/// <summary>
/// A delegating handler that tracks Set-Cookie headers and injects Cookie headers into requests.
/// Use this with WebApplicationFactory.Server.CreateHandler() to get proper cookie handling.
/// </summary>
public class CookieTrackingHandler : DelegatingHandler
{
    private readonly Dictionary<string, string> _cookies = new(StringComparer.OrdinalIgnoreCase);

    public CookieTrackingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    public List<string> Log { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Inject stored cookies into the request
        if (_cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", _cookies.Select(kv => $"{kv.Key}={kv.Value}"));
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            var cookieNames = string.Join(", ", _cookies.Keys);
            Log.Add($"SEND {request.RequestUri?.PathAndQuery}: Sending {_cookies.Count} cookies: [{cookieNames}]");
        }
        else
        {
            Log.Add($"SEND {request.RequestUri?.PathAndQuery}: (no cookies)");
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Collect Set-Cookie headers from the response
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                // Parse "name=value; Path=...; ..." format
                var parts = setCookie.Split(';');
                var nameValue = parts[0].Trim();
                var eqIdx = nameValue.IndexOf('=');
                if (eqIdx > 0)
                {
                    var name = nameValue[..eqIdx].Trim();
                    var value = nameValue[(eqIdx + 1)..].Trim();
                    _cookies[name] = value;
                    Log.Add($"RECV Set-Cookie: {name}=... ({value.Length} chars)");
                }
            }
        }

        return response;
    }
}
