using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// 供应商表
    /// </summary>
    [Table("Supplier")]
    public class Supplier
    {
        [Key]
        [StringLength(50)]
        public string SupplyID { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierCode { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierName { get; set; }

        [StringLength(50)]
        [Required]
        public string People { get; set; }

        [StringLength(50)]
        [Required]
        public string PhoneNumber { get; set; }

        [StringLength(50)]
        [Required]
        public string Address { get; set; }

        [Required]
        public bool IsDel { get; set; }

        [StringLength(50)]
        public string Memo { get; set; }

        // 导航属性
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }
        public virtual ICollection<DeliveryNote> DeliveryNotes { get; set; }
        public virtual ICollection<ReceiveRecord> ReceiveRecords { get; set; }
    }
}
