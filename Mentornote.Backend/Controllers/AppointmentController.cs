using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Mvc;


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
          

            BackgroundJob job = new BackgroundJob()
            {
                JobType = "AppointmentFileUpload",
                ReferenceType = "Appointment",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Payload = $"UserId: {UserId}, Title: {appointmentDTO.Title}"
            };

            job.Id = dBServices.CreateJob(job);

            Appointment appointment = new Appointment()
            {
                UserId = UserId,
                Title = appointmentDTO.Title,
                Description = appointmentDTO.Description,
                StartTime = appointmentDTO.StartTime,
                EndTime = appointmentDTO.EndTime,
                Organizer = appointmentDTO.Organizer,
                Date = appointmentDTO.Date,
                Status = "Scheduled"
            };

            var AppointmentId = dBServices.AddAppointment(appointment, UserId);

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    throw new Exception("No file uploaded.");
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
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    job.Status = "Processing";
                    dBServices.UpdateJob(job);
                    foreach (var path in documentPaths)
                    { 
                        AppointmentDocuments newDoc = new AppointmentDocuments
                        {
                            AppointmentId = AppointmentId,
                            UserId = UserId,
                            DocumentPath = path,
                        };
                        documentID = dBServices.AddAppointmentDocument(newDoc);
                        await fileServices.ProcessFileAsync(path, documentID, AppointmentId);
                    }

                    job.Status = "Completed";
                    job.ResultMessage = "Appointment uploaded and processed.";
                    dBServices.UpdateJob(job);
                }
                catch (Exception ex)
                {
                    job.Status = "Failed";
                    job.ResultMessage = ex.Message;
                    dBServices.UpdateJob(job);
                }
            });



            // 4️⃣  Respond immediately so client can poll
            return Accepted(new { jobId = job.Id });
        }

        [HttpGet("status/{jobId}")]
        public IActionResult GetStatus(long jobId)
        {
            try
            {
                var job = dBServices.GetJobStatus(jobId);

                if (job == null)
                    return NotFound(new { ResultMessage = "Job not found" });

                return Ok(new
                {
                    job.Id,
                    job.JobType,
                    job.Status,
                    job.ResultMessage,
                    job.CreatedAt,
                    job.StartedAt,
                    job.CompletedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ResultMessage = $"Error fetching job status: {ex.Message}"});
            }
        }

        [HttpDelete("{appointmentId}")]
        public async Task<IActionResult> DeleteAppointment(int appointmentId)
        {
            try
            {
                await dBServices.DeleteAppointmentAsync(appointmentId);
                return Ok(new { message = "Appointment deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

