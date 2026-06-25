namespace backend.Models.Dto.Supplier
{
    /// <summary>
    /// 创建子账号请求
    /// </summary>
    public class CreateSubAccountDto
    {
        public string SupplierID { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Memo { get; set; }
    }

    /// <summary>
    /// 供应商子账号列表项
    /// </summary>
    public class SupplierUserItemDto
    {
        public string SupplierUserID { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsMainAccount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Memo { get; set; }
    }
}
