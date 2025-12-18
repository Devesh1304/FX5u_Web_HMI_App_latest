using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using FX5u_Web_HMI_App.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization; // Needed for CultureInfo

namespace FX5u_Web_HMI_App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ISLMPService _slmpService;
        private readonly LogDbContext _db;

        public IndexModel(ILogger<IndexModel> logger, ISLMPService slmpService, LogDbContext db)
        {
            _logger = logger;
            _slmpService = slmpService;
            _db = db;
        }

        [BindProperty] public float PositionAtEmergency { get; set; }
        [BindProperty] public string ConnectionStatus { get; set; } = "Disconnected";
        [BindProperty] public string ErrorMessage { get; set; } = string.Empty;

        // Registers
        [BindProperty] public short D4 { get; set; }
        [BindProperty] public short D1001 { get; set; }
        [BindProperty] public short D2 { get; set; }
        [BindProperty] public short D3 { get; set; }
        [BindProperty] public short D5 { get; set; }
        [BindProperty] public short D1402 { get; set; }
        [BindProperty] public short D1002 { get; set; }

        // Dynamic Labels (Default / Fallback)
        [BindProperty] public string BreakerTypeName1 { get; set; }
        [BindProperty] public string BreakerTypeName2 { get; set; }
        [BindProperty] public string BreakerTypeName3 { get; set; }
        [BindProperty] public string BreakerTypeName4 { get; set; }

        // PLC Address Constants for Strings (10 words each)
        private const string BTN1_D = "D4410";
        private const string BTN2_D = "D4420";
        private const string BTN3_D = "D4430";
        private const string BTN4_D = "D4440";

        // Mapping for Auto-Read
        private readonly Dictionary<string, string> _intAddressMap = new()
        {
            { nameof(D4), "D4" },
            { nameof(D1001), "D1001" },
            { nameof(D2), "D2" },
            { nameof(D3), "D3" },
            { nameof(D5), "D5" },
            { nameof(D1402), "D1402" },
            { nameof(D1002), "D1002" },
            { nameof(PositionAtEmergency), "D1708" }
        };

        // ========================== HANDLERS ==========================

        public async Task OnGet()
        {
          //   // Index Page ID
        //    await UpdateModelValuesAsync();
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();
            var properties = GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p.GetValue(this));
            return new JsonResult(properties);
        }

        // Helper: read 4 English names from PLC
        private async Task<string[]> ReadBreakerTypesEnAsync()
        {
            var n1 = await _slmpService.ReadStringAsync(BTN1_D, 10);
            var n2 = await _slmpService.ReadStringAsync(BTN2_D, 10);
            var n3 = await _slmpService.ReadStringAsync(BTN3_D, 10);
            var n4 = await _slmpService.ReadStringAsync(BTN4_D, 10);

            return new[]
            {
                (n1.IsSuccess ? n1.Content?.TrimEnd('\0') : "") ?? "",
                (n2.IsSuccess ? n2.Content?.TrimEnd('\0') : "") ?? "",
                (n3.IsSuccess ? n3.Content?.TrimEnd('\0') : "") ?? "",
                (n4.IsSuccess ? n4.Content?.TrimEnd('\0') : "") ?? "",
            };
        }

        // Simple English Fetch
        public async Task<JsonResult> OnGetReadBreakerTypes()
        {
            try
            {
                var names = await ReadBreakerTypesEnAsync();
                return new JsonResult(new { success = true, names });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnGetReadBreakerTypes failed");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Localized Fetch with Database Fallback
        public async Task<JsonResult> OnGetReadBreakerTypesLocalized(string lang)
        {
            try
            {
                // If param is missing, use current culture
                if (string.IsNullOrEmpty(lang))
                {
                    lang = CultureInfo.CurrentUICulture.Name.StartsWith("gu", StringComparison.OrdinalIgnoreCase) ? "gu" : "en";
                }

                var en = await ReadBreakerTypesEnAsync();

                // If not Gujarati, return English immediately
                if (!string.Equals(lang, "gu", StringComparison.OrdinalIgnoreCase))
                    return new JsonResult(new { success = true, names = en });

                // 1. Auto-Seed: Add missing English keys to DB
                foreach (var key in en.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
                {
                    if (!await _db.NameTranslations.AnyAsync(t => t.En == key))
                        _db.NameTranslations.Add(new NameTranslation { En = key, Gu = "" });
                }
                await _db.SaveChangesAsync();

                // 2. Map EN -> GU
                var keys = en.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                var maps = await _db.NameTranslations.Where(t => keys.Contains(t.En)).ToListAsync();
                var dict = maps.ToDictionary(t => t.En, t => t.Gu ?? string.Empty, StringComparer.Ordinal);

                var outNames = en.Select(s => dict.TryGetValue(s ?? string.Empty, out var gu) ? gu : string.Empty).ToArray();

                // 3. Fallback: LocaleBreakerNames (Static ID 1-4)
                if (outNames.Any(x => string.IsNullOrWhiteSpace(x)))
                {
                    var guFallback = await _db.LocaleBreakerNames
                        .Where(x => x.Lang == "gu" && x.Id >= 1 && x.Id <= 4)
                        .OrderBy(x => x.Id)
                        .Select(x => x.Text)
                        .ToListAsync();
                    while (guFallback.Count < 4) guFallback.Add(string.Empty);

                    for (int i = 0; i < 4; i++)
                        if (string.IsNullOrWhiteSpace(outNames[i]) && !string.IsNullOrWhiteSpace(guFallback[i]))
                            outNames[i] = guFallback[i];
                }

                // 4. Final Fallback: English
                for (int i = 0; i < 4; i++)
                    if (string.IsNullOrWhiteSpace(outNames[i])) outNames[i] = en[i] ?? string.Empty;

                return new JsonResult(new { success = true, names = outNames });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnGetReadBreakerTypesLocalized failed");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task UpdateModelValuesAsync()
        {
            try
            {
                _slmpService.SetHeartbeatValue(7);
                foreach (var entry in _intAddressMap)
                {
                    var result = await _slmpService.ReadInt16Async(entry.Value);
                    if (!result.IsSuccess) throw new Exception(result.Message);
                    switch (entry.Key)
                    {
                        case nameof(D4): D4 = result.Content; break;
                        case nameof(D1001): D1001 = result.Content; break;
                        case nameof(D2): D2 = result.Content; break;
                        case nameof(D3): D3 = result.Content; break;
                        case nameof(D5): D5 = result.Content; break;
                        case nameof(D1402): D1402 = result.Content; break;
                        case nameof(D1002): D1002 = result.Content; break;
                    }
                }

                var posResult = await _slmpService.ReadInt32Async("D1708");
                if (!posResult.IsSuccess) throw new Exception(posResult.Message);
                PositionAtEmergency = posResult.Content;

                // Init names for first load (default EN)
                var names = await ReadBreakerTypesEnAsync();
                BreakerTypeName1 = names[0];
                BreakerTypeName2 = names[1];
                BreakerTypeName3 = names[2];
                BreakerTypeName4 = names[3];

                ConnectionStatus = "Connected";
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLMP read failed.");
                ConnectionStatus = "Error";
                ErrorMessage = ex.Message;
            }
        }

        public async Task<JsonResult> OnPostWriteRegister([FromBody] WriteRequest request)
        {
            if (string.IsNullOrEmpty(request?.RegisterName) || string.IsNullOrEmpty(request.Value))
                return new JsonResult(new { status = "Error", message = "Request data is missing." });

            try
            {
                if (_intAddressMap.TryGetValue(request.RegisterName, out string address))
                {
                    if (short.TryParse(request.Value, out short intValue))
                    {
                        var result = await _slmpService.WriteAsync(address, intValue);
                        if (result.IsSuccess)
                            return new JsonResult(new { status = "Success", message = $"Wrote {intValue} to {request.RegisterName}." });
                        throw new Exception(result.Message);
                    }
                    throw new FormatException("Invalid integer.");
                }
                throw new KeyNotFoundException($"Register '{request.RegisterName}' not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Write failed");
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        public async Task<JsonResult> OnPostWriteBit([FromBody] WriteBitRequest request)
        {
            if (string.IsNullOrEmpty(request?.Address))
                return new JsonResult(new { status = "Error", message = "Request data is missing." });
            try
            {
                var result = await _slmpService.WriteBoolAsync(request.Address, request.Value);
                if (result.IsSuccess)
                    return new JsonResult(new { status = "Success", message = $"Wrote {request.Value} to {request.Address}." });
                throw new Exception(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Write bit failed");
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }
    }

    public class WriteRequest
    {
        public string RegisterName { get; set; }
        public string Value { get; set; }
    }

    public class WriteBitRequest
    {
        public string Address { get; set; }
        public bool Value { get; set; }
    }
}