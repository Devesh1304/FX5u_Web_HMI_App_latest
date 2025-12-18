using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FX5u_Web_HMI_App.Pages
{
    public class PointTable3Model : PointTableModelBase
    {
        public override async Task OnGet()
        {
          _slmpService.SetHeartbeatValue(3); // CORRECTED: Set a unique value for this screen
        //  base.OnGet();
        }

        // --- Addresses for Point Table 3 ---
        protected override Dictionary<string, string> WritableInt32Map => new()
        {
            { nameof(PositionWrite1), "D3809" }, { nameof(PositionWrite2), "D3827" }, { nameof(PositionWrite3), "D3845" },
        };
        protected override Dictionary<string, string> WritableInt16Map => new()
        {
            { nameof(SpeedWrite1), "D3811" }, { nameof(AccWrite1), "D3812" }, { nameof(DeccWrite1), "D3813" }, { nameof(DwellTimeWrite1), "D3814" }, { nameof(AuxWrite1), "D3815" }, { nameof(MWrite1), "D3816" },
            { nameof(SpeedWrite2), "D3829" }, { nameof(AccWrite2), "D3830" }, { nameof(DeccWrite2), "D3831" }, { nameof(DwellTimeWrite2), "D3832" }, { nameof(AuxWrite2), "D3833" }, { nameof(MWrite2), "D3834" },
            { nameof(SpeedWrite3), "D3847" }, { nameof(AccWrite3), "D3848" }, { nameof(DeccWrite3), "D3849" }, { nameof(DwellTimeWrite3), "D3850" }, { nameof(AuxWrite3), "D3851" }, { nameof(MWrite3), "D3852" },
            { nameof(MaxForwardTorque), "D3994" }, { nameof(MaxReverseTorque), "D3998" }, { nameof(PointControlRegister), "D90" }, { nameof(SetTolerance), "D3950" }
        };
        protected override Dictionary<string, string> ReadOnlyInt32Map => new()
        {
            { nameof(PositionRead1), "D1855" }, { nameof(PositionRead2), "D1864" }, { nameof(PositionRead3), "D1873" },
        };
        protected override Dictionary<string, string> ReadOnlyInt16Map => new()
        {
            { nameof(SpeedRead1), "D1857" }, { nameof(AccRead1), "D1858" }, { nameof(DeccRead1), "D1859" }, { nameof(DwellTimeRead1), "D1860" }, { nameof(AuxRead1), "D1861" }, { nameof(MRead1), "D1862" },
            { nameof(SpeedRead2), "D1866" }, { nameof(AccRead2), "D1867" }, { nameof(DeccRead2), "D1868" }, { nameof(DwellTimeRead2), "D1869" }, { nameof(AuxRead2), "D1870" }, { nameof(MRead2), "D1871" },
            { nameof(SpeedRead3), "D1875" }, { nameof(AccRead3), "D1876" }, { nameof(DeccRead3), "D1877" }, { nameof(DwellTimeRead3), "D1878" }, { nameof(AuxRead3), "D1879" },
            { nameof(MRead3), "D1880" }, // CORRECTED: Assumed next available address
        };

        public PointTable3Model(ILogger<PointTable3Model> logger, ISLMPService slmpService)
            : base(logger, slmpService)
        {
        }
    }
}