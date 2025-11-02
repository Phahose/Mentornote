using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Mentornote.Backend.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentController : Controller
    {
        DBServices dBServices = new DBServices();
        FileServices fileServices = new FileServices();

  

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] AppointmentFileUploadDTO appointmentFileUpload)
        {
            var file = appointmentFileUpload.File;
            var AppointmentId = appointmentFileUpload.AppointmentId;
            var UserId = appointmentFileUpload.UserId;
            
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            // 1️⃣ Save to local directory
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "UploadedFiles");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var filePath = Path.Combine(uploadDir, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = fileServices.ProcessFileAsync(filePath);


            Appointment appointment = new Appointment()
            {
                Id = AppointmentId,
                UserId = UserId,
                Title = "Sample Appointment",
                Description = "This is a sample appointment description.",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                Status = "Scheduled"
            };


            AppointmentDocuments newNote = new AppointmentDocuments
            {
                AppointmentId = AppointmentId,
                UserId = UserId,
                DocumentPath = filePath,
                Vector = result.Result.ToString(),
            };

            // Combe back here to fix
            return Ok(new { File = file.FileName, Path = filePath });
        }
    }
}

