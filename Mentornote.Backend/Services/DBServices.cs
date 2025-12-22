using Mentornote.Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;


namespace Mentornote.Backend.Services
{
    public class DBServices
    {
        private string? _connectionString;
        public DBServices()
        {
            ConfigurationBuilder DatabaseUserBuilder = new ConfigurationBuilder();
            DatabaseUserBuilder.SetBasePath(Directory.GetCurrentDirectory());
            DatabaseUserBuilder.AddJsonFile("appsettings.json");
            IConfiguration DatabaseUserConfiguration = DatabaseUserBuilder.Build();
            _connectionString = DatabaseUserConfiguration.GetConnectionString("DefaultConnection");
        }

        public int AddAppointment(Appointment appointment, int userId)
        {
            try
            {
                int appointmentId;

                using SqlConnection connection = new SqlConnection(_connectionString);
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
                    cmd.Parameters.Add(new SqlParameter("@AppointmentType", SqlDbType.NVarChar, 200)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.AppointmentType ?? (object)DBNull.Value
                    });


                    appointmentId = Convert.ToInt32((decimal)cmd.ExecuteScalar());

                    connection.Close();

                }

                return appointmentId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding appointment: {ex.Message}");
                return 0;
            }
        }

        public int AddAppointmentDocument(AppointmentDocument appointmentDoc)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
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
                    cmd.Parameters.Add(new SqlParameter("@FileHash", SqlDbType.NVarChar)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointmentDoc.FileHash ?? (object)DBNull.Value
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
            using var connection = new SqlConnection(_connectionString);
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
                using SqlConnection connection = new SqlConnection(_connectionString);
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
                            AppointmentType = reader.GetString(reader.GetOrdinal("AppointmentType")),
                            StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("StartTime")),
                            EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            Date = reader.IsDBNull(reader.GetOrdinal("Date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Date")),
                            Organizer = reader.GetString(reader.GetOrdinal("Organizer")),
                            SummaryExists = reader.GetBoolean(reader.GetOrdinal("SummaryExists"))
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
            using SqlConnection connection = new SqlConnection(_connectionString);
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
                        AppointmentType = reader.GetString(reader.GetOrdinal("AppointmentType")),
                        StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("StartTime")),
                        EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        Date = reader.IsDBNull(reader.GetOrdinal("Date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Date")),
                        Organizer = reader.GetString(reader.GetOrdinal("Organizer")),
                        SummaryExists = reader.GetBoolean(reader.GetOrdinal("SummaryExists"))
                    };
                }
                connection.Close();
            }
            return appointment;
        }

        public List<AppointmentDocument> GetAppointmentDocumentsByAppointmentId(int appointmentId, int userId)
        {
            List<AppointmentDocument> documents = new List<AppointmentDocument>();
            using SqlConnection connection = new SqlConnection(_connectionString);
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
                    AppointmentDocument document = new AppointmentDocument
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        AppointmentId = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                        DocumentPath = reader.GetString(reader.GetOrdinal("DocumentPath")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        FileHash = reader.IsDBNull(reader.GetOrdinal("FileHash")) ? null: reader.GetString(reader.GetOrdinal("FileHash"))
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
            using SqlConnection connection = new SqlConnection(_connectionString);
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
            using var conn = new SqlConnection(_connectionString);
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
            using var conn = new SqlConnection(_connectionString);
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

            using var conn = new SqlConnection(_connectionString);
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

                using (var conn = new SqlConnection(_connectionString))
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
                using SqlConnection conn = new SqlConnection(_connectionString);
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

        public int UpdateAppointment(Appointment appointment, int userId)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "UpdateAppointment"
                    };
                    // Parameters
                    cmd.Parameters.Add(new SqlParameter("@AppointmentId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Id
                    });
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
                    cmd.Parameters.Add(new SqlParameter("@Date", SqlDbType.Date)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Date ?? (object)DBNull.Value
                    });
                    cmd.Parameters.Add(new SqlParameter("@Organizer", SqlDbType.NVarChar, 200)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.Organizer ?? (object)DBNull.Value
                    });
                    cmd.Parameters.Add(new SqlParameter("@AppointmentType", SqlDbType.NVarChar, 200)
                    {
                        Direction = ParameterDirection.Input,
                        SqlValue = appointment.AppointmentType ?? (object)DBNull.Value
                    });

                    //  cmd.ExecuteNonQuery();

                    cmd.ExecuteNonQuery();
                    connection.Close();

                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating appointment: {ex.Message}");
                return 0;
            }

          

        }

        public async Task<List<AppointmentDocument>> GetAppointmentDocumentsById(int appointmentId, int userId)
        {
            var documents = new List<AppointmentDocument>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("GetAppointmentDocumentsById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@AppointmentId", SqlDbType.Int)
            {
                Value = appointmentId
            });
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
            {
                Value = userId
            });

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                documents.Add(new AppointmentDocument
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    AppointmentId = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                    DocumentPath = reader.GetString(reader.GetOrdinal("DocumentPath")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    FileHash = reader.IsDBNull(reader.GetOrdinal("FileHash")) ? null : reader.GetString(reader.GetOrdinal("FileHash"))
                });
            }

            return documents;
        }

        public bool DeleteAppointmentDocument(int documentId, int userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("DeleteAppointmentDocument", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Add parameters
                cmd.Parameters.Add(new SqlParameter("@DocumentId", SqlDbType.Int)
                {
                    Value = documentId
                });

                cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
                {
                    Value = userId
                });

                conn.Open();

                try
                {
                    // Execute and read return value
                    var result = cmd.ExecuteScalar();
                    conn.Close();

                    // Stored proc returns "1" on success
                    return result != null && Convert.ToInt32(result) == 1;

                }
                catch (Exception ex)
                {
                    // Log or rethrow depending on your architecture
                    throw new Exception($"Failed to delete document {documentId}. Error: {ex.Message}", ex);
                }
            }
        }

        public int AddAppointmentSummary(int appointmentId, string summaryText)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlCommand cmd = new SqlCommand("AddAppointmentSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
            cmd.Parameters.AddWithValue("@SummaryText", summaryText);

            int newId = Convert.ToInt32(cmd.ExecuteScalar());
            return newId;
        }

        public AppointmentSummary GetSummaryByAppointmentId(int appointmentId)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand("GetAppointmentSummary", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new AppointmentSummary
                {
                    AppointmentId = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                    SummaryText = reader["SummaryText"] as string ?? "",
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                };
            }

            return new AppointmentSummary(); 
        }

        public User GetUserByEmail(string email)
        {
            User user = new();
            SqlConnection mentornoteCoonnection = new SqlConnection();
            mentornoteCoonnection.ConnectionString = _connectionString;
            mentornoteCoonnection.Open();

            SqlCommand GetUser = new()
            {
                CommandType = CommandType.StoredProcedure,
                Connection = mentornoteCoonnection,
                CommandText = "GetUserByEmail"
            };

            SqlParameter EmailParameter = new()
            {
                ParameterName = "@Email",
                SqlDbType = SqlDbType.VarChar,
                Direction = ParameterDirection.Input,
                SqlValue = email
            };

            GetUser.Parameters.Add(EmailParameter);
            SqlDataReader reader = GetUser.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    user = new User
                    {
                        Id = (int)reader["Id"],
                        FirstName = (string)reader["FirstName"],
                        LastName = (string)reader["LastName"],
                        Email = (string)reader["Email"],
                        PasswordSalt = (byte[])reader["PasswordSalt"],
                        PasswordHash = (byte[])reader["PasswordHash"],
                        UserType = (string)reader["UserType"],
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        AuthProvider = (string)reader["AuthProvider"],
                        TrialMeetingsRemaining = (int)reader["TrialMeetingsRemaining"],
                        IsSubscribed = (bool)reader["IsSubscribed"],
                        PasswordChangedAt = reader.GetDateTime(reader.GetOrdinal("PasswordChangedAt")),
                    };
                }
            }
            reader.Close();
            mentornoteCoonnection.Close();
            return user;
        }
        public User GetUserById(int userid)
        {
            User user = new();
            SqlConnection mentornoteCoonnection = new SqlConnection();
            mentornoteCoonnection.ConnectionString = _connectionString;
            mentornoteCoonnection.Open();

            SqlCommand GetUser = new()
            {
                CommandType = CommandType.StoredProcedure,
                Connection = mentornoteCoonnection,
                CommandText = "GetUserById"
            };

            SqlParameter IdParameter = new()
            {
                ParameterName = "@UserId",
                SqlDbType = SqlDbType.VarChar,
                Direction = ParameterDirection.Input,
                SqlValue = userid
            };

            GetUser.Parameters.Add(IdParameter);
            SqlDataReader reader = GetUser.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    user = new User
                    {
                        Id = (int)reader["Id"],
                        FirstName = (string)reader["FirstName"],
                        LastName = (string)reader["LastName"],
                        Email = (string)reader["Email"],
                        PasswordSalt = (byte[])reader["PasswordSalt"],
                        PasswordHash = (byte[])reader["PasswordHash"],
                        UserType = (string)reader["UserType"],
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        AuthProvider = (string)reader["AuthProvider"],
                        TrialMeetingsRemaining = (int)reader["TrialMeetingsRemaining"],
                        IsSubscribed = (bool)reader["IsSubscribed"],
                        PasswordChangedAt = reader.GetDateTime(reader.GetOrdinal("PasswordChangedAt")),
                    };
                }
            }
            reader.Close();
            mentornoteCoonnection.Close();
            return user;
        }

        public int RegisterUser(User user)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("RegisterUser", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                cmd.Parameters.AddWithValue("@LastName", user.LastName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@PasswordSalt", user.PasswordSalt);
                cmd.Parameters.AddWithValue("@UserType", user.UserType);
                cmd.Parameters.AddWithValue("@AuthProvider", user.AuthProvider);
                cmd.Parameters.AddWithValue("@TrialMeetingsRemaining", user.TrialMeetingsRemaining);
                cmd.Parameters.AddWithValue("@IsSubscribed", user.IsSubscribed);

                SqlParameter outputId = new SqlParameter("@UserId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outputId);

                conn.Open();
                cmd.ExecuteNonQuery();

                return (int)outputId.Value;
            }
        }

        public void UpdateUserPassword(User user)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("UpdateUserPassword", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", user.Id);
            cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@PasswordSalt", user.PasswordSalt);
            cmd.Parameters.AddWithValue("@PasswordChangedAt", user.PasswordChangedAt);

            cmd.ExecuteNonQuery();
            conn.Close();
        }


        public void SaveRefreshToken(RefreshToken token)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SaveRefreshToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", token.UserId);
            cmd.Parameters.AddWithValue("@Token", token.Token);
            cmd.Parameters.AddWithValue("@Email", token.Email);
            cmd.Parameters.AddWithValue("@ExpiresAt", token.ExpiresAt);
            cmd.Parameters.AddWithValue("@CreatedAt", token.CreatedAt);

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public RefreshToken GetRefreshToken(string token)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("GetRefreshToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Token", token);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new RefreshToken
            {
                Id = Convert.ToInt32(reader["Id"]),
                UserId = Convert.ToInt32(reader["UserId"]),
                Token = reader["Token"].ToString(),
                ExpiresAt = Convert.ToDateTime(reader["ExpiresAt"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                Email = reader["Email"].ToString(),
                RevokedAt = reader["RevokedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["RevokedAt"]),
                ReplacedByToken = reader["ReplacedByToken"] == DBNull.Value ? null : reader["ReplacedByToken"].ToString()
            };
        }

        public void RevokeToken(int id, string replacedByToken)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("RevokeRefreshToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ReplacedByToken", replacedByToken);

            cmd.ExecuteNonQuery();
        }

        public void DeleteRefreshToken(int tokenId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("DeleteRefreshToken", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@TokenId", tokenId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public async Task<AppSettings> GetAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("GetUserSettings", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read())
            {
                // No row → return defaults
                return GetDefaultSettings();
            }

            return new AppSettings
            {
                ResponseFormat = (ResponseFormat)reader.GetInt32(reader.GetOrdinal("ResponseFormat")),
                ResponseTone = (ResponseTone)reader.GetInt32(reader.GetOrdinal("ResponseTone")),
                ResumeUsage = (ResumeUsage)reader.GetInt32(reader.GetOrdinal("ResumeUsage")),
                Theme = (Theme)reader.GetInt32(reader.GetOrdinal("Theme")),
                RecentUtteranceCount = reader.GetInt32(reader.GetOrdinal("RecentUtteranceCount")),
                Creativity = reader.GetDouble(reader.GetOrdinal("Creativity"))
            };
        }

        public async Task SaveAsync(int userId, AppSettings settings)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SaveUserSettings", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ResponseFormat", settings.ResponseFormat);
            cmd.Parameters.AddWithValue("@ResponseTone", settings.ResponseTone);
            cmd.Parameters.AddWithValue("@ResumeUsage", settings.ResumeUsage);
            cmd.Parameters.AddWithValue("@Theme", settings.Theme);
            cmd.Parameters.AddWithValue("@RecentUtteranceCount", settings.RecentUtteranceCount);
            cmd.Parameters.AddWithValue("@Creativity", settings.Creativity);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task UpdateUserTrialAfterMeetingAsync(int userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand("UpdateUserTrialAfterMeeting", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task ActivateSubscriptionAsync(int userId, string stripeCustomerId, string stripeSubscriptionId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("ActivateSubscription", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@StripeCustomerId", stripeCustomerId);
                    cmd.Parameters.AddWithValue("@StripeSubscriptionId", stripeSubscriptionId);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<string> GetStripeCustomerIdAsync(int userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetStripeCustomerId", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    await conn.OpenAsync();

                    object result = await cmd.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }

        public async Task DeactivateSubscriptionAsync(string stripeSubscriptionId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("DeactivateSubscription", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StripeSubscriptionId", stripeSubscriptionId);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        // Looks like I will have to move into deplpoymeny I am so scared right now 


        private static AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                ResponseFormat = (ResponseFormat)1,        // Guided
                ResponseTone = 0,          // Professional
                ResumeUsage = 0,           // Relevant only
                Theme = 0,                 // Dark
                RecentUtteranceCount = 15,
                Creativity = 0.6
            };
        }
    }
}  