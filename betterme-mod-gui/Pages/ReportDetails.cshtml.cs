using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MultimediaService;
namespace MyApp.Namespace
{
    public class ReportDetailsModel : PageModel
    {
private readonly IHttpClientFactory    _http;
        private readonly MultimediaService.MultimediaService.MultimediaServiceClient _grpc;

        public ReportDetailsModel(
            IHttpClientFactory http,
            MultimediaService.MultimediaService.MultimediaServiceClient grpc)
        {
            _http = http;
            _grpc = grpc;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = "";

        public ReportDto? Report { get; private set; }
        public PostDto?   Post   { get; private set; }
        public string     ImageUrl { get; private set; } = "/images/default.png";
        public string?    Message  { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1) fetch report
            var repClient = _http.CreateClient("ReportsApi");
            var token = HttpContext.Session.GetString("token");
            if (!string.IsNullOrEmpty(token))
                repClient.DefaultRequestHeaders.Authorization = 
                  new AuthenticationHeaderValue("Bearer", token);

            var repResp = await repClient.GetAsync($"/reports/{Id}");
            if (repResp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();
            repResp.EnsureSuccessStatusCode();
            Report = await repResp.Content.ReadFromJsonAsync<ReportDto>();

            // 2) fetch post body
            var postClient = _http.CreateClient("PostsApi");
            var postResp = await postClient.GetAsync($"/posts/{Report!.PostId}");
            if (postResp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound("Post not found");
            postResp.EnsureSuccessStatusCode();
            Post = await postResp.Content.ReadFromJsonAsync<PostDto>();

            // 3) fetch image via gRPC
            var ms = new MemoryStream();
            using var call = _grpc.GetPostMultimedia(new PostInfo { Id = Post!.Id });
            while (await call.ResponseStream.MoveNext())
                call.ResponseStream.Current.Chunk.WriteTo(ms);
            ImageUrl = "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());

            return Page();
        }

        public async Task<IActionResult> OnPostEvaluateAsync(string action)
        {
            // action == "delete" => postState=Deleted   (Ok=false)
            // action == "keep"   => postState=Published (Ok=true)
            var ok = action == "keep";

            var repClient = _http.CreateClient("ReportsApi");
            var token = HttpContext.Session.GetString("token");
            if (!string.IsNullOrEmpty(token))
                repClient.DefaultRequestHeaders.Authorization = 
                  new AuthenticationHeaderValue("Bearer", token);

            var patch = new { ok = ok };
            var resp  = await repClient.PatchAsJsonAsync($"/reports/{Id}", patch);
            if (resp.IsSuccessStatusCode)
            {
                Message = ok
                  ? "Reporte rechazado, publicación restablecida."
                  : "Publicación eliminada satisfactoriamente.";
            }
            else
            {
                Message = "Error al procesar: " + await resp.Content.ReadAsStringAsync();
            }

            // reload to show updated status:
            return await OnGetAsync() is PageResult pr ? pr : RedirectToPage();
        }

        public class ReportDto
        {
          public string Id                { get; set; } = "";
          public string PostId            { get; set; } = "";
          public string ReporterUsername  { get; set; } = "";
          public string ReportedUsername  { get; set; } = "";
          public DateTime ReportDate      { get; set; }
          public string Reason            { get; set; } = "";
        }

        public class PostDto
        {
          public string Id          { get; set; } = "";
          public string Title       { get; set; } = "";
          public string Description { get; set; } = "";
          public string Category    { get; set; } = "";
          public string UserId      { get; set; } = "";
        }
    }
}
