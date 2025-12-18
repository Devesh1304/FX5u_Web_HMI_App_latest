using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FX5u_Web_HMI_App.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization; // Needed

namespace FX5u_Web_HMI_App.Pages
{
    public class BrakerSelectModel : PageModel
    {
        private readonly ILogger<BrakerSelectModel> _logger;
        private readonly ISLMPService _slmpService;
        private readonly LogDbContext _db;

        public BrakerSelectModel(ILogger<BrakerSelectModel> logger, ISLMPService slmpService, LogDbContext db)
        {
            _logger = logger;
            _slmpService = slmpService;
            _db = db;
        }

        #region Bound props
        [BindProperty] public string ConnectionStatus { get; set; } = "Disconnected";
        [BindProperty] public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty] public string BrakerName1 { get; set; }
        [BindProperty] public string BrakerName2 { get; set; }
        [BindProperty] public string BrakerName3 { get; set; }
        [BindProperty] public string BrakerName4 { get; set; }
        [BindProperty] public string BrakerName5 { get; set; }
        [BindProperty] public string BrakerName6 { get; set; }
        [BindProperty] public string BrakerName7 { get; set; }
        [BindProperty] public string BrakerName8 { get; set; }
        [BindProperty] public string BrakerName9 { get; set; }
        [BindProperty] public string BrakerName10 { get; set; }
        [BindProperty] public string BrakerName11 { get; set; }
        [BindProperty] public string BrakerName12 { get; set; }
        [BindProperty] public string BrakerName13 { get; set; }
        [BindProperty] public string BrakerName14 { get; set; }
        [BindProperty] public string BrakerName15 { get; set; }
        [BindProperty] public string BrakerName16 { get; set; }
        [BindProperty] public string BrakerName17 { get; set; }
        [BindProperty] public string BrakerName18 { get; set; }
        [BindProperty] public string BrakerName19 { get; set; }
        [BindProperty] public string BrakerName20 { get; set; }

        [BindProperty] public int BrakerSelect1 { get; set; }
        [BindProperty] public int BrakerSelect2 { get; set; }
        [BindProperty] public int D1001 { get; set; }
        [BindProperty] public int ButtonStatusBits1_10 { get; set; }
        [BindProperty] public int ButtonStatusBits11_20 { get; set; }
        #endregion

        public async Task OnGet()
        {
            _slmpService.SetHeartbeatValue(22);
        //    await UpdateModelValuesAsync();
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();
            var props = GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p.GetValue(this));
            return new JsonResult(props);
        }

        // ===== EN grid (from PLC D4000..D4190) =====
        public async Task<JsonResult> OnGetReadBreakerGrid()
        {
            try
            {
                var names = await Read20NamesEnAsync();
                return new JsonResult(new { success = true, names });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadBreakerGrid failed");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // ===== GU grid via NameTranslations, fallback to LocaleBreakerNames(gu, Id 1..20) =====
        public async Task<JsonResult> OnGetReadBreakerGridLocalized(string lang)
        {
            try
            {
                // Detect lang if missing
                if (string.IsNullOrEmpty(lang))
                {
                    lang = CultureInfo.CurrentUICulture.Name.StartsWith("gu", StringComparison.OrdinalIgnoreCase) ? "gu" : "en";
                }

                var en = await Read20NamesEnAsync();

                if (!string.Equals(lang, "gu", StringComparison.OrdinalIgnoreCase))
                    return new JsonResult(new { success = true, names = en });

                // Seed missing EN keys
                var toSeed = en.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                if (toSeed.Count > 0)
                {
                    var existing = await _db.NameTranslations
                        .Where(t => toSeed.Contains(t.En))
                        .Select(t => t.En)
                        .ToListAsync();

                    var newOnes = toSeed.Except(existing);
                    foreach (var key in newOnes)
                        _db.NameTranslations.Add(new NameTranslation { En = key, Gu = "" });
                    if (newOnes.Any()) await _db.SaveChangesAsync();
                }

                // Map EN -> GU
                var maps = await _db.NameTranslations
                                    .Where(t => toSeed.Contains(t.En))
                                    .ToListAsync();

                var dict = maps.ToDictionary(t => t.En, t => t.Gu ?? string.Empty, StringComparer.Ordinal);
                var outNames = en.Select(s => dict.TryGetValue(s ?? string.Empty, out var gu) ? gu : string.Empty).ToArray();

                // Positional fallback
                if (outNames.Any(x => string.IsNullOrWhiteSpace(x)))
                {
                    var guFallback = await _db.LocaleBreakerNames
                        .Where(x => x.Lang == "gu" && x.Id >= 1 && x.Id <= 20)
                        .OrderBy(x => x.Id)
                        .Select(x => x.Text)
                        .ToListAsync();

                    while (guFallback.Count < 20) guFallback.Add(string.Empty);

                    for (int i = 0; i < 20; i++)
                        if (string.IsNullOrWhiteSpace(outNames[i]) && !string.IsNullOrWhiteSpace(guFallback[i]))
                            outNames[i] = guFallback[i];
                }

                // Final fallback to English
                for (int i = 0; i < 20; i++)
                    if (string.IsNullOrWhiteSpace(outNames[i])) outNames[i] = en[i] ?? string.Empty;

                return new JsonResult(new { success = true, names = outNames });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadBreakerGridLocalized failed");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public class BreakerSelectionRequest { public int BreakerNumber { get; set; } }

        public async Task<JsonResult> OnPostSelectBreaker([FromBody] BreakerSelectionRequest request)
        {
            if (request.BreakerNumber < 1 || request.BreakerNumber > 20)
                return new JsonResult(new { status = "Error", message = "Invalid breaker number." });

            try
            {
                var d1001 = await _slmpService.ReadInt16Async("D1001");
                if (!d1001.IsSuccess) throw new Exception("Failed to read D1001.");
                short d1001v = d1001.Content;

                if (request.BreakerNumber <= 10)
                {
                    d1001v ^= (1 << 0);
                    await _slmpService.WriteAsync("D4401", (short)request.BreakerNumber);
                }
                else
                {
                    d1001v ^= (1 << 1);
                    await _slmpService.WriteAsync("D4402", (short)(request.BreakerNumber - 10));
                }

                var wr = await _slmpService.WriteAsync("D1001", d1001v);
                if (!wr.IsSuccess) throw new Exception(wr.Message);

                return new JsonResult(new { status = "Success", message = $"Selected Breaker {request.BreakerNumber}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelectBreaker failed");
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        private async Task UpdateModelValuesAsync()
        {
            try
            {
                var namesTask = _slmpService.ReadInt16BlockAsync("D4000", 200);
                var select1Task = _slmpService.ReadInt16Async("D4401");
                var select2Task = _slmpService.ReadInt16Async("D4402");
                var bitsTask = _slmpService.ReadBoolAsync("M201", 20);
                var d1001Task = _slmpService.ReadInt16Async("D1001");

                await Task.WhenAll(namesTask, select1Task, select2Task, bitsTask, d1001Task);

                var namesRes = namesTask.Result;
                if (!namesRes.IsSuccess) throw new Exception("Failed to read data.");

                var data = namesRes.Content;
                for (int i = 0; i < 20; i++)
                {
                    byte[] bytes = new byte[20];
                    for (int w = 0; w < 10; w++)
                    {
                        var wb = BitConverter.GetBytes(data[i * 10 + w]);
                        bytes[w * 2] = wb[0];
                        bytes[w * 2 + 1] = wb[1];
                    }
                    string value = Encoding.ASCII.GetString(bytes).TrimEnd('\0');
                    GetType().GetProperty($"BrakerName{i + 1}")?.SetValue(this, value);
                }

                var bits = bitsTask.Result.Content;
                ButtonStatusBits1_10 = 0;
                ButtonStatusBits11_20 = 0;
                for (int i = 0; i < 20; i++)
                {
                    if (bits[i])
                    {
                        if (i < 10) ButtonStatusBits1_10 |= (1 << i);
                        else ButtonStatusBits11_20 |= (1 << (i - 10));
                    }
                }

                ConnectionStatus = "Connected";
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateModelValuesAsync failed");
                ConnectionStatus = "Error";
                ErrorMessage = ex.Message;
            }
        }

        private async Task<string[]> Read20NamesEnAsync()
        {
            var block = await _slmpService.ReadInt16BlockAsync("D4000", 200);
            if (!block.IsSuccess) throw new Exception(block.Message);

            var names = new string[20];
            for (int i = 0; i < 20; i++)
            {
                byte[] bytes = new byte[20];
                for (int w = 0; w < 10; w++)
                {
                    var wb = BitConverter.GetBytes(block.Content[i * 10 + w]);
                    bytes[w * 2] = wb[0];
                    bytes[w * 2 + 1] = wb[1];
                }
                names[i] = Encoding.ASCII.GetString(bytes).TrimEnd('\0').Trim();
            }
            return names;
        }
    }
}