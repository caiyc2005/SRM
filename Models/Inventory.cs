using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("Inventory")]
    public class Inventory
    {
        [Key]
        [StringLength(50)]
        public string InventoryId { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialId { get; set; }

        [StringLength(20)]
        [Required]
        public string WareID { get; set; }  // 文档中是 WareID

        [Column(TypeName = "decimal(18,4)")]
        [Required]
        public decimal Qty { get; set; }

        [Required]
        public DateTime LastReceiveTime { get; set; }

        [StringLength(50)]
        public string UpdateBy { get; set; }

        public DateTime? UpdateTime { get; set; }

        // 外键关联：一个库存记录属于一个物料
        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; }

        // 外键关联：一个库存记录属于一个仓库
        [ForeignKey(nameof(WareID))]
        public virtual Warehouse Warehouse { get; set; }
    }
}
