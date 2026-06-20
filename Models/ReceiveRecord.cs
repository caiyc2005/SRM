using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("ReceiveRecord")]
    public class ReceiveRecord
    {
        [Key]
        [StringLength(50)]
        public string RecordId { get; set; }

        [StringLength(50)]
        [Required]
        public string RecordCode { get; set; }

        [StringLength(50)]
        [Required]
        public string NoteId { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierId { get; set; }

        [StringLength(50)]
        [Required]
        public string Operator { get; set; }

        [Required]
        public DateTime ReceiveDate { get; set; }

        [Required]
        public bool IsDel { get; set; }

        [StringLength(200)]
        public string Memo { get; set; }

        // 外键关联：一个收料记录属于一个送货单
        [ForeignKey(nameof(NoteId))]
        public virtual DeliveryNote DeliveryNote { get; set; }

        // 外键关联：一个收料记录属于一个供应商
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; }

        // 导航属性：一个收料记录有多个明细
        public virtual ICollection<ReceiveDetail> ReceiveDetails { get; set; }
    }
}
