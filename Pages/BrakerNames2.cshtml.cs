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
    public class BrakerNames2Model : PageModel
    {
        private readonly ILogger<BrakerNames2Model> _logger;
        private readonly ISLMPService _slmpService;
        private readonly LogDbContext _db;

        // Bound Properties 1..20 (Mapping to 21..40 on screen)
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

        private const int WORDS_PER_NAME = 10;
        private const string BASE_D = "D4200"; // Page 2 starts at D4200
        private static readonly Dictionary<string, string> _stringAddressMap = new();

        static BrakerNames2Model()
        {
            // Map BrakerName1..20 -> D4200..D4399
            for (int i = 0; i < 20; i++)
                _stringAddressMap.Add($"BrakerName{i + 1}", $"D{4200 + (i * WORDS_PER_NAME)}");
        }

        public BrakerNames2Model(ILogger<BrakerNames2Model> logger, ISLMPService slmpService, LogDbContext db)
        {
            _logger = logger;
            _slmpService = slmpService;
            _db = db;
        }

        public async Task OnGet()
        {
            _slmpService.SetHeartbeatValue(21); // Heartbeat for page 2
        //    await UpdateModelValuesAsync();
        }

        public class WriteStringRequest
        {
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();
            var dict = GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p.GetValue(this));
            return new JsonResult(dict);
        }

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

                // Write to PLC (using string empty clear method)
                var ok = await WriteFixedAsciiAsync(address, val);
                if (!ok) throw new Exception("PLC write failed.");

                await UpsertTranslationAsync(en: val, gu: null);

                return new JsonResult(new { status = "Success", message = $"Wrote '{val}' to {request.Name}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write string: {Name}", request?.Name);
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        // ---------- Gujarati (DB Ids 21..40) ----------
        public JsonResult OnGetGetGujarati()
        {
            var values = Enumerable.Range(1, 20)
                .Select(i => new
                {
                    Key = $"BrakerName{i}",
                    Val = _db.LocaleBreakerNames
                             .Where(x => x.Id == (20 + i) && x.Lang == "gu") // Offset by 20
                             .Select(x => x.Text)
                             .FirstOrDefault() ?? string.Empty
                })
                .ToDictionary(x => x.Key, x => x.Val);

            return new JsonResult(new { values });
        }

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

                int idx = int.Parse(req.Name.Replace("BrakerName", "")); // 1..20
                int dbId = 20 + idx; // Calculate DB ID (21..40)

                var row = _db.LocaleBreakerNames.SingleOrDefault(x => x.Id == dbId && x.Lang == "gu");
                if (row == null)
                    _db.LocaleBreakerNames.Add(new LocaleBreakerName { Id = dbId, Lang = "gu", Text = req.Value ?? string.Empty });
                else
                    row.Text = req.Value ?? string.Empty;

                await _db.SaveChangesAsync();

                // Auto-maintain Translations
                var read = await _slmpService.ReadInt16BlockAsync($"D{4200 + (idx - 1) * WORDS_PER_NAME}", WORDS_PER_NAME);
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

        // ---------- Helpers ----------
        private async Task UpdateModelValuesAsync()
        {
            try
            {
                var read = await _slmpService.ReadInt16BlockAsync(BASE_D, WORDS_PER_NAME * 20);
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
                row.Gu = gu;

            await _db.SaveChangesAsync();
        }

        private async Task<bool> WriteFixedAsciiAsync(string dAddress, string value)
        {
            // Use string empty to clear instead of int block write
            var zeros = new string('\0', WORDS_PER_NAME * 2);
            var clear = await _slmpService.WriteStringAsync(dAddress, zeros);
            if (!clear.IsSuccess) return false;

            if (string.IsNullOrEmpty(value)) return true;
            var wr = await _slmpService.WriteStringAsync(dAddress, value);
            return wr.IsSuccess;
        }
    }
}