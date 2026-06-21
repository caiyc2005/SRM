using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("ReceiveRecord")]
    public class ReceiveRecord
    {
        [Key]
        [StringLength(50)]
        public string RecordID { get; set; }
        public string ReceiveID { get; set; }

        [StringLength(50)]
        [Required]
        public string ReceiveCode { get; set; }

        [StringLength(50)]
        [Required]
        public string NoteID { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierID { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierName { get; set; }

        [StringLength(50)]
        [Required]
        public string ReceiveUserID { get; set; }

        [StringLength(50)]
        [Required]
        public string ReceiveUserName { get; set; }

        [Required]
        public DateTime ReceiveDate { get; set; }

        [Required]
        public bool IsDel { get; set; }

        [StringLength(200)]
        public string? Memo { get; set; }

        // 外键关联：一个收料记录属于一个送货单
        [ForeignKey(nameof(NoteID))]
        public virtual DeliveryNote DeliveryNote { get; set; }

        // 外键关联：一个收料记录属于一个供应商
        [ForeignKey(nameof(SupplierID))]
        public virtual Supplier Supplier { get; set; }

        // 外键关联：一个收料记录由一个用户（收料人）操作
        [ForeignKey(nameof(ReceiveUserID))]
        public virtual User ReceiveUser { get; set; }

        // 导航属性：一个收料记录有多个明细
        public virtual ICollection<ReceiveDetail> ReceiveDetails { get; set; }
    }
}
