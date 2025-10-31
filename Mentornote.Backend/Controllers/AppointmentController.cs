using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentController : Controller
    {
        DBServices DBServices = new DBServices();
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] int AppointmentId, [FromForm] int UserId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // 1️⃣ Save to local directory
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "UploadedFiles");
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2️⃣ Create a text chunk (for now, raw filename as placeholder)
            string chunk = $"Uploaded file: {file.FileName}";

            // 3️⃣ Vector placeholder (you’ll replace this later with Gemini embeddings)
            string vector = "[0.0, 0.0, 0.0]";

            // 4️⃣ Insert into AppointmentNotesVectors table using stored procedure
            AppointmentNote appointmentNote = new AppointmentNote
            {
                AppointmentId = AppointmentId,
                UserId = UserId,
                Chunk = chunk,
                Vector = vector,
                DocumentPath = filePath
            };

            return Ok(new { File = file.FileName, Path = filePath });
        }
    }
}

