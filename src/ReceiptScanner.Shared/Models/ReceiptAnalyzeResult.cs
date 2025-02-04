namespace ReceiptScanner.Shared.Models
{
    public class ReceiptAnalyzeResult
    {
        public DateTime CreatedAt { get; set; }
        public ReceiptData? Result { get; set; }
    }
}
