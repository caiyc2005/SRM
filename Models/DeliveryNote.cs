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
        public string SupplierID { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierName { get; set; }

        [Required]
        public bool Status { get; set; }

        public DateTime? ExpectedDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [StringLength(50)]
        [Required]
        public string CreateByID { get; set; }

        [StringLength(50)]
        [Required]
        public string CreateByName { get; set; }

        public DateTime CreatedTime { get; set; }

        public DateTime? UpdatedTime { get; set; }

        [Required]
        public bool IsDel { get; set; }

        // 一个送货单可能包含多个采购订单的物料，订单关联通过 DeliveryDetail.OrderDetailID 追溯
        // 外键关联：一个送货单属于一个供应商
        [ForeignKey(nameof(SupplierID))]
        public virtual Supplier Supplier { get; set; }

        // 外键关联：一个送货单由一个用户创建
        [ForeignKey(nameof(CreateByID))]
        public virtual User CreateByUser { get; set; }

        // 导航属性：一个送货单有多个明细
        public virtual ICollection<DeliveryDetail> DeliveryDetails { get; set; }

        // 导航属性：一个送货单有多个收料记录
        public virtual ICollection<ReceiveRecord> ReceiveRecords { get; set; }
    }
}
