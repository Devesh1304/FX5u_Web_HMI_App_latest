using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using FX5u_Web_HMI_App.Data;

namespace FX5u_Web_HMI_App.Pages
{
    public class LogTableModel : PageModel
    {
        private readonly ILogger<LogTableModel> _logger;
        private readonly LogDbContext _context;

        public List<HistoricalDataRow> DataRows { get; set; } = new();

        // Filters (GET-bound)
        [BindProperty(SupportsGet = true)] public DateTime StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime EndDate { get; set; }
        [BindProperty(SupportsGet = true)] public int ClientOffsetMinutes { get; set; }

        // Language (GET-bound) - Used to translate breaker names
        [BindProperty(SupportsGet = true)] public string Lang { get; set; } = "en";

        // Pagination (GET-bound)
        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public LogTableModel(ILogger<LogTableModel> logger, LogDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task OnGetAsync()
        {
            try
            {
                // 1) Compute UTC window for querying
                DateTime utcStart, utcEnd;

                if (StartDate == DateTime.MinValue || EndDate == DateTime.MinValue)
                {
                    // Default: last 24h
                    utcEnd = DateTime.UtcNow;
                    utcStart = utcEnd.AddDays(-1);

                    // Update local props so the date pickers show correct time
                    StartDate = utcStart.AddMinutes(-ClientOffsetMinutes);
                    EndDate = utcEnd.AddMinutes(-ClientOffsetMinutes);
                }
                else
                {
                    // Convert Browser Local time to UTC using the offset provided by JS
                    utcStart = DateTime.SpecifyKind(StartDate.AddMinutes(ClientOffsetMinutes), DateTimeKind.Utc);
                    utcEnd = DateTime.SpecifyKind(EndDate.AddMinutes(ClientOffsetMinutes), DateTimeKind.Utc);
                }

                // 2) Query Data
                var query = _context.DataLogs
                    .Where(x => x.Timestamp >= utcStart && x.Timestamp <= utcEnd)
                    .OrderByDescending(x => x.Timestamp);

                // 3) Pagination
                var total = await query.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;

                var page = await query
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // 4) Map to DTO & Convert UTC -> Local
                int offset = ClientOffsetMinutes;
                DataRows = page.Select(log =>
                {
                    var utc = DateTime.SpecifyKind(log.Timestamp, DateTimeKind.Utc);
                    var local = utc.AddMinutes(-offset);
                    return new HistoricalDataRow
                    {
                        Timestamp = utc,
                        LocalTime = local.ToString("dd-MMM-yyyy HH:mm:ss"),
                        Torque = log.Torque,
                        Position = log.Position,
                        RPM = log.RPM,
                        BrakerNo = log.BrakerNo,
                        BreakerDescription = log.BreakerDescription ?? string.Empty
                    };
                }).ToList();

                // 5) Apply Gujarati Translations if selected
                // Logic: Look up the English descriptions in the DB and replace them if a translation exists.
                if (string.Equals(Lang, "gu", StringComparison.OrdinalIgnoreCase) && DataRows.Count > 0)
                {
                    var keys = DataRows
                        .Select(r => r.BreakerDescription)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct()
                        .ToList();

                    if (keys.Count > 0)
                    {
                        var trans = await _context.NameTranslations
                            .Where(t => keys.Contains(t.En))
                            .ToListAsync();

                        var map = trans.ToDictionary(
                            t => t.En ?? string.Empty,
                            t => t.Gu ?? string.Empty,
                            StringComparer.OrdinalIgnoreCase
                        );

                        foreach (var row in DataRows)
                        {
                            if (map.TryGetValue(row.BreakerDescription, out var guText) && !string.IsNullOrWhiteSpace(guText))
                            {
                                row.BreakerDescription = guText;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load LogTable.");
                DataRows = new List<HistoricalDataRow>();
            }
        }
    }

    // DTO Class for the View

}