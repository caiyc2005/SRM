using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("DeliveryNote")]
    public class DeliveryNote
    {
        [Key]
        [StringLength(50)]
        public string NoteID { get; set; }

        [StringLength(50)]
        [Required]
        public string NoteCode { get; set; }

        [StringLength(50)]
        [Required]
        public string OrderID { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierID { get; set; }

        [Required]
        public bool Status { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ExpectedDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DeliveryDate { get; set; }

        [StringLength(50)]
        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }

        [Required]
        public bool IsDel { get; set; }

        // 外键关联：一个送货单属于一个采购订单
        [ForeignKey(nameof(OrderID))]
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        // 外键关联：一个送货单属于一个供应商
        [ForeignKey(nameof(SupplierID))]
        public virtual Supplier Supplier { get; set; }

        // 导航属性：一个送货单有多个明细
        public virtual ICollection<DeliveryDetail> DeliveryDetails { get; set; }

        // 导航属性：一个送货单有多个收料记录
        public virtual ICollection<ReceiveRecord> ReceiveRecords { get; set; }
    }
}
