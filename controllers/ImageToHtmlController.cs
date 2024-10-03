using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using dotenv.net;
using Microsoft.AspNetCore.Mvc;

namespace ImageToHtmlService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageToHtmlController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ImageToHtmlController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? imageUrl)
        {
            if (imageUrl != null && imageUrl.Equals("test"))
            {
                return Ok("Test");
            }
            // Load environment variables from .env file
            DotEnv.Load();

            // Access the environment variables
            string endpoint = Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_ENDPOINT") ?? "";
            string apiKey = Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_KEY") ?? "";

            Stream imageStream;

            if (string.IsNullOrEmpty(imageUrl))
            {
                // Use local file if no imageUrl is provided
                string imagePath = "population.png";
                imageStream = new FileStream(imagePath, FileMode.Open);
            }
            else
            {
                // Download image from URL
                var response = await _httpClient.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to download image from URL.");
                }
                imageStream = await response.Content.ReadAsStreamAsync();
            }

            var credential = new AzureKeyCredential(apiKey);
            var client = new DocumentAnalysisClient(new Uri(endpoint), credential);

            AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", imageStream);
            AnalyzeResult result = operation.Value;

            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine("<html>");
            htmlBuilder.AppendLine("<body>");
            htmlBuilder.AppendLine("<table border='1'>");

            // Extract table data
            foreach (var table in result.Tables)
            {
                for (int i = 0; i < table.RowCount; i++)
                {
                    htmlBuilder.AppendLine("<tr>");
                    foreach (var cell in table.Cells)
                    {
                        if (cell.RowIndex == i)
                        {
                            htmlBuilder.AppendLine($"<td>{cell.Content}</td>");
                        }
                    }
                    htmlBuilder.AppendLine("</tr>");
                }
            }

            htmlBuilder.AppendLine("</table>");
            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            return Content(htmlBuilder.ToString(), "text/html");
        }
    }
}