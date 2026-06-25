using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    /// <summary>
    /// 供应商用户关联表 — 一个供应商可关联多个用户（主账号 + 子账号）
    /// </summary>
    [Table("SupplierUser")]
    public class SupplierUser
    {
        [Key]
        [StringLength(50)]
        public string SupplierUserID { get; set; }

        [StringLength(50)]
        [Required]
        public string SupplierID { get; set; }

        [StringLength(50)]
        [Required]
        public string UserID { get; set; }

        /// <summary>
        /// 是否为主账号（一个供应商仅有一个主账号）
        /// </summary>
        public bool IsMainAccount { get; set; }

        public DateTime CreatedAt { get; set; }

        // 导航属性
        [ForeignKey(nameof(SupplierID))]
        public virtual Supplier Supplier { get; set; }

        [ForeignKey(nameof(UserID))]
        public virtual User User { get; set; }
    }
}
