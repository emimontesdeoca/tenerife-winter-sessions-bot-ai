namespace ReceiptScanner.Shared.Models
{
    public class ReceiptData
    {
        public string? Store { get; set; }
        public string? Date { get; set; }
        public List<ItemData>? Items { get; set; }
        public decimal? Total { get; set; }
    }
}
