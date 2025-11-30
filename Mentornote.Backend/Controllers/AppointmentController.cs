#nullable disable
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Claims;


namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentController : Controller
    {
        private readonly FileServices _fileServices;
        private readonly DBServices _dBServices;
        

        public AppointmentController(FileServices fileServices, DBServices dBServices)
        {
            _fileServices = fileServices;   
            _dBServices = dBServices;
        }
        

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadFile([FromForm] AppointmentDTO appointmentDTO)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var files = appointmentDTO.Files;
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

            job.Id = _dBServices.CreateJob(job);

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

            var appointmentId = _dBServices.AddAppointment(appointment, userId);

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
                    _dBServices.UpdateJob(job);

                    foreach (var path in documentPaths)
                    {
                        // compute hash SAFELY
                        string hash = _fileServices.ComputeHashFromFilePath(path);

                        var newDoc = new AppointmentDocument
                        {
                            AppointmentId = appointmentId,
                            UserId = userId,
                            DocumentPath = path,
                            FileHash = hash
                        };

                        documentID = _dBServices.AddAppointmentDocument(newDoc);

                        await _fileServices.ProcessFileAsync(path, documentID, appointmentId);
                    }

                    job.Status = "Completed";
                    job.ResultMessage = "Appointment uploaded and processed.";
                    _dBServices.UpdateJob(job);
                }
                catch (Exception ex)
                {
                    job.Status = "Failed";
                    job.ResultMessage = ex.Message;
                    _dBServices.UpdateJob(job);
                }
            });

            return Accepted(new { jobId = job.Id });
        }


        [HttpPut("update/{appointmentId}")]
        [Authorize]
        public IActionResult UpdateAppointment(int appointmentId, [FromForm] AppointmentDTO appointmentDTO)
        {
            try
            {
                var uploadedFiles = appointmentDTO.Files;
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var removeFilesIds = appointmentDTO.FilesIDsToRemove;

                BackgroundJob job = new BackgroundJob()
                {
                    JobType = "AppointmentFileUpload",
                    ReferenceType = "Appointment",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    Payload = $"UserId: {userId}, Title: {appointmentDTO.Title}"
                };

                job.Id = _dBServices.CreateJob(job);

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

                _dBServices.UpdateAppointment(updatedAppointment, userId);

                // -----------------------------------
                // PREPARE ALL FILES FOR BACKGROUND TASK
                // Save physically now so background can process safely
                // -----------------------------------

                var savedFiles = new List<(string Path, string OriginalName)>();
                var filesToRemove = new List<(string Path, string OriginalName, int fileId)>();

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
                        _dBServices.UpdateJob(job);

                        // Load existing docs
                        var existingDocs = await _dBServices.GetAppointmentDocumentsById(appointmentId, userId);

                        // Compute hashes for new files
                        var hashedUploads = new List<(string Path, string Hash)>();

                        if (removeFilesIds != null)
                        {
                            // Assume FilesToRemove contains the original file names
                            //existingDocs = await dBServices.GetAppointmentDocumentsById(appointmentId, userId);
                            foreach (var fileId in removeFilesIds)
                            {
                                var docsToRemove = existingDocs.Where(d => d.Id == fileId).FirstOrDefault();
                                if (docsToRemove != null)
                                {
                                    if (System.IO.File.Exists(docsToRemove.DocumentPath))
                                    {
                                        System.IO.File.Delete(docsToRemove.DocumentPath);
                                        _dBServices.DeleteAppointmentDocument(docsToRemove.Id, userId);
                                    }

                                }
                            }
                        }

                        foreach (var file in savedFiles)
                        {
                            string newHash = _fileServices.ComputeHashFromFilePath(file.Path);
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
                         .Where(upload => !newqueUploads.Any(accepted => accepted.Path == upload.Path));


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

                            int newDocId = _dBServices.AddAppointmentDocument(newDoc);

                            // Process embeddings now
                            await _fileServices.ProcessFileAsync(
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
                            {
                                System.IO.File.Delete(doc.Path);
                            }
                        }

                       

                        job.Status = "Completed";
                        job.ResultMessage = "Appointment updated and processed.";
                        _dBServices.UpdateJob(job);
                    }
                    catch (Exception ex)
                    {
                        job.Status = "Failed";
                        job.ResultMessage = ex.Message;
                        _dBServices.UpdateJob(job);
                    }
                });

                return Accepted(new { jobId = job.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpDelete("deleteAppointment/{appointmentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteAppointment(int appointmentId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                List<AppointmentDocument> docs = await _dBServices.GetAppointmentDocumentsById(appointmentId, userId);
                foreach (var doc in docs)
                {
                    if (System.IO.File.Exists(doc.DocumentPath))
                    {
                        System.IO.File.Delete(doc.DocumentPath);
                    }
                }
                await _dBServices.DeleteAppointmentAsync(appointmentId);
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
                var job = _dBServices.GetJobStatus(jobId);

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

        [HttpGet("getAppointmentById/{id}")]
        [Authorize]
        public IActionResult GetAppointmentById(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var appointment = _dBServices.GetAppointmentById(id, userId); // backend service, not WPF

            if (appointment == null)
            {
                return NotFound();
            }
                

            return Ok(appointment);
        }


        [HttpGet("getAppointmentDocumentsByAppointmentId/{appointmentid}")]
        [Authorize]
        public IActionResult GetAppointmentDocumentsByAppointmentId(int appointmentId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var appointmentdocuments = _dBServices.GetAppointmentDocumentsByAppointmentId(appointmentId, userId);

            if (appointmentdocuments == null)
            {
                return NotFound();
            }
                

            return Ok(appointmentdocuments);
        }

        [HttpGet("getAppointmentsByUserId")]
        [Authorize]
        public IActionResult GetAppointmentsByUserId()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var appointments = _dBServices.GetAppointmentsByUserId(userId); 

            if (appointments == null)
            {
                return NotFound();
            }
                

            return Ok(appointments);
        }

        [HttpGet("getSummaryByAppointmentId/{appointmentId}")]
        [Authorize]
        public IActionResult GetSummaryByAppointmentId(int appointmentId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var Summaryresponse = _dBServices.GetSummaryByAppointmentId(appointmentId);

            if (Summaryresponse == null)
            {
                return NotFound();
            }
                

            return Ok(Summaryresponse);
        }


        [Authorize]
        [HttpGet("debug-token")]
        public IActionResult DebugToken()
        {
            return Ok(new
            {
                auth = User.Identity.IsAuthenticated,
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            });
        }

    }
}

