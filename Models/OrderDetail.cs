using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("OrderDetail")]
    public class OrderDetail
    {
        [Key]
        [StringLength(50)]
        public string OrderDetailID { get; set; }

        [StringLength(50)]
        [Required]
        public string OrderID { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialCode { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal Qty { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? Amount { get; set; }
        public bool IsConfirm { get; set; } //= false;


        // 外键关联：多个明细属于一个采购订单
        [ForeignKey(nameof(OrderID))]
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        // 外键关联：物料
        [ForeignKey(nameof(MaterialCode))]
        public virtual Material Material { get; set; }
    }
}
