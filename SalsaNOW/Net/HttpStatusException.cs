using System.Net;
using System.Net.Http;

namespace SalsaNOW.Net;

internal sealed class HttpStatusException(HttpStatusCode statusCode, string? reason)
    : HttpRequestException($"The server responded with {(int)statusCode}{(string.IsNullOrEmpty(reason) ? "" : " " + reason)}.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public bool IsTransient => (int)StatusCode >= 500 || StatusCode == HttpStatusCode.RequestTimeout || (int)StatusCode == 429;
}
