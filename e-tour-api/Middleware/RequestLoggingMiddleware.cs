using System.Diagnostics;
using Microsoft.IO;

namespace e_tour_api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly RecyclableMemoryStreamManager _streamManager;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _streamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var requestBody = await ReadRequestBodyAsync(context.Request);
            var originalBodyStream = context.Response.Body;

            using var responseBody = _streamManager.GetStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var response = await ReadResponseBodyAsync(context.Response);
                await responseBody.CopyToAsync(originalBodyStream);

                _logger.LogInformation(
                    "HTTP {RequestMethod} {RequestPath} completed in {ElapsedMilliseconds}ms with status code {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds,
                    context.Response.StatusCode);

                if (context.Response.StatusCode >= 400)
                {
                    _logger.LogWarning(
                        "Request: {RequestMethod} {RequestPath}\nRequest Body: {RequestBody}\nResponse: {ResponseBody}",
                        context.Request.Method,
                        context.Request.Path,
                        requestBody,
                        response);
                }
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }

        private async Task<string> ReadResponseBodyAsync(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return text;
        }
    }
} 