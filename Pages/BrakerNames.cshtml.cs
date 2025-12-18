using FX5u_Web_HMI_App.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public class BrakerNamesModel : PageModel
    {
        private readonly ILogger<BrakerNamesModel> _logger;
        private readonly ISLMPService _slmpService;
        private readonly LogDbContext _db;

        // --- Bound Properties for the 20 Breaker Names ---
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

        private const int WORDS_PER_NAME = 10; // 10 words = 20 ASCII characters
        private const string BASE_D = "D4000";
        private static readonly Dictionary<string, string> _stringAddressMap = new();

        static BrakerNamesModel()
        {
            // Map BrakerName1..20 -> D4000..D4199
            for (int i = 0; i < 20; i++)
                _stringAddressMap.Add($"BrakerName{i + 1}", $"D{4000 + (i * WORDS_PER_NAME)}");
        }

        public BrakerNamesModel(ILogger<BrakerNamesModel> logger, ISLMPService slmpService, LogDbContext db)
        {
            _logger = logger;
            _slmpService = slmpService;
            _db = db;
        }

        public async Task OnGet()
        {
            _slmpService.SetHeartbeatValue(20); // Heartbeat for this screen
        //    await UpdateModelValuesAsync();
        }

        // --- Class for English Write Request ---
        public class WriteStringRequest
        {
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
        }

        // --- READ ALL REGISTERS (Called by JS fetch) ---
        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();

            var dict = GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(
                    p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1),
                    p => p.GetValue(this)
                );

            return new JsonResult(dict);
        }

        // --- WRITE ENGLISH TO PLC ---
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostWriteString([FromBody] WriteStringRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
                return new JsonResult(new { status = "Error", message = "Request data is missing." });

            try
            {
                if (!_stringAddressMap.TryGetValue(request.Name, out var address))
                    throw new KeyNotFoundException($"Register '{request.Name}' not found.");

                var val = request.Value ?? string.Empty;
                if (val.Length > 20)
                    return new JsonResult(new { status = "Error", message = "Max 20 characters." });

                // 1. Clear Memory & Write New Value to PLC
                var ok = await WriteFixedAsciiAsync(address, val);
                if (!ok) throw new Exception("PLC write failed.");

                // 2. Update Database (Link English Key to Gujarati)
                await UpsertTranslationAsync(en: val, gu: null);

                return new JsonResult(new { status = "Success", message = $"Wrote '{val}' to {request.Name}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write string: {Name}", request?.Name);
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        // --- GET GUJARATI VALUES (DB) ---
        public JsonResult OnGetGetGujarati()
        {
            var values = Enumerable.Range(1, 20)
                .Select(i => new
                {
                    Key = $"BrakerName{i}",
                    Val = _db.LocaleBreakerNames
                             .Where(x => x.Id == i && x.Lang == "gu")
                             .Select(x => x.Text)
                             .FirstOrDefault() ?? string.Empty
                })
                .ToDictionary(x => x.Key, x => x.Val);

            return new JsonResult(new { values });
        }

        // --- SAVE GUJARATI TO DB ---
        public class SetGujaratiRequest
        {
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostSetGujarati([FromBody] SetGujaratiRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req?.Name) || !req.Name.StartsWith("BrakerName"))
                    return new JsonResult(new { status = "Error", message = "Invalid name" });

                if ((req.Value ?? "").Length > 50)
                    return new JsonResult(new { status = "Error", message = "Text too long" });

                int idx = int.Parse(req.Name.Replace("BrakerName", "")); // Extract ID (1-20)

                // 1. Update Fallback Table (LocaleBreakerNames)
                var row = _db.LocaleBreakerNames.SingleOrDefault(x => x.Id == idx && x.Lang == "gu");
                if (row == null)
                    _db.LocaleBreakerNames.Add(new LocaleBreakerName { Id = idx, Lang = "gu", Text = req.Value ?? string.Empty });
                else
                    row.Text = req.Value ?? string.Empty;

                await _db.SaveChangesAsync();

                // 2. Update Translation Table (Link current PLC English value to this Gujarati value)
                // We read the PLC first to know what English word corresponds to this Gujarati word
                var read = await _slmpService.ReadInt16BlockAsync($"D{4000 + (idx - 1) * WORDS_PER_NAME}", WORDS_PER_NAME);
                if (read.IsSuccess)
                {
                    var en = DecodeAscii(read.Content);
                    await UpsertTranslationAsync(en: en, gu: req.Value ?? "");
                }

                return new JsonResult(new { status = "Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Gujarati text");
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        // ================= HELPERS =================

        private async Task UpdateModelValuesAsync()
        {
            try
            {
                // Read all 20 names in one block (20 names * 10 words = 200 words)
                var read = await _slmpService.ReadInt16BlockAsync(BASE_D, 200);
                if (!read.IsSuccess) throw new Exception(read.Message);

                var words = read.Content;

                for (int i = 0; i < 20; i++)
                {
                    var prop = GetType().GetProperty($"BrakerName{i + 1}");
                    if (prop is null) continue;

                    var slice = new ArraySegment<short>(words, i * WORDS_PER_NAME, WORDS_PER_NAME);
                    prop.SetValue(this, DecodeAscii(slice));
                }
                ConnectionStatus = "Connected";
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateModelValuesAsync failed");
                ConnectionStatus = "Disconnected";
                ErrorMessage = ex.Message;
            }
        }

        private static string DecodeAscii(IReadOnlyList<short> words)
        {
            byte[] bytes = new byte[words.Count * 2];
            for (int j = 0; j < words.Count; j++)
            {
                ushort w = (ushort)words[j];
                bytes[j * 2 + 0] = (byte)(w & 0xFF);
                bytes[j * 2 + 1] = (byte)((w >> 8) & 0xFF);
            }
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ');
        }

        private static string Normalize(string s) => (s ?? "").Trim();

        private async Task UpsertTranslationAsync(string en, string? gu)
        {
            en = Normalize(en);
            gu = Normalize(gu ?? "");
            if (string.IsNullOrEmpty(en)) return;

            var row = await _db.NameTranslations.SingleOrDefaultAsync(t => t.En == en);
            if (row == null)
                _db.NameTranslations.Add(new NameTranslation { En = en, Gu = gu ?? "" });
            else if (!string.IsNullOrEmpty(gu))
                row.Gu = gu; // Update existing translation

            await _db.SaveChangesAsync();
        }

        private async Task<bool> WriteFixedAsciiAsync(string dAddress, string value)
        {
            // This version relies only on WriteStringAsync (Available in your current Service)
            // It clears the register by writing empty characters
            var zeros = new string('\0', WORDS_PER_NAME * 2); // 20 Null characters
            var clear = await _slmpService.WriteStringAsync(dAddress, zeros);

            if (!clear.IsSuccess) return false;

            if (string.IsNullOrEmpty(value)) return true;

            var wr = await _slmpService.WriteStringAsync(dAddress, value);
            return wr.IsSuccess;
        }
    }
}