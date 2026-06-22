namespace backend.Models.Dto
{
    public class MaterialDto
    {
        public string MaterialID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Spec {  get; set; }
        public string Unit { get; set; }

        public string? Memo { get; set; }
    }
}
