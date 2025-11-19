using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;


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
            var userId = appointmentDTO.UserId;
            List<string> documentPaths = new();
            int documentID = 0;

            BackgroundJob job = new BackgroundJob()
            {
                JobType = "AppointmentFileUpload",
                ReferenceType = "Appointment",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Payload = $"UserId: {userId}, Title: {appointmentDTO.Title}"
            };

            job.Id = dBServices.CreateJob(job);

            Appointment appointment = new Appointment()
            {
                UserId = userId,
                Title = appointmentDTO.Title,
                Description = appointmentDTO.Description,
                StartTime = appointmentDTO.StartTime,
                EndTime = appointmentDTO.EndTime,
                Organizer = appointmentDTO.Organizer,
                Date = appointmentDTO.Date,
                Status = "Scheduled"
            };

            var appointmentId = dBServices.AddAppointment(appointment, userId);

            // Save files to disk (synchronous)
            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                     throw new Exception("No file uploaded.");
                }
                   

                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "UploadedFiles");
                Directory.CreateDirectory(uploadDir);

                var filePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{file.FileName}");
                documentPaths.Add(filePath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            //  Heavy work: do hashing + embeddings in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    job.Status = "Processing";
                    dBServices.UpdateJob(job);

                    foreach (var path in documentPaths)
                    {
                        // compute hash SAFELY
                        string hash = fileServices.ComputeHashFromFilePath(path);

                        var newDoc = new AppointmentDocument
                        {
                            AppointmentId = appointmentId,
                            UserId = userId,
                            DocumentPath = path,
                            FileHash = hash
                        };

                        documentID = dBServices.AddAppointmentDocument(newDoc);

                        await fileServices.ProcessFileAsync(path, documentID, appointmentId);
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

            return Accepted(new { jobId = job.Id });
        }


        [HttpPut("update/{appointmentId}")]
        public IActionResult UpdateAppointment(int appointmentId, [FromForm] AppointmentDTO appointmentDTO)
        {
            try
            {
                var uploadedFiles = appointmentDTO.Files;
                var userId = appointmentDTO.UserId;

                BackgroundJob job = new BackgroundJob()
                {
                    JobType = "AppointmentFileUpload",
                    ReferenceType = "Appointment",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    Payload = $"UserId: {userId}, Title: {appointmentDTO.Title}"
                };

                job.Id = dBServices.CreateJob(job);

                // Update appointment info
                Appointment updatedAppointment = new Appointment
                {
                    Id = appointmentId,
                    UserId = appointmentDTO.UserId,
                    Title = appointmentDTO.Title,
                    Description = appointmentDTO.Description,
                    StartTime = appointmentDTO.StartTime,
                    EndTime = appointmentDTO.EndTime,
                    Organizer = appointmentDTO.Organizer,
                    Date = appointmentDTO.Date,
                    Status = appointmentDTO.Status
                };

                dBServices.UpdateAppointment(updatedAppointment, userId);

                // Run async processing
                _ = Task.Run(async () =>
                {
                    try
                    {
                        job.Status = "Processing";
                        dBServices.UpdateJob(job);

                        // Get existing documents
                        List<AppointmentDocument> existingDocs =
                            await dBServices.GetAppointmentDocumentsById(appointmentId, userId);

                        //Hash uploaded files first
                        var uploadedInfos = new List<(IFormFile File, string Hash)>();

                        foreach (var file in uploadedFiles)
                        {
                            if (file == null || file.Length == 0)
                            {
                                continue;
                            }

                            using var stream = file.OpenReadStream();
                            string hash = fileServices.ComputeFileHash(stream);
                            uploadedInfos.Add((file, hash));
                        }

                        //Remove duplicates among uploaded files
                        var uniqueUploaded = uploadedInfos
                                            .GroupBy(x => x.Hash)
                                            .Select(g => g.First())
                                            .ToList();

                        // Remove files that already exist in DB
                        uniqueUploaded = uniqueUploaded
                                        .Where(x => !existingDocs.Any(d => d.FileHash == x.Hash))
                                        .ToList();

                        // Save and process each new file
                        foreach (var (file, hash) in uniqueUploaded)
                        {
                            // Save file to server
                            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "UploadedFiles");
                            Directory.CreateDirectory(uploadDir);

                            var savedPath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{file.FileName}");

                            using (var stream = new FileStream(savedPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Insert into DB
                            var newDoc = new AppointmentDocument
                            {
                                AppointmentId = appointmentId,
                                UserId = userId,
                                DocumentPath = savedPath,
                                FileHash = hash
                            };

                            int docId = dBServices.AddAppointmentDocument(newDoc);

                            // Generate embeddings
                            await fileServices.ProcessFileAsync(savedPath, docId, appointmentId);
                        }

                        job.Status = "Completed";
                        job.ResultMessage = "Appointment updated and processed.";
                        dBServices.UpdateJob(job);
                    }
                    catch (Exception ex)
                    {
                        job.Status = "Failed";
                        job.ResultMessage = ex.Message;
                        dBServices.UpdateJob(job);
                    }
                });

                return Accepted(new { jobId = job.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
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


    }
}

