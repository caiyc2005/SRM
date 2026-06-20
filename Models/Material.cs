using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("Material")]
    public class Material
    {
        [Key]
        [StringLength(50)]
        public string MaterialID { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialCode { get; set; }

        [StringLength(50)]
        [Required]
        public string MaterialName { get; set; }

        [StringLength(50)]
        [Required]
        public string Spec { get; set; }

        [StringLength(50)]
        [Required]
        public string Unit { get; set; }

        [Required]
        public bool IsDel { get; set; }

        [StringLength(50)]
        public string? Memo { get; set; }

        // 导航属性：一个物料有多个收料明细
        public virtual ICollection<ReceiveDetail> ReceiveDetails { get; set; }

        // 导航属性：一个物料有多个库存记录
        public virtual ICollection<Inventory> Inventories { get; set; }
    }
}
