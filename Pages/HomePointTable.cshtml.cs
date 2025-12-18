using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public class HomePointTableModel : PointTableModelBase
    {
        // This class DOES NOT need its own _logger or _slmpService fields.
        // It inherits them automatically from PointTableModelBase.

        // This method now has the correct 'override async Task' signature.
        public override async Task OnGet()
        {
            _slmpService.SetHeartbeatValue(8); // Set a unique value for this screen
            await base.OnGet(); // This calls the base class's OnGet method
        }

        // --- Provide the specific addresses for Point Table 2 ---
        protected override Dictionary<string, string> WritableInt32Map => new()
        {
            { nameof(PositionWrite1), "D909" },

        };
        protected override Dictionary<string, string> WritableInt16Map => new()
        {
            { nameof(SpeedWrite1), "D911" }, { nameof(AccWrite1), "D912" }, { nameof(DeccWrite1), "D913" }, { nameof(DwellTimeWrite1), "D914" }, { nameof(AuxWrite1), "D915" }, { nameof(MWrite1), "D916" },
            //{ nameof(SpeedWrite2), "D3775" }, { nameof(AccWrite2), "D3776" }, { nameof(DeccWrite2), "D3777" }, { nameof(DwellTimeWrite2), "D3778" }, { nameof(AuxWrite2), "D3779" }, { nameof(MWrite2), "D3780" },
            //{ nameof(SpeedWrite3), "D3793" }, { nameof(AccWrite3), "D3794" }, { nameof(DeccWrite3), "D3795" }, { nameof(DwellTimeWrite3), "D3796" }, { nameof(AuxWrite3), "D3797" }, { nameof(MWrite3), "D3798" },
            { nameof(MaxForwardTorque), "D3994" },
            { nameof(MaxReverseTorque), "D3998" },
            { nameof(PointControlRegister), "D1402" },
            { nameof(PointControlRegisterD89), "D89" },           
            { nameof(PointControlRegisterD1001), "D1001" },
            { nameof(PointControlRegisterD2), "D2" },
            { nameof(PointControlRegisterD3), "D3" },
            { nameof(PointControlRegisterD4), "D4" },
            { nameof(PointControlRegisterD1402), "D1402" },
            { nameof(PointControlRegisterD91), "D91" },

        };
        protected override Dictionary<string, string> ReadOnlyInt32Map => new()
        {
            { nameof(CurrentPosition), "D1702" }
        };
        protected override Dictionary<string, string> ReadOnlyInt16Map => new()
        {
            

        };

        // The constructor now uses the correct logger type
        public HomePointTableModel(ILogger<HomePointTableModel> logger, ISLMPService slmpService)
            : base(logger, slmpService)
        {
            // This constructor's only job is to pass the services to the base class.
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
    }
}