namespace backend.Models.Dto.Supplier
{
    public class UpdateSupplierDto
    {
        public string supplierID { get; set; }

        public string supplierCode { get; set; }

        public string supplierName { get; set; }

        public string people { get; set; }

        public string phoneNumber { get; set; }

        public string? address { get; set; }

        public string? memo { get; set; }
    }
}
