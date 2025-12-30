using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FX5u_Web_HMI_App.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Globalization; // Needed for CultureInfo

namespace FX5u_Web_HMI_App.Pages
{
    public class MainScreenModel : PageModel
    {
        private readonly ILogger<MainScreenModel> _logger;
        private readonly ISLMPService _slmpService;
        private readonly LogDbContext _db;

        [BindProperty] public int MaxForwardTorque { get; set; }
        [BindProperty] public int MaxReverseTorque { get; set; }
        [BindProperty] public int ServoError { get; set; }
        [BindProperty] public int ContOffTime { get; set; }
        [BindProperty] public int InstantTorque { get; set; }
        [BindProperty] public int TotalTravelLength { get; set; }
        [BindProperty] public int CurrentPosition { get; set; }
        [BindProperty] public int PositionAtEmergency { get; set; }
        [BindProperty] public int RackingInValue { get; set; }
        [BindProperty] public int RackingOutValue { get; set; }

        [BindProperty] public int D1001 { get; set; }
        [BindProperty] public int D1002 { get; set; }
        [BindProperty] public int D100 { get; set; }
        [BindProperty] public int D2 { get; set; }
        [BindProperty] public int D3 { get; set; }
        [BindProperty] public int D4 { get; set; }
        [BindProperty] public int D1402 { get; set; }
        [BindProperty] public int D90 { get; set; }

        [BindProperty] public string BreakerTypeName1 { get; set; }
        [BindProperty] public string BreakerTypeName2 { get; set; }
        [BindProperty] public string BreakerTypeName3 { get; set; }
        [BindProperty] public string BreakerTypeName4 { get; set; }

        [BindProperty] public string ConnectionStatus { get; set; } = "Disconnected";
        [BindProperty] public string ErrorMessage { get; set; } = string.Empty;

        public MainScreenModel(ILogger<MainScreenModel> logger, ISLMPService slmpService, LogDbContext db)
        {
            _logger = logger;
            _slmpService = slmpService;
            _db = db;
        }

        public async Task OnGet()
        {
           // 
           // await UpdateModelValuesAsync();
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();
            var props = GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p.GetValue(this));
            return new JsonResult(props);
        }

        // ============= Breaker names (EN direct from PLC) =============
        public async Task<JsonResult> OnGetReadBreakerTypes()
        {
            try
            {
                var names = await ReadBreakerTypesEnAsync();
                return new JsonResult(new { success = true, names });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadBreakerTypes failed.");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // ============= Localized Breaker Names (DB Fallback) =============
        // --- UPDATED METHOD: Read PLC -> Check Translation -> Return Result ---
        public async Task<JsonResult> OnGetReadBreakerTypesLocalized(string lang)
        {
            try
            {
                // 1. Default to English if language is not provided or not Gujarati
                if (string.IsNullOrEmpty(lang))
                {
                    lang = CultureInfo.CurrentUICulture.Name.StartsWith("gu", StringComparison.OrdinalIgnoreCase) ? "gu" : "en";
                }

                // Read English Names directly from PLC (Helper method you already have)
                var en = await ReadBreakerTypesEnAsync();

                // If not Gujarati, return English immediately
                if (!string.Equals(lang, "gu", StringComparison.OrdinalIgnoreCase))
                    return new JsonResult(new { success = true, names = en });

                // 2. FETCH TRANSLATIONS (Case-Insensitive)
                var allTranslations = await _db.NameTranslations.ToListAsync();

                // 3. Auto-Seed Missing Keys (Case Insensitive)
                bool anyAdded = false;
                foreach (var key in en.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
                {
                    var cleanKey = key.Trim();
                    // Check existence ignoring case
                    bool exists = allTranslations.Any(t => t.En.Trim().Equals(cleanKey, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        var newRow = new NameTranslation { En = cleanKey, Gu = "" };
                        _db.NameTranslations.Add(newRow);
                        allTranslations.Add(newRow); // Add to local list
                        anyAdded = true;
                    }
                }
                if (anyAdded) await _db.SaveChangesAsync();

                // 4. Create Dictionary (Case Insensitive Lookup)
                var dict = allTranslations
                    .GroupBy(t => t.En.Trim())
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Gu ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase
                    );

                // 5. Map Names (Directly to Gujarati OR Fallback to English)
                var outNames = new List<string>(4);
                foreach (var enName in en)
                {
                    var k = (enName ?? "").Trim();
                    // Try to find Gujarati translation
                    if (dict.TryGetValue(k, out var gu) && !string.IsNullOrWhiteSpace(gu))
                    {
                        outNames.Add(gu); // Found translation
                    }
                    else
                    {
                        outNames.Add(k); // Not found? Return English (Step 4)
                    }
                }

                // NOTE: "Step 3" (LocaleBreakerNames positional fallback) is strictly skipped.

                return new JsonResult(new { success = true, names = outNames });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadBreakerTypesLocalized failed");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Helper: Read names from PLC
        private async Task<string[]> ReadBreakerTypesEnAsync()
        {
            var n1 = await _slmpService.ReadStringAsync("D4410", 10);
            var n2 = await _slmpService.ReadStringAsync("D4420", 10);
            var n3 = await _slmpService.ReadStringAsync("D4430", 10);
            var n4 = await _slmpService.ReadStringAsync("D4440", 10);

            return new[]
            {
                (n1.Content ?? "").TrimEnd('\0').Trim(),
                (n2.Content ?? "").TrimEnd('\0').Trim(),
                (n3.Content ?? "").TrimEnd('\0').Trim(),
                (n4.Content ?? "").TrimEnd('\0').Trim(),
            };
        }

        // Whitelist of whole-word registers we allow writing
        private static readonly HashSet<string> _writableWordRegisters = new(new[]
        {
            "D2","D3","D4","D90","D100","D1001","D1002","D1402","D3520" // D3520 = ContOffTime
        });

        public async Task<JsonResult> OnPostWriteRegister([FromBody] WriteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RegisterName) || string.IsNullOrWhiteSpace(request.Value))
                return new JsonResult(new { status = "Error", message = "Request data is missing." });

            try
            {
                // --- FIX START: Map Friendly Name to PLC Address ---
                string targetAddress = request.RegisterName;

                // If the frontend sent "ContOffTime", change it to "D3520"
                if (targetAddress == "ContOffTime")
                {
                    targetAddress = "D3520";
                }
                // --- FIX END ---

                // Check if the RESOLVED address (D3520) is in the whitelist
                if (!_writableWordRegisters.Contains(targetAddress))
                    throw new KeyNotFoundException($"Writable register '{request.RegisterName}' is not allowed.");

                if (!short.TryParse(request.Value, out var intValue))
                    throw new FormatException("Value could not be parsed as a short integer.");

                // Write to the ADDRESS (D3520), not the name
                var result = await _slmpService.WriteAsync(targetAddress, intValue);

                if (!result.IsSuccess) throw new Exception(result.Message);

                return new JsonResult(new { status = "Success", message = $"Wrote {intValue} to {request.RegisterName}." });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to register: {RegisterName}", request.RegisterName);
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        public async Task<JsonResult> OnPostWriteBit([FromBody] WriteBitRequest request)
        {
            if (string.IsNullOrEmpty(request?.Address))
                return new JsonResult(new { status = "Error", message = "Request data is missing." });

            try
            {
                var res = await _slmpService.WriteBoolAsync(request.Address, request.Value);
                if (!res.IsSuccess) throw new Exception(res.Message);
                return new JsonResult(new { status = "Success", message = $"Wrote {request.Value} to {request.Address}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write bit: {Address}", request.Address);
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        private async Task UpdateModelValuesAsync()
        {
            try
            {
                _slmpService.SetHeartbeatValue(5);
                // Batch reads
                var block1 = await _slmpService.ReadInt16BlockAsync("D2", 99);
                if (!block1.IsSuccess) throw new Exception(block1.Message);
                D2 = block1.Content[0];
                D3 = block1.Content[1];
                D4 = block1.Content[2];
                D90 = block1.Content[88];
                D100 = block1.Content[98];

                var block2 = await _slmpService.ReadInt16BlockAsync("D1001", 2);
                if (!block2.IsSuccess) throw new Exception(block2.Message);
                D1001 = block2.Content[0];
                D1002 = block2.Content[1];

                // Singles (16-bit)
                ContOffTime = (await _slmpService.ReadInt16Async("D3520")).Content;
                MaxForwardTorque = (await _slmpService.ReadInt16Async("D800")).Content;
                MaxReverseTorque = (await _slmpService.ReadInt16Async("D802")).Content;
                D1402 = (await _slmpService.ReadInt16Async("D1402")).Content;
                ServoError = (await _slmpService.ReadInt16Async("D1706")).Content;


                // Singles (32-bit)
                InstantTorque = (await _slmpService.ReadInt32Async("D532")).Content;
                TotalTravelLength = (await _slmpService.ReadInt32Async("D900")).Content;
                CurrentPosition = (await _slmpService.ReadInt32Async("D1702")).Content;
                PositionAtEmergency = (await _slmpService.ReadInt32Async("D1708")).Content;
                RackingInValue = (await _slmpService.ReadInt32Async("D1702")).Content;
                RackingOutValue = (await _slmpService.ReadInt32Async("D1712")).Content;

                // English names for initial render
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
    }
}