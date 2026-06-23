namespace backend.Models.Dto
{
    public class WarehouseDto
    {
        public string WareID { get; set; }
        public string WareCode { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string? Memo { get; set; }

        public bool IsDel { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class CreateWarehouseDto
    {
        public string WareCode { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string? Memo { get; set; }
    }

    public class UpdateWarehouseDto
    {
        public string WareID { get; set; }
        public string WareCode { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string? Memo { get; set; }
    }

    public class SetWarehouseStatusDto
    {
        public string WareID { get; set; }
        public bool IsEnable { get; set; }
    }
}
