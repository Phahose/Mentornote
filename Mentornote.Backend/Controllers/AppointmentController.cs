using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Numerics;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentController : Controller
    {
        DBServices dBServices = new DBServices();
        FileServices fileServices = new FileServices();

  

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] AppointmentDTO appointmentDTO)
        {
            var files = appointmentDTO.Files; 
            var UserId = appointmentDTO.UserId;
            List<string> documentPaths = new();
            int documentID = 0;

            Appointment appointment = new Appointment()
            {
                UserId = UserId,
                Title = appointmentDTO.Title,
                Description = appointmentDTO.Description,
                StartTime = appointmentDTO.StartTime,
                EndTime = appointmentDTO.EndTime,
                Status = "Scheduled"
            };

            var AppointmentId = dBServices.AddAppointment(appointment, UserId);

            //var AppointmentId = 1; // Placeholder for AppointmentId

            foreach (var file in files)
            {
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
                documentPaths.Add(filePath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                AppointmentDocuments newDoc = new AppointmentDocuments
                {
                    AppointmentId = AppointmentId,
                    UserId = UserId,
                    DocumentPath = filePath,
                };

                documentID = dBServices.AddAppointmentDocument(newDoc);

                await fileServices.ProcessFileAsync(filePath, documentID);
                
            }

            return Ok();
        }
    }
}

