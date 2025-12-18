using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FX5u_Web_HMI_App.Pages
{
    public class JogModeModel : PageModel
    {
        private readonly ILogger<JogModeModel> _logger;
        private readonly ISLMPService _slmpService;

        [BindProperty] public int InstantTorque { get; set; }
        [BindProperty] public int CurrentPosition { get; set; }
        [BindProperty] public int PositionAtEmergency { get; set; }
        [BindProperty] public int ServoJogSpeed { get; set; }
        [BindProperty] public int ActuatorInSetTime { get; set; }
        [BindProperty] public int ActuatorOutSetTime { get; set; }

        // Register Buffers
        [BindProperty] public int D1001 { get; set; }
        [BindProperty] public int D1002 { get; set; }
        [BindProperty] public int D100 { get; set; }
        [BindProperty] public int D1402 { get; set; }

        // Bit Status
        [BindProperty] public int StatusBits { get; set; }
        [BindProperty] public int OutputStatusBits { get; set; }

        [BindProperty] public string ConnectionStatus { get; set; } = "Disconnected";
        [BindProperty] public string ErrorMessage { get; set; } = string.Empty;

        // Write Maps
        private readonly Dictionary<string, string> _int16WriteAddressMap = new()
        {
            { nameof(ActuatorInSetTime), "D3504"},
            { nameof(ActuatorOutSetTime), "D3506" },
            { "D1402", "D1402" },
            { "D1001", "D1001" },
            { "D1002", "D1002" },
            { "D100", "D100" }
        };

        private readonly Dictionary<string, string> _int32WriteAddressMap = new()
        {
            { nameof(ServoJogSpeed), "D530" },
        };

        public JogModeModel(ILogger<JogModeModel> logger, ISLMPService slmpService)
        {
            _logger = logger;
            _slmpService = slmpService;
        }

        public async Task OnGet()
        {
          //   // Jog Mode Screen ID
           // await UpdateModelValuesAsync();
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();
            // Reflection to return all properties as JSON (camelCase keys by default in MVC)
            var properties = this.GetType().GetProperties()
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
                // Check 32-bit first
                if (_int32WriteAddressMap.TryGetValue(request.RegisterName, out string? address32))
                {
                    if (int.TryParse(request.Value, out int intValue32))
                    {
                        var result = await _slmpService.WriteInt32Async(address32, intValue32);
                        if (result.IsSuccess)
                            return new JsonResult(new { status = "Success", message = $"Wrote {intValue32} to {request.RegisterName}." });
                        throw new Exception(result.Message);
                    }
                    throw new FormatException("Value could not be parsed as a 32-bit integer.");
                }
                // Check 16-bit
                else if (_int16WriteAddressMap.TryGetValue(request.RegisterName, out string? address16))
                {
                    if (short.TryParse(request.Value, out short intValue16))
                    {
                        var result = await _slmpService.WriteAsync(address16, intValue16);
                        if (result.IsSuccess)
                            return new JsonResult(new { status = "Success", message = $"Wrote {intValue16} to {request.RegisterName}." });
                        throw new Exception(result.Message);
                    }
                    throw new FormatException("Value could not be parsed as a 16-bit integer.");
                }
                else
                {
                    throw new KeyNotFoundException($"Writable register '{request.RegisterName}' not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to register: {RegisterName}", request.RegisterName);
                return new JsonResult(new { status = "Error", message = ex.Message });
            }
        }

        public async Task<JsonResult> OnPostWriteBit([FromBody] WriteBitRequest request)
        {
            if (string.IsNullOrEmpty(request.Address))
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
                _slmpService.SetHeartbeatValue(9);
                // --- BATCH READ 1: 16-bit integers ---
                // Reading 99 words starting at D2 covers up to D100 (D2 + 98 = D100)
                var block1Result = await _slmpService.ReadInt16BlockAsync("D2", 99);
                if (!block1Result.IsSuccess) throw new Exception(block1Result.Message);
                D100 = block1Result.Content[98];

                var block2Result = await _slmpService.ReadInt16BlockAsync("D1001", 2);
                if (!block2Result.IsSuccess) throw new Exception(block2Result.Message);
                D1001 = block2Result.Content[0];
                D1002 = block2Result.Content[1];

                // --- INDIVIDUAL READS: 16-bit integers ---
                ActuatorInSetTime = (await _slmpService.ReadInt16Async("D3504")).Content;
                ActuatorOutSetTime = (await _slmpService.ReadInt16Async("D3506")).Content;
                D1402 = (await _slmpService.ReadInt16Async("D1402")).Content;

                // --- INDIVIDUAL READS: 32-bit integers ---
                InstantTorque = (await _slmpService.ReadInt32Async("D532")).Content;
                CurrentPosition = (await _slmpService.ReadInt32Async("D1702")).Content;
                PositionAtEmergency = (await _slmpService.ReadInt32Async("D1708")).Content;
                ServoJogSpeed = (await _slmpService.ReadInt32Async("D530")).Content;

                // --- BIT READS ---
                var m31Result = await _slmpService.ReadBoolAsync("M31", 1);
                if (m31Result.IsSuccess && m31Result.Content[0]) StatusBits |= (1 << 0);
                else StatusBits &= ~(1 << 0);

                // Read Outputs Y4, Y5, Y0 for status display
                // Ideally, read these in a block if they were contiguous, but Y0, Y4, Y5 are close.
                // Reading individually for now as requested.
                OutputStatusBits = 0;

                var y4Result = await _slmpService.ReadBoolAsync("Y4", 1);
                if (y4Result.IsSuccess && y4Result.Content[0]) OutputStatusBits |= (1 << 0); // Y4 is Bit 0

                var y5Result = await _slmpService.ReadBoolAsync("Y5", 1);
                if (y5Result.IsSuccess && y5Result.Content[0]) OutputStatusBits |= (1 << 1); // Y5 is Bit 1

                var y0Result = await _slmpService.ReadBoolAsync("Y0", 1);
                if (y0Result.IsSuccess && y0Result.Content[0]) OutputStatusBits |= (1 << 2); // Y0 is Bit 2

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