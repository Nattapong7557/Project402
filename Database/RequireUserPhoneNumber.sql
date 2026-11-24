UPDATE users
SET phone_number = ''
WHERE phone_number IS NULL;

ALTER TABLE users
MODIFY COLUMN phone_number VARCHAR(20) NOT NULL;
