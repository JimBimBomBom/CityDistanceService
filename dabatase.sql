-- Create the CityDistanceService database
CREATE DATABASE IF NOT EXISTS CityDistanceService;
USE CityDistanceService;

-- Create the Cities table
CREATE TABLE IF NOT EXISTS Cities (
    CityId INT AUTO_INCREMENT PRIMARY KEY,
    CityName VARCHAR(255) NOT NULL,
    Latitude DECIMAL(9,6),  -- Format: DD.DDDDDD
    Longitude DECIMAL(9,6)  -- Format: DD.DDDDDD
);

-- Insert sample data into the Cities table only if the city does not already exist
INSERT INTO Cities (CityName, Latitude, Longitude)
SELECT * FROM (SELECT 'New York', 40.712776, -74.005974) AS tmp
WHERE NOT EXISTS (
    SELECT CityName FROM Cities WHERE CityName = 'New York'
) LIMIT 1;

INSERT INTO Cities (CityName, Latitude, Longitude)
SELECT * FROM (SELECT 'Los Angeles', 34.052235, -118.243683) AS tmp
WHERE NOT EXISTS (
    SELECT CityName FROM Cities WHERE CityName = 'Los Angeles'
) LIMIT 1;
