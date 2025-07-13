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
