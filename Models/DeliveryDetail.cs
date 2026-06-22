// backend/Models/DeliveryDetail.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("DeliveryDetail")]
    public class DeliveryDetail
    {
        [Key]
        [StringLength(50)]
        public string DeliveryDetailID { get; set; } = string.Empty;

        [StringLength(50)]
        [Required]
        public string NoteID { get; set; } = string.Empty;

        [StringLength(50)]
        [Required]
        public string MaterialCode { get; set; } = string.Empty;

        [StringLength(200)]
        public string? MaterialName { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal Quantity { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReceivedQty { get; set; } = 0;

        // ✅ 必须存在
        public bool IsDel { get; set; } = false;
        public DateTime? UpdatedTime { get; set; }
        public DateTime? CreatedTime { get; set; }

        [ForeignKey(nameof(NoteID))]
        public virtual DeliveryNote? DeliveryNote { get; set; }

        public virtual ICollection<ReceiveDetail>? ReceiveDetails { get; set; }
    }
}