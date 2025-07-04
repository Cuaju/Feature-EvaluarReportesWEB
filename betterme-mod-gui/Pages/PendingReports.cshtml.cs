using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Namespace
{
    public class PendingReportsModel : PageModel
    {
private readonly IHttpClientFactory _http;

        public PendingReportsModel(IHttpClientFactory http)
        {
            _http = http;
        }

        public List<ReportDto> Reports { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // grab token from cookie
            if (!Request.Cookies.TryGetValue("accessToken", out var token))
                return;

            var client = _http.CreateClient("ReportsApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var pending = await client
                    .GetFromJsonAsync<List<ReportDto>>("reports/pending");
                if (pending != null)
                    Reports = pending;
            }
            catch
            {
                // TODO: log or show error
            }
        }

        public class ReportDto
        {
            public string Id                   { get; set; } = "";
            public string PostId               { get; set; } = "";
            public string ReporterUsername     { get; set; } = "";
            public string ReportedUsername     { get; set; } = "";
            public DateTime ReportDate         { get; set; }
            public string Reason               { get; set; } = "";
            public bool   Evaluated            { get; set; }
        }
    }
}
