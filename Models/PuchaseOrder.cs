using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("PurchaseOrder")]
    public class PurchaseOrder
    {
        [Key]
        [StringLength(50)]
        public string OrderID { get; set; }

        [StringLength(50)]
        [Required]
        public string OrderCode { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierId { get; set; }

        [Required]
        public int Status { get; set; }

        [StringLength(50)]
        public string CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }

        [Required]
        public bool IsDel { get; set; }

        [StringLength(50)]
        public string? Memo { get; set; }

        // 外键关联：一个采购订单属于一个供应商
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; }

        // 导航属性：一个采购订单有多个明细
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        // 导航属性：一个采购订单有多个送货单
        public virtual ICollection<DeliveryNote> DeliveryNotes { get; set; }
    }
}
