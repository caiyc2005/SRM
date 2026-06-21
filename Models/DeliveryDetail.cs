using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("DeliveryDetail")]
    public class DeliveryDetail
    {
        [Key]
        [StringLength(50)]
        public string DeliveryDetailID { get; set; }

        [StringLength(50)]
        [Required]
        public string NoteID { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialCode { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialName { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal Quantity { get; set; }

        [StringLength(20)]
        [Required]
        public string Unit { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal ReceivedQty { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? Amount { get; set; }

        [Required]
        public bool IsDel { get; set; }

        // 外键关联：多个明细属于一个送货单
        [ForeignKey(nameof(NoteID))]
        public virtual DeliveryNote DeliveryNote { get; set; }

        // 导航属性：一个送货明细有多个收料明细
        public virtual ICollection<ReceiveDetail> ReceiveDetails { get; set; }
    }
}
