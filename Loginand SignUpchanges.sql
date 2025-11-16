

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
    DurationSeconds INT NULL,             
    CreatedAt DATETIME DEFAULT GETDATE()       
);

Alter Table SpeechCapture 
ADD AudioFilePath NVARCHAR(255) NULL, 

CREATE PROCEDURE AddSpeechCapture
    @UserId INT,
    @TranscriptFilePath NVARCHAR(255),
	@AudioFilePath NVARCHAR(255),
    @DurationSeconds INT = NULL,
	@Title NVARCHAR(MAX) 
AS
BEGIN
    INSERT INTO SpeechCapture (UserId, TranscriptFilePath, AudioFilePath, DurationSeconds, CreatedAt, Title)
    VALUES (@UserId, @TranscriptFilePath, @AudioFilePath , @DurationSeconds, GETDATE(), @Title);
END

DROP PROCEDURE AddSpeechCapture


CREATE PROCEDURE GetSpeechCaptureById
    @Id INT
AS
BEGIN
    SELECT 
        Id,
        UserId,
        TranscriptFilePath,
		AudioFilePath,
        DurationSeconds,
        CreatedAt,
		Title
    FROM SpeechCapture
    WHERE Id = @Id;
END

DROP PROCEDURE GetSpeechCaptureById


CREATE PROCEDURE GetAllSpeechCaptures
    @UserId INT = NULL
AS
BEGIN
        SELECT 
            Id,
            UserId,
            TranscriptFilePath,
			AudioFilePath,
            DurationSeconds,
            CreatedAt,
			Title
        FROM SpeechCapture
        WHERE UserId = @UserId
        ORDER BY CreatedAt DESC;
END

DROP Procedure GetAllSpeechCaptures

CREATE PROCEDURE DeleteSpeechCapture
    @Id INT
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM SpeechCaptureChat
        WHERE SpeechCaptureId = @Id;
   
        DELETE FROM SpeechCaptureSummary
        WHERE SpeechCaptureId = @Id;      
        DELETE FROM SpeechCaptureEmbeddings
        WHERE CaptureId = @Id;

 
        DELETE FROM SpeechCapture
        WHERE Id = @Id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;


DROP Procedure DeleteSpeechCapture
--Speech Capture

CREATE TABLE SpeechCaptureChat (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SpeechCaptureId INT NOT NULL,       -- link to SpeechCapture table
    UserId INT NOT NULL,                -- who owns this chat
    SenderType NVARCHAR(10) NOT NULL,   -- 'user' or 'ai'
    Message NVARCHAR(MAX) NOT NULL,     -- message text
    CreatedAt DATETIME DEFAULT GETDATE()
);

Alter Table SpeechCaptureChat
Add Response NVARCHAR(MAX) NOT NULL

CREATE PROCEDURE AddSpeechCaptureChat
    @SpeechCaptureId INT,
    @UserId INT,
    @SenderType NVARCHAR(10),
    @Message NVARCHAR(MAX),
	@Response NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- Add new message
    INSERT INTO SpeechCaptureChat (SpeechCaptureId, UserId, SenderType, Message, Response, CreatedAt)
    VALUES (@SpeechCaptureId, @UserId, @SenderType, @Message, @Response, GETDATE());

    -- Keep only the 100 most recent messages per recording
    DECLARE @MaxMessages INT = 100;

    WITH OrderedMessages AS (
        SELECT Id,
               ROW_NUMBER() OVER (PARTITION BY SpeechCaptureId ORDER BY CreatedAt DESC) AS RowNum
        FROM SpeechCaptureChat
        WHERE SpeechCaptureId = @SpeechCaptureId
    )
    DELETE FROM SpeechCaptureChat
    WHERE Id IN (SELECT Id FROM OrderedMessages WHERE RowNum > @MaxMessages);
END;

DROP Procedure AddSpeechCaptureChat


CREATE PROCEDURE GetSpeechCaptureChat
    @SpeechCaptureId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, SpeechCaptureId, UserId, SenderType, Message, Response, CreatedAt
    FROM SpeechCaptureChat
    WHERE SpeechCaptureId = @SpeechCaptureId
    ORDER BY CreatedAt ASC;
END;

DROP Procedure GetSpeechCaptureChat

CREATE PROCEDURE DeleteSpeechCaptureChat
    @SpeechCaptureId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM SpeechCaptureChat
    WHERE SpeechCaptureId = @SpeechCaptureId;
END;


-- Capture Summary 

CREATE TABLE SpeechCaptureSummary (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SpeechCaptureId INT NOT NULL,
    SummaryText NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SpeechCaptureId) REFERENCES SpeechCapture(Id)
);


CREATE PROCEDURE AddSpeechCaptureSummary
    @SpeechCaptureId INT,
    @SummaryText NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO SpeechCaptureSummary (SpeechCaptureId, SummaryText)
    VALUES (@SpeechCaptureId, @SummaryText);
END;


CREATE PROCEDURE GetSpeechCaptureSummaryByCapture 
	@CaptureId INT
AS 
BEGIN
  SELECT * FROM SpeechCaptureSummary 
  WHERE SpeechCaptureId = @CaptureId
END;

CREATE PROCEDURE DeleteSpeechCaptureSummary
    @Id INT
AS
BEGIN
    Delete FROM  SpeechCaptureSummary 
    WHERE Id = @Id
END;

DROP PROCEDURE DeleteSpeechCaptureSummary

CREATE TABLE SpeechCaptureEmbeddings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CaptureId INT NOT NULL,
    Embedding NVARCHAR(MAX),  
	ChunkIndex INT NOT NULL,
    ChunkText NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CaptureId) REFERENCES SpeechCapture(Id)
);


DROP Table SpeechCaptureEmbeddings
DROP Column  SummaryId

CREATE PROCEDURE AddSpeechCaptureEmbedding
    @CaptureId INT,
    @ChunkIndex INT,
    @ChunkText NVARCHAR(MAX),
    @Embedding NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO SpeechCaptureEmbeddings (CaptureId, ChunkIndex, ChunkText, Embedding, CreatedAt)
    VALUES (@CaptureId, @ChunkIndex, @ChunkText, @Embedding, GETDATE());
END;


Drop Procedure GetSpeechCaptureEmbeddingById

CREATE PROCEDURE GetSpeechCaptureEmbeddingById
    @CaptureId INT
AS
BEGIN
    SELECT *
    FROM SpeechCaptureEmbeddings
    WHERE CaptureId = @CaptureId;
END;





CREATE TABLE Appointments
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,  
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    StartTime DATETIME2 NULL,
    EndTime DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    Status NVARCHAR(50) NULL,
    Notes NVARCHAR(MAX) NULL,

    CONSTRAINT FK_Appointments_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users(Id)
);

Alter Table Appointments
ADD Organizer NVARCHAR(200) NULL,
   [Date] DATE NULL;

DROP Table AppointmentNotesVectors

CREATE TABLE AppointmentNotes
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL, 
    AppointmentId INT NOT NULL,
    DocumentId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    DocumentPath NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AppointmentVectors_Appointments FOREIGN KEY (AppointmentId)
        REFERENCES dbo.Appointments(Id),

    CONSTRAINT FK_AppointmentVectors_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users(Id)
);

CREATE OR ALTER PROCEDURE AddAppointment
    @UserId INT,
    @Title NVARCHAR(200),
    @Description NVARCHAR(MAX) = NULL,
    @StartTime DATETIME2 = NULL,
    @EndTime DATETIME2 = NULL,
    @Status NVARCHAR(50) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
	@Date DATE = NULL,
	@Organizer NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Appointments (UserId, Title, Description, StartTime, EndTime, Status, Notes, CreatedAt, [Date], Organizer)
    VALUES (@UserId, @Title, @Description, @StartTime, @EndTime, @Status, @Notes, SYSUTCDATETIME(), @Date, @Organizer);

    SELECT SCOPE_IDENTITY() AS AppointmentId;
END;

CREATE OR ALTER PROCEDURE GetUserAppointments
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Appointments
    WHERE UserId = @UserId
    ORDER BY CreatedAt DESC;
END;

EXEC GetUserAppointments 1

CREATE OR ALTER PROCEDURE GetAppointmentById
    @UserId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Appointments
    WHERE Id = @Id AND UserId = @UserId;
END;




CREATE OR ALTER PROCEDURE AddAppointmentDocument
    @UserId INT,
    @AppointmentId INT,
    @DocumentPath NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AppointmentNotes (UserId, AppointmentId, DocumentPath, CreatedAt)
    VALUES (@UserId, @AppointmentId, @DocumentPath, SYSUTCDATETIME());

  
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS AppointmentNoteId;
END;





DROP PROCEDURE GetAppointmentVectors

CREATE OR ALTER PROCEDURE GetAppointmentNotes
    @UserId INT,
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM AppointmentNotes
    WHERE UserId = @UserId AND AppointmentId = @AppointmentId
    ORDER BY CreatedAt DESC;
END;

CREATE OR ALTER PROCEDURE DeleteAppointment
    @UserId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM Appointments
    WHERE Id = @Id AND UserId = @UserId;
END;

CREATE TABLE AppointmentDocumentEmbeddings (
    EmbeddingId INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentDocumentId INT FOREIGN KEY REFERENCES AppointmentNotes(Id),
	AppointmentId INT FOREIGN KEY REFERENCES Appointments(Id),
    ChunkIndex INT NOT NULL,
    ChunkText NVARCHAR(MAX),
    Vector NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

Alter Table 
AppointmentDocumentEmbeddings 
ADD AppointmentId INT FOREIGN KEY REFERENCES Appointments(Id),

CREATE OR ALTER PROCEDURE AddAppointmentDocumentEmbedding
    @AppointmentDocumentId INT,
	@AppointmentId INT,
    @ChunkIndex INT,
    @ChunkText NVARCHAR(MAX),
    @Vector NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AppointmentDocumentEmbeddings
        (AppointmentDocumentId, AppointmentId, ChunkIndex, ChunkText, Vector, CreatedAt)
    VALUES
        (@AppointmentDocumentId, @AppointmentId , @ChunkIndex, @ChunkText, @Vector, SYSUTCDATETIME());

    -- Return the ID of the inserted embedding (if you need it in C#)
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS EmbeddingId;
END;


CREATE TABLE BackgroundJobs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobType NVARCHAR(100) NOT NULL,
    ReferenceId INT NULL,
    ReferenceType NVARCHAR(100) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    Payload NVARCHAR(MAX) NULL,
    ResultMessage NVARCHAR(MAX) NULL,
    ErrorTrace NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    StartedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL
);


CREATE PROCEDURE CreateBackgroundJob
    @JobType NVARCHAR(100),
    @ReferenceId INT = NULL,
    @ReferenceType NVARCHAR(100) = NULL,
    @Payload NVARCHAR(MAX) = NULL,
    @JobId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO BackgroundJobs (JobType, ReferenceId, ReferenceType, Payload, Status, CreatedAt)
    VALUES (@JobType, @ReferenceId, @ReferenceType, @Payload, 'Pending', SYSUTCDATETIME());

    SET @JobId = SCOPE_IDENTITY();
END

CREATE PROCEDURE UpdateBackgroundJob
    @JobId BIGINT,
    @Status NVARCHAR(50),
    @ResultMessage NVARCHAR(MAX) = NULL,
    @ErrorTrace NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE BackgroundJobs
    SET
        Status = @Status,
        ResultMessage = @ResultMessage,
        ErrorTrace = @ErrorTrace,
        UpdatedAt = SYSUTCDATETIME(),
        StartedAt = CASE WHEN @Status = 'Processing' THEN SYSUTCDATETIME() ELSE StartedAt END,
        CompletedAt = CASE WHEN @Status IN ('Completed', 'Failed') THEN SYSUTCDATETIME() ELSE CompletedAt END
    WHERE Id = @JobId;
END

CREATE PROCEDURE GetBackgroundJob
    @JobId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, JobType, Status, ResultMessage, CreatedAt, StartedAt, CompletedAt
    FROM BackgroundJobs
    WHERE Id = @JobId;
END


CREATE OR Alter PROCEDURE GetDocumentChunksForAppointment
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        EmbeddingId,
        AppointmentId,
        AppointmentDocumentId,
        ChunkText,
		ChunkIndex,
        Vector
    FROM 
       AppointmentDocumentEmbeddings
    WHERE 
        AppointmentId = @AppointmentId;
END


Exec GetDocumentChunksForAppointment 2

SELECT ChunkText
FROM AppointmentDocumentEmbeddings
WHERE AppointmentId = 2


DELETE FROM AppointmentDocumentEmbeddings;
-- Reset identity seed
DBCC CHECKIDENT ('Appointments', RESEED, 0);

DELETE FROM [AppointmentNotes];
-- Reset identity seed
DBCC CHECKIDENT ('Appointments', RESEED, 0);

DELETE FROM Appointments;
-- Reset identity seed
DBCC CHECKIDENT ('Appointments', RESEED, 0);

DELETE FROM BackgroundJobs