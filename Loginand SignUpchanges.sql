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
        f.Back
    FROM dbo.FlashcardSets fcs
    INNER JOIN dbo.Flashcards f 
        ON fcs.Id = f.FlashcardSetId
    WHERE fcs.UserId = @UserId
    ORDER BY fcs.CreatedAt DESC, f.Id;
END;

Drop Procedure GetUserFlashcards


DELETE FROM FlashcardSets