-- SQLite script to initialize database with the correct schema

-- Drop Friendships table if it exists and recreate with the correct columns
DROP TABLE IF EXISTS Friendships;

-- Create Friendships table with UserId and FriendId columns
CREATE TABLE Friendships (
    FriendshipId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL,
    FriendId TEXT NOT NULL,
    Status TEXT NOT NULL,
    RequestedAt TEXT NOT NULL,
    AcceptedAt TEXT,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    FOREIGN KEY (FriendId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    UNIQUE (UserId, FriendId),
    CHECK (Status IN ('Pending', 'Accepted', 'Declined', 'Blocked'))
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS IX_Friendships_UserId ON Friendships(UserId);
CREATE INDEX IF NOT EXISTS IX_Friendships_FriendId ON Friendships(FriendId);

-- Insert sample friendship data for testing
-- These will be inserted only if the tables are empty
INSERT OR IGNORE INTO Friendships (UserId, FriendId, Status, RequestedAt, AcceptedAt)
SELECT 
    (SELECT Id FROM AspNetUsers WHERE Email = 'john@example.com') AS UserId,
    (SELECT Id FROM AspNetUsers WHERE Email = 'jane@example.com') AS FriendId,
    'Accepted',
    datetime('now', '-5 days'),
    datetime('now', '-4 days')
WHERE EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'john@example.com')
  AND EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'jane@example.com')
  AND NOT EXISTS (SELECT 1 FROM Friendships);

INSERT OR IGNORE INTO Friendships (UserId, FriendId, Status, RequestedAt, AcceptedAt)
SELECT 
    (SELECT Id FROM AspNetUsers WHERE Email = 'john@example.com') AS UserId,
    (SELECT Id FROM AspNetUsers WHERE Email = 'bob@example.com') AS FriendId,
    'Accepted',
    datetime('now', '-3 days'),
    datetime('now', '-2 days')
WHERE EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'john@example.com')
  AND EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'bob@example.com')
  AND NOT EXISTS (SELECT 1 FROM Friendships WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'john@example.com') AND FriendId = (SELECT Id FROM AspNetUsers WHERE Email = 'bob@example.com'));

INSERT OR IGNORE INTO Friendships (UserId, FriendId, Status, RequestedAt)
SELECT 
    (SELECT Id FROM AspNetUsers WHERE Email = 'jane@example.com') AS UserId,
    (SELECT Id FROM AspNetUsers WHERE Email = 'bob@example.com') AS FriendId,
    'Pending',
    datetime('now', '-5 hours')
WHERE EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'jane@example.com')
  AND EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'bob@example.com')
  AND NOT EXISTS (SELECT 1 FROM Friendships WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'jane@example.com') AND FriendId = (SELECT Id FROM AspNetUsers WHERE Email = 'bob@example.com'));