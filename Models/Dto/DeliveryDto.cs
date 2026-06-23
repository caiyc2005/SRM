namespace backend.Models.Dto
{

    public class DeliveryGetDto
    {
        public string? noteCode { get; set; }
        public string? supplierId { get; set; }
        public bool? status { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 10;
    }
    public class DeliveryDto
    {
        /// <summary>
        /// 采购订单ID（必填）
        /// </summary>
        public string OrderID { get; set; } = string.Empty;

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

        /// <summary>
        /// 送货明细配置（可选）
        /// 若不传，则默认使用采购订单中的 Qty；
        /// 若传入，则按此配置生成送货单（支持部分送货、分批送货及前端覆盖物料信息）
        /// </summary>
        public List<DeliveryDetailQuantity>? DetailQuantities { get; set; }
    }

    public class ConfirmDeliveryDto
    {
        /// <summary>
        /// 采购订单ID（必填）
        /// </summary>
        public string OrderID { get; set; } = string.Empty;

        /// <summary>
        /// 供应商ID（必填）
        /// </summary>
        public string SupplierID { get; set; } = string.Empty;

        /// <summary>
        /// 供应商名称（可选）
        /// </summary>
        public string? SupplierName { get; set; }
    }

    public class DeliveryDetailQuantity
    {
        /// <summary>
        /// 物料编码（必填）
        /// </summary>
        public string MaterialCode { get; set; } = string.Empty;

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