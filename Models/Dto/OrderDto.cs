namespace backend.Models.Dto
{
    public class OrderIdRequest
    {
        public string orderID { get; set; }
    }

    public class OrderQueryDto
    {
        public string? OrderCode { get; set; }
        public string? SupplierID { get; set; }
        public int? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortField { get; set; }
        public string? SortOrder { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ConfirmOrderDto
    {
        public List<string> OrderDetailIDs { get; set; } = new List<string>();


    }

    /// <summary>
    /// 创建采购订单请求 DTO
    /// </summary>
    public class OrderDto
    {
        /// <summary>供应商ID</summary>
        public string SupplierID { get; set; }

        /// <summary>供应商名称（快照）</summary>
        public string SupplierName { get; set; }

        /// <summary>采购订单备注</summary>
        public string? Memo { get; set; }

        /// <summary>物料明细</summary>
        public List<OrderMaterialItem> Materials { get; set; }
    }

    /// <summary>
    /// 订单物料明细项
    /// </summary>
    public class OrderMaterialItem
    {
        /// <summary>物料ID</summary>
        public string MaterialID { get; set; }

        /// <summary>数量</summary>
        public decimal Qty { get; set; }

        /// <summary>单价（可选）</summary>
        public decimal? UnitPrice { get; set; }
    }
}
