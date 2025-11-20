-- Check existing foreign keys on PreBookingConversations table
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM
    INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE
    TABLE_SCHEMA = 'moto_micro_tours'
    AND TABLE_NAME = 'PreBookingConversations'
    AND REFERENCED_TABLE_NAME IS NOT NULL;

-- Check all columns on PreBookingConversations
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_KEY
FROM
    INFORMATION_SCHEMA.COLUMNS
WHERE
    TABLE_SCHEMA = 'moto_micro_tours'
    AND TABLE_NAME = 'PreBookingConversations';
