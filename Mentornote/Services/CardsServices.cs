using Elfie.Serialization;
using Mentornote.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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

    }
}
