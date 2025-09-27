using Elfie.Serialization;
using Mentornote.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using UglyToad.PdfPig;
namespace Mentornote.Services
{
    public class CardsServices
    {
        private string? connectionString;
        public CardsServices()
        {
            ConfigurationBuilder DatabaseUserBuilder = new ConfigurationBuilder();
            DatabaseUserBuilder.SetBasePath(Directory.GetCurrentDirectory());
            DatabaseUserBuilder.AddJsonFile("appsettings.json");
            IConfiguration DatabaseUserConfiguration = DatabaseUserBuilder.Build();
            connectionString = DatabaseUserConfiguration.GetConnectionString("DefaultConnection");
        }
        public List<FlashcardSet> GetUserFlashcards(int userId)
        {
            List<FlashcardSet> flashcardSets = new List<FlashcardSet>();
            Dictionary<int, FlashcardSet> flashcardSetMap = new Dictionary<int, FlashcardSet>();

            using (SqlConnection mentornoteConnection = new SqlConnection(connectionString))
            {
                mentornoteConnection.Open();

                SqlCommand getFlashcards = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = mentornoteConnection,
                    CommandText = "GetUserFlashcards"
                };

                SqlParameter userIdParameter = new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = userId
                };

                getFlashcards.Parameters.Add(userIdParameter);

                SqlDataReader reader = getFlashcards.ExecuteReader();

                while (reader.Read())
                {
                    int flashcardSetId = (int)reader["FlashcardSetId"];

                    // Check if the set already exists in our dictionary
                    if (!flashcardSetMap.ContainsKey(flashcardSetId))
                    {
                        FlashcardSet set = new FlashcardSet();

                        set.Id = flashcardSetId;
                        set.Title = (string)reader["FlashcardSetTitle"];
                        set.CreatedAt = (DateTime)reader["CreatedAt"];
                        //set.Source = (string)reader["Source"];
                        set.Flashcards = new List<Flashcard>();
                        set.UserId = (int)reader["UserId"];
                        flashcardSetMap.Add(flashcardSetId, set);
                        flashcardSets.Add(set);
                    }

                    // Create a flashcard object and add it to the set
                    Flashcard flashcard = new Flashcard
                    {
                        Id = (int)reader["FlashcardId"],
                        Front = (string)reader["Front"],
                        Back = (string)reader["Back"]
                    };

                    flashcardSetMap[flashcardSetId].Flashcards.Add(flashcard);
                }

                reader.Close();
            }

            return flashcardSets;
        }

        public int AddNote(Note note, int userId)
        {
            try
            {
                int noteID;
                using SqlConnection mentornoteConnection = new SqlConnection(connectionString);
                {
                    mentornoteConnection.Open();

                    SqlCommand AddNote = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = mentornoteConnection,
                        CommandText = "AddNote"
                    };

                    SqlParameter userIdParameter = new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = userId
                    };

                    SqlParameter titlePaarameter = new SqlParameter
                    {
                        ParameterName = "@Title",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = note.Title
                    };

                    SqlParameter filepath = new SqlParameter
                    {
                        ParameterName = "@FilePath",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = note.FilePath
                    };

                    // Output param to capture new Note ID
                    var outputIdParam = new SqlParameter("@NewNoteId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    AddNote.Parameters.Add(outputIdParam);
                    AddNote.Parameters.Add(userIdParameter);
                    AddNote.Parameters.Add(titlePaarameter);
                    AddNote.Parameters.Add(filepath);
                    AddNote.ExecuteNonQuery();
                    mentornoteConnection.Close();

                    noteID = (int)outputIdParam.Value;
                }

                return noteID;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
        public string DeleteNote(int noteId)
        {
            try
            {
                using SqlConnection mentornoteConnection = new SqlConnection(connectionString);
                {
                    mentornoteConnection.Open();

                    SqlCommand AddNote = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = mentornoteConnection,
                        CommandText = "DeleteNote"
                    };

                    SqlParameter noteIdParameter = new SqlParameter
                    {
                        ParameterName = "@NoteId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteId
                    };

                    AddNote.Parameters.Add(noteIdParameter);
                    AddNote.ExecuteNonQuery();
                    mentornoteConnection.Close();

                }

                return "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
        }

        public List<Note> GetUserNotes(int userId)
        {
            List<Note> NoteList = new List<Note>();

            using (SqlConnection mentornoteConnection = new SqlConnection(connectionString))
            {
                mentornoteConnection.Open();

                SqlCommand getFlashcards = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = mentornoteConnection,
                    CommandText = "GetNotes"
                };

                SqlParameter userIdParameter = new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = userId
                };

                getFlashcards.Parameters.Add(userIdParameter);

                SqlDataReader reader = getFlashcards.ExecuteReader();

                if (reader.HasRows)
                {
                    Note note = new();

                    while (reader.Read())
                    {
                        note.Id = (int)reader["Id"];
                        note.UserId = (int)reader["UserId"];
                        note.Title = (string)reader["Title"];
                        note.FilePath = (string)reader["FilePath"];
                        note.UploadedAt = (DateTime)reader["UploadedAt"];

                        NoteList.Add(note);
                    }
                }

                reader.Close();
            }

            return NoteList;
        }

        public bool UpdateNote(Note note, string title)
        {
            try
            {
                using SqlConnection mentornoteConnection = new SqlConnection(connectionString);
                {
                    mentornoteConnection.Open();
                    SqlCommand UpdateNote = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = mentornoteConnection,
                        CommandText = "UpdateNote"
                    };
                    SqlParameter noteIdParameter = new SqlParameter
                    {
                        ParameterName = "@NoteId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = note.Id
                    };
                    SqlParameter userIdParameter = new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = note.UserId
                    };
                    SqlParameter titlePaarameter = new SqlParameter
                    {
                        ParameterName = "@Title",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = title
                    };
                    UpdateNote.Parameters.Add(noteIdParameter);
                    UpdateNote.Parameters.Add(titlePaarameter);
                    UpdateNote.Parameters.Add(userIdParameter);
                    UpdateNote.ExecuteNonQuery();
                    mentornoteConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        public string AddNoteSummary(NoteSummary noteSummary)
        {
            try
            {
                using SqlConnection mentornoteConnection = new SqlConnection(connectionString);
                {
                    mentornoteConnection.Open();

                    SqlCommand AddNote = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = mentornoteConnection,
                        CommandText = "AddNoteSummary"
                    };

                    SqlParameter noteIdParameter = new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteSummary.NoteId
                    };

                    SqlParameter summaryPaarameter = new SqlParameter
                    {
                        ParameterName = "@SummaryText",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteSummary.SummaryText
                    };

                   
                    AddNote.Parameters.Add(noteIdParameter);
                    AddNote.Parameters.Add(summaryPaarameter);
                    AddNote.ExecuteNonQuery();
                    mentornoteConnection.Close();

                }

                return "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
        }
    }
}