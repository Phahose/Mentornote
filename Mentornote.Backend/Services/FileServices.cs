#nullable disable
using DocumentFormat.OpenXml.Packaging;

namespace Mentornote.Backend.Services
{
    public class FileServices
    {
        private readonly HttpClient _httpClient;

        public FileServices()
        {
            _httpClient = new();
        }

        public async Task<(string TextChunk, string VectorJson)> ProcessFileAsync(string filePath)
        {
            // 1️⃣ Extract text
            string text = ExtractText(filePath);

            // 2️⃣ Send to embedding API (Gemini, OpenAI, etc.)
            var vector = await _httpClient.PostAsync("http://localhost:5085/api/gemini/notevector", new StringContent(text)).Result.Content.ReadAsStringAsync();

            return (text, vector);
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
      
    }
}
