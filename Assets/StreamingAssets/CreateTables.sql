CREATE TABLE IF NOT EXISTS Inventory
(
    Id INTEGER PRIMARY KEY,
    Name TEXT,
    Weight TEXT,
    Rarity TEXT,
    AssetPath TEXT
);

-- Create the Fish table
CREATE TABLE IF NOT EXISTS Fish (
    Name TEXT PRIMARY KEY, -- Unique fish name
    Description TEXT,
    Rarity TEXT,
    AssetPath TEXT,
    MinWeight REAL,
    MaxWeight REAL,
    TopSpeed REAL,
    HookedFuncNum INTEGER,
    IsDiscovered INTEGER
);

-- Create MarketPrices table
CREATE TABLE IF NOT EXISTS MarketPrices
(
    ID INTEGER PRIMARY KEY AUTOINCREMENT, -- Primary key with auto-increment
    FishName TEXT NOT NULL,
    Day INTEGER NOT NULL,
    Price REAL NOT NULL,
    FOREIGN KEY (FishName) REFERENCES Fish(Name), -- Foreign key constraint
    UNIQUE(FishName, Day) -- Ensure unique fish names for each day
);

-- Create MarketListings table
CREATE TABLE IF NOT EXISTS MarketListings
(
    ListingID INTEGER PRIMARY KEY AUTOINCREMENT,
    FishName TEXT NOT NULL,
    ListedPrice REAL NOT NULL,
    IsSold INTEGER DEFAULT 0,    -- 0 = not sold, 1 = sold
    SellerID INTEGER NOT NULL,   -- 0 for player, 1+ for NPCs/bots
    FOREIGN KEY (FishName) REFERENCES Fish(Name)
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

-- Populate MarketPrice table with fish names and prices for 5 days

-- Day 1 Prices
INSERT OR REPLACE INTO MarketPrices (FishName, Day, Price) VALUES
('Golden Trout', 1, 15.00),
('Silver Salmon', 1, 10.00),
('Crimson Snapper', 1, 20.00),
('Emerald Catfish', 1, 8.00),
('Bluefin Tuna', 1, 30.00),
('Rainbow Trout', 1, 12.00),
('Mysterious Anglerfish', 1, 25.00),
('Giant Squid', 1, 40.00),
('Spotted Pike', 1, 18.00),
('Tropical Clownfish', 1, 5.00),
('Black Marlin', 1, 50.00),
('Tiger Shark', 1, 200.00),
('Guppy', 1, 2.00),
('Pufferfish', 1, 10.00),
('Lionfish', 1, 15.00),
('Swordfish', 1, 100.00),
('Mahi-Mahi', 1, 20.00),
('Koi', 1, 10.00),
('Electric Eel', 1, 30.00),
('Barracuda', 1, 25.00);

-- Day 2 Prices
INSERT OR REPLACE INTO MarketPrices (FishName, Day, Price) VALUES
('Golden Trout', 2, 14.50),
('Silver Salmon', 2, 9.50),
('Crimson Snapper', 2, 19.50),
('Emerald Catfish', 2, 7.50),
('Bluefin Tuna', 2, 28.50),
('Rainbow Trout', 2, 11.50),
('Mysterious Anglerfish', 2, 24.50),
('Giant Squid', 2, 38.50),
('Spotted Pike', 2, 17.50),
('Tropical Clownfish', 2, 4.50),
('Black Marlin', 2, 48.00),
('Tiger Shark', 2, 190.00),
('Guppy', 2, 1.50),
('Pufferfish', 2, 9.00),
('Lionfish', 2, 14.00),
('Swordfish', 2, 95.00),
('Mahi-Mahi', 2, 18.00),
('Koi', 2, 9.00),
('Electric Eel', 2, 28.00),
('Barracuda', 2, 22.00);

-- Day 3 Prices
INSERT OR REPLACE INTO MarketPrices (FishName, Day, Price) VALUES
('Golden Trout', 3, 16.00),
('Silver Salmon', 3, 11.00),
('Crimson Snapper', 3, 21.00),
('Emerald Catfish', 3, 8.50),
('Bluefin Tuna', 3, 32.00),
('Rainbow Trout', 3, 12.50),
('Mysterious Anglerfish', 3, 26.00),
('Giant Squid', 3, 42.00),
('Spotted Pike', 3, 19.00),
('Tropical Clownfish', 3, 5.50),
('Black Marlin', 3, 52.00),
('Tiger Shark', 3, 210.00),
('Guppy', 3, 2.50),
('Pufferfish', 3, 11.00),
('Lionfish', 3, 16.00),
('Swordfish', 3, 105.00),
('Mahi-Mahi', 3, 22.00),
('Koi', 3, 11.00),
('Electric Eel', 3, 32.00),
('Barracuda', 3, 26.00);

-- Day 4 Prices
INSERT OR REPLACE INTO MarketPrices (FishName, Day, Price) VALUES
('Golden Trout', 4, 15.75),
('Silver Salmon', 4, 10.75),
('Crimson Snapper', 4, 20.75),
('Emerald Catfish', 4, 8.25),
('Bluefin Tuna', 4, 31.75),
('Rainbow Trout', 4, 12.25),
('Mysterious Anglerfish', 4, 25.75),
('Giant Squid', 4, 41.75),
('Spotted Pike', 4, 18.75),
('Tropical Clownfish', 4, 5.25),
('Black Marlin', 4, 49.00),
('Tiger Shark', 4, 205.00),
('Guppy', 4, 2.25),
('Pufferfish', 4, 10.50),
('Lionfish', 4, 15.50),
('Swordfish', 4, 102.00),
('Mahi-Mahi', 4, 19.00),
('Koi', 4, 10.50),
('Electric Eel', 4, 31.00),
('Barracuda', 4, 24.00);

-- Day 5 Prices
INSERT OR REPLACE INTO MarketPrices (FishName, Day, Price) VALUES
('Golden Trout', 5, 17.00),
('Silver Salmon', 5, 12.00),
('Crimson Snapper', 5, 22.00),
('Emerald Catfish', 5, 9.00),
('Bluefin Tuna', 5, 33.00),
('Rainbow Trout', 5, 13.00),
('Mysterious Anglerfish', 5, 27.00),
('Giant Squid', 5, 43.00),
('Spotted Pike', 5, 20.00),
('Tropical Clownfish', 5, 6.00),
('Black Marlin', 5, 55.00),
('Tiger Shark', 5, 215.00),
('Guppy', 5, 2.75),
('Pufferfish', 5, 12.00),
('Lionfish', 5, 17.00),
('Swordfish', 5, 110.00),
('Mahi-Mahi', 5, 21.00),
('Koi', 5, 11.00),
('Electric Eel', 5, 33.00),
('Barracuda', 5, 27.00);