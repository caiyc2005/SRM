namespace backend.Models.Dto
{

    public class DeliveryGetDto
    {
        public string? noteCode { get; set; }

        public string? orderCode { get; set; }
        public string? supplierId { get; set; }

        public string? userID { get; set; }//新增
        public int? status { get; set; }
        public bool? isReceived { get; set; }
        /// <summary>创建时间起始</summary>
        public DateTime? StartTime { get; set; }
        /// <summary>创建时间截止</summary>
        public DateTime? EndTime { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 10;
    }
    public class DeliveryDto
    {
        /// <summary>
        /// 送货明细列表（含订单明细ID + 本次送货数量）
        /// </summary>
        public List<DeliveryItem> Items { get; set; } = new List<DeliveryItem>();

        /// <summary>
        /// 预计到货日期（可选）
        /// </summary>
        public DateTime? ExpectedDate { get; set; }

        /// <summary>
        /// 创建人ID（必填）
        /// </summary>
        public string CreateByID { get; set; } = string.Empty;

        /// <summary>
        /// 创建人姓名（必填）
        /// </summary>
        public string CreateByName { get; set; } = string.Empty;
    }

    public class DeliveryItem
    {
        /// <summary>
        /// 订单明细ID
        /// </summary>
        public string OrderDetailID { get; set; } = string.Empty;

        /// <summary>
        /// 本次送货数量
        /// </summary>
        public decimal DeliveryQty { get; set; }
    }

    public class ConfirmDeliveryDto
    {
        ///// <summary>
        ///// 采购订单ID（必填）
        ///// </summary>
        //public string OrderID { get; set; } = string.Empty;

        ///// <summary>
        ///// 供应商ID（必填）
        ///// </summary>
        //public string SupplierID { get; set; } = string.Empty;

        public string noteID { get; set; }

        /// <summary>
        /// 预计送达时间（可选）
        /// </summary>
        public DateTime? ExpectedDeliveryDate { get; set; }
    }

    public class DeliveryDetailQuantity
    {
        /// <summary>
        /// 订单明细ID（必填，对应 OrderDetailID）
        /// </summary>
        public string OrderDetailID { get; set; } = string.Empty;

        /// <summary>
        /// 物料名称（可选，前端传入时优先使用）
        /// </summary>
        public string? MaterialName { get; set; }

        /// <summary>
        /// 单位（可选，前端传入时优先使用）
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 本次送货数量（必填，且 > 0）
        /// </summary>
        public decimal Quantity { get; set; }
    }
}