namespace backend.Models
{
    // ========== 通用响应 ==========

    /// <summary>
    /// 通用操作响应
    /// </summary>
    public class ApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static ApiResult Ok(string message = "操作成功", object? data = null) =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResult Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
