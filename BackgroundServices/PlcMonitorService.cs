using FX5u_Web_HMI_App.Hubs;
using FX5u_Web_HMI_App.Pages;
using FX5u_Web_HMI_App.Data;
using FX5u_Web_HMI_App;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic; // 2. Add this for using List
using System.Linq; // 3. Add this for using .Any()
using System.Threading;
using System.Threading.Tasks;
using HslCommunication;

namespace FX5u_Web_HMI_App.BackgroundServices
{
    public class PlcMonitorService : BackgroundService
    {
        private readonly ILogger<PlcMonitorService> _logger;
        private readonly ISLMPService _slmpService;
        private readonly IHubContext<PlcHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly PageStateTracker _tracker;
        // State variables to track the last known values
        private int _lastNavigationValue = 0;
        private int _lastAlarmCode = 0;
       // private DateTime _lastLogTime = DateTime.MinValue;

        // --- THIS IS THE CORRECTED CONSTRUCTOR ---
        public PlcMonitorService(ILogger<PlcMonitorService> logger,
                                 ISLMPService slmpService,
                                 IHubContext<PlcHub> hubContext,
                                 IServiceProvider serviceProvider,
                                 PageStateTracker tracker) // 1. Add the service here
        {
            _logger = logger;
            _slmpService = slmpService;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _tracker = tracker;// 2. Assign the service here
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PLC Monitor Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // --- MONITOR POINT TABLE 1 ---
                    if (_tracker.IsPageActive("PointTable1"))
                    {
                        var data = new Dictionary<string, object>();

                        // 1. READ "READ-ONLY" BLOCK (D1800 - D1850)
                        // -----------------------------------------
                        var readBlock = await _slmpService.ReadInt16BlockAsync("D1800", 50);
                        if (readBlock.IsSuccess)
                        {
                            var r = readBlock.Content;
                            // Row 1
                            data["PositionRead1"] = GetInt32(r, 1); // D1801
                            data["SpeedRead1"] = r[3];           // D1803
                            data["AccRead1"] = r[4];
                            data["DeccRead1"] = r[5];
                            data["DwellTimeRead1"] = r[6];
                            data["AuxRead1"] = r[7];
                            data["MRead1"] = r[8];

                            // Row 2
                            data["PositionRead2"] = GetInt32(r, 10); // D1810
                            data["SpeedRead2"] = r[12];
                            data["AccRead2"] = r[13];
                            data["DeccRead2"] = r[14];
                            data["DwellTimeRead2"] = r[15];
                            data["AuxRead2"] = r[16];
                            data["MRead2"] = r[17];

                            // Row 3
                            data["PositionRead3"] = GetInt32(r, 19); // D1819
                            data["SpeedRead3"] = r[21];
                            data["AccRead3"] = r[22];
                            data["DeccRead3"] = r[23];
                            data["DwellTimeRead3"] = r[24];
                            data["AuxRead3"] = r[25];
                            data["MRead3"] = r[26];
                        }

                        // 2. READ "WRITE" BLOCK (D3700 - D3750) <--- THIS WAS MISSING
                        // -------------------------------------
                        var writeBlock = await _slmpService.ReadInt16BlockAsync("D3700", 50);
                        if (writeBlock.IsSuccess)
                        {
                            var w = writeBlock.Content;

                            // Row 1 (Write)
                            data["PositionWrite1"] = GetInt32(w, 1); // D3701
                            data["SpeedWrite1"] = w[3];           // D3703
                            data["AccWrite1"] = w[4];
                            data["DeccWrite1"] = w[5];
                            data["DwellTimeWrite1"] = w[6];
                            data["AuxWrite1"] = w[7];
                            data["MWrite1"] = w[8];

                            // Row 2 (Write)
                            data["PositionWrite2"] = GetInt32(w, 19); // D3719
                            data["SpeedWrite2"] = w[21];
                            data["AccWrite2"] = w[22];
                            data["DeccWrite2"] = w[23];
                            data["DwellTimeWrite2"] = w[24];
                            data["AuxWrite2"] = w[25];
                            data["MWrite2"] = w[26];

                            // Row 3 (Write)
                            data["PositionWrite3"] = GetInt32(w, 37); // D3737
                            data["SpeedWrite3"] = w[39];
                            data["AccWrite3"] = w[40];
                            data["DeccWrite3"] = w[41];
                            data["DwellTimeWrite3"] = w[42];
                            data["AuxWrite3"] = w[43];
                            data["MWrite3"] = w[44];
                        }

                        // 3. READ TORQUE & TOLERANCE (D3990 - D4000)
                        // ------------------------------------------
                        var miscBlock = await _slmpService.ReadInt16BlockAsync("D3990", 10);
                        if (miscBlock.IsSuccess)
                        {
                            var m = miscBlock.Content;
                            data["SetTolerance"] = m[0]; // D3990
                            data["MaxForwardTorque"] = m[4]; // D3994
                            data["MaxReverseTorque"] = m[8]; // D3998
                        }

                        await _hubContext.Clients.Group("PointTable1").SendAsync("ReceivePlcData", data);
                    }
                    // --- MONITOR POINT TABLE 2 ---
                    if (_tracker.IsPageActive("PointTable2"))
                    {
                        var data = new Dictionary<string, object>();

                        // 1. READ "READ-ONLY" BLOCK (D1828 - D1860)
                        // Range covers PositionRead1 (D1828) to MRead3 (D1853)
                        var readBlock = await _slmpService.ReadInt16BlockAsync("D1828", 40);
                        if (readBlock.IsSuccess)
                        {
                            var r = readBlock.Content;
                            // Row 1 (Starts at offset 0 relative to D1828)
                            data["PositionRead1"] = GetInt32(r, 0); // D1828
                            data["SpeedRead1"] = r[2];           // D1830
                            data["AccRead1"] = r[3];
                            data["DeccRead1"] = r[4];
                            data["DwellTimeRead1"] = r[5];
                            data["AuxRead1"] = r[6];
                            data["MRead1"] = r[7];

                            // Row 2 (Starts at D1837 -> offset 9)
                            data["PositionRead2"] = GetInt32(r, 9); // D1837
                            data["SpeedRead2"] = r[11];
                            data["AccRead2"] = r[12];
                            data["DeccRead2"] = r[13];
                            data["DwellTimeRead2"] = r[14];
                            data["AuxRead2"] = r[15];
                            data["MRead2"] = r[16];

                            // Row 3 (Starts at D1846 -> offset 18)
                            data["PositionRead3"] = GetInt32(r, 18); // D1846
                            data["SpeedRead3"] = r[20];
                            data["AccRead3"] = r[21];
                            data["DeccRead3"] = r[22];
                            data["DwellTimeRead3"] = r[23];
                            data["AuxRead3"] = r[24];
                            data["MRead3"] = r[25];
                        }

                        // 2. READ "WRITE" BLOCK (D3755 - D3800)
                        // Range covers PositionWrite1 (D3755) to MWrite3 (D3798)
                        var writeBlock = await _slmpService.ReadInt16BlockAsync("D3755", 50);
                        if (writeBlock.IsSuccess)
                        {
                            var w = writeBlock.Content;

                            // Row 1 (Offset 0 relative to D3755)
                            data["PositionWrite1"] = GetInt32(w, 0); // D3755
                            data["SpeedWrite1"] = w[2];           // D3757
                            data["AccWrite1"] = w[3];
                            data["DeccWrite1"] = w[4];
                            data["DwellTimeWrite1"] = w[5];
                            data["AuxWrite1"] = w[6];
                            data["MWrite1"] = w[7];

                            // Row 2 (Starts at D3773 -> offset 18)
                            data["PositionWrite2"] = GetInt32(w, 18); // D3773
                            data["SpeedWrite2"] = w[20];
                            data["AccWrite2"] = w[21];
                            data["DeccWrite2"] = w[22];
                            data["DwellTimeWrite2"] = w[23];
                            data["AuxWrite2"] = w[24];
                            data["MWrite2"] = w[25];

                            // Row 3 (Starts at D3791 -> offset 36)
                            data["PositionWrite3"] = GetInt32(w, 36); // D3791
                            data["SpeedWrite3"] = w[38];
                            data["AccWrite3"] = w[39];
                            data["DeccWrite3"] = w[40];
                            data["DwellTimeWrite3"] = w[41];
                            data["AuxWrite3"] = w[42];
                            data["MWrite3"] = w[43];
                        }

                        // 3. READ TORQUE & TOLERANCE (D3938 and D3994/98)
                        // These are far apart, so we read 2 small blocks or 1 large one. 
                        // D3938 to D3998 is 60 words. Let's read one block for simplicity.
                        var miscBlock = await _slmpService.ReadInt16BlockAsync("D3938", 65);
                        if (miscBlock.IsSuccess)
                        {
                            var m = miscBlock.Content;
                            data["SetTolerance"] = m[0];  // D3938 (Offset 0)
                            data["MaxForwardTorque"] = m[56]; // D3994 (Offset 56)
                            data["MaxReverseTorque"] = m[60]; // D3998 (Offset 60)
                        }

                        await _hubContext.Clients.Group("PointTable2").SendAsync("ReceivePlcData", data);
                    }
                    // --- MONITOR POINT TABLE 3 ---
                    if (_tracker.IsPageActive("PointTable3"))
                    {
                        var data = new Dictionary<string, object>();

                        // 1. READ "READ-ONLY" BLOCK (D1855 - D1881)
                        // Covers PositionRead1 (D1855) to MRead3 (D1880)
                        var readBlock = await _slmpService.ReadInt16BlockAsync("D1855", 30);
                        if (readBlock.IsSuccess)
                        {
                            var r = readBlock.Content;
                            // Row 1 (Offset 0 relative to D1855)
                            data["PositionRead1"] = GetInt32(r, 0); // D1855
                            data["SpeedRead1"] = r[2];           // D1857
                            data["AccRead1"] = r[3];
                            data["DeccRead1"] = r[4];
                            data["DwellTimeRead1"] = r[5];
                            data["AuxRead1"] = r[6];
                            data["MRead1"] = r[7];

                            // Row 2 (Starts at D1864 -> offset 9)
                            data["PositionRead2"] = GetInt32(r, 9); // D1864
                            data["SpeedRead2"] = r[11];
                            data["AccRead2"] = r[12];
                            data["DeccRead2"] = r[13];
                            data["DwellTimeRead2"] = r[14];
                            data["AuxRead2"] = r[15];
                            data["MRead2"] = r[16];

                            // Row 3 (Starts at D1873 -> offset 18)
                            data["PositionRead3"] = GetInt32(r, 18); // D1873
                            data["SpeedRead3"] = r[20];
                            data["AccRead3"] = r[21];
                            data["DeccRead3"] = r[22];
                            data["DwellTimeRead3"] = r[23];
                            data["AuxRead3"] = r[24];
                            data["MRead3"] = r[25]; // D1880
                        }

                        // 2. READ "WRITE" BLOCK (D3809 - D3860)
                        // Covers PositionWrite1 (D3809) to MWrite3 (D3852)
                        var writeBlock = await _slmpService.ReadInt16BlockAsync("D3809", 50);
                        if (writeBlock.IsSuccess)
                        {
                            var w = writeBlock.Content;

                            // Row 1 (Offset 0 relative to D3809)
                            data["PositionWrite1"] = GetInt32(w, 0); // D3809
                            data["SpeedWrite1"] = w[2];           // D3811
                            data["AccWrite1"] = w[3];
                            data["DeccWrite1"] = w[4];
                            data["DwellTimeWrite1"] = w[5];
                            data["AuxWrite1"] = w[6];
                            data["MWrite1"] = w[7];

                            // Row 2 (Starts at D3827 -> offset 18)
                            data["PositionWrite2"] = GetInt32(w, 18); // D3827
                            data["SpeedWrite2"] = w[20];
                            data["AccWrite2"] = w[21];
                            data["DeccWrite2"] = w[22];
                            data["DwellTimeWrite2"] = w[23];
                            data["AuxWrite2"] = w[24];
                            data["MWrite2"] = w[25];

                            // Row 3 (Starts at D3845 -> offset 36)
                            data["PositionWrite3"] = GetInt32(w, 36); // D3845
                            data["SpeedWrite3"] = w[38];
                            data["AccWrite3"] = w[39];
                            data["DeccWrite3"] = w[40];
                            data["DwellTimeWrite3"] = w[41];
                            data["AuxWrite3"] = w[42];
                            data["MWrite3"] = w[43];
                        }

                        // 3. READ TORQUE & TOLERANCE
                        // SetTolerance: D3950, Torques: D3994/D3998
                        var miscBlock = await _slmpService.ReadInt16BlockAsync("D3950", 50);
                        if (miscBlock.IsSuccess)
                        {
                            var m = miscBlock.Content;
                            data["SetTolerance"] = m[0];  // D3950
                            data["MaxForwardTorque"] = m[44]; // D3994 (Offset 44 from 3950)
                            data["MaxReverseTorque"] = m[48]; // D3998 (Offset 48 from 3950)
                        }

                        await _hubContext.Clients.Group("PointTable3").SendAsync("ReceivePlcData", data);
                    }
                    // --- MONITOR POINT TABLE 4 ---
                    if (_tracker.IsPageActive("PointTable4"))
                    {
                        var data = new Dictionary<string, object>();

                        // 1. READ "READ-ONLY" BLOCK (D1882 - D1910)
                        // Covers PositionRead1 (D1882) to MRead3 (D1907)
                        var readBlock = await _slmpService.ReadInt16BlockAsync("D1882", 30);
                        if (readBlock.IsSuccess)
                        {
                            var r = readBlock.Content;
                            // Row 1 (Offset 0 relative to D1882)
                            data["PositionRead1"] = GetInt32(r, 0); // D1882
                            data["SpeedRead1"] = r[2];           // D1884
                            data["AccRead1"] = r[3];
                            data["DeccRead1"] = r[4];
                            data["DwellTimeRead1"] = r[5];
                            data["AuxRead1"] = r[6];
                            data["MRead1"] = r[7];

                            // Row 2 (Starts at D1891 -> offset 9)
                            data["PositionRead2"] = GetInt32(r, 9); // D1891
                            data["SpeedRead2"] = r[11];
                            data["AccRead2"] = r[12];
                            data["DeccRead2"] = r[13];
                            data["DwellTimeRead2"] = r[14];
                            data["AuxRead2"] = r[15];
                            data["MRead2"] = r[16];

                            // Row 3 (Starts at D1900 -> offset 18)
                            data["PositionRead3"] = GetInt32(r, 18); // D1900
                            data["SpeedRead3"] = r[20];
                            data["AccRead3"] = r[21];
                            data["DeccRead3"] = r[22];
                            data["DwellTimeRead3"] = r[23];
                            data["AuxRead3"] = r[24];
                            data["MRead3"] = r[25];
                        }

                        // 2. READ "WRITE" BLOCK (D3863 - D3910)
                        // Covers PositionWrite1 (D3863) to MWrite3 (D3906)
                        var writeBlock = await _slmpService.ReadInt16BlockAsync("D3863", 50);
                        if (writeBlock.IsSuccess)
                        {
                            var w = writeBlock.Content;

                            // Row 1 (Offset 0 relative to D3863)
                            data["PositionWrite1"] = GetInt32(w, 0); // D3863
                            data["SpeedWrite1"] = w[2];           // D3865
                            data["AccWrite1"] = w[3];
                            data["DeccWrite1"] = w[4];
                            data["DwellTimeWrite1"] = w[5];
                            data["AuxWrite1"] = w[6];
                            data["MWrite1"] = w[7];

                            // Row 2 (Starts at D3881 -> offset 18)
                            data["PositionWrite2"] = GetInt32(w, 18); // D3881
                            data["SpeedWrite2"] = w[20];
                            data["AccWrite2"] = w[21];
                            data["DeccWrite2"] = w[22];
                            data["DwellTimeWrite2"] = w[23];
                            data["AuxWrite2"] = w[24];
                            data["MWrite2"] = w[25];

                            // Row 3 (Starts at D3899 -> offset 36)
                            data["PositionWrite3"] = GetInt32(w, 36); // D3899
                            data["SpeedWrite3"] = w[38];
                            data["AccWrite3"] = w[39];
                            data["DeccWrite3"] = w[40];
                            data["DwellTimeWrite3"] = w[41];
                            data["AuxWrite3"] = w[42];
                            data["MWrite3"] = w[43];
                        }

                        // 3. READ TORQUE & TOLERANCE
                        // SetTolerance: D3962, Torques: D3994/D3998
                        var miscBlock = await _slmpService.ReadInt16BlockAsync("D3962", 40);
                        if (miscBlock.IsSuccess)
                        {
                            var m = miscBlock.Content;
                            data["SetTolerance"] = m[0];  // D3962
                            data["MaxForwardTorque"] = m[32]; // D3994 (Offset 32 from 3962)
                            data["MaxReverseTorque"] = m[36]; // D3998 (Offset 36 from 3962)
                        }

                        await _hubContext.Clients.Group("PointTable4").SendAsync("ReceivePlcData", data);
                    }
                    // --- 1. HANDLE PAGE NAVIGATION LOGIC ---
                    var navResult = await _slmpService.ReadInt16Async("D1"); // Read navigation register
                    if (navResult.IsSuccess)
                    {
                        int currentNavValue = navResult.Content;
                        if (currentNavValue != _lastNavigationValue)
                        {
                            _lastNavigationValue = currentNavValue; // Update state
                            // Check if the value matches the condition to navigate
                            if (currentNavValue == 11)
                            {
                                _logger.LogInformation("Navigation trigger detected! Sending command.");
                                await _hubContext.Clients.All.SendAsync("NavigateToPage", "/PowerMainScreen");
                            }
                        }
                    }

                    // --- 2. HANDLE ALARM POP-UP LOGIC ---
                    var alarmResult = await _slmpService.ReadInt16Async("D102"); // Read alarm register
                    if (alarmResult.IsSuccess)
                    {
                        int currentAlarmCode = alarmResult.Content;
                        if (currentAlarmCode != _lastAlarmCode)
                        {
                            _lastAlarmCode = currentAlarmCode; // Update state
                            _logger.LogInformation($"PLC Alarm code changed to {currentAlarmCode}. Sending update.");
                            // Send the new code to all browsers. The JS will handle the rest.
                            await _hubContext.Clients.All.SendAsync("UpdateAlarmState", currentAlarmCode);
                        }
                    }
                    var logTriggerResult = await _slmpService.ReadBoolAsync("M160", 1);

                    // We must check if the read succeeded AND if the *first item*
                    // in the returned list is 'true'.
                    if (logTriggerResult.IsSuccess && logTriggerResult.Content != null && logTriggerResult.Content.FirstOrDefault() == true)
                    {
                        _logger.LogInformation("M160 data log trigger detected. Logging data...");

                        // Call your existing function
                        await LogPlcDataAsync();

                        _logger.LogInformation("Resetting M160 trigger bit.");

                        // Reset the bit. We must pass an *array* of bools to write.
                        var resetResult = await _slmpService.WriteBoolAsync("M160", false);

                        if (!resetResult.IsSuccess)
                        {
                            _logger.LogWarning("Failed to reset M160 trigger bit. It may re-trigger on the next cycle.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in PLC Monitor Service.");
                }

                // Wait before the next check
                await Task.Delay(500, stoppingToken);
            }
        }

        private async Task LogPlcDataAsync()
        {
            try
            {
                // We must create a "scope" to get a DbContext in a background service
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<LogDbContext>();

                    // 1. Read all required 32-bit values in parallel
                    var torqueTask = _slmpService.ReadInt32Async("D1982");
                    var positionTask = _slmpService.ReadInt32Async("D1984");
                    var rpmTask = _slmpService.ReadInt32Async("D1980");
                    var brakerNoTask = _slmpService.ReadInt32Async("D1988");
                    var nameTask = _slmpService.ReadStringAsync("D4450", 20);

                    await Task.WhenAll(torqueTask, positionTask, rpmTask, brakerNoTask, nameTask);

                    // 3. Get all results

                    var torqueResult = await torqueTask;
                    var positionResult = await positionTask;
                    var rpmResult = await rpmTask;
                    var brakerNoResult = await brakerNoTask;
                    var nameResult = await nameTask;
                    // 2. Check if all essential reads were successful
                    if (!torqueResult.IsSuccess || !positionResult.IsSuccess || !rpmResult.IsSuccess || !brakerNoResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to read one or more values for logging.");
                        return;
                    }

                    // 3. Get the Breaker Number and find its name
                    string brakerName = "---"; // Default name
                    if (nameResult.IsSuccess)
                    {
                        brakerName = nameResult.Content.TrimEnd('\0');
                    }
                    else
                    {
                        _logger.LogWarning("Failed to read Breaker Description from D4450.");
                    }

                    // 4. Create the single log entry with all data
                    var newLog = new DataLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Torque = torqueResult.IsSuccess ? torqueResult.Content : 0,
                        Position = positionResult.IsSuccess ? positionResult.Content  : 0, // Assuming decimal format
                        RPM = rpmResult.IsSuccess ? rpmResult.Content : 0,
                        BrakerNo = brakerNoResult.IsSuccess ? brakerNoResult.Content : 0,
                        BreakerDescription = brakerName // Use the string from D4450
                    };

                    // 5. Add and save the new row
                    await dbContext.DataLogs.AddAsync(newLog);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Successfully logged data snapshot to database.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log PLC data to database.");
            }
        }
        private int GetInt32(short[] data, int startIndex)
        {
            if (startIndex + 1 >= data.Length) return 0;         
        
            return (int)(data[startIndex] + (data[startIndex + 1] << 16));
        }
    }
 }
