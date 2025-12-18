using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public class AlarmsModel : PageModel
    {
        private readonly ILogger<AlarmsModel> _logger;
        private readonly ISLMPService _slmpService;

        [BindProperty] public int InstantTorque { get; set; }
        [BindProperty] public int ServoError { get; set; }
        [BindProperty] public int ServoCommError { get; set; }
        [BindProperty] public int ServoCommErrorCode { get; set; }
        [BindProperty] public int StatusBits { get; set; }

        [BindProperty] public string ConnectionStatus { get; set; } = "Disconnected";
        [BindProperty] public string ErrorMessage { get; set; } = string.Empty;

        // Define empty writable maps to prevent errors in generic write handler
        private static readonly Dictionary<string, string> _writableInt32Map = new();
        private static readonly Dictionary<string, string> _writableInt16Map = new();

        private static readonly Dictionary<string, string> _readOnlyInt32Map = new();

        private static readonly Dictionary<string, string> _readOnlyInt16Map = new()
        {
            { nameof(ServoError), "D534" },
            { nameof(ServoCommError), "D5501" },
            { nameof(ServoCommErrorCode), "D4998" },
        };

        public AlarmsModel(ILogger<AlarmsModel> logger, ISLMPService slmpService)
        {
            _logger = logger;
            _slmpService = slmpService;
        }

        public async Task OnGet()
        {
            _slmpService.SetHeartbeatValue(6); // Alarms Screen ID
           // await UpdateModelValuesAsync();
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();
            var properties = GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p.GetValue(this));
            return new JsonResult(properties);
        }

        public async Task<JsonResult> OnPostWriteRegister([FromBody] WriteRequest request)
        {
            if (string.IsNullOrEmpty(request?.RegisterName) || string.IsNullOrEmpty(request.Value))
                return new JsonResult(new { status = "Error", message = "Request data is missing." });

            try
            {
                if (_writableInt32Map.TryGetValue(request.RegisterName, out var address32))
                {
                    if (int.TryParse(request.Value, out int intValue32))
                    {
                        var result = await _slmpService.WriteInt32Async(address32, intValue32);
                        if (!result.IsSuccess) throw new Exception(result.Message);
                    }
                    else throw new FormatException("Value is not a valid 32-bit integer.");
                }
                else if (_writableInt16Map.TryGetValue(request.RegisterName, out var address16))
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
                _logger.LogError(ex, "Failed to write to bit: {Address}", request.Address);
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        private async Task UpdateModelValuesAsync()
        {
            try
            {
                // Read all read-only 16-bit values
                foreach (var entry in _readOnlyInt16Map)
                {
                    var val = await _slmpService.ReadInt16Async(entry.Value);
                    if (val.IsSuccess)
                        this.GetType().GetProperty(entry.Key)?.SetValue(this, (int)val.Content);
                }

                // Read Status Bits (M20 & M498)
                StatusBits = 0;

                var m31Result = await _slmpService.ReadBoolAsync("M20", 1);
                if (m31Result.IsSuccess && m31Result.Content[0]) StatusBits |= (1 << 0); // Bit 0 = M20 Status (Reset Btn)

                var sm8500Result = await _slmpService.ReadBoolAsync("M498", 1);
                if (sm8500Result.IsSuccess && sm8500Result.Content[0]) StatusBits |= (1 << 1); // Bit 1 = Servo Error Lamp

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