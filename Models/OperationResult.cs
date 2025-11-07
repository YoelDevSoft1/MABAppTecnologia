namespace MABAppTecnologia.Models
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public Exception? Exception { get; set; }

        public static OperationResult Ok(string message, string details = "")
        {
            return new OperationResult
            {
                Success = true,
                Message = message,
                Details = details
            };
        }

        public static OperationResult Fail(string message, string details = "", Exception? ex = null)
        {
            return new OperationResult
            {
                Success = false,
                Message = message,
                Details = details,
                Exception = ex
            };
        }
    }
}
