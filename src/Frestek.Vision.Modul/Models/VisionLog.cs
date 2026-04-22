using System;
using DotNetNuke.ComponentModel.DataAnnotations;

namespace Frestek.Vision.Modul.Models
{
    [TableName("Frestek_VisionLog")]
    [PrimaryKey("LogID", AutoIncrement = true)]
    [Scope("ModuleId")]
    public class VisionLog
    {
        public int LogID { get; set; }
        public int ModuleId { get; set; }
        public string DetectedHex { get; set; }
        public int RecommendedColorID { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}