namespace e_tour_api.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public object Data { get; set; } = new();
        public int StatusCode { get; set; }
        public string? Message { get; set; }

        public static ServiceResult Ok(object? data = null, string? message = null) => new() 
        { 
            Success = true, 
            Data = data ?? new { },
            StatusCode = 200,
            Message = message
        };

        public static ServiceResult Error(string message, int code, object? details = null) => new() 
        { 
            Success = false, 
            Data = details ?? new { },
            StatusCode = code,
            Message = message
        };
    }

    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; } = default!;
        public int StatusCode { get; set; }
        public string? Message { get; set; }

        public static ServiceResult<T> Ok(T data, string? message = null) => new() 
        { 
            Success = true, 
            Data = data,
            StatusCode = 200,
            Message = message
        };

        public static ServiceResult<T> Error(string message, int code, object? details = null) => new() 
        { 
            Success = false, 
            Data = default!,
            StatusCode = code,
            Message = message
        };
    }
} 