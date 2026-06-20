using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("Warehouse")]
    public class Warehouse
    {
        [Key]
        [StringLength(50)]
        public string WareID { get; set; }  // 文档中是 WareID

        [StringLength(50)]
        [Required]
        public string WareCode { get; set; }

        [StringLength(50)]
        [Required]
        public string Name { get; set; }

        [StringLength(50)]
        [Required]
        public string Address { get; set; }

        [Required]
        public DateTime CreateTime { get; set; }

        [Required]
        public bool IsDel { get; set; }

        [StringLength(200)]
        public string Memo { get; set; }

        // 导航属性：一个仓库有多个库存记录
        public virtual ICollection<Inventory> Inventories { get; set; }
    }
}
