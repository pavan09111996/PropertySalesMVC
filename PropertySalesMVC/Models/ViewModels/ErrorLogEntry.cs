namespace PropertySalesMVC.Models
{
    public class ErrorLogEntry
    {
        public int Id { get; set; }
        public string Severity { get; set; } = "";
        public string Message { get; set; } = "";
        public string? ExceptionType { get; set; }
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public string? RequestPath { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
