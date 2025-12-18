using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HslCommunication;
using HslCommunication.Profinet.Melsec;

namespace FX5u_Web_HMI_App.Pages
{
    public class PowerMainScreen : PageModel
    {
        private readonly ILogger<PowerMainScreen> _logger;

        private readonly ISLMPService _slmpService;
        [BindProperty] public int D3 { get; set; }
        [BindProperty] public int D90 { get; set; }
        [BindProperty] public int D1002 { get; set; }
        [BindProperty] public int D1402 { get; set; }

        [BindProperty] public int StatusBits { get; set; }
        [BindProperty] public string ConnectionStatus { get; set; } = "Disconnected";
        [BindProperty] public string ErrorMessage { get; set; } = string.Empty;

        public async Task OnGet()
        {
            _slmpService.SetHeartbeatValue(11); // Set value for Home Screen
            await UpdateModelValuesAsync();
        }

        private readonly Dictionary<string, string> _intAddressMap = new()
        {

             { "D1002", "D1002" },
             { "D3", "D3" },
             { "D90", "D90" },
             { "D1402", "D1402" },

        };

        // Map for 16-BIT writable integers
        private readonly Dictionary<string, string> _int16WriteAddressMap = new()
    {

            { "D1002", "D1002" },
             { "D3", "D3" },
             { "D90", "D90" },
             { "D1402", "D1402" },


    };

        // Map for 32-BIT writable integers
        private readonly Dictionary<string, string> _int32WriteAddressMap = new()
    {
    
    };

        public PowerMainScreen(ILogger<PowerMainScreen> logger, ISLMPService slmpService)
        {
            _logger = logger;
            _slmpService = slmpService;
        }

        public async Task<JsonResult> OnGetReadRegisters()
        {
            await UpdateModelValuesAsync();
            var properties = this.GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(BindPropertyAttribute), false))
                .ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p.GetValue(this));
            return new JsonResult(properties);
        }


        public async Task<JsonResult> OnPostWriteRegister([FromBody] WriteRequest request)
        {
            if (string.IsNullOrEmpty(request.RegisterName) || string.IsNullOrEmpty(request.Value))
            {
                return new JsonResult(new { status = "Error", message = "Request data is missing." });
            }

            try
            {
                // Check if it's a 32-bit integer first
                if (_int32WriteAddressMap.TryGetValue(request.RegisterName, out string? address32))
                {
                    if (int.TryParse(request.Value, out int intValue32))
                    {
                        var result = await _slmpService.WriteInt32Async(address32, intValue32);
                        if (result.IsSuccess)
                        {
                            return new JsonResult(new { status = "Success", message = $"Wrote {intValue32} to {request.RegisterName}." });
                        }
                        throw new Exception(result.Message);
                    }
                    throw new FormatException("Value could not be parsed as a 32-bit integer.");
                }
                // Then check if it's a 16-bit integer
                else if (_int16WriteAddressMap.TryGetValue(request.RegisterName, out string? address16))
                {
                    if (short.TryParse(request.Value, out short intValue16))
                    {
                        var result = await _slmpService.WriteAsync(address16, intValue16);
                        if (result.IsSuccess)
                        {
                            return new JsonResult(new { status = "Success", message = $"Wrote {intValue16} to {request.RegisterName}." });
                        }
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
            {
                return new JsonResult(new { status = "Error", message = "Request data is missing." });
            }
            try
            {
                var result = await _slmpService.WriteBoolAsync(request.Address, request.Value);
                if (result.IsSuccess)
                {
                    return new JsonResult(new { status = "Success", message = $"Wrote {request.Value} to {request.Address}." });
                }
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




                var d1 = await _slmpService.WriteAsync("D1", 0);

                // --- INDIVIDUAL READS: 16-bit integers ---

                D3 = (await _slmpService.ReadInt16Async("D3")).Content;
                D90 = (await _slmpService.ReadInt16Async("D90")).Content;
                D1002 = (await _slmpService.ReadInt16Async("D1002")).Content;
                D1402 = (await _slmpService.ReadInt16Async("D1402")).Content;


                var y02Result = await _slmpService.ReadBoolAsync("M900", 1);
                if (y02Result.IsSuccess && y02Result.Content[0])
                {
                    StatusBits |= (1 << 0); // Set bit 0 to ON if M31 is true
                }
                else
                {
                    StatusBits &= ~(1 << 0); // Set bit 0 to OFF if M31 is false or read fails
                }

                var y03Result = await _slmpService.ReadBoolAsync("M901", 1);
                if (y03Result.IsSuccess && y03Result.Content[0])
                {
                    StatusBits |= (1 << 1); // Set bit 0 to ON if M31 is true
                }
                else
                {
                    StatusBits &= ~(1 << 1); // Set bit 0 to OFF if M31 is false or read fails
                }



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
