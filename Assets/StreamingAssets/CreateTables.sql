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
    IsDiscovered INTEGER,
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

-- Populate Fish table with enhanced test data
INSERT OR REPLACE INTO Fish (Name, Description, Rarity, AssetPath, MinWeight, MaxWeight, TopSpeed, HookedFuncNum, IsDiscovered) VALUES
('Golden Trout', 'The Golden Trout is a rare fish known for its vibrant golden scales. It thrives in cold, clear waters and is a favorite among anglers.', 'RARE', 'Art/Sprites/Fish/FoundFish', 0.5, 1.5, 4.0, 1, 0),
('Silver Salmon', 'Silver Salmon are known for their acrobatic leaps and strong fighting spirit. They are often found in rivers and coastal waters.', 'UNCOMMON', 'Art/Sprites/Fish/FoundFish', 1.0, 3.0, 5.0, 2, 1),
('Crimson Snapper', 'The Crimson Snapper is a vibrant red fish that inhabits coral reefs. Its striking color makes it a popular catch for divers.', 'EPIC', 'Art/Sprites/Fish/FoundFish', 0.8, 2.5, 3.5, 1, 0),
('Emerald Catfish', 'Emerald Catfish are known for their unique greenish hue and whisker-like barbels. They prefer murky waters and are bottom feeders.', 'COMMON', 'Art/Sprites/Fish/FoundFish', 1.2, 4.0, 2.0, 1, 1),
('Bluefin Tuna', 'The Bluefin Tuna is a large and powerful fish, highly sought after for its delicious meat. It can swim at incredible speeds and is a challenge to catch.', 'LEGENDARY', 'Art/Sprites/Fish/FoundFish', 10.0, 30.0, 8.0, 2, 0),
('Rainbow Trout', 'Rainbow Trout are known for their colorful appearance and are a popular target for sport fishing. They are often found in freshwater lakes and streams.', 'COMMON', 'Art/Sprites/Fish/FoundFish', 0.3, 1.0, 3.0, 1, 1),
('Mysterious Anglerfish', 'The Mysterious Anglerfish is a deep-sea creature known for its bioluminescent lure. It is rarely seen by humans and remains a subject of fascination.', 'RARE', 'Art/Sprites/Fish/FoundFish', 2.0, 5.0, 1.5, 2, 1),
('Giant Squid', 'The Giant Squid is a legendary sea creature that has captured the imagination of sailors for centuries. Its elusive nature makes it a rare sight in the ocean.', 'LEGENDARY', 'Art/Sprites/Fish/FoundFish', 15.0, 50.0, 3.0, 2, 1),
('Spotted Pike', 'The Spotted Pike is a fierce predator known for its sharp teeth and aggressive behavior. It is often found lurking in weedy areas of lakes and rivers.', 'UNCOMMON', 'Art/Sprites/Fish/FoundFish', 1.5, 5.0, 4.5, 1, 1),
('Tropical Clownfish', 'The Tropical Clownfish is a small, colorful fish that lives among sea anemones. Its symbiotic relationship with the anemone provides protection from predators.', 'COMMON', 'Art/Sprites/Fish/FoundFish', 0.1, 0.3, 2.0, 0, 0),
('Black Marlin', 'The Black Marlin is one of the fastest fish in the ocean, known for its incredible speed and agility. It is a prized catch for sport fishermen.', 'LEGENDARY', 'Art/Sprites/Fish/FoundFish', 20.0, 100.0, 10.0, 2, 1),
('Tiger Shark', 'The Tiger Shark is a large predator known for its distinctive stripes and voracious appetite. It is often found in warm coastal waters.', 'RARE', 'Art/Sprites/Fish/FoundFish', 200.0, 400.0, 5.0, 2, 1),
('Guppy', 'Guppies are small, colorful freshwater fish that are popular in home aquariums. They are known for their lively behavior and ease of care.', 'COMMON', 'Art/Sprites/Fish/FoundFish', 0.01, 0.02, 1.0, 0, 1),
('Pufferfish', 'Pufferfish are unique for their ability to inflate when threatened. They are often found in tropical waters and are known for their toxic spines.', 'UNCOMMON', 'Art/Sprites/Fish/FoundFish', 0.5, 1.0, 2.0, 1, 0),
('Lionfish', 'The Lionfish is a venomous fish known for its striking appearance and long, spiky fins. It is a popular aquarium fish but can be invasive in some regions.', 'EPIC', 'Art/Sprites/Fish/FoundFish', 0.5, 1.5, 3.0, 1, 1),
('Swordfish', 'Swordfish are large, powerful fish known for their long, flat bills. They are fast swimmers and are often sought after by sport fishermen.', 'LEGENDARY', 'Art/Sprites/Fish/FoundFish', 100.0, 200.0, 6.0, 2, 0),
('Mahi-Mahi', 'Mahi-Mahi, also known as Dorado, are colorful fish known for their delicious taste. They are often found in warm ocean waters and are a favorite among anglers.', 'UNCOMMON', 'Art/Sprites/Fish/FoundFish', 5.0, 15.0, 7.0, 1, 1),
('Koi', 'Koi are ornamental fish that are often kept in outdoor ponds. They are known for their beautiful colors and patterns, and they can live for many years.', 'COMMON', 'Art/Sprites/Fish/FoundFish', 1.0, 3.0, 2.0, 0, 0),
('Electric Eel', 'Electric Eels are fascinating creatures capable of generating electric shocks. They are often found in freshwater rivers and are known for their unique hunting methods.', 'RARE', 'Art/Sprites/Fish/FoundFish', 5.0, 10.0, 3.0, 2, 1),
('Barracuda', 'Barracudas are fierce predators known for their sharp teeth and streamlined bodies. They are often found in tropical and subtropical oceans.', 'UNCOMMON', 'Art/Sprites/Fish/FoundFish', 5.0, 15.0, 7.0, 1, 1);

-- Populate MarketPrices table with 5 days of data
-- INSERT OR IGNORE INTO MarketPrices (FishID, Day, Price) VALUES
-- -- Day 1
-- (1, 1, 25.50), (2, 1, 30.00), (3, 1, 75.00), (4, 1, 150.00), (5, 1, 300.00),
-- (6, 1, 15.00), (7, 1, 85.00), (8, 1, 180.00), (9, 1, 500.00), (10, 1, 28.00),
-- (11, 1, 80.00), (12, 1, 200.00), (13, 1, 20.00), (14, 1, 160.00), (15, 1, 450.00),
-- (16, 1, 27.00), (17, 1, 280.00), (18, 1, 70.00), (19, 1, 190.00), (20, 1, 550.00),

-- -- Day 2
-- (1, 2, 23.00), (2, 2, 32.00), (3, 2, 80.00), (4, 2, 145.00), (5, 2, 320.00),
-- (6, 2, 14.00), (7, 2, 90.00), (8, 2, 175.00), (9, 2, 520.00), (10, 2, 26.00),
-- (11, 2, 85.00), (12, 2, 195.00), (13, 2, 18.00), (14, 2, 170.00), (15, 2, 460.00),
-- (16, 2, 29.00), (17, 2, 290.00), (18, 2, 75.00), (19, 2, 185.00), (20, 2, 540.00),

-- -- Day 3
-- (1, 3, 26.00), (2, 3, 29.00), (3, 3, 70.00), (4, 3, 160.00), (5, 3, 310.00),
-- (6, 3, 16.00), (7, 3, 82.00), (8, 3, 190.00), (9, 3, 490.00), (10, 3, 30.00),
-- (11, 3, 78.00), (12, 3, 210.00), (13, 3, 21.00), (14, 3, 155.00), (15, 3, 470.00),
-- (16, 3, 25.00), (17, 3, 275.00), (18, 3, 72.00), (19, 3, 195.00), (20, 3, 560.00),

-- -- Day 4
-- (1, 4, 24.00), (2, 4, 31.00), (3, 4, 77.00), (4, 4, 155.00), (5, 4, 305.00),
-- (6, 4, 13.00), (7, 4, 88.00), (8, 4, 185.00), (9, 4, 510.00), (10, 4, 27.00),
-- (11, 4, 83.00), (12, 4, 205.00), (13, 4, 19.00), (14, 4, 165.00), (15, 4, 455.00),
-- (16, 4, 28.00), (17, 4, 285.00), (18, 4, 73.00), (19, 4, 192.00), (20, 4, 545.00),

-- -- Day 5
-- (1, 5, 27.00), (2, 5, 33.00), (3, 5, 82.00), (4, 5, 158.00), (5, 5, 315.00),
-- (6, 5, 17.00), (7, 5, 86.00), (8, 5, 182.00), (9, 5, 515.00), (10, 5, 29.00),
-- (11, 5, 81.00), (12, 5, 208.00), (13, 5, 22.00), (14, 5, 168.00), (15, 5, 465.00),
-- (16, 5, 26.00), (17, 5, 295.00), (18, 5, 76.00), (19, 5, 188.00), (20, 5, 555.00); 