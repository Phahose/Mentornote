#nullable disable
using DocumentFormat.OpenXml.Packaging;
using Mentornote.Backend.Models;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Mentornote.Backend.Services
{
    public class FileServices
    {
        private readonly HttpClient _httpClient;
        DBServices dBServices = new DBServices();

        public FileServices()
        {
            _httpClient = new();
        }

        public async Task ProcessFileAsync(string filePath, int documentID)
        {
            // 1️⃣ Extract text
            string text = ExtractText(filePath);
            var chunks = ChunkText(text);
            int chunkIndex = 0;
            try
            {
                foreach (var chunk in chunks)
                {
                    var json = JsonSerializer.Serialize(new { Content = chunk });
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 2️⃣ Send to embedding API (Gemini, OpenAI, etc.)
                    var response = await _httpClient.PostAsync("http://localhost:5085/api/gemini/notevector", content);

                    var vector = await response.Content.ReadAsStringAsync();

                    AppointmentDocumentEmbedding embedding = new AppointmentDocumentEmbedding
                    {
                        AppointmentDocumentId = documentID,
                        ChunkIndex = chunkIndex++,
                        ChunkText = chunk,
                        Vector = vector
                    };

                    // Save embedding to DB
                    dBServices.AddAppointmentDocumentEmbedding(embedding);
                }

            }
            catch (Exception)
            {

                throw;
            }  
        }

        public string ExtractText(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".txt")
            {
                return File.ReadAllText(filePath);
            }
            if (ext == ".pdf")
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath);
                return string.Join(" ", pdf.GetPages().Select(p => p.Text));
            }
            if (ext == ".docx")
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                return string.Join(" ", doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
            }

            throw new NotSupportedException($"Unsupported file type {ext}");
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
    }
}
