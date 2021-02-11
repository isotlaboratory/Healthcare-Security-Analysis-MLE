
using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CDTS_PROJECT.Pages
{
    public class DownloadModel : PageModel
    {
        private readonly ILogger<DownloadModel> _logger;
        private IWebHostEnvironment _environment;

        public DownloadModel(ILogger<DownloadModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public void OnGet()
        {
        }
    }
}
