namespace backend.Models.Dto
{
    public class ReceiveGetDto
    {
        public string? ReceiveCode { get; set; }
        public string? NoteCode { get; set; }
        public string? SupplierId { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 10;
    }

    public class ReceiveCreateDto
    {
        public string NoteCode { get; set; } = string.Empty;

        public string ReceiveUserID { get; set; } = string.Empty;

        public string ReceiveUserName { get; set; } = string.Empty;

        public string? Memo { get; set; }

        public List<ReceiveDetailInput>? Details { get; set; }
    }

    public class ReceiveDetailInput
    {
        public string MaterialCode { get; set; } = string.Empty;

        public decimal ReceivedQty { get; set; }
    }
}
