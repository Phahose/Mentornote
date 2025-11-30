#nullable disable
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml;
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace Mentornote.Backend.Services
{
    public class FileServices
    {
        private readonly HttpClient _httpClient;
        private readonly RagService _ragService;
        DBServices dBServices = new DBServices();

        public FileServices(RagService ragService)
        {
            _httpClient = new();
            _ragService = ragService;
        }

        public async Task ProcessFileAsync(string filePath, int documentID, int appointmentId)
        {
            // 1️⃣ Extract text
            string text = ExtractText(filePath);
            var chunks = ChunkText(text);
            int chunkIndex = 0;
            try
            {
                foreach (var chunk in chunks)
                {
                    var vector = await _ragService.GetDocumentEmbeddingAsync(chunk);

                    AppointmentDocumentEmbedding embedding = new AppointmentDocumentEmbedding
                    {
                        AppointmentDocumentId = documentID,
                        ChunkIndex = chunkIndex++,
                        ChunkText = chunk,
                        Vector = vector,
                        AppointmentId = appointmentId,
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
            var ext = System.IO.Path.GetExtension(filePath).ToLower();
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

        public string ComputeHashFromFilePath(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(stream);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        public string ComputeFileHash(Stream fileStream)
        {
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(fileStream);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}
