-- Create Inventory table
CREATE TABLE IF NOT EXISTS Inventory
(
    Id INTEGER PRIMARY KEY,
    Name TEXT,
    Type TEXT,
    Rarity TEXT,
    AssetPath TEXT
);

-- Create Fish table
CREATE TABLE IF NOT EXISTS Fish
(
    FishID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Description TEXT,
    Rarity TEXT,
    AssetPath TEXT,
    MinWeight REAL,
    MaxWeight REAL,
    TopSpeed REAL,
    HookedFuncNum INTEGER
);

-- Create MarketPrices table
CREATE TABLE IF NOT EXISTS MarketPrices
(
    PriceID INTEGER PRIMARY KEY AUTOINCREMENT,
    FishID INTEGER,
    Day INTEGER,
    Price REAL,
    FOREIGN KEY (FishID) REFERENCES Fish(FishID)
);

-- Populate Fish table with test data
INSERT OR IGNORE INTO Fish (Name, Description, Rarity, AssetPath, MinWeight, MaxWeight, TopSpeed, HookedFuncNum) VALUES
('Fish 1', 'A common small fish', 'COMMON', NULL, 0.1, 0.5, 3.0, 1),
('Fish 2', 'A quick swimmer', 'COMMON', NULL, 0.2, 0.8, 5.0, 1),
('Fish 3', 'A heavy fish', 'UNCOMMON', NULL, 1.0, 3.0, 2.5, 2),
('Fish 4', 'An elusive fish', 'RARE', NULL, 0.3, 1.2, 6.0, 1),
('Fish 5', 'A massive specimen', 'EPIC', NULL, 5.0, 15.0, 4.0, 2),
('Fish 6', 'A tiny fish', 'COMMON', NULL, 0.05, 0.2, 4.5, 0),
('Fish 7', 'A colorful fish', 'UNCOMMON', NULL, 0.4, 1.5, 3.5, 1),
('Fish 8', 'A deep-sea dweller', 'RARE', NULL, 2.0, 5.0, 2.0, 2),
('Fish 9', 'A legendary fish', 'LEGENDARY', NULL, 10.0, 30.0, 7.0, 2),
('Fish 10', 'A common fish', 'COMMON', NULL, 0.3, 1.0, 3.0, 1),
('Fish 11', 'A swift fish', 'UNCOMMON', NULL, 0.4, 1.2, 5.5, 1),
('Fish 12', 'A heavy fighter', 'RARE', NULL, 3.0, 8.0, 4.0, 2),
('Fish 13', 'A tiny swimmer', 'COMMON', NULL, 0.1, 0.4, 3.5, 0),
('Fish 14', 'A rare specimen', 'RARE', NULL, 1.5, 4.0, 4.5, 2),
('Fish 15', 'An ancient fish', 'LEGENDARY', NULL, 8.0, 25.0, 3.0, 2),
('Fish 16', 'A common catch', 'COMMON', NULL, 0.2, 0.7, 3.0, 1),
('Fish 17', 'A mysterious fish', 'EPIC', NULL, 4.0, 12.0, 5.0, 2),
('Fish 18', 'A quick fish', 'UNCOMMON', NULL, 0.3, 0.9, 6.0, 1),
('Fish 19', 'A massive fish', 'RARE', NULL, 6.0, 18.0, 2.5, 2),
('Fish 20', 'A legendary beast', 'LEGENDARY', NULL, 15.0, 40.0, 8.0, 2);

-- Populate MarketPrices table with 5 days of data
INSERT OR IGNORE INTO MarketPrices (FishID, Day, Price) VALUES
-- Day 1
(1, 1, 25.50), (2, 1, 30.00), (3, 1, 75.00), (4, 1, 150.00), (5, 1, 300.00),
(6, 1, 15.00), (7, 1, 85.00), (8, 1, 180.00), (9, 1, 500.00), (10, 1, 28.00),
(11, 1, 80.00), (12, 1, 200.00), (13, 1, 20.00), (14, 1, 160.00), (15, 1, 450.00),
(16, 1, 27.00), (17, 1, 280.00), (18, 1, 70.00), (19, 1, 190.00), (20, 1, 550.00),

-- Day 2
(1, 2, 23.00), (2, 2, 32.00), (3, 2, 80.00), (4, 2, 145.00), (5, 2, 320.00),
(6, 2, 14.00), (7, 2, 90.00), (8, 2, 175.00), (9, 2, 520.00), (10, 2, 26.00),
(11, 2, 85.00), (12, 2, 195.00), (13, 2, 18.00), (14, 2, 170.00), (15, 2, 460.00),
(16, 2, 29.00), (17, 2, 290.00), (18, 2, 75.00), (19, 2, 185.00), (20, 2, 540.00),

-- Day 3
(1, 3, 26.00), (2, 3, 29.00), (3, 3, 70.00), (4, 3, 160.00), (5, 3, 310.00),
(6, 3, 16.00), (7, 3, 82.00), (8, 3, 190.00), (9, 3, 490.00), (10, 3, 30.00),
(11, 3, 78.00), (12, 3, 210.00), (13, 3, 21.00), (14, 3, 155.00), (15, 3, 470.00),
(16, 3, 25.00), (17, 3, 275.00), (18, 3, 72.00), (19, 3, 195.00), (20, 3, 560.00),

-- Day 4
(1, 4, 24.00), (2, 4, 31.00), (3, 4, 77.00), (4, 4, 155.00), (5, 4, 305.00),
(6, 4, 13.00), (7, 4, 88.00), (8, 4, 185.00), (9, 4, 510.00), (10, 4, 27.00),
(11, 4, 83.00), (12, 4, 205.00), (13, 4, 19.00), (14, 4, 165.00), (15, 4, 455.00),
(16, 4, 28.00), (17, 4, 285.00), (18, 4, 73.00), (19, 4, 192.00), (20, 4, 545.00),

-- Day 5
(1, 5, 27.00), (2, 5, 33.00), (3, 5, 82.00), (4, 5, 158.00), (5, 5, 315.00),
(6, 5, 17.00), (7, 5, 86.00), (8, 5, 182.00), (9, 5, 515.00), (10, 5, 29.00),
(11, 5, 81.00), (12, 5, 208.00), (13, 5, 22.00), (14, 5, 168.00), (15, 5, 465.00),
(16, 5, 26.00), (17, 5, 295.00), (18, 5, 76.00), (19, 5, 188.00), (20, 5, 555.00); 