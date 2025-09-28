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
Drop COLUMN NoteId;

--Other functionalities
CREATE TABLE Notes (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    Title NVARCHAR(255),
    FilePath NVARCHAR(MAX), -- Where the uploaded file is stored
    UploadedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);



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
	@NewNoteId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Notes (UserId, Title, FilePath, UploadedAt)
    VALUES (@UserId, @Title, @FilePath, SYSDATETIME());

	-- Get the ID of the inserted note
    SET @NewNoteId = SCOPE_IDENTITY()
END;

CREATE PROCEDURE DeleteNote
    @NoteId INT
AS
BEGIN
    SET NOCOUNT ON;

	DELETE FROM NoteSummaries WHERE UploadedNoteId = @NoteId
	DELETE FROM Flashcards WHERE NoteId = @NoteId
    DELETE FROM dbo.Notes
    WHERE Id = @NoteId;
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