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
