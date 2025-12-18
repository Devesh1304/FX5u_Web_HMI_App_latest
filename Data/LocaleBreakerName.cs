using System.ComponentModel.DataAnnotations;

namespace FX5u_Web_HMI_App.Data
{
    public class LocaleBreakerName
    {
        [Key] public int Id { get; set; }          // 1..20 (breaker index)
        [Required, MaxLength(5)] public string Lang { get; set; } = "gu";
        [Required, MaxLength(20)] public string Text { get; set; } = string.Empty;
    }
}