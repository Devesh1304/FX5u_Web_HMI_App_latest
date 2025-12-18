using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public abstract class PointTableModelBase : PageModel
    {
        protected readonly ILogger<PointTableModelBase> _logger;
        protected readonly ISLMPService _slmpService;

        #region Properties (All 42 data points)
        [BindProperty] public string ConnectionStatus { get; set; } = "Disconnected";
        [BindProperty] public string ErrorMessage { get; set; } = string.Empty;

        // Writable Properties
        [BindProperty] public int PositionWrite1 { get; set; }
        [BindProperty] public int PositionWrite2 { get; set; }
        [BindProperty] public int PositionWrite3 { get; set; }
        [BindProperty] public int SpeedWrite1 { get; set; }
        [BindProperty] public int SpeedWrite2 { get; set; }
        [BindProperty] public int SpeedWrite3 { get; set; }
        [BindProperty] public int AccWrite1 { get; set; }
        [BindProperty] public int AccWrite2 { get; set; }
        [BindProperty] public int AccWrite3 { get; set; }
        [BindProperty] public int DeccWrite1 { get; set; }
        [BindProperty] public int DeccWrite2 { get; set; }
        [BindProperty] public int DeccWrite3 { get; set; }
        [BindProperty] public int DwellTimeWrite1 { get; set; }
        [BindProperty] public int DwellTimeWrite2 { get; set; }
        [BindProperty] public int DwellTimeWrite3 { get; set; }
        [BindProperty] public int AuxWrite1 { get; set; }
        [BindProperty] public int AuxWrite2 { get; set; }
        [BindProperty] public int AuxWrite3 { get; set; }
        [BindProperty] public int MWrite1 { get; set; }
        [BindProperty] public int MWrite2 { get; set; }
        [BindProperty] public int MWrite3 { get; set; }

        // Read-Only Properties
        [BindProperty] public int PositionRead1 { get; set; }
        [BindProperty] public int PositionRead2 { get; set; }
        [BindProperty] public int PositionRead3 { get; set; }
        [BindProperty] public int SpeedRead1 { get; set; }
        [BindProperty] public int SpeedRead2 { get; set; }
        [BindProperty] public int SpeedRead3 { get; set; }
        [BindProperty] public int AccRead1 { get; set; }
        [BindProperty] public int AccRead2 { get; set; }
        [BindProperty] public int AccRead3 { get; set; }
        [BindProperty] public int DeccRead1 { get; set; }
        [BindProperty] public int DeccRead2 { get; set; }
        [BindProperty] public int DeccRead3 { get; set; }
        [BindProperty] public int DwellTimeRead1 { get; set; }
        [BindProperty] public int DwellTimeRead2 { get; set; }
        [BindProperty] public int DwellTimeRead3 { get; set; }
        [BindProperty] public int AuxRead1 { get; set; }
        [BindProperty] public int AuxRead2 { get; set; }
        [BindProperty] public int AuxRead3 { get; set; }
        [BindProperty] public int MRead1 { get; set; }
        [BindProperty] public int MRead2 { get; set; }
        [BindProperty] public int MRead3 { get; set; }
        [BindProperty] public int MaxForwardTorque { get; set; }
        [BindProperty] public int MaxReverseTorque { get; set; }
        [BindProperty] public int PointControlRegister { get; set; }
        [BindProperty] public int CurrentPosition { get; set; }
        [BindProperty] public int PointControlRegisterD4 { get; set; }
        [BindProperty] public int PointControlRegisterD89 { get; set; }
        [BindProperty] public int PointControlRegisterD1001 { get; set; }
        [BindProperty] public int PointControlRegisterD2 { get; set; }
        [BindProperty] public int PointControlRegisterD3 { get; set; }
        [BindProperty] public int PointControlRegisterD1402 { get; set; }
        [BindProperty] public int PointControlRegisterD91 { get; set; }
        [BindProperty] public int PointControlRegisterD5 { get; set; }
        [BindProperty] public int StatusBits { get; set; }
        [BindProperty] public int SetTolerance { get; set; }
        #endregion

        // --- Abstract Maps ---
        protected abstract Dictionary<string, string> WritableInt32Map { get; }
        protected abstract Dictionary<string, string> WritableInt16Map { get; }
        protected abstract Dictionary<string, string> ReadOnlyInt32Map { get; }
        protected abstract Dictionary<string, string> ReadOnlyInt16Map { get; }

        public PointTableModelBase(ILogger<PointTableModelBase> logger, ISLMPService slmpService)
        {
            _logger = logger;
            _slmpService = slmpService;
        }

        // --- CRITICAL FIX: Empty OnGet ---
        // This stops the server from reading 60+ registers before loading the page.
        // SignalR will populate the data immediately after load.
        public virtual async Task OnGet()
        {
            await Task.CompletedTask;
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            // This is kept for compatibility if any old JS still calls it, 
            // but your new SignalR logic replaces this.
            await UpdateModelValuesAsync();
            var properties = GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p.GetValue(this));
            return new JsonResult(properties);
        }

        public async Task<JsonResult> OnPostWriteRegister([FromBody] WriteRequest request)
        {
            if (string.IsNullOrEmpty(request.RegisterName) || string.IsNullOrEmpty(request.Value))
                return new JsonResult(new { status = "Error", message = "Request data is missing." });
            try
            {
                if (WritableInt32Map.TryGetValue(request.RegisterName, out var address32))
                {
                    if (int.TryParse(request.Value, out int intValue32))
                    {
                        var result = await _slmpService.WriteInt32Async(address32, intValue32);
                        if (!result.IsSuccess) throw new Exception(result.Message);
                    }
                    else throw new FormatException("Value is not a valid 32-bit integer.");
                }
                else if (WritableInt16Map.TryGetValue(request.RegisterName, out var address16))
                {
                    if (short.TryParse(request.Value, out short intValue16))
                    {
                        var result = await _slmpService.WriteAsync(address16, intValue16);
                        if (!result.IsSuccess) throw new Exception(result.Message);
                    }
                    else throw new FormatException("Value is not a valid 16-bit integer.");
                }
                else
                {
                    throw new KeyNotFoundException($"Writable register '{request.RegisterName}' not found.");
                }
                return new JsonResult(new { status = "Success", message = $"Wrote {request.Value} to {request.RegisterName}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to register: {RegisterName}", request.RegisterName);
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        // --- Helper Methods ---
        private async Task UpdateModelValuesAsync()
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var entry in WritableInt32Map.Concat(ReadOnlyInt32Map))
                {
                    tasks.Add(ReadAndSetValueAsync(entry.Key, entry.Value, is32bit: true));
                }
                foreach (var entry in WritableInt16Map.Concat(ReadOnlyInt16Map))
                {
                    tasks.Add(ReadAndSetValueAsync(entry.Key, entry.Value, is32bit: false));
                }

                // Optional: Read Y0 status if needed for base logic
                // var y0Result = await _slmpService.ReadBoolAsync("Y0", 1);
                // if (y0Result.IsSuccess && y0Result.Content[0]) StatusBits |= (1 << 0); 

                await Task.WhenAll(tasks);

                ConnectionStatus = "Connected";
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during SLMP update.");
                ConnectionStatus = "Error";
                ErrorMessage = ex.Message;
            }
        }

        private async Task ReadAndSetValueAsync(string key, string address, bool is32bit)
        {
            object value;
            if (is32bit)
            {
                var result = await _slmpService.ReadInt32Async(address);
                if (!result.IsSuccess) throw new Exception($"Failed to read {key}: {result.Message}");
                value = result.Content;
            }
            else
            {
                var result = await _slmpService.ReadInt16Async(address);
                if (!result.IsSuccess) throw new Exception($"Failed to read {key}: {result.Message}");
                value = (int)result.Content;
            }
            this.GetType().GetProperty(key)?.SetValue(this, value);
        }
    }
}