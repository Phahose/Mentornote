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

        public List<AppointmentDocuments> GetAppointmentDocumentsByAppointmentId(int appointmentId)
        {
            List<AppointmentDocuments> documents = new List<AppointmentDocuments>();
            using SqlConnection connection = new SqlConnection(connectionString);
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "GetAppointmentDocumentsByAppointmentId"
                };
                cmd.Parameters.Add(new SqlParameter("@AppointmentId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    SqlValue = appointmentId
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
    }
}
