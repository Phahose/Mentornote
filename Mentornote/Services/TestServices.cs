#nullable disable
using Mentornote.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace Mentornote.Services
{
    public class TestServices
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Helpers _helpers;


        public TestServices(HttpClient httpClient, IConfiguration config, Helpers helpers)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
        }

        public async Task<bool> CreateTestQuestion(int noteId, int userId)
        {
            CardsServices cardsServices = new();


            Note activenote = cardsServices.GetNoteById(noteId, userId);
            if (activenote == null) throw new Exception("Note not found.");


            //Find the File 
            string filePath = activenote.FilePath;
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(physicalPath))
                throw new FileNotFoundException("PDF not found at: " + physicalPath);

            //xtract the Test from File
            string text;

            using var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read);
            IFormFile fakeFormFile = new FormFile(fs, 0, fs.Length, "file", Path.GetFileName(physicalPath));

            text = _helpers.ExtractText(fakeFormFile);

            //Create The Test
            Test test = new() 
            {
                NoteId = noteId,
                UserId = userId.ToString(),
                Title = activenote.Title + " Test",
                TotalQuestions = 0
            };
           
            int testId = cardsServices.AddTest(test);


            //Chunk Text
            var chunks = _helpers.ChunkText(text);


        
            foreach (var chunk in chunks)
            {
                try
                {
                    List<TestQuestion> questionsFromChunk = await GenerateQuestionsFromChunkAsync(chunk);

                    // Save Questions to DB
                   
                    test.Id = testId;

                    foreach (TestQuestion q in questionsFromChunk)
                    {
                        q.TestId = test.Id;
                        int questionId =  cardsServices.AddTestQuestion(q);

                        foreach (var choice in q.Choices)
                        {
                            choice.TestQuestionId = questionId;
                            cardsServices.AddTestQuestionChoice(choice);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in chunk {chunk}: {ex.Message}");
                    continue; // skip this one and keep going;
                }
            }
            return true;
        }

        public async Task<List<TestQuestion>> GenerateQuestionsFromChunkAsync(string chunk)
        {
            List<TestQuestion> questions = new();
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            var prompt = $@"
                    Generate a set of multiple-choice practice test questions from the following notes:

                    {chunk}

                    Return the result strictly as a JSON array. 
                    Each object in the array must include:
                    - 'question' (string)
                    - 'choices' (array of strings, at least 4 choices)
                    - 'answer' (string, the correct answer from choices)
                ";

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                         new { role = "user", content = prompt }
                    },
                temperature = 0.7,
                max_tokens = 800
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<OpenAIResponse>(json);
            var content = responseObj?.choices?[0]?.message?.content?.Trim();

            if (string.IsNullOrEmpty(content))
                throw new Exception("No questions generated from OpenAI.");

            // Step 4: Deserialize into your TestQuestion model
            var generatedQuestions = JsonSerializer.Deserialize<List<TestQuestion>>(content);

            if (generatedQuestions == null || generatedQuestions.Count == 0)
                throw new Exception("No questions generated.");

            // Step 5: Map raw choices into TestQuestionChoice objects
            foreach (var q in generatedQuestions)
            {
                var question = new TestQuestion
                {
                    QuestionText = q.QuestionText,
                    AnswerText = q.AnswerText,
                    QuestionType = "MultipleChoice",
                    Choices = q.ChoicesRaw.Select(c => new TestQuestionChoice
                    {
                        ChoiceText = c,
                        IsCorrect = (c == q.AnswerText)
                    }).ToList()
                };

                questions.Add(question);
            }
            return questions;
        }

    }
}

