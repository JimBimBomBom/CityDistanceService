CREATE DATABASE CityDistanceService;

USE CityDistanceService;

CREATE TABLE City (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100),
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8)
);

INSERT INTO City (name, latitude, longitude)
VALUES  ('Bratislava', 48.148598, 17.107748),
        ('Brno', 49.195061, 16.606836);