using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BBD.BodyMonitor.Web.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    /// <summary>
    /// Model for the Error page. It provides information about the error that occurred.
    /// </summary>
    public class ErrorModel : PageModel
    {
        /// <summary>
        /// Gets or sets the request identifier associated with the current request.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Gets a value indicating whether to show the request identifier.
        /// It returns true if the RequestId is not null or empty.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorModel"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles the GET request for the error page.
        /// It sets the RequestId based on the current Activity Id or the HttpContext TraceIdentifier.
        /// This information is then displayed on the error page.
        /// </summary>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}