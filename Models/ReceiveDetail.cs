using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("ReceiveDetail")]
    public class ReceiveDetail
    {
        [Key]
        [StringLength(50)]
        public string ReceiveDetailID { get; set; }

        [StringLength(50)]
        [Required]
        public string ReceiveId { get; set; }

        [StringLength(50)]
        [Required]
        public string DeliveryDetailId { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialId { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialCode { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal PlanQty { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal ReceivedQty { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal DiffQty { get; set; }

        [StringLength(50)]
        public string CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        [Required]
        public bool IsDel { get; set; }

        // 外键关联：多个明细属于一个收料记录
        [ForeignKey(nameof(ReceiveId))]
        public virtual ReceiveRecord ReceiveRecord { get; set; }

        // 外键关联：多个明细属于一个送货明细
        [ForeignKey(nameof(DeliveryDetailId))]
        public virtual DeliveryDetail DeliveryDetail { get; set; }

        // 外键关联：多个明细属于一个物料
        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; }
    }
}
