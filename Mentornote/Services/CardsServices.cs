using Mentornote.Models;
using Microsoft.Data.SqlClient;
using System.Data;
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
                        set.NoteId = reader["NoteId"] == DBNull.Value ? 0 : (int)reader["NoteId"];
                        flashcardSetMap.Add(flashcardSetId, set);
                        flashcardSets.Add(set);
                    }

                    // Create a flashcard object and add it to the set
                    Flashcard flashcard = new Flashcard
                    {
                        Id = (int)reader["FlashcardId"],
                        Front = (string)reader["Front"],
                        Back = (string)reader["Back"],
                        NoteId = reader["NoteId"] == DBNull.Value ? 0 : (int)reader["NoteId"]

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

                    SqlParameter SourceType = new SqlParameter
                    {
                        ParameterName = "@SourceType",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = note.SourceType
                    };

                    SqlParameter SourceURL = new SqlParameter
                    {
                        ParameterName = "@SourceUrl",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = note.SourceUrl
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
                    AddNote.Parameters.Add(SourceType);
                    AddNote.Parameters.Add(SourceURL);

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
                    while (reader.Read())
                    {
                        Note note = new();

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

        public Note GetNoteById(int noteId, int userId)
        {
            Note note = new();

            using (SqlConnection mentornoteConnection = new SqlConnection(connectionString))
            {
                mentornoteConnection.Open();

                SqlCommand getNote = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = mentornoteConnection,
                    CommandText = "GetNoteById"
                };

                SqlParameter noteIdParameter = new SqlParameter
                {
                    ParameterName = "@NoteId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = noteId
                };

                SqlParameter userIdParameter = new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = userId
                };

                getNote.Parameters.Add(noteIdParameter);
                getNote.Parameters.Add(userIdParameter);

                SqlDataReader reader = getNote.ExecuteReader();

                if (reader.HasRows && reader.Read())
                {
                    note = new Note
                    {
                        Id = (int)reader["Id"],
                        UserId = (int)reader["UserId"],
                        Title = (string)reader["Title"],
                        FilePath = (string)reader["FilePath"],
                        UploadedAt = (DateTime)reader["UploadedAt"]
                    };
                }

                reader.Close();
            }

            return note;
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
                        ParameterName = "@UploadedNoteId",
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

        public NoteSummary GetUserNotesSummary(int noteId)
        {
            NoteSummary notesummary = new();
            using (SqlConnection mentornoteConnection = new SqlConnection(connectionString))
            {
                mentornoteConnection.Open();

                SqlCommand getFlashcards = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = mentornoteConnection,
                    CommandText = "GetNotesSummary"
                };

                SqlParameter noteIdParameter = new SqlParameter
                {
                    ParameterName = "@NoteId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = noteId
                };

                getFlashcards.Parameters.Add(noteIdParameter);

                SqlDataReader reader = getFlashcards.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        notesummary.Id = (int)reader["Id"];
                        notesummary.NoteId = (int)reader["UploadedNoteId"];
                        notesummary.SummaryText = (string)reader["SummaryText"];
                        notesummary.CreatedAt = (DateTime)reader["GeneratedAt"];
                    }
                }

                reader.Close();
            }

            return notesummary;
        }

        public string AddNoteEmbedding(NoteEmbedding noteEmbedding)
        {
            try
            {
                using SqlConnection mentornoteConnection = new SqlConnection(connectionString);
                {
                    mentornoteConnection.Open();

                    SqlCommand addEmbedding = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = mentornoteConnection,
                        CommandText = "AddNoteEmbedding"
                    };

                    addEmbedding.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@NoteId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteEmbedding.NoteId
                    });

                    addEmbedding.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@ChunkText",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteEmbedding.ChunkText
                    });

                    addEmbedding.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@EmbeddingJson",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteEmbedding.EmbeddingJson
                    });

                    addEmbedding.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@ChunkIndex",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteEmbedding.ChunkIndex
                    });

                    addEmbedding.ExecuteNonQuery();
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

        public List<NoteEmbedding> GetNoteEmbeddingsByNoteId(int noteId)
        {
            var embeddings = new List<NoteEmbedding>();

            try
            {
                using SqlConnection mentornoteConnection = new SqlConnection(connectionString);
                {
                    mentornoteConnection.Open();

                    SqlCommand cmd = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = mentornoteConnection,
                        CommandText = "GetNoteEmbeddingsByNoteId"
                    };

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@NoteId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteId
                    });

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var noteEmbedding = new NoteEmbedding
                        {
                            NoteId = noteId,
                            ChunkText = reader["ChunkText"].ToString(),
                            EmbeddingJson = reader["EmbeddingJson"].ToString(),
                            ChunkIndex = Convert.ToInt32(reader["ChunkIndex"])
                        };

                        embeddings.Add(noteEmbedding);
                    }

                    mentornoteConnection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving embeddings: {ex.Message}");
            }

            return embeddings;
        }

        public string AddTutorMessage(TutorMessage tutorMessage)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = conn,
                        CommandText = "AddTutorMessage" // Stored procedure name
                    };

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@NoteId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = tutorMessage.NoteId
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = tutorMessage.UserId
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@Message",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = tutorMessage.Message
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@Response",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = tutorMessage.Response
                    });

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                return "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
        }

        public List<TutorMessage> GetTutorMessages(int noteId, int userId)
        {
            var messages = new List<TutorMessage>();

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = conn,
                        CommandText = "GetTutorMessagesByNoteId" // Your stored proc
                    };

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@NoteId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = noteId
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = userId
                    });

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        messages.Add(new TutorMessage
                        {
                            NoteId = noteId,
                            UserId = userId,
                            Message = reader["Message"].ToString(),
                            Response = reader["Response"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        });
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return messages;
        }

        public int AddTest(Test test)
        {
            try
            {
                int testId;
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand addTest = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "AddPracticeExam"
                    };

                    SqlParameter noteIdParam = new SqlParameter
                    {
                        ParameterName = "@NoteId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = test.NoteId
                    };

                    SqlParameter userIdParam = new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.NVarChar,
                        Size = 450,
                        Direction = ParameterDirection.Input,
                        SqlValue = test.UserId
                    };

                    SqlParameter titleParam = new SqlParameter
                    {
                        ParameterName = "@Title",
                        SqlDbType = SqlDbType.NVarChar,
                        Size = 255,
                        Direction = ParameterDirection.Input,
                        SqlValue = test.Title
                    };

                    SqlParameter totalQuestionsParam = new SqlParameter
                    {
                        ParameterName = "@TotalQuestions",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = test.TotalQuestions
                    };

                    // Output param
                    var outputIdParam = new SqlParameter("@NewExamId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };

                    addTest.Parameters.Add(noteIdParam);
                    addTest.Parameters.Add(userIdParam);
                    addTest.Parameters.Add(titleParam);
                    addTest.Parameters.Add(totalQuestionsParam);
                    addTest.Parameters.Add(outputIdParam);

                    addTest.ExecuteNonQuery();
                    connection.Close();

                    testId = (int)outputIdParam.Value;
                }

                return testId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public int AddTestQuestion(TestQuestion question)
        {
            try
            {
                int questionId;
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand addQuestion = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "AddPracticeExamQuestion" // DB procedure name
                    };

                    SqlParameter examIdParam = new SqlParameter
                    {
                        ParameterName = "@PracticeExamId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = question.TestId
                    };

                    SqlParameter questionTextParam = new SqlParameter
                    {
                        ParameterName = "@QuestionText",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = question.QuestionText
                    };

                    SqlParameter answerTextParam = new SqlParameter
                    {
                        ParameterName = "@AnswerText",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = question.AnswerText
                    };

                    SqlParameter questionTypeParam = new SqlParameter
                    {
                        ParameterName = "@QuestionType",
                        SqlDbType = SqlDbType.NVarChar,
                        Size = 50,
                        Direction = ParameterDirection.Input,
                        SqlValue = question.QuestionType
                    };

                    // Output param
                    var outputIdParam = new SqlParameter("@NewQuestionId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };

                    addQuestion.Parameters.Add(examIdParam);
                    addQuestion.Parameters.Add(questionTextParam);
                    addQuestion.Parameters.Add(answerTextParam);
                    addQuestion.Parameters.Add(questionTypeParam);
                    addQuestion.Parameters.Add(outputIdParam);

                    addQuestion.ExecuteNonQuery();
                    connection.Close();

                    questionId = (int)outputIdParam.Value;
                }

                return questionId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public int AddTestQuestionChoice(TestQuestionChoice choice)
        {
            try
            {
                int choiceId;
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand addChoice = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "AddPracticeQuestionChoice" // DB procedure name
                    };

                    SqlParameter questionIdParam = new SqlParameter
                    {
                        ParameterName = "@PracticeExamQuestionId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = choice.TestQuestionId
                    };

                    SqlParameter choiceTextParam = new SqlParameter
                    {
                        ParameterName = "@ChoiceText",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = choice.ChoiceText
                    };

                    SqlParameter isCorrectParam = new SqlParameter
                    {
                        ParameterName = "@IsCorrect",
                        SqlDbType = SqlDbType.Bit,
                        Direction = ParameterDirection.Input,
                        SqlValue = choice.IsCorrect
                    };

                    // Output param
                    var outputIdParam = new SqlParameter("@NewChoiceId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };

                    addChoice.Parameters.Add(questionIdParam);
                    addChoice.Parameters.Add(choiceTextParam);
                    addChoice.Parameters.Add(isCorrectParam);
                    addChoice.Parameters.Add(outputIdParam);

                    addChoice.ExecuteNonQuery();
                    connection.Close();

                    choiceId = (int)outputIdParam.Value;
                }

                return choiceId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public List<Test>? GetTestsWithQuestions(int noteId)
        {
            List<Test> tests = new();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand cmd = new SqlCommand("GetPracticeExamWithQuestions", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@NoteId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = noteId
                });

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // --- First result set: Exams ---
                    while (reader.HasRows && reader.Read())
                    {
                        var test = new Test
                        {
                            Id = (int)reader["Id"],
                            NoteId = (int)reader["NoteId"],
                            UserId = reader["UserId"].ToString(),
                            Title = reader["Title"].ToString(),
                            Score = (int)reader["Score"],
                            TotalQuestions = (int)reader["TotalQuestions"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            CompletedAt = reader["CompletedAt"] == DBNull.Value ? null : (DateTime?)reader["CompletedAt"],
                            Questions = new List<TestQuestion>()
                        };

                        tests.Add(test);
                    }


                    if(tests.Count > 0 && tests != null)
                    {
                        // --- Second result set: Questions ---
                        if (reader.NextResult() && reader.HasRows)
                        {
                            var questionLookup = new Dictionary<int, TestQuestion>();

                            while (reader.Read())
                            {
                                var question = new TestQuestion
                                {
                                    Id = (int)reader["Id"],
                                    TestId = (int)reader["PracticeExamId"],
                                    QuestionText = reader["QuestionText"].ToString(),
                                    AnswerText = reader["AnswerText"].ToString(),
                                    QuestionType = reader["QuestionType"].ToString(),
                                    Choices = new List<TestQuestionChoice>()
                                };

                                // Attach to correct exam
                                var parentTest = tests.FirstOrDefault(t => t.Id == question.TestId);
                                parentTest?.Questions.Add(question);

                                questionLookup[question.Id] = question;
                            }

                            // --- Third result set: Choices ---
                            if (reader.NextResult() && reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var choice = new TestQuestionChoice
                                    {
                                        Id = (int)reader["Id"],
                                        TestQuestionId = (int)reader["PracticeExamQuestionId"],
                                        ChoiceText = reader["ChoiceText"].ToString(),
                                        IsCorrect = (bool)reader["IsCorrect"]
                                    };

                                    if (questionLookup.ContainsKey(choice.TestQuestionId))
                                    {
                                        questionLookup[choice.TestQuestionId].Choices.Add(choice);
                                    }
                                }
                            }
                        }
                    }
                   
                }
            }

            return tests;
        }

        public bool AddSpeechCapture(SpeechCapture capture)
        {
            try
            {

                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand addCapture = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "AddSpeechCapture"
                    };

                    SqlParameter userIdParam = new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = capture.UserId
                    };

                    SqlParameter transcriptFileParam = new SqlParameter
                    {
                        ParameterName = "@TranscriptFilePath",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = capture.TranscriptFilePath
                    };

                    SqlParameter audioFileParam = new SqlParameter
                    {
                        ParameterName = "@AudioFilePath",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = capture.AudioFilePath
                    };

                    SqlParameter durationParam = new SqlParameter
                    {
                        ParameterName = "@DurationSeconds",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = capture.DurationSeconds
                    };

                    SqlParameter titleParameter = new SqlParameter
                    {
                        ParameterName = "@Title",
                        SqlDbType = SqlDbType.NVarChar,
                        Direction = ParameterDirection.Input,
                        SqlValue = capture.Title
                    };

                    addCapture.Parameters.Add(userIdParam);
                    addCapture.Parameters.Add(transcriptFileParam);
                    addCapture.Parameters.Add(durationParam);
                    addCapture.Parameters.Add(titleParameter);
                    addCapture.Parameters.Add(audioFileParam);

                    addCapture.ExecuteNonQuery();
                    connection.Close();

                    
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public SpeechCapture GetSpeechCaptureById(int id)
        {
            SpeechCapture capture = new();

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand getCapture = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "GetSpeechCaptureById"
                    };

                    SqlParameter idParam = new SqlParameter
                    {
                        ParameterName = "@Id",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = id
                    };

                    getCapture.Parameters.Add(idParam);

                    SqlDataReader reader = getCapture.ExecuteReader();

                    if (reader.Read())
                    {
                        capture = new SpeechCapture
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            UserId = Convert.ToInt32(reader["UserId"]),
                            TranscriptFilePath = reader["TranscriptFilePath"].ToString(),
                            AudioFilePath = reader["AudioFilePath"].ToString(),
                            DurationSeconds = reader["DurationSeconds"] != DBNull.Value ? Convert.ToInt32(reader["DurationSeconds"]) : 0,
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            Title = (string)reader["Title"]
                        };
                    }

                    reader.Close();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return capture;
        }

        public List<SpeechCapture> GetAllSpeechCaptures(int? userId = null)
        {
            List<SpeechCapture> captures = new List<SpeechCapture>();

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand getAll = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "GetAllSpeechCaptures"
                    };

                    SqlParameter userIdParam = new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = userId.HasValue ? userId.Value : DBNull.Value
                    };

                    getAll.Parameters.Add(userIdParam);

                    SqlDataReader reader = getAll.ExecuteReader();

                    while (reader.Read())
                    {
                        SpeechCapture capture = new SpeechCapture
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            UserId = Convert.ToInt32(reader["UserId"]),
                            TranscriptFilePath = reader["TranscriptFilePath"].ToString(),
                            AudioFilePath = reader["AudioFilePath"].ToString(),
                            DurationSeconds = reader["DurationSeconds"] != DBNull.Value ? Convert.ToInt32(reader["DurationSeconds"]) : 0,
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            Title = (string)reader["Title"]
                        };
                        captures.Add(capture);
                    }

                    reader.Close();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return captures;
        }

        public bool DeleteSpeechCapture(int id)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand deleteCapture = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "DeleteSpeechCapture"
                    };

                    SqlParameter idParam = new SqlParameter
                    {
                        ParameterName = "@Id",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = id
                    };

                    deleteCapture.Parameters.Add(idParam);

                    int rowsAffected = deleteCapture.ExecuteNonQuery();
                    connection.Close();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool AddSpeechCaptureSummary(SpeechCaptureSummary speechCaptureSummary)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                SqlCommand addSummary = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "AddSpeechCaptureSummary"
                };

                addSummary.Parameters.AddWithValue("@SpeechCaptureId", speechCaptureSummary.SpeechCaptureId);
                addSummary.Parameters.AddWithValue("@SummaryText", speechCaptureSummary.SummaryText);

                addSummary.ExecuteNonQuery();
                connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public List<SpeechCaptureSummary> GetSpeechCaptureSummaryByCapture(int captureId)
        {
            List<SpeechCaptureSummary> summaries = new List<SpeechCaptureSummary>();

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand getSummary = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "GetSpeechCaptureSummaryByCapture"
                    };

                    SqlParameter captureIdParam = new SqlParameter
                    {
                        ParameterName = "@CaptureId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = captureId
                    };

                    getSummary.Parameters.Add(captureIdParam);

                    SqlDataReader reader = getSummary.ExecuteReader();

                    while (reader.Read())
                    {
                        SpeechCaptureSummary summary = new SpeechCaptureSummary
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            SpeechCaptureId = Convert.ToInt32(reader["SpeechCaptureId"]),
                            SummaryText = reader["SummaryText"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };
                        summaries.Add(summary);
                    }

                    reader.Close();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return summaries;
        }

        public bool DeleteSpeechCaptureSummary(int id)
        {
            bool isDeleted = false;

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand deleteCommand = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "DeleteSpeechCaptureSummary"
                    };

                    SqlParameter idParam = new SqlParameter
                    {
                        ParameterName = "@Id",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = id
                    };

                    deleteCommand.Parameters.Add(idParam);

                    int rowsAffected = deleteCommand.ExecuteNonQuery();

                    isDeleted = rowsAffected > 0;

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return isDeleted;
        }

        public bool AddSpeechCaptureEmbedding(SpeechCaptureEmbedding speechCaptureEmbedding)
        {
            bool isAdded = false;

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand addCommand = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "AddSpeechCaptureEmbedding"
                    };

                    addCommand.Parameters.AddWithValue("@CaptureId", speechCaptureEmbedding.CaptureId);
                    addCommand.Parameters.AddWithValue("@ChunkIndex", speechCaptureEmbedding.ChunkIndex);
                    addCommand.Parameters.AddWithValue("@ChunkText", speechCaptureEmbedding.ChunkText);
                    addCommand.Parameters.AddWithValue("@Embedding", speechCaptureEmbedding.Embedding);

                    int rowsAffected = addCommand.ExecuteNonQuery();
                    isAdded = rowsAffected > 0;

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return isAdded;
        }

        public List<SpeechCaptureEmbedding> GetSpeechCaptureEmbeddings(int captureId)
        {
            List<SpeechCaptureEmbedding> embeddings = new List<SpeechCaptureEmbedding>();

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                {
                    connection.Open();

                    SqlCommand getCommand = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = "GetSpeechCaptureEmbeddingById"
                    };

                    SqlParameter summaryIdParam = new SqlParameter
                    {
                        ParameterName = "@CaptureId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                        SqlValue = captureId
                    };

                    getCommand.Parameters.Add(summaryIdParam);

                    SqlDataReader reader = getCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        SpeechCaptureEmbedding embedding = new SpeechCaptureEmbedding
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            CaptureId = Convert.ToInt32(reader["CaptureId"]),
                            ChunkIndex = Convert.ToInt32(reader["ChunkIndex"]),
                            ChunkText = reader["ChunkText"].ToString(),
                            Embedding = reader["Embedding"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };
                        embeddings.Add(embedding);
                    }

                    reader.Close();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return embeddings;
        }




        public void AddSpeechCaptureChat(SpeechCaptureChat chat)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                SqlCommand command = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "AddSpeechCaptureChat" // DB procedure name
                };

                SqlParameter captureIdParam = new SqlParameter
                {
                    ParameterName = "@SpeechCaptureId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = chat.SpeechCaptureId
                };

                SqlParameter userIdParam = new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = chat.UserId
                };

                SqlParameter senderTypeParam = new SqlParameter
                {
                    ParameterName = "@SenderType",
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                    SqlValue = chat.SenderType
                };

                SqlParameter messageParam = new SqlParameter
                {
                    ParameterName = "@Message",
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                    SqlValue = chat.Message
                };

                SqlParameter responseParam = new SqlParameter
                {
                    ParameterName = "@Response",
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                    SqlValue = chat.Response
                };

                command.Parameters.Add(captureIdParam);
                command.Parameters.Add(userIdParam);
                command.Parameters.Add(senderTypeParam);
                command.Parameters.Add(messageParam);
                command.Parameters.Add(responseParam);

                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<SpeechCaptureChat> GetSpeechCaptureChat(int speechCaptureId)
        {
            List<SpeechCaptureChat> chatMessages = new List<SpeechCaptureChat>();

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                SqlCommand command = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "GetSpeechCaptureChat"
                };

                SqlParameter captureIdParam = new SqlParameter
                {
                    ParameterName = "@SpeechCaptureId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = speechCaptureId
                };

                command.Parameters.Add(captureIdParam);

                using SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    SpeechCaptureChat chat = new SpeechCaptureChat
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        SpeechCaptureId = Convert.ToInt32(reader["SpeechCaptureId"]),
                        UserId = Convert.ToInt32(reader["UserId"]),
                        SenderType = reader["SenderType"].ToString(),
                        Message = reader["Message"].ToString(),
                        Response = reader["Response"].ToString(),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    };

                    chatMessages.Add(chat);
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return chatMessages;
        }

        public void DeleteSpeechCaptureChat(int speechCaptureId)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                SqlCommand command = new SqlCommand
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = connection,
                    CommandText = "DeleteSpeechCaptureChat" // DB procedure name
                };

                SqlParameter captureIdParam = new SqlParameter
                {
                    ParameterName = "@SpeechCaptureId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Input,
                    SqlValue = speechCaptureId
                };

                command.Parameters.Add(captureIdParam);

                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}