using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.IO;
using System.Numerics;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Mentornote.Backend.Models;

namespace Mentornote.Backend.Services
{
    public class DBServices
    {
        private string? connectionString;
        public DBServices()
        {
            ConfigurationBuilder DatabaseUserBuilder = new ConfigurationBuilder();
            DatabaseUserBuilder.SetBasePath(Directory.GetCurrentDirectory());
            DatabaseUserBuilder.AddJsonFile("appsettings.json");
            IConfiguration DatabaseUserConfiguration = DatabaseUserBuilder.Build();
            connectionString = DatabaseUserConfiguration.GetConnectionString("DefaultConnection");
        }

        public int AddAppointment(Appointment appointment, int userId)
        {
            try
            {
                int appointmentId;

                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "AddAppointment"
                    };

                    // Parameters
                    cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = userId
                    });

                    cmd.Parameters.Add(new SqlParameter("@Title", SqlDbType.NVarChar, 200)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Title ?? (object)DBNull.Value
                    });

                    cmd.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Description ?? (object)DBNull.Value
                    });

                    cmd.Parameters.Add(new SqlParameter("@StartTime", SqlDbType.DateTime2)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.StartTime ?? (object)DBNull.Value
                    });

                    cmd.Parameters.Add(new SqlParameter("@EndTime", SqlDbType.DateTime2)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.EndTime ?? (object)DBNull.Value
                    });

                    cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Status ?? (object)"Scheduled"
                    });

                    cmd.Parameters.Add(new SqlParameter("@Notes", SqlDbType.NVarChar)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Notes ?? (object)DBNull.Value
                    });

                    cmd.Parameters.Add(new SqlParameter("@Date", SqlDbType.Date)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Date
                    });
                    cmd.Parameters.Add(new SqlParameter("@Organizer", SqlDbType.NVarChar, 200)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Organizer ?? (object)DBNull.Value
                    });


                    appointmentId = Convert.ToInt32((decimal)cmd.ExecuteScalar());

                    connection.Close();

                    //appointmentId = (int)outputIdParam.Value;
                }

                return appointmentId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding appointment: {ex.Message}");
                return 0;
            }
        }

        public int AddAppointmentDocument(AppointmentDocuments appointmentDoc)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "AddAppointmentDocument"
                    };
                    // Parameters
                    cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointmentDoc.UserId,
                    });
                    cmd.Parameters.Add(new SqlParameter("@AppointmentId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointmentDoc.AppointmentId
                    });
                    cmd.Parameters.Add(new SqlParameter("@DocumentPath", SqlDbType.NVarChar)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointmentDoc.DocumentPath ?? (object)DBNull.Value
                    });

                    var result = cmd.ExecuteScalar();


                    connection.Close();

                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding appointment note: {ex.Message}");
                return 0;
            }
        }

        public int AddAppointmentDocumentEmbedding(AppointmentDocumentEmbedding embedding)
        {
            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand("AddAppointmentDocumentEmbedding", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@AppointmentDocumentId", embedding.AppointmentDocumentId);
            command.Parameters.AddWithValue("@AppointmentId", embedding.AppointmentId);
            command.Parameters.AddWithValue("@ChunkIndex", embedding.ChunkIndex);
            command.Parameters.AddWithValue("@ChunkText", embedding.ChunkText);
            command.Parameters.AddWithValue("@Vector", embedding.Vector);

            connection.Open();
            var result = command.ExecuteScalar();
            return Convert.ToInt32(result); // returns the new EmbeddingId
        }

        public List<Appointment> GetAppointmentsByUserId(int userId)
        {
            try
            {
                List<Appointment> appointments = new List<Appointment>();
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "GetUserAppointments"
                    };
                    cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = userId
                    });
                    using SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Appointment appointment = new Appointment
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                            StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("StartTime")),
                            EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            Date = reader.IsDBNull(reader.GetOrdinal("Date"))  ? (DateTime?)null: reader.GetDateTime(reader.GetOrdinal("Date")),
                            Organizer = reader.GetString(reader.GetOrdinal("Organizer"))
                            //  UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                        };
                        appointments.Add(appointment);
                    }
                    connection.Close();
                }
                return appointments;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<Appointment>();
            }
           
        }

        public Appointment GetAppointmentById(int appointmentId, int userId)
        {
            Appointment appointment = new();
            using SqlConnection connection = new SqlConnection(connectionString);
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "GetAppointmentById"
                };
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    SqlValue = appointmentId
                });
                cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    SqlValue = userId
                });
                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    appointment = new Appointment
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("StartTime")),
                        EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        Date = reader.IsDBNull(reader.GetOrdinal("Date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Date")),
                        Organizer = reader.GetString(reader.GetOrdinal("Organizer"))
                        // UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };
                }
                connection.Close();
            }
            return appointment;
        }

        public List<AppointmentDocuments> GetAppointmentDocumentsByAppointmentId(int appointmentId, int userId)
        {
            List<AppointmentDocuments> documents = new List<AppointmentDocuments>();
            using SqlConnection connection = new SqlConnection(connectionString);
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "GetAppointmentNotes"
                };
                cmd.Parameters.Add(new SqlParameter("@AppointmentId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    SqlValue = appointmentId
                });
                cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    SqlValue = userId
                });
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AppointmentDocuments document = new AppointmentDocuments
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        AppointmentId = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                        DocumentPath = reader.GetString(reader.GetOrdinal("DocumentPath")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                    documents.Add(document);
                }
                connection.Close();
            }
            return documents;
        }

        public List<AppointmentDocumentEmbedding> GetEmbeddingsByDocumentId(int appointmentDocumentId)
        {
            List<AppointmentDocumentEmbedding> embeddings = new List<AppointmentDocumentEmbedding>();
            using SqlConnection connection = new SqlConnection(connectionString);
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "GetEmbeddingsByDocumentId"
                };
                cmd.Parameters.Add(new SqlParameter("@AppointmentDocumentId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    SqlValue = appointmentDocumentId
                });
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AppointmentDocumentEmbedding embedding = new AppointmentDocumentEmbedding
                    {
                        EmbeddingId = reader.GetInt32(reader.GetOrdinal("EmbeddingId")),
                        AppointmentDocumentId = reader.GetInt32(reader.GetOrdinal("AppointmentDocumentId")),
                        ChunkIndex = reader.GetInt32(reader.GetOrdinal("ChunkIndex")),
                        ChunkText = reader.GetString(reader.GetOrdinal("ChunkText")),
                        Vector = reader.GetString(reader.GetOrdinal("Vector")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                    embeddings.Add(embedding);
                }
                connection.Close();
            }
            return embeddings;
        }

        public long CreateJob(BackgroundJob job)
        {
            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("CreateBackgroundJob", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@JobType", job.JobType);
            cmd.Parameters.AddWithValue("@ReferenceId", (object?)job.ReferenceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferenceType", (object?)job.ReferenceType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Payload", (object?)job.Payload ?? DBNull.Value);

            var output = new SqlParameter("@JobId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(output);

            conn.Open();
            cmd.ExecuteNonQuery();

            job.Id = (long)output.Value;  // Assign SQL-generated ID
            return job.Id;
        }

        public void UpdateJob(BackgroundJob job)
        {
            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("UpdateBackgroundJob", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@JobId", job.Id);
            cmd.Parameters.AddWithValue("@Status", job.Status);
            cmd.Parameters.AddWithValue("@ResultMessage", (object?)job.ResultMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ErrorTrace", (object?)job.ErrorTrace ?? DBNull.Value);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
        public BackgroundJob GetJobStatus(long jobId)
        {
            BackgroundJob job = new();

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("GetBackgroundJob", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@JobId", jobId);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                job = new BackgroundJob
                {
                    Id = reader.GetInt64(0),
                    JobType = reader.GetString(1),
                    Status = reader.GetString(2),
                    ResultMessage = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4),
                    StartedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    CompletedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
                };
            }

            return job;
        }

        public async Task<List<AppointmentDocumentEmbedding>> GetChunksForAppointment(int appointmentId)
        {
            try
            {
                var results = new List<AppointmentDocumentEmbedding>();

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("GetDocumentChunksForAppointment", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        results.Add(new AppointmentDocumentEmbedding
                        {
                            EmbeddingId = reader.GetInt32(reader.GetOrdinal("EmbeddingId")),
                            AppointmentId = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                            AppointmentDocumentId = reader.GetOrdinal("AppointmentDocumentId"),
                            ChunkText = reader.GetString(reader.GetOrdinal("ChunkText")),
                            ChunkIndex = reader.GetInt32(reader.GetOrdinal("ChunkIndex")),
                            Vector = reader.GetString(reader.GetOrdinal("Vector"))
                        });
                    }
                }

                return results;
            }
            catch (Exception)
            {

                throw;
            }
          
        }

        public async Task DeleteAppointmentAsync(int appointmentId)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                using SqlCommand cmd = new SqlCommand("DeleteAppointment", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Add parameter
                cmd.Parameters.Add(new SqlParameter("@AppointmentId", SqlDbType.Int)
                {
                    Value = appointmentId
                });

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log error here if needed
                throw new Exception($"Error deleting appointment {appointmentId}: {ex.Message}", ex);
            }
        }
    }
}
