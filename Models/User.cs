using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models.User
{
    [Table("User")]
    public class User
    {
        [Key]
        [StringLength(50)]
        public string UserID { get; set; }

        [StringLength(50)]
        [Required]
        public string UserCode { get; set; }

        [StringLength(50)]
        [Required]
        public string UserName { get; set; }

        [StringLength(50)]
        [Required]
        public string Password { get; set; }

        [Required]
        public bool IsDel { get; set; }

        public DateTime? CreateTime { get; set; } = DateTime.Now;

        public DateTime? UpdateTime { get; set; }

        [StringLength(50)]
        public string? Memo { get; set; }

        // 导航属性
        public virtual ICollection<UserRole> UserRoles { get; set; }

        // 一个用户创建了多个采购订单
        public virtual ICollection<PurchaseOrder> CreatedPurchaseOrders { get; set; }

        // 一个用户更新了多个采购订单
        public virtual ICollection<PurchaseOrder> UpdatedPurchaseOrders { get; set; }

        // 一个用户创建了多个送货单
        public virtual ICollection<DeliveryNote> CreatedDeliveryNotes { get; set; }

        // 一个用户进行了多次收料
        public virtual ICollection<ReceiveRecord> ReceiveRecords { get; set; }

        // 一个用户更新了多个库存记录
        public virtual ICollection<Inventory> UpdatedInventories { get; set; }
    }
}
