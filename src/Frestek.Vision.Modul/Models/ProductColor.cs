using DotNetNuke.ComponentModel.DataAnnotations;

namespace Frestek.Vision.Modul.Models
{
    [TableName("Frestek_ProductColors")]
    [PrimaryKey("ColorID", AutoIncrement = true)]
    [Scope("ModuleId")]
    public class ProductColor
    {
        public int ColorID { get; set; }
        public int ModuleId { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public string HexCode { get; set; }
        public decimal PriceHUF { get; set; }
    }
}