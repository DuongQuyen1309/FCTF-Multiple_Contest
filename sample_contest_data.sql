-- ============================================
-- Sample Contest Data for Testing
-- ============================================
-- This script creates sample contests and assigns the student1 user as a participant

-- First, let's check if student1 exists and get their ID
-- Assuming student1 has ID = 5 based on the logs

-- Insert sample contests
INSERT INTO contests (name, description, slug, start_time, end_time, state, owner_id, created_at, updated_at)
VALUES 
    ('Spring CTF 2024', 'Beginner-friendly CTF competition for spring semester', 'spring-ctf-2024',
     '2024-03-01 09:00:00', '2024-03-15 18:00:00', 'visible', 1, NOW(), NOW()),
    
    ('Summer Security Challenge', 'Advanced security challenges for summer training', 'summer-security-2024',
     '2024-06-01 10:00:00', '2024-06-30 20:00:00', 'visible', 1, NOW(), NOW()),
    
    ('Fall Hacking Contest', 'Intermediate level CTF for fall semester', 'fall-hacking-2024',
     '2024-09-01 08:00:00', '2024-09-20 17:00:00', 'visible', 1, NOW(), NOW()),
    
    ('Winter Cyber Games', 'Year-end cybersecurity competition', 'winter-cyber-2024',
     '2024-12-01 09:00:00', '2024-12-25 23:59:59', 'visible', 1, NOW(), NOW()),
    
    ('Practice Arena', 'Always-on practice environment for students', 'practice-arena',
     '2024-01-01 00:00:00', '2025-12-31 23:59:59', 'visible', 1, NOW(), NOW());

-- Get the IDs of the contests we just created
SET @contest1_id = LAST_INSERT_ID();
SET @contest2_id = @contest1_id + 1;
SET @contest3_id = @contest1_id + 2;
SET @contest4_id = @contest1_id + 3;
SET @contest5_id = @contest1_id + 4;

-- Add student1 (ID=5) as a participant in all contests
INSERT INTO ContestParticipants (ContestId, UserId, JoinedAt)
VALUES 
    (@contest1_id, 5, NOW()),
    (@contest2_id, 5, NOW()),
    (@contest3_id, 5, NOW()),
    (@contest4_id, 5, NOW()),
    (@contest5_id, 5, NOW());

-- Optional: Create some sample challenges for the contests
-- (You can skip this if you want to add challenges manually later)

INSERT INTO Challenges (Name, Category, Description, Points, Flag, Difficulty, CreatedAt, UpdatedAt)
VALUES 
    ('Welcome Challenge', 'Misc', 'Find the flag in the description. Flag: FCTF{welcome_to_ctf}', 
     10, 'FCTF{welcome_to_ctf}', 'easy', NOW(), NOW()),
    
    ('Basic Web', 'Web', 'Simple web exploitation challenge', 
     50, 'FCTF{basic_web_exploit}', 'easy', NOW(), NOW()),
    
    ('Crypto 101', 'Crypto', 'Introduction to cryptography', 
     75, 'FCTF{caesar_cipher_solved}', 'medium', NOW(), NOW()),
    
    ('Binary Basics', 'Pwn', 'Basic binary exploitation', 
     100, 'FCTF{buffer_overflow_found}', 'medium', NOW(), NOW()),
    
    ('Reverse Me', 'Reverse', 'Reverse engineering challenge', 
     150, 'FCTF{reversed_successfully}', 'hard', NOW(), NOW());

-- Get challenge IDs
SET @challenge1_id = LAST_INSERT_ID();
SET @challenge2_id = @challenge1_id + 1;
SET @challenge3_id = @challenge1_id + 2;
SET @challenge4_id = @challenge1_id + 3;
SET @challenge5_id = @challenge1_id + 4;

-- Link challenges to contests
-- Add all challenges to Practice Arena (contest 5)
INSERT INTO ContestsChallenges (ContestId, ChallengeId, AddedAt)
VALUES 
    (@contest5_id, @challenge1_id, NOW()),
    (@contest5_id, @challenge2_id, NOW()),
    (@contest5_id, @challenge3_id, NOW()),
    (@contest5_id, @challenge4_id, NOW()),
    (@contest5_id, @challenge5_id, NOW());

-- Add some challenges to Spring CTF
INSERT INTO ContestsChallenges (ContestId, ChallengeId, AddedAt)
VALUES 
    (@contest1_id, @challenge1_id, NOW()),
    (@contest1_id, @challenge2_id, NOW()),
    (@contest1_id, @challenge3_id, NOW());

-- Add some challenges to Summer Challenge
INSERT INTO ContestsChallenges (ContestId, ChallengeId, AddedAt)
VALUES 
    (@contest2_id, @challenge3_id, NOW()),
    (@contest2_id, @challenge4_id, NOW()),
    (@contest2_id, @challenge5_id, NOW());

-- Display summary
SELECT 'Sample data inserted successfully!' AS Status;
SELECT COUNT(*) AS 'Total Contests' FROM Contests;
SELECT COUNT(*) AS 'Total Challenges' FROM Challenges;
SELECT COUNT(*) AS 'Student1 Participations' FROM ContestParticipants WHERE UserId = 5;
