-- ============================================
-- FCTF Multiple Contest - Test Data
-- ============================================
-- This script creates test data for local testing
-- 
-- IMPORTANT: Replace REPLACE_WITH_HASH with actual password hash
-- Generate hash using: cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash && dotnet run password123
-- ============================================

-- Clean existing data (optional - comment out if you want to keep existing data)
-- SET FOREIGN_KEY_CHECKS = 0;
-- TRUNCATE TABLE submissions;
-- TRUNCATE TABLE solves;
-- TRUNCATE TABLE contests_challenges;
-- TRUNCATE TABLE contest_participants;
-- TRUNCATE TABLE flags;
-- TRUNCATE TABLE challenges;
-- TRUNCATE TABLE contests;
-- TRUNCATE TABLE users;
-- SET FOREIGN_KEY_CHECKS = 1;

-- ============================================
-- 1. Insert Test Users
-- ============================================
-- Password for all users: password123
-- Replace REPLACE_WITH_HASH with the hash generated from GeneratePasswordHash tool

INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', 'REPLACE_WITH_HASH', 'admin', 1, 0, 0, NOW()),
('teacher1', 'teacher1@test.com', 'REPLACE_WITH_HASH', 'teacher', 1, 0, 0, NOW()),
('teacher2', 'teacher2@test.com', 'REPLACE_WITH_HASH', 'teacher', 1, 0, 0, NOW()),
('student1', 'student1@test.com', 'REPLACE_WITH_HASH', 'user', 1, 0, 0, NOW()),
('student2', 'student2@test.com', 'REPLACE_WITH_HASH', 'user', 1, 0, 0, NOW()),
('student3', 'student3@test.com', 'REPLACE_WITH_HASH', 'user', 1, 0, 0, NOW()),
('student4', 'student4@test.com', 'REPLACE_WITH_HASH', 'user', 1, 0, 0, NOW())
ON DUPLICATE KEY UPDATE name=VALUES(name);

-- ============================================
-- 2. Insert Test Contests
-- ============================================

INSERT INTO contests (name, description, slug, owner_id, semester_name, state, user_mode, start_time, end_time, created_at) VALUES
('Test Contest 1', 'First test contest for multiple contest system', 'test-contest-1', 
    (SELECT id FROM users WHERE email = 'admin@test.com'), 
    'Fall 2024', 'visible', 'users', 
    DATE_SUB(NOW(), INTERVAL 1 DAY), 
    DATE_ADD(NOW(), INTERVAL 7 DAY), 
    NOW()),
('Test Contest 2', 'Second test contest for teacher1', 'test-contest-2', 
    (SELECT id FROM users WHERE email = 'teacher1@test.com'), 
    'Fall 2024', 'visible', 'users', 
    DATE_SUB(NOW(), INTERVAL 2 DAY), 
    DATE_ADD(NOW(), INTERVAL 14 DAY), 
    NOW()),
('Test Contest 3', 'Third test contest (empty)', 'test-contest-3', 
    (SELECT id FROM users WHERE email = 'teacher2@test.com'), 
    'Spring 2025', 'draft', 'users', 
    DATE_ADD(NOW(), INTERVAL 7 DAY), 
    DATE_ADD(NOW(), INTERVAL 21 DAY), 
    NOW())
ON DUPLICATE KEY UPDATE name=VALUES(name);

-- ============================================
-- 3. Insert Contest Participants
-- ============================================

-- Contest 1 participants
INSERT INTO contest_participants (contest_id, user_id, team_id, role, score, joined_at)
SELECT 
    c.id,
    u.id,
    NULL,
    'contestant',
    0,
    NOW()
FROM contests c
CROSS JOIN users u
WHERE c.slug = 'test-contest-1'
AND u.email IN ('student1@test.com', 'student2@test.com', 'student3@test.com')
ON DUPLICATE KEY UPDATE role=VALUES(role);

-- Contest 2 participants
INSERT INTO contest_participants (contest_id, user_id, team_id, role, score, joined_at)
SELECT 
    c.id,
    u.id,
    NULL,
    'contestant',
    0,
    NOW()
FROM contests c
CROSS JOIN users u
WHERE c.slug = 'test-contest-2'
AND u.email IN ('student1@test.com', 'student4@test.com')
ON DUPLICATE KEY UPDATE role=VALUES(role);

-- ============================================
-- 4. Insert Test Challenges (Bank)
-- ============================================

INSERT INTO challenges (name, description, category, value, type, state, connection_protocol, connection_info, max_attempts, require_deploy, max_deploy_count) VALUES
-- Web Challenges
('SQL Injection Basic', 'Find the flag using SQL injection', 'Web', 100, 'standard', 'visible', 'http', NULL, 10, 0, 0),
('XSS Vulnerability', 'Exploit XSS to get the flag', 'Web', 150, 'standard', 'visible', 'http', NULL, 10, 0, 0),
('CSRF Attack', 'Perform CSRF attack to get admin access', 'Web', 200, 'standard', 'visible', 'http', NULL, 5, 0, 0),

-- Crypto Challenges
('Caesar Cipher', 'Decode the Caesar cipher', 'Crypto', 50, 'standard', 'visible', NULL, NULL, 0, 0, 0),
('RSA Decryption', 'Decrypt RSA encrypted message', 'Crypto', 250, 'standard', 'visible', NULL, NULL, 5, 0, 0),

-- Pwn Challenges
('Buffer Overflow', 'Exploit buffer overflow vulnerability', 'Pwn', 300, 'dynamic', 'visible', 'tcp', NULL, 3, 1, 3),
('Format String', 'Exploit format string vulnerability', 'Pwn', 350, 'dynamic', 'visible', 'tcp', NULL, 3, 1, 3),

-- Reverse Engineering
('Reverse Me', 'Reverse engineer the binary', 'Reverse', 200, 'standard', 'visible', NULL, NULL, 5, 0, 0),

-- Forensics
('Hidden in Image', 'Find the flag hidden in the image', 'Forensics', 100, 'standard', 'visible', NULL, NULL, 0, 0, 0),

-- Misc
('OSINT Challenge', 'Use OSINT techniques to find the flag', 'Misc', 150, 'standard', 'visible', NULL, NULL, 0, 0, 0)
ON DUPLICATE KEY UPDATE name=VALUES(name);

-- ============================================
-- 5. Insert Flags for Bank Challenges
-- ============================================

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{sql_injection_basic}', NULL FROM challenges WHERE name = 'SQL Injection Basic'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{xss_vulnerability}', NULL FROM challenges WHERE name = 'XSS Vulnerability'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{csrf_attack_success}', NULL FROM challenges WHERE name = 'CSRF Attack'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{caesar_cipher_decoded}', NULL FROM challenges WHERE name = 'Caesar Cipher'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{rsa_decrypted}', NULL FROM challenges WHERE name = 'RSA Decryption'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{buffer_overflow_pwned}', NULL FROM challenges WHERE name = 'Buffer Overflow'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{format_string_exploited}', NULL FROM challenges WHERE name = 'Format String'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{reversed_successfully}', NULL FROM challenges WHERE name = 'Reverse Me'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{hidden_in_image}', NULL FROM challenges WHERE name = 'Hidden in Image'
ON DUPLICATE KEY UPDATE content=VALUES(content);

INSERT INTO flags (challenge_id, type, content, data)
SELECT id, 'static', 'flag{osint_master}', NULL FROM challenges WHERE name = 'OSINT Challenge'
ON DUPLICATE KEY UPDATE content=VALUES(content);

-- ============================================
-- 6. Pull Challenges to Contest 1
-- ============================================

INSERT INTO contests_challenges (contest_id, bank_id, name, value, max_attempts, state, time_limit, cooldown, require_deploy, max_deploy_count, connection_protocol, connection_info, deploy_status, last_update)
SELECT 
    c.id,
    ch.id,
    ch.name,
    ch.value,
    ch.max_attempts,
    ch.state,
    NULL,
    0,
    ch.require_deploy,
    ch.max_deploy_count,
    ch.connection_protocol,
    ch.connection_info,
    'CREATED',
    NOW()
FROM contests c
CROSS JOIN challenges ch
WHERE c.slug = 'test-contest-1'
AND ch.name IN ('SQL Injection Basic', 'XSS Vulnerability', 'Caesar Cipher', 'RSA Decryption', 'Hidden in Image')
ON DUPLICATE KEY UPDATE name=VALUES(name);

-- ============================================
-- 7. Pull Challenges to Contest 2
-- ============================================

INSERT INTO contests_challenges (contest_id, bank_id, name, value, max_attempts, state, time_limit, cooldown, require_deploy, max_deploy_count, connection_protocol, connection_info, deploy_status, last_update)
SELECT 
    c.id,
    ch.id,
    ch.name,
    ch.value,
    ch.max_attempts,
    ch.state,
    NULL,
    0,
    ch.require_deploy,
    ch.max_deploy_count,
    ch.connection_protocol,
    ch.connection_info,
    'CREATED',
    NOW()
FROM contests c
CROSS JOIN challenges ch
WHERE c.slug = 'test-contest-2'
AND ch.name IN ('Buffer Overflow', 'Format String', 'Reverse Me', 'OSINT Challenge')
ON DUPLICATE KEY UPDATE name=VALUES(name);

-- ============================================
-- 8. Verification Queries
-- ============================================

-- Check users
SELECT '=== Users ===' as Info;
SELECT id, name, email, type, verified FROM users ORDER BY id;

-- Check contests
SELECT '=== Contests ===' as Info;
SELECT c.id, c.name, c.slug, u.name as owner, c.state FROM contests c
LEFT JOIN users u ON c.owner_id = u.id
ORDER BY c.id;

-- Check contest participants
SELECT '=== Contest Participants ===' as Info;
SELECT cp.id, c.name as contest, u.name as user, cp.role FROM contest_participants cp
JOIN contests c ON cp.contest_id = c.id
JOIN users u ON cp.user_id = u.id
ORDER BY c.id, u.name;

-- Check challenges in bank
SELECT '=== Challenge Bank ===' as Info;
SELECT id, name, category, value, type, state FROM challenges ORDER BY category, name;

-- Check contest challenges
SELECT '=== Contest Challenges ===' as Info;
SELECT cc.id, c.name as contest, cc.name as challenge, cc.value, cc.state FROM contests_challenges cc
JOIN contests c ON cc.contest_id = c.id
ORDER BY c.id, cc.name;

-- Summary counts
SELECT '=== Summary ===' as Info;
SELECT 
    (SELECT COUNT(*) FROM users) as total_users,
    (SELECT COUNT(*) FROM contests) as total_contests,
    (SELECT COUNT(*) FROM contest_participants) as total_participants,
    (SELECT COUNT(*) FROM challenges) as total_bank_challenges,
    (SELECT COUNT(*) FROM contests_challenges) as total_contest_challenges;

-- ============================================
-- Test Data Import Complete!
-- ============================================
-- 
-- Test Accounts (all passwords: password123):
-- - admin@test.com (Admin)
-- - teacher1@test.com (Teacher)
-- - teacher2@test.com (Teacher)
-- - student1@test.com (Student - in Contest 1 & 2)
-- - student2@test.com (Student - in Contest 1)
-- - student3@test.com (Student - in Contest 1)
-- - student4@test.com (Student - in Contest 2)
--
-- Test Contests:
-- - Test Contest 1 (admin, 5 challenges, 3 students)
-- - Test Contest 2 (teacher1, 4 challenges, 2 students)
-- - Test Contest 3 (teacher2, empty, no students)
--
-- ============================================
