using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public class PointTable1Model : PointTableModelBase
    {
        // 1. Define Addresses specific to Point Table 1
        protected override Dictionary<string, string> WritableInt32Map => new()
        {
            { nameof(PositionWrite1), "D3701" },
            { nameof(PositionWrite2), "D3719" },
            { nameof(PositionWrite3), "D3737" }
        };

        protected override Dictionary<string, string> WritableInt16Map => new()
        {
            { nameof(SpeedWrite1), "D3703" }, { nameof(AccWrite1), "D3704" }, { nameof(DeccWrite1), "D3705" }, { nameof(DwellTimeWrite1), "D3706" }, { nameof(AuxWrite1), "D3707" }, { nameof(MWrite1), "D3708" },
            { nameof(SpeedWrite2), "D3721" }, { nameof(AccWrite2), "D3722" }, { nameof(DeccWrite2), "D3723" }, { nameof(DwellTimeWrite2), "D3724" }, { nameof(AuxWrite2), "D3725" }, { nameof(MWrite2), "D3726" },
            { nameof(SpeedWrite3), "D3739" }, { nameof(AccWrite3), "D3740" }, { nameof(DeccWrite3), "D3741" }, { nameof(DwellTimeWrite3), "D3742" }, { nameof(AuxWrite3), "D3743" }, { nameof(MWrite3), "D3744" },
            { nameof(MaxForwardTorque), "D3994" }, { nameof(MaxReverseTorque), "D3998" },
            { nameof(PointControlRegister), "D90" }, { nameof(SetTolerance), "D3990" }
        };

        protected override Dictionary<string, string> ReadOnlyInt32Map => new()
        {
            { nameof(PositionRead1), "D1801" },
            { nameof(PositionRead2), "D1810" },
            { nameof(PositionRead3), "D1819" }
        };

        protected override Dictionary<string, string> ReadOnlyInt16Map => new()
        {
            { nameof(SpeedRead1), "D1803" }, { nameof(AccRead1), "D1804" }, { nameof(DeccRead1), "D1805" }, { nameof(DwellTimeRead1), "D1806" }, { nameof(AuxRead1), "D1807" }, { nameof(MRead1), "D1808" },
            { nameof(SpeedRead2), "D1812" }, { nameof(AccRead2), "D1813" }, { nameof(DeccRead2), "D1814" }, { nameof(DwellTimeRead2), "D1815" }, { nameof(AuxRead2), "D1816" }, { nameof(MRead2), "D1817" },
            { nameof(SpeedRead3), "D1821" }, { nameof(AccRead3), "D1822" }, { nameof(DeccRead3), "D1823" }, { nameof(DwellTimeRead3), "D1824" }, { nameof(AuxRead3), "D1825" }, { nameof(MRead3), "D1826" }
        };

        public PointTable1Model(ILogger<PointTable1Model> logger, ISLMPService slmpService)
            : base(logger, slmpService)
        {
        }

        public override async Task OnGet()
        {
            // Set Heartbeat only. Do NOT read data here (SignalR will handle it).
            _slmpService.SetHeartbeatValue(1);
        //   base.OnGet();
        }
    }
}