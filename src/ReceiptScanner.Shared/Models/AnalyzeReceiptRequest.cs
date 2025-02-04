namespace ReceiptScanner.Shared.Models
{
    public class AnalyzeReceiptRequest
    {
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    }
}
