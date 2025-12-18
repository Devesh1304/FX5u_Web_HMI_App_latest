using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public class PointTable2Model : PointTableModelBase
    {
        // --- 1. Address Maps for Point Table 2 ---
        protected override Dictionary<string, string> WritableInt32Map => new()
        {
            { nameof(PositionWrite1), "D3755" },
            { nameof(PositionWrite2), "D3773" },
            { nameof(PositionWrite3), "D3791" }
        };

        protected override Dictionary<string, string> WritableInt16Map => new()
        {
            { nameof(SpeedWrite1), "D3757" }, { nameof(AccWrite1), "D3758" }, { nameof(DeccWrite1), "D3759" }, { nameof(DwellTimeWrite1), "D3760" }, { nameof(AuxWrite1), "D3761" }, { nameof(MWrite1), "D3762" },
            { nameof(SpeedWrite2), "D3775" }, { nameof(AccWrite2), "D3776" }, { nameof(DeccWrite2), "D3777" }, { nameof(DwellTimeWrite2), "D3778" }, { nameof(AuxWrite2), "D3779" }, { nameof(MWrite2), "D3780" },
            { nameof(SpeedWrite3), "D3793" }, { nameof(AccWrite3), "D3794" }, { nameof(DeccWrite3), "D3795" }, { nameof(DwellTimeWrite3), "D3796" }, { nameof(AuxWrite3), "D3797" }, { nameof(MWrite3), "D3798" },
            { nameof(MaxForwardTorque), "D3994" }, { nameof(MaxReverseTorque), "D3998" },
            { nameof(PointControlRegister), "D90" }, { nameof(SetTolerance), "D3938" }
        };

        protected override Dictionary<string, string> ReadOnlyInt32Map => new()
        {
            { nameof(PositionRead1), "D1828" },
            { nameof(PositionRead2), "D1837" },
            { nameof(PositionRead3), "D1846" }
        };

        protected override Dictionary<string, string> ReadOnlyInt16Map => new()
        {
            { nameof(SpeedRead1), "D1830" }, { nameof(AccRead1), "D1831" }, { nameof(DeccRead1), "D1832" }, { nameof(DwellTimeRead1), "D1833" }, { nameof(AuxRead1), "D1834" }, { nameof(MRead1), "D1835" },
            { nameof(SpeedRead2), "D1839" }, { nameof(AccRead2), "D1840" }, { nameof(DeccRead2), "D1841" }, { nameof(DwellTimeRead2), "D1842" }, { nameof(AuxRead2), "D1843" }, { nameof(MRead2), "D1844" },
            { nameof(SpeedRead3), "D1848" }, { nameof(AccRead3), "D1849" }, { nameof(DeccRead3), "D1850" }, { nameof(DwellTimeRead3), "D1851" }, { nameof(AuxRead3), "D1852" }, { nameof(MRead3), "D1853" }
        };

        public PointTable2Model(ILogger<PointTable2Model> logger, ISLMPService slmpService)
            : base(logger, slmpService)
        {
        }

        public override async Task OnGet()
        {
            // Empty OnGet for instant page load. SignalR populates data.
           _slmpService.SetHeartbeatValue(2);
        //    base.OnGet();
        }
    }
}