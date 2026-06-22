namespace backend.Models.Dto
{
    public class OrderIdRequest
    {
        public string ID { get; set; }
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
