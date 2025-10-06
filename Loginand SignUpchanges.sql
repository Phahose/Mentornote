-- 1. Add 'AuthProvider' columns to Users table
ALTER TABLE Users
ADD 
    AuthProvider NVARCHAR(100);      -- E.g., "Email", "Google", "Apple"

-- 2. Drop old 'PasswordHash' column if it exists as NVARCHAR
ALTER TABLE Users
DROP COLUMN PasswordHash;

-- 3. Add corrected 'PasswordHash' and 'PasswordSalt' columns as VARBINARY
ALTER TABLE Users
ADD 
    PasswordHash VARBINARY(MAX) NOT NULL,
    PasswordSalt VARBINARY(MAX) NOT NULL;


DROP TABLE Users;

EXEC sp_rename 'Users', 'Users_OLD';

-- Create new table
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash VARBINARY(MAX) NOT NULL,
    PasswordSalt VARBINARY(MAX) NOT NULL,
    AuthProvider NVARCHAR(100)
);

-- Flashcard Changes 
DROP TABLE IF EXISTS Flashcards;
DROP TABLE IF EXISTS FlashcardSets;


CREATE TABLE FlashcardSets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserId INT NOT NULL,
    Source NVARCHAR(MAX),
    PromptUsed NVARCHAR(MAX)
);

CREATE TABLE Flashcards (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Front NVARCHAR(MAX) NOT NULL,
    Back NVARCHAR(MAX) NOT NULL,
    FlashcardSetId INT NOT NULL,
    FOREIGN KEY (FlashcardSetId) REFERENCES FlashcardSets(Id)
);


ALTER TABLE Flashcards
ADD NoteId INT NULL;

CREATE PROCEDURE GetUserByEmail
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        Id,
        Email,
        PasswordHash,
        PasswordSalt,
        AuthProvider,
        FirstName,
        LastName
    FROM dbo.Users
    WHERE Email = @Email;
END;



CREATE PROCEDURE GetUserFlashcards
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        fcs.Id AS FlashcardSetId,
        fcs.Title AS FlashcardSetTitle,
        fcs.CreatedAt,
        fcs.Source,
		fcs.UserId,
        f.Id AS FlashcardId,
        f.Front,
        f.Back,
		f.NoteId
    FROM dbo.FlashcardSets fcs
    INNER JOIN dbo.Flashcards f 
        ON fcs.Id = f.FlashcardSetId
    WHERE fcs.UserId = @UserId
    ORDER BY fcs.CreatedAt DESC, f.Id;
END;

Drop Procedure GetUserFlashcards


DELETE FROM FlashcardSets

ALTER TABLE FlashcardSets
Add  NoteId nvarchar(max) NULL;

--Other functionalities
CREATE TABLE Notes (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    Title NVARCHAR(255),
    FilePath NVARCHAR(MAX), -- Where the uploaded file is stored
    UploadedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);


ALTER TABLE Notes
Add SourceURL nvarchar(max) NULL;

CREATE TABLE NoteSummaries (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UploadedNoteId INT NOT NULL,
    SummaryText NVARCHAR(MAX) NOT NULL,
    GeneratedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_NoteSummaries_UploadedNotes FOREIGN KEY (UploadedNoteId) REFERENCES Notes(Id)
);


CREATE TABLE TutorMessages (
    Id INT PRIMARY KEY IDENTITY,
    NoteId INT NOT NULL,
    UserId INT NOT NULL,
    Message TEXT NOT NULL,
    Response TEXT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (NoteId) REFERENCES Notes(Id)
);

-- PRACTICE EXAMS
CREATE TABLE PracticeExams (
    Id INT PRIMARY KEY IDENTITY(1,1),
    NoteId INT NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Score INT, -- Out of 100 or raw score depending on your logic
    TotalQuestions INT,
    CompletedAt DATETIME,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (NoteId) REFERENCES Notes(Id)
);

-- PRACTICE EXAM QUESTIONS
CREATE TABLE PracticeExamQuestions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PracticeExamId INT NOT NULL,
    QuestionText NVARCHAR(MAX) NOT NULL,
    AnswerText NVARCHAR(MAX), -- for short/long answer or correct MCQ answer
    QuestionType NVARCHAR(50) NOT NULL, -- e.g., 'MultipleChoice', 'ShortAnswer'
    FOREIGN KEY (PracticeExamId) REFERENCES PracticeExams(Id)
);

-- PRACTICE EXAM QUESTION CHOICES (only used for multiple choice questions)
CREATE TABLE PracticeQuestionChoices (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PracticeExamQuestionId INT NOT NULL,
    ChoiceText NVARCHAR(MAX) NOT NULL,
    IsCorrect BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (PracticeExamQuestionId) REFERENCES PracticeExamQuestions(Id)
);

CREATE PROCEDURE AddNote
	@UserId INT,
    @Title NVARCHAR(255),
    @FilePath NVARCHAR(MAX),
	@NewNoteId INT OUTPUT,
	@SourceType NVARCHAR(MAX),
	@SourceUrl NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Notes (UserId, Title, FilePath, UploadedAt,SourceType,SourceURL)
    VALUES (@UserId, @Title, @FilePath, SYSDATETIME(),@SourceType,@SourceUrl);

	-- Get the ID of the inserted note
    SET @NewNoteId = SCOPE_IDENTITY()
END;

DROP PROCEDURE AddNote

CREATE PROCEDURE DeleteNote
    @NoteId INT
AS
BEGIN
    SET NOCOUNT ON;

	DELETE FROM NoteSummaries WHERE UploadedNoteId = @NoteId
	DELETE FROM Flashcards WHERE NoteId = @NoteId
	DELETE FROM FlashcardSets WHERE NoteId = @NoteId
	DELETE FROM NoteEmbeddings WHERE NoteId = @NoteId
	DELETE FROM TutorMessages WHERE NoteId = @NoteId
    DELETE FROM dbo.Notes
    WHERE Id = @NoteId
	Exec DeletePracticeExamByNote @NoteId;
END


DROP PROCEDURE DeleteNote

CREATE PROCEDURE GetNotes
	@UserId INT 
AS 
BEGIN
SET NOCOUNT ON;
	  SELECT 
	   Id,
	   UserId,
	   Title,
	   FilePath,
	   UploadedAt
	   FROM Notes WHERE UserId = @UserId
END

CREATE PROCEDURE GetNoteById
    @NoteId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        Id,
        UserId,
        Title,
        FilePath,
        UploadedAt
    FROM Notes
    WHERE Id = @NoteId AND UserId = @UserId;
END


Exec   GetNotes 1

CREATE PROCEDURE UpdateNote
    @NoteId INT,
    @UserId INT,
    @Title NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Notes
    SET
        Title = @Title
    WHERE
        Id = @NoteId AND UserId = @UserId;
END;

CREATE PROCEDURE AddNoteSummary
    @UploadedNoteId INT,
    @SummaryText NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO dbo.NoteSummaries (UploadedNoteId, SummaryText, GeneratedAt)
    VALUES (@UploadedNoteId, @SummaryText, SYSDATETIME())
END


CREATE PROCEDURE GetNotesSummary
	@NoteId INT 
AS 
BEGIN
SET NOCOUNT ON;
	  SELECT 
	   Id,
	   UploadedNoteId,
	   SummaryText,
	   GeneratedAt
	   FROM NoteSummaries WHERE UploadedNoteId = @NoteId
END


-- 1. Create Table: NoteEmbeddings
CREATE TABLE NoteEmbeddings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    NoteId INT NOT NULL,
    ChunkText NVARCHAR(MAX) NOT NULL,
    EmbeddingJson NVARCHAR(MAX) NOT NULL,
    ChunkIndex INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (NoteId) REFERENCES Notes(Id) 
);

-- 2. Stored Procedure: Add a Chunk + Embedding
CREATE PROCEDURE AddNoteEmbedding
    @NoteId INT,
    @ChunkText NVARCHAR(MAX),
    @EmbeddingJson NVARCHAR(MAX),
    @ChunkIndex INT
AS
BEGIN
    INSERT INTO NoteEmbeddings (NoteId, ChunkText, EmbeddingJson, ChunkIndex)
    VALUES (@NoteId, @ChunkText, @EmbeddingJson, @ChunkIndex);
END;

-- 3. Stored Procedure: Get All Embeddings for a Note
CREATE PROCEDURE GetNoteEmbeddingsByNoteId
    @NoteId INT
AS
BEGIN
    SELECT ChunkText, EmbeddingJson, ChunkIndex
    FROM NoteEmbeddings
    WHERE NoteId = @NoteId
    ORDER BY ChunkIndex;
END;

-- 4. Stored Procedure: Delete All Embeddings for a Note
CREATE PROCEDURE DeleteNoteEmbeddingsByNoteId
    @NoteId INT
AS
BEGIN
    DELETE FROM NoteEmbeddings
    WHERE NoteId = @NoteId;
END;

-- 5. Stored Procedure: Get Specific Chunk (Optional)
CREATE PROCEDURE GetNoteEmbeddingByChunkIndex
    @NoteId INT,
    @ChunkIndex INT
AS
BEGIN
    SELECT * FROM NoteEmbeddings
    WHERE NoteId = @NoteId AND ChunkIndex = @ChunkIndex;
END;

-- 6. Stored Procedure: Delete One Chunk (Optional)
CREATE PROCEDURE DeleteNoteEmbeddingByChunkIndex
    @NoteId INT,
    @ChunkIndex INT
AS
BEGIN
    DELETE FROM NoteEmbeddings
    WHERE NoteId = @NoteId AND ChunkIndex = @ChunkIndex;
END;


CREATE PROCEDURE AddTutorMessage
    @NoteId INT,
    @UserId INT,
    @Message NVARCHAR(MAX),
    @Response NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO TutorMessages (NoteId, UserId, Message, Response, CreatedAt)
    VALUES (@NoteId, @UserId, @Message, @Response, GETDATE());
END;

CREATE PROCEDURE GetTutorMessagesByNoteId
    @NoteId INT,
    @UserId INT
AS
BEGIN
    SELECT Message, Response, CreatedAt
    FROM TutorMessages
    WHERE NoteId = @NoteId AND UserId = @UserId
    ORDER BY CreatedAt;
END;


CREATE PROCEDURE AddPracticeExam
    @NoteId INT,
    @UserId NVARCHAR(450),
    @Title NVARCHAR(255),
    @TotalQuestions INT,
	@NewExamId INT OUTPUT
AS
BEGIN
    INSERT INTO PracticeExams (NoteId, UserId, Title, Score, TotalQuestions, CreatedAt)
    VALUES (@NoteId, @UserId, @Title, 0, @TotalQuestions, GETDATE());

    SET @NewExamId = SCOPE_IDENTITY() ;
END


Drop Procedure CreatePracticeExam

CREATE PROCEDURE AddPracticeExamQuestion
    @PracticeExamId INT,
    @QuestionText NVARCHAR(MAX),
    @AnswerText NVARCHAR(MAX),
    @QuestionType NVARCHAR(50),
	@NewQuestionId INT OUTPUT
AS
BEGIN
    INSERT INTO PracticeExamQuestions (PracticeExamId, QuestionText, AnswerText, QuestionType)
    VALUES (@PracticeExamId, @QuestionText, @AnswerText, @QuestionType);

    SET @NewQuestionId = SCOPE_IDENTITY();
END

Drop Procedure AddPracticeExamQuestion

CREATE PROCEDURE AddPracticeQuestionChoice
    @PracticeExamQuestionId INT,
    @ChoiceText NVARCHAR(MAX),
    @IsCorrect BIT,
	@NewChoiceId INT OUTPUT
AS
BEGIN
    INSERT INTO PracticeQuestionChoices (PracticeExamQuestionId, ChoiceText, IsCorrect)
    VALUES (@PracticeExamQuestionId, @ChoiceText, @IsCorrect);

	SET @NewChoiceId = SCOPE_IDENTITY();
END

Drop Procedure AddPracticeQuestionChoice



CREATE PROCEDURE GetPracticeExamWithQuestions
     @NoteId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Get the exam linked to this note
    SELECT * 
    FROM PracticeExams
    WHERE NoteId = @NoteId;

    -- Get the questions for that exam
    SELECT q.* 
    FROM PracticeExamQuestions q
    INNER JOIN PracticeExams e ON e.Id = q.PracticeExamId
    WHERE e.NoteId = @NoteId;

    -- Get the choices for those questions
    SELECT c.* 
    FROM PracticeQuestionChoices c
    INNER JOIN PracticeExamQuestions q ON q.Id = c.PracticeExamQuestionId
    INNER JOIN PracticeExams e ON e.Id = q.PracticeExamId
    WHERE e.NoteId = @NoteId;
END

DROP PROCEDURE GetPracticeExamWithQuestions

CREATE PROCEDURE CompletePracticeExam
    @ExamId INT,
    @Score INT
AS
BEGIN
    UPDATE PracticeExams
    SET Score = @Score,
        CompletedAt = GETDATE()
    WHERE Id = @ExamId;
END


CREATE PROCEDURE DeletePracticeExam
    @PracticeExamId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Step 1: Delete Choices for Questions under this exam
        DELETE FROM PracticeQuestionChoices
        WHERE PracticeExamQuestionId IN (
            SELECT Id FROM PracticeExamQuestions WHERE PracticeExamId = @PracticeExamId
        );

        -- Step 2: Delete Questions under this exam
        DELETE FROM PracticeExamQuestions
        WHERE PracticeExamId = @PracticeExamId;

        -- Step 3: Delete the exam itself
        DELETE FROM PracticeExams
        WHERE Id = @PracticeExamId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;

CREATE PROCEDURE DeletePracticeExamByNote
    @NoteId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Step 1: Delete choices for all questions belonging to exams tied to this note
        DELETE FROM PracticeQuestionChoices
        WHERE PracticeExamQuestionId IN (
            SELECT q.Id
            FROM PracticeExamQuestions q
            INNER JOIN PracticeExams e ON q.PracticeExamId = e.Id
            WHERE e.NoteId = @NoteId
        );

        -- Step 2: Delete questions for exams tied to this note
        DELETE FROM PracticeExamQuestions
        WHERE PracticeExamId IN (
            SELECT Id FROM PracticeExams WHERE NoteId = @NoteId
        );

        -- Step 3: Delete the exams themselves
        DELETE FROM PracticeExams
        WHERE NoteId = @NoteId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;


CREATE TABLE SpeechCapture (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    TranscriptFilePath NVARCHAR(255) NOT NULL, 
    SummaryText NVARCHAR(MAX) NOT NULL,      
    DurationSeconds INT NULL,             
    CreatedAt DATETIME DEFAULT GETDATE()       
);

CREATE PROCEDURE AddSpeechCapture
    @UserId INT,
    @TranscriptFilePath NVARCHAR(255),
    @SummaryText NVARCHAR(MAX),
    @DurationSeconds INT = NULL
AS
BEGIN
    INSERT INTO SpeechCapture (UserId, TranscriptFilePath, SummaryText, DurationSeconds, CreatedAt)
    VALUES (@UserId, @TranscriptFilePath, @SummaryText, @DurationSeconds, GETDATE());
END


CREATE PROCEDURE GetSpeechCaptureById
    @Id INT
AS
BEGIN
    SELECT 
        Id,
        UserId,
        TranscriptFilePath,
        SummaryText,
        DurationSeconds,
        CreatedAt
    FROM SpeechCapture
    WHERE Id = @Id;
END


CREATE PROCEDURE GetAllSpeechCaptures
    @UserId INT = NULL
AS
BEGIN
    IF @UserId IS NULL
        SELECT 
            Id,
            UserId,
            TranscriptFilePath,
            SummaryText,
            DurationSeconds,
            CreatedAt
        FROM SpeechCapture
        ORDER BY CreatedAt DESC;
    ELSE
        SELECT 
            Id,
            UserId,
            TranscriptFilePath,
            SummaryText,
            DurationSeconds,
            CreatedAt
        FROM SpeechCapture
        WHERE UserId = @UserId
        ORDER BY CreatedAt DESC;
END

CREATE PROCEDURE DeleteSpeechCapture
    @Id INT
AS
BEGIN
    DELETE FROM SpeechCapture
    WHERE Id = @Id;
END
