namespace RaceDay.Web.Services
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }

        public static ApiResponse<T> Ok(int statusCode, T? data)
        {
            return new ApiResponse<T> { Success = true, StatusCode = statusCode, Data = data };
        }

        public static ApiResponse<T> Fail(int statusCode, string error)
        {
            return new ApiResponse<T> { Success = false, StatusCode = statusCode, Error = error };
        }
    }
}
