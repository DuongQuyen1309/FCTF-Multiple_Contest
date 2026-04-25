-- ============================================
-- Simple Sample Contest Data for Testing
-- ============================================
-- This script creates sample contests and assigns student1 as a participant

-- Insert sample contests (owner_id=2 is the admin user)
INSERT INTO contests (name, description, slug, start_time, end_time, state, owner_id, created_at, updated_at)
VALUES 
    ('Spring CTF 2024', 'Beginner-friendly CTF competition for spring semester', 'spring-ctf-2024',
     '2024-03-01 09:00:00', '2024-03-15 18:00:00', 'visible', 2, NOW(), NOW()),
    
    ('Summer Security Challenge', 'Advanced security challenges for summer training', 'summer-security-2024',
     '2024-06-01 10:00:00', '2024-06-30 20:00:00', 'visible', 2, NOW(), NOW()),
    
    ('Fall Hacking Contest', 'Intermediate level CTF for fall semester', 'fall-hacking-2024',
     '2024-09-01 08:00:00', '2024-09-20 17:00:00', 'visible', 2, NOW(), NOW()),
    
    ('Winter Cyber Games', 'Year-end cybersecurity competition', 'winter-cyber-2024',
     '2024-12-01 09:00:00', '2024-12-25 23:59:59', 'visible', 2, NOW(), NOW()),
    
    ('Practice Arena', 'Always-on practice environment for students', 'practice-arena',
     '2024-01-01 00:00:00', '2025-12-31 23:59:59', 'visible', 2, NOW(), NOW());

-- Get the IDs of the contests we just created
SET @contest1_id = LAST_INSERT_ID();
SET @contest2_id = @contest1_id + 1;
SET @contest3_id = @contest1_id + 2;
SET @contest4_id = @contest1_id + 3;
SET @contest5_id = @contest1_id + 4;

-- Add student1 (ID=5) as a participant in all contests
INSERT INTO contest_participants (contest_id, user_id, joined_at)
VALUES 
    (@contest1_id, 5, NOW()),
    (@contest2_id, 5, NOW()),
    (@contest3_id, 5, NOW()),
    (@contest4_id, 5, NOW()),
    (@contest5_id, 5, NOW());

-- Display summary
SELECT 'Sample contests created successfully!' AS Status;
SELECT id, name, slug, state, start_time, end_time FROM contests WHERE slug LIKE '%-2024' OR slug = 'practice-arena';
SELECT COUNT(*) AS 'Student1 Participations' FROM contest_participants WHERE user_id = 5;
