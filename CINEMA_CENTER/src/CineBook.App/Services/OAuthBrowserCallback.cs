using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CineBook.Services;

internal sealed class OAuthBrowserCallback : IDisposable
{
    private readonly HttpListener _listener = new();
    public int Port { get; }

    public OAuthBrowserCallback()
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        Port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        try
        {
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/cinebook-auth/");
            _listener.Start();
        }
        catch
        {
            _listener.Close();
            throw;
        }
    }

    public async Task<string?> ReceiveAsync(Guid expectedFlow, CancellationToken ct)
    {
        while (true)
        {
            var context = await _listener.GetContextAsync().WaitAsync(ct);
            var request = context.Request;
            using var response = context.Response;
            response.Headers["Cache-Control"] = "no-store";
            response.Headers["Referrer-Policy"] = "no-referrer";
            if (request.HttpMethod != "GET" || request.Url?.AbsolutePath != "/cinebook-auth/" ||
                !Guid.TryParseExact(request.QueryString["flow"], "N", out var flow) || flow != expectedFlow)
            {
                response.StatusCode = 404;
                continue;
            }

            var bytes = Encoding.UTF8.GetBytes("Hãy quay lại CineBook để xem kết quả.");
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            try { await response.OutputStream.WriteAsync(bytes, ct); }
            catch (HttpListenerException) { }
            return request.QueryString["code"];
        }
    }

    public void Dispose() => _listener.Close();
}
