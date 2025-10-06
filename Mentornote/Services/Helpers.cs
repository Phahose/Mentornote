#nullable disable
using DocumentFormat.OpenXml.Packaging;
using Mentornote.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace Mentornote.Services
{
    public class Helpers
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public Helpers(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public List<string> ChunkText(string fullText, int maxChunkSize = 1500)
        {
            var chunks = new List<string>();
            for (int i = 0; i < fullText.Length; i += maxChunkSize)
            {
                var chunk = fullText.Substring(i, Math.Min(maxChunkSize, fullText.Length - i));
                chunks.Add(chunk);
            }
            return chunks;
        }

        public static string DetectFileType(IFormFile file)
        {
            byte[] header = new byte[8];
            using (var stream = file.OpenReadStream())
            {
                stream.Read(header, 0, header.Length);
            }

            // PDF files start with "%PDF"
            if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
                return "pdf";

            // DOCX (and other Office files) start with "PK" (zipped format)
            if (header[0] == 0x50 && header[1] == 0x4B)
                return "docx";

            // Plain text files often contain readable ASCII/UTF8
            if (header.All(b => b == 0x09 || b == 0x0A || b == 0x0D || (b >= 0x20 && b <= 0x7E)))
                return "txt";

            return "unknown";
        }


        public string ExtractText(IFormFile file, string filePath = null)
        {
            string fileType = DetectFileType(file);
            Stream stream;

            if (fileType == "pdf")
            {
                Stream pdfstream = file.OpenReadStream();
                using var pdf = PdfDocument.Open(pdfstream);
                var sb = new StringBuilder();

                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }

                return sb.ToString();
            }
            else if (fileType == "txt")
            {            
                if (file != null)
                {
                    stream = file.OpenReadStream();
                }
                else
                {
                    stream = File.OpenRead(filePath);
                }
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            else if (fileType == "docx")
            {
                
                if (file != null)
                {
                    stream = file.OpenReadStream();
                }
                else
                {
                    stream = File.OpenRead(filePath);
                }
                using var doc = WordprocessingDocument.Open(stream, false);

                return string.Join(Environment.NewLine,
                    doc.MainDocumentPart.Document.Body
                        .Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                        .Select(p => p.InnerText));
            }
            else
            {
                throw new NotSupportedException("Unsupported file type. Only PDF and TXT are supported.");
            }
            
        }

        public async Task<string> SaveNoteFileAsync(IFormFile uploadedNote, string textContent = null, string fileExtension = ".txt")
        {
         /*   if (uploadedNote == null || uploadedNote.Length == 0)
                return null;*/

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notes");

            // Create folder if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileName;
            string filePath;

            // Case 1: Saving a physical uploaded file
            if (uploadedNote != null && uploadedNote.Length > 0)
            {
                fileName = $"{Guid.NewGuid()}_{uploadedNote.FileName}";
                filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedNote.CopyToAsync(stream);
                }
            }

            // Case 2: Saving text content (e.g. transcript)
            else if (!string.IsNullOrEmpty(textContent))
            {
                fileName = $"{Guid.NewGuid()}_Transcript{fileExtension}";
                filePath = Path.Combine(uploadsFolder, fileName);

                await File.WriteAllTextAsync(filePath, textContent);
            }
            else
            {
                throw new ArgumentException("Either a file or text content must be provided.");
            }

          
            return filePath.Replace("\\", "/");
        }

        //  Helper to generate summary
        public async Task<string> GenerateSummaryFromText(string text)
        {
            using var client = new HttpClient();
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                new { role = "system", content = "You are a summarization assistant for lecture transcripts." },
                new { role = "user", content = $"Summarize the following text:\n{text}" }
            }
            };

            // Building API Request
            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return $"OpenAI API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }

            var responseContentString = await response.Content.ReadAsStringAsync();
            // Parse JSON and extract message content
            using var doc = JsonDocument.Parse(responseContentString);
            var summary = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                .Trim();


            return summary;
        }
    }
}
