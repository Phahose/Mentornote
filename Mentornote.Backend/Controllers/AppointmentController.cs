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

                // -----------------------------------
                // UPDATE APPOINTMENT BASIC FIELDS
                // -----------------------------------
                Appointment updatedAppointment = new Appointment
                {
                    Id = appointmentId,
                    UserId = userId,
                    Title = appointmentDTO.Title,
                    Description = appointmentDTO.Description,
                    StartTime = appointmentDTO.StartTime,
                    EndTime = appointmentDTO.EndTime,
                    Organizer = appointmentDTO.Organizer,
                    Date = appointmentDTO.Date,
                    Status = appointmentDTO.Status
                };

                dBServices.UpdateAppointment(updatedAppointment, userId);

                // -----------------------------------
                // PREPARE ALL FILES FOR BACKGROUND TASK
                // Save physically now so background can process safely
                // -----------------------------------

                var savedFiles = new List<(string Path, string OriginalName)>();

                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "UploadedFiles");
                Directory.CreateDirectory(uploadDir);

                if (uploadedFiles != null)
                {
                    foreach (var file in uploadedFiles)
                    {
                        if (file == null || file.Length == 0)
                        {
                            continue;
                        }
                           

                        string safeName = $"{Guid.NewGuid()}_{file.FileName}";
                        string fullPath = Path.Combine(uploadDir, safeName);

                        // SAVE FILE SAFELY NOW — BEFORE BACKGROUND THREAD
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            file.CopyTo(stream);            
                        }

                        savedFiles.Add((fullPath, file.FileName));
                    }
                }

                // -----------------------------------
                // BACKGROUND PROCESSING
                // -----------------------------------

                _ = Task.Run(async () =>
                {
                    try
                    {
                        job.Status = "Processing";
                        dBServices.UpdateJob(job);

                        // Load existing docs
                        var existingDocs = await dBServices.GetAppointmentDocumentsById(appointmentId, userId);

                        // Compute hashes for new files
                        var hashedUploads = new List<(string Path, string Hash)>();
                       
                        
                        foreach (var file in savedFiles)
                        {
                            string newHash = fileServices.ComputeHashFromFilePath(file.Path);
                            hashedUploads.Add((file.Path, newHash));
                        }


                        // Remove duplicates among uploads
                        var uniqueUploads = hashedUploads
                            .GroupBy(x => x.Hash)
                            .Select(g => g.First())
                            .ToList();

                        // Remove files already in DB
                        var newqueUploads = uniqueUploads
                            .Where(u => !existingDocs.Any(d => d.FileHash == u.Hash))
                            .ToList();

                        // Find old documents to delete
                        var badUploads = hashedUploads
                            .Where(d => !newqueUploads.Any(u => u.Hash == d.Hash))
                            .ToList();

                        // -----------------------------------
                        // ADD NEW UNIQUE DOCS
                        // -----------------------------------
                        foreach (var up in newqueUploads)
                        {
                            var newDoc = new AppointmentDocument
                            {
                                AppointmentId = appointmentId,
                                UserId = userId,
                                DocumentPath = up.Path,
                                FileHash = up.Hash
                            };

                            int newDocId = dBServices.AddAppointmentDocument(newDoc);

                            // Process embeddings now
                            await fileServices.ProcessFileAsync(
                                up.Path,
                                newDocId,
                                appointmentId
                            );
                        }

                        // -----------------------------------
                        // DELETE REMOVED DOCUMENT FILES
                        // -----------------------------------
                        foreach (var doc in badUploads)
                        {
                            if (System.IO.File.Exists(doc.Path))
                                System.IO.File.Delete(doc.Path);
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
        public async Task<IActionResult> DeleteAppointment(int appointmentId, [FromQuery]int userId)
        {
            try
            {
                List<AppointmentDocument> docs = await dBServices.GetAppointmentDocumentsById(appointmentId, userId);
                foreach (var doc in docs)
                {
                    if (System.IO.File.Exists(doc.DocumentPath))
                    {
                        System.IO.File.Delete(doc.DocumentPath);
                    }
                }
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

