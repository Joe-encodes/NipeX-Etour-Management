using System.Text.Json;
using System.Text;
using e_tour_api.Models;

namespace e_tour_api.Middleware
{
    public class RequestValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestValidationMiddleware> _logger;

        public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation for GET requests and file uploads
            if (context.Request.Method == "GET" || 
                context.Request.ContentType?.Contains("multipart/form-data") == true)
            {
                await _next(context);
                return;
            }

            // Read the request body
            var originalBody = context.Request.Body;
            try
            {
                var requestBody = await ReadRequestBodyAsync(context.Request);
                if (!string.IsNullOrEmpty(requestBody))
                {
                    // Validate JSON format
                    try
                    {
                        JsonDocument.Parse(requestBody);
                    }
                    catch (JsonException)
                    {
                        await HandleInvalidJsonResponse(context);
                        return;
                    }

                    // Restore the request body for downstream middleware
                    var requestData = Encoding.UTF8.GetBytes(requestBody);
                    context.Request.Body = new MemoryStream(requestData);
                }

                await _next(context);
            }
            finally
            {
                context.Request.Body = originalBody;
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

        private async Task HandleInvalidJsonResponse(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var response = ServiceResult<object>.Error(
                "Invalid JSON format in request body",
                400
            );

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
} 