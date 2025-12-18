using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public class PointTable4Model : PointTableModelBase
    {
        // This class DOES NOT need its own _logger or _slmpService fields.
        // It inherits them automatically from PointTableModelBase.

        // This method now has the correct 'override async Task' signature.
        public override async Task OnGet()
        {
            _slmpService.SetHeartbeatValue(4); // Set a unique value for this screen
          //  await base.OnGet(); // This calls the base class's OnGet method
        }

        // --- Provide the specific addresses for Point Table 2 ---
        protected override Dictionary<string, string> WritableInt32Map => new()
        {
            { nameof(PositionWrite1), "D3863" },
            { nameof(PositionWrite2), "D3881" },
            { nameof(PositionWrite3), "D3899" },
        };
        protected override Dictionary<string, string> WritableInt16Map => new()
        {
            { nameof(SpeedWrite1), "D3865" }, { nameof(AccWrite1), "D3866" }, { nameof(DeccWrite1), "D3867" }, { nameof(DwellTimeWrite1), "D3868" }, { nameof(AuxWrite1), "D3869" }, { nameof(MWrite1), "D3870" },
            { nameof(SpeedWrite2), "D3883" }, { nameof(AccWrite2), "D3884" }, { nameof(DeccWrite2), "D3885" }, { nameof(DwellTimeWrite2), "D3886" }, { nameof(AuxWrite2), "D3887" }, { nameof(MWrite2), "D3888" },
            { nameof(SpeedWrite3), "D3901" }, { nameof(AccWrite3), "D3902" }, { nameof(DeccWrite3), "D3903" }, { nameof(DwellTimeWrite3), "D3904" }, { nameof(AuxWrite3), "D3905" }, { nameof(MWrite3), "D3906" },
            { nameof(MaxForwardTorque), "D3994" },
            { nameof(MaxReverseTorque), "D3998" },
            { nameof(PointControlRegister), "D90" },
             { nameof(SetTolerance), "D3962" }

        };
        protected override Dictionary<string, string> ReadOnlyInt32Map => new()
        {
            { nameof(PositionRead1), "D1882" },
            { nameof(PositionRead2), "D1891" },
            { nameof(PositionRead3), "D1900" },
        };
        protected override Dictionary<string, string> ReadOnlyInt16Map => new()
        {
            { nameof(SpeedRead1), "D1884" }, { nameof(AccRead1), "D1885" }, { nameof(DeccRead1), "D1886" }, { nameof(DwellTimeRead1), "D1887" }, { nameof(AuxRead1), "D1888" }, { nameof(MRead1), "D1889" },
            { nameof(SpeedRead2), "D1893" }, { nameof(AccRead2), "D1894" }, { nameof(DeccRead2), "D1895" }, { nameof(DwellTimeRead2), "D1896" }, { nameof(AuxRead2), "D1897" }, { nameof(MRead2), "D1898" },
            { nameof(SpeedRead3), "D1902" }, { nameof(AccRead3), "D1903" }, { nameof(DeccRead3), "D1904" }, { nameof(DwellTimeRead3), "D1905" }, { nameof(AuxRead3), "D1906" }, { nameof(MRead3), "D1907" },

        };

        // The constructor now uses the correct logger type
        public PointTable4Model(ILogger<PointTable4Model> logger, ISLMPService slmpService)
            : base(logger, slmpService)
        {
            // This constructor's only job is to pass the services to the base class.
        }
    }
}