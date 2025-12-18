using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using FX5u_Web_HMI_App.Data;
using System.Text.Json;

namespace FX5u_Web_HMI_App.Pages
{
    public class HistoryModel : PageModel
    {
        private readonly ILogger<HistoryModel> _logger;
        private readonly LogDbContext _context;

        // Chart data strings (JSON)
        public string ChartDataLabelsLocal { get; set; }
        public string ChartDataValues_Torque { get; set; }
        public string ChartDataValues_Position { get; set; }
        public string ChartDataValues_RPM { get; set; }
        public string ChartDataValues_BrakerNo { get; set; }

        // Internal UTC window
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }

        // UI filter fields (LOCAL time)
        [BindProperty(SupportsGet = true)] public DateTime DisplayStart { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime DisplayEnd { get; set; }

        [BindProperty(SupportsGet = true)] public int ClientOffsetMinutes { get; set; }
        [BindProperty(SupportsGet = true)] public string Range { get; set; }

        // Paging
        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public int TotalPages { get; set; }

        public HistoryModel(ILogger<HistoryModel> logger, LogDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task OnGetAsync()
        {
            // 1) Decide UTC window and LOCAL fields
            if (!string.IsNullOrEmpty(Range))
            {
                EndDateUtc = DateTime.UtcNow;
                StartDateUtc = Range switch
                {
                    "1h" => EndDateUtc.AddHours(-1),
                    "24h" => EndDateUtc.AddHours(-24),
                    "7d" => EndDateUtc.AddDays(-7),
                    _ => EndDateUtc.AddHours(-24)
                };
                var ist = GetIndiaTimeZone();
                DisplayStart = TimeZoneInfo.ConvertTimeFromUtc(StartDateUtc, ist);
                DisplayEnd = TimeZoneInfo.ConvertTimeFromUtc(EndDateUtc, ist);
            }
            else if (DisplayStart == DateTime.MinValue || DisplayEnd == DateTime.MinValue)
            {
                var ist = GetIndiaTimeZone();
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
                DisplayEnd = nowIst;
                DisplayStart = nowIst.AddHours(-24);

                StartDateUtc = DateTime.SpecifyKind(DisplayStart.AddMinutes(ClientOffsetMinutes), DateTimeKind.Utc);
                EndDateUtc = DateTime.SpecifyKind(DisplayEnd.AddMinutes(ClientOffsetMinutes), DateTimeKind.Utc);
            }
            else
            {
                StartDateUtc = DateTime.SpecifyKind(DisplayStart.AddMinutes(ClientOffsetMinutes), DateTimeKind.Utc);
                EndDateUtc = DateTime.SpecifyKind(DisplayEnd.AddMinutes(ClientOffsetMinutes), DateTimeKind.Utc);
            }

            // 2) Query
            var query = _context.DataLogs
                .Where(log => log.Timestamp >= StartDateUtc && log.Timestamp <= EndDateUtc)
                .OrderBy(log => log.Timestamp);

            int totalCount = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            var page = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // 3) Build Labels (IST)
            var istTz = GetIndiaTimeZone();
            var labelsLocal = page.Select(log =>
            {
                var utc = DateTime.SpecifyKind(log.Timestamp, DateTimeKind.Utc);
                var ist = TimeZoneInfo.ConvertTimeFromUtc(utc, istTz);
                return ist.ToString("yyyy-MM-dd HH:mm:ss");
            }).ToList();

            ChartDataLabelsLocal = JsonSerializer.Serialize(labelsLocal);
            ChartDataValues_Torque = JsonSerializer.Serialize(page.Select(log => log.Torque));
            ChartDataValues_Position = JsonSerializer.Serialize(page.Select(log => log.Position));
            ChartDataValues_RPM = JsonSerializer.Serialize(page.Select(log => log.RPM));
            ChartDataValues_BrakerNo = JsonSerializer.Serialize(page.Select(log => log.BrakerNo));
        }

        private static TimeZoneInfo GetIndiaTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        }
    }
}