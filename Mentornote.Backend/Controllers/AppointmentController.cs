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
        private readonly ILogger<AppointmentController> _logger;


        public AppointmentController(FileServices fileServices, DBServices dBServices, ILogger<AppointmentController> logger)
        {
            _fileServices = fileServices;
            _dBServices = dBServices;
            _logger = logger;
        }


        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadFile([FromForm] AppointmentDTO appointmentDTO)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _logger.LogInformation("Starting appointment file upload for UserId: {UserId}, Title: {Title}", userId, appointmentDTO.Title);

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
            _logger.LogInformation("Created background job with Id: {JobId} for UserId: {UserId}", job.Id, userId);

            Appointment appointment = new Appointment()
            {
                UserId = userId,
                Title = appointmentDTO.Title,
                StartTime = appointmentDTO.StartTime,
                EndTime = appointmentDTO.EndTime,
                Organizer = appointmentDTO.Organizer,
                Date = appointmentDTO.Date,
                Status = "Scheduled",
                AppointmentType = appointmentDTO.AppointmentType
            };

            var appointmentId = _dBServices.AddAppointment(appointment, userId);
            _logger.LogInformation("Created appointment with Id: {AppointmentId} for UserId: {UserId}", appointmentId, userId);

            // Save files to disk (synchronous)
            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Empty or null file encountered during upload for UserId: {UserId}, AppointmentId: {AppointmentId}", userId, appointmentId);
                    throw new Exception("No file uploaded.");
                }


                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "UploadedFiles");
                Directory.CreateDirectory(uploadDir);

                var filePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{file.FileName}");
                documentPaths.Add(filePath);

                _logger.LogInformation("Saving file to path: {FilePath} for UserId: {UserId}", filePath, userId);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            _logger.LogInformation("Successfully saved {FileCount} files for AppointmentId: {AppointmentId}", files.Count, appointmentId);

            //  Heavy work: do hashing + embeddings in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background processing for JobId: {JobId}, AppointmentId: {AppointmentId}", job.Id, appointmentId);

                    job.Status = "Processing";
                    _dBServices.UpdateJob(job);

                    foreach (var path in documentPaths)
                    {
                        _logger.LogInformation("Processing file: {FilePath} for AppointmentId: {AppointmentId}", path, appointmentId);

                        // compute hash SAFELY
                        string hash = _fileServices.ComputeHashFromFilePath(path);
                        _logger.LogInformation("Computed hash for file: {FilePath}, Hash: {FileHash}", path, hash);

                        var newDoc = new AppointmentDocument
                        {
                            AppointmentId = appointmentId,
                            UserId = userId,
                            DocumentPath = path,
                            FileHash = hash
                        };

                        documentID = _dBServices.AddAppointmentDocument(newDoc);
                        _logger.LogInformation("Added appointment document with Id: {DocumentId} for AppointmentId: {AppointmentId}", documentID, appointmentId);

                        await _fileServices.ProcessFileAsync(path, documentID, appointmentId);
                        _logger.LogInformation("Completed file processing for DocumentId: {DocumentId}", documentID);
                    }

                    job.Status = "Completed";
                    job.ResultMessage = "Appointment uploaded and processed.";
                    _dBServices.UpdateJob(job);

                    _logger.LogInformation("Successfully completed background job {JobId} for AppointmentId: {AppointmentId}", job.Id, appointmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background job {JobId} failed for AppointmentId: {AppointmentId}. Error: {ErrorMessage}", job.Id, appointmentId, ex.Message);

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

                _logger.LogInformation("Starting appointment update for AppointmentId: {AppointmentId}, UserId: {UserId}", appointmentId, userId);

                BackgroundJob job = new BackgroundJob()
                {
                    JobType = "AppointmentFileUpload",
                    ReferenceType = "Appointment",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    Payload = $"UserId: {userId}, Title: {appointmentDTO.Title}"
                };

                job.Id = _dBServices.CreateJob(job);
                _logger.LogInformation("Created background job {JobId} for appointment update", job.Id);

                // -----------------------------------
                // UPDATE APPOINTMENT BASIC FIELDS
                // -----------------------------------
                Appointment updatedAppointment = new Appointment
                {
                    Id = appointmentId,
                    UserId = userId,
                    Title = appointmentDTO.Title,
                    StartTime = appointmentDTO.StartTime,
                    EndTime = appointmentDTO.EndTime,
                    Organizer = appointmentDTO.Organizer,
                    Date = appointmentDTO.Date,
                    Status = appointmentDTO.Status,
                    AppointmentType = appointmentDTO.AppointmentType
                };

                _dBServices.UpdateAppointment(updatedAppointment, userId);
                _logger.LogInformation("Updated appointment basic fields for AppointmentId: {AppointmentId}", appointmentId);

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
                    _logger.LogInformation("Processing {FileCount} uploaded files for AppointmentId: {AppointmentId}", uploadedFiles.Count, appointmentId);

                    foreach (var file in uploadedFiles)
                    {
                        if (file == null || file.Length == 0)
                        {
                            _logger.LogWarning("Skipping empty or null file during update for AppointmentId: {AppointmentId}", appointmentId);
                            continue;
                        }


                        string safeName = $"{Guid.NewGuid()}_{file.FileName}";
                        string fullPath = Path.Combine(uploadDir, safeName);

                        _logger.LogInformation("Saving uploaded file to: {FilePath}", fullPath);

                        // SAVE FILE SAFELY NOW — BEFORE BACKGROUND THREAD
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        savedFiles.Add((fullPath, file.FileName));
                    }

                    _logger.LogInformation("Successfully saved {SavedFileCount} files to disk for AppointmentId: {AppointmentId}", savedFiles.Count, appointmentId);
                }

                // -----------------------------------
                // BACKGROUND PROCESSING
                // -----------------------------------

                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Starting background update processing for JobId: {JobId}, AppointmentId: {AppointmentId}", job.Id, appointmentId);

                        job.Status = "Processing";
                        _dBServices.UpdateJob(job);

                        // Load existing docs
                        var existingDocs = await _dBServices.GetAppointmentDocumentsById(appointmentId, userId);
                        _logger.LogInformation("Loaded {ExistingDocCount} existing documents for AppointmentId: {AppointmentId}", existingDocs.Count, appointmentId);

                        // Compute hashes for new files
                        var hashedUploads = new List<(string Path, string Hash)>();

                        if (removeFilesIds != null)
                        {
                            _logger.LogInformation("Removing {RemoveCount} files for AppointmentId: {AppointmentId}", removeFilesIds.Count, appointmentId);

                            // Assume FilesToRemove contains the original file names
                            //existingDocs = await dBServices.GetAppointmentDocumentsById(appointmentId, userId);
                            foreach (var fileId in removeFilesIds)
                            {
                                var docsToRemove = existingDocs.Where(d => d.Id == fileId).FirstOrDefault();
                                if (docsToRemove != null)
                                {
                                    if (System.IO.File.Exists(docsToRemove.DocumentPath))
                                    {
                                        _logger.LogInformation("Deleting file: {FilePath} with DocumentId: {DocumentId}", docsToRemove.DocumentPath, docsToRemove.Id);
                                        System.IO.File.Delete(docsToRemove.DocumentPath);
                                        _dBServices.DeleteAppointmentDocument(docsToRemove.Id, userId);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("File not found for deletion: {FilePath}, DocumentId: {DocumentId}", docsToRemove.DocumentPath, docsToRemove.Id);
                                    }
                                }
                            }
                        }

                        foreach (var file in savedFiles)
                        {
                            string newHash = _fileServices.ComputeHashFromFilePath(file.Path);
                            hashedUploads.Add((file.Path, newHash));
                            _logger.LogInformation("Computed hash for uploaded file: {FilePath}, Hash: {FileHash}", file.Path, newHash);
                        }

                        // Remove duplicates among uploads
                        var uniqueUploads = hashedUploads
                            .GroupBy(x => x.Hash)
                            .Select(g => g.First())
                            .ToList();

                        _logger.LogInformation("Filtered to {UniqueCount} unique uploads from {TotalCount} hashed uploads", uniqueUploads.Count, hashedUploads.Count);

                        // Remove files already in DB
                        var newqueUploads = uniqueUploads
                            .Where(u => !existingDocs.Any(d => d.FileHash == u.Hash))
                            .ToList();

                        _logger.LogInformation("Identified {NewFileCount} new unique files to add for AppointmentId: {AppointmentId}", newqueUploads.Count, appointmentId);

                        // Find old documents to delete
                        var badUploads = hashedUploads
                         .Where(upload => !newqueUploads.Any(accepted => accepted.Path == upload.Path));

                        int duplicateCount = badUploads.Count();
                        if (duplicateCount > 0)
                        {
                            _logger.LogInformation("Found {DuplicateCount} duplicate files to remove for AppointmentId: {AppointmentId}", duplicateCount, appointmentId);
                        }

                        // -----------------------------------
                        // ADD NEW UNIQUE DOCS
                        // -----------------------------------
                        foreach (var up in newqueUploads)
                        {
                            _logger.LogInformation("Adding new document: {FilePath} for AppointmentId: {AppointmentId}", up.Path, appointmentId);

                            var newDoc = new AppointmentDocument
                            {
                                AppointmentId = appointmentId,
                                UserId = userId,
                                DocumentPath = up.Path,
                                FileHash = up.Hash
                            };

                            int newDocId = _dBServices.AddAppointmentDocument(newDoc);
                            _logger.LogInformation("Added new appointment document with Id: {DocumentId}", newDocId);

                            // Process embeddings now
                            await _fileServices.ProcessFileAsync(
                                up.Path,
                                newDocId,
                                appointmentId
                            );

                            _logger.LogInformation("Completed processing for new DocumentId: {DocumentId}", newDocId);
                        }

                        // -----------------------------------
                        // DELETE REMOVED DOCUMENT FILES
                        // -----------------------------------
                        foreach (var doc in badUploads)
                        {
                            if (System.IO.File.Exists(doc.Path))
                            {
                                _logger.LogInformation("Deleting duplicate file: {FilePath}", doc.Path);
                                System.IO.File.Delete(doc.Path);
                            }
                            else
                            {
                                _logger.LogWarning("Duplicate file not found for deletion: {FilePath}", doc.Path);
                            }
                        }



                        job.Status = "Completed";
                        job.ResultMessage = "Appointment updated and processed.";
                        _dBServices.UpdateJob(job);

                        _logger.LogInformation("Successfully completed background update job {JobId} for AppointmentId: {AppointmentId}", job.Id, appointmentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background update job {JobId} failed for AppointmentId: {AppointmentId}. Error: {ErrorMessage}", job.Id, appointmentId, ex.Message);

                        job.Status = "Failed";
                        job.ResultMessage = ex.Message;
                        _dBServices.UpdateJob(job);
                    }
                });

                return Accepted(new { jobId = job.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update appointment {AppointmentId}. Error: {ErrorMessage}", appointmentId, ex.Message);
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
                _logger.LogInformation("Starting appointment deletion for AppointmentId: {AppointmentId}, UserId: {UserId}", appointmentId, userId);

                List<AppointmentDocument> docs = await _dBServices.GetAppointmentDocumentsById(appointmentId, userId);
                _logger.LogInformation("Found {DocumentCount} documents to delete for AppointmentId: {AppointmentId}", docs.Count, appointmentId);

                foreach (var doc in docs)
                {
                    if (System.IO.File.Exists(doc.DocumentPath))
                    {
                        _logger.LogInformation("Deleting document file: {FilePath}, DocumentId: {DocumentId}", doc.DocumentPath, doc.Id);
                        System.IO.File.Delete(doc.DocumentPath);
                    }
                    else
                    {
                        _logger.LogWarning("Document file not found: {FilePath}, DocumentId: {DocumentId}", doc.DocumentPath, doc.Id);
                    }
                }

                await _dBServices.DeleteAppointmentAsync(appointmentId);
                _logger.LogInformation("Successfully deleted appointment {AppointmentId} and {DocumentCount} associated documents", appointmentId, docs.Count);

                return Ok(new { message = "Appointment deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete appointment {AppointmentId}. Error: {ErrorMessage}", appointmentId, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("status/{jobId}")]
        public IActionResult GetStatus(long jobId)
        {
            try
            {
                _logger.LogInformation("Fetching status for JobId: {JobId}", jobId);

                var job = _dBServices.GetJobStatus(jobId);

                if (job == null)
                {
                    _logger.LogWarning("Job not found: {JobId}", jobId);
                    return NotFound(new { ResultMessage = "Job not found" });
                }

                _logger.LogInformation("Retrieved job status: {Status} for JobId: {JobId}", job.Status, jobId);

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
                _logger.LogError(ex, "Error fetching job status for JobId: {JobId}. Error: {ErrorMessage}", jobId, ex.Message);
                return StatusCode(500, new { ResultMessage = $"Error fetching job status: {ex.Message}" });
            }
        }

        [HttpGet("getAppointmentById/{id}")]
        [Authorize]
        public IActionResult GetAppointmentById(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _logger.LogInformation("Fetching appointment {AppointmentId} for UserId: {UserId}", id, userId);

            var appointment = _dBServices.GetAppointmentById(id, userId); // backend service, not WPF

            if (appointment == null)
            {
                _logger.LogWarning("Appointment not found: {AppointmentId} for UserId: {UserId}", id, userId);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved appointment {AppointmentId}", id);
            return Ok(appointment);
        }


        [HttpGet("getAppointmentDocumentsByAppointmentId/{appointmentid}")]
        [Authorize]
        public IActionResult GetAppointmentDocumentsByAppointmentId(int appointmentId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _logger.LogInformation("Fetching documents for AppointmentId: {AppointmentId}, UserId: {UserId}", appointmentId, userId);

            var appointmentdocuments = _dBServices.GetAppointmentDocumentsByAppointmentId(appointmentId, userId);

            if (appointmentdocuments == null)
            {
                _logger.LogWarning("No documents found for AppointmentId: {AppointmentId}, UserId: {UserId}", appointmentId, userId);
                return NotFound();
            }

            _logger.LogInformation("Retrieved {DocumentCount} documents for AppointmentId: {AppointmentId}", appointmentdocuments.Count, appointmentId);
            return Ok(appointmentdocuments);
        }

        [HttpGet("getAppointmentsByUserId")]
        [Authorize]
        public IActionResult GetAppointmentsByUserId()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _logger.LogInformation("Fetching all appointments for UserId: {UserId}", userId);

            var appointments = _dBServices.GetAppointmentsByUserId(userId);

            if (appointments == null)
            {
                _logger.LogWarning("No appointments found for UserId: {UserId}", userId);
                return NotFound();
            }

            _logger.LogInformation("Retrieved {AppointmentCount} appointments for UserId: {UserId}", appointments.Count, userId);
            return Ok(appointments);
        }

        [HttpGet("getSummaryByAppointmentId/{appointmentId}")]
        [Authorize]
        public IActionResult GetSummaryByAppointmentId(int appointmentId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _logger.LogInformation("Fetching summary for AppointmentId: {AppointmentId}, UserId: {UserId}", appointmentId, userId);

            var Summaryresponse = _dBServices.GetSummaryByAppointmentId(appointmentId);

            if (Summaryresponse == null)
            {
                _logger.LogWarning("Summary not found for AppointmentId: {AppointmentId}", appointmentId);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved summary for AppointmentId: {AppointmentId}", appointmentId);
            return Ok(Summaryresponse);
        }


        [Authorize]
        [HttpGet("debug-token")]
        public IActionResult DebugToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAuthenticated = User.Identity.IsAuthenticated;

            _logger.LogInformation("Debug token endpoint accessed. IsAuthenticated: {IsAuthenticated}, UserId: {UserId}", isAuthenticated, userId ?? "null");

            return Ok(new
            {
                auth = isAuthenticated,
                userId = userId
            });
        }

    }
}