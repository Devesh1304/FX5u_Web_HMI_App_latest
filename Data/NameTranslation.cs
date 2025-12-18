using System.ComponentModel.DataAnnotations;

namespace FX5u_Web_HMI_App.Data
{
    public class NameTranslation
    {
        [Key] public int Id { get; set; }

        // English source text (as it appears from PLC)
        [Required, MaxLength(40)]
        public string En { get; set; } = string.Empty;

        // Gujarati translated text to show
        [Required, MaxLength(40)]
        public string Gu { get; set; } = string.Empty;
    }
}
