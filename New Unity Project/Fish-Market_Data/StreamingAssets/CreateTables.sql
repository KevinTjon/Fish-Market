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
    Rarity TEXT NOT NULL,
    ListedPrice REAL NOT NULL,
    IsSold INTEGER DEFAULT 0,    -- 0 = not sold, 1 = sold
    SellerID INTEGER NOT NULL,   -- 0 for player, 1+ for NPCs/bots
    FOREIGN KEY (FishName) REFERENCES Fish(Name)
);

CREATE TABLE IF NOT EXISTS Customers (
    CustomerID INTEGER PRIMARY KEY,
    CustomerType INTEGER,
    Budget INTEGER,
    IsActive INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS CustomerBiases (
    CustomerID INTEGER,
    SellerID INTEGER,
    Rarity INTEGER,
    BiasValue REAL,
    FOREIGN KEY(CustomerID) REFERENCES Customers(CustomerID),
    PRIMARY KEY(CustomerID, SellerID, Rarity)
);

CREATE TABLE IF NOT EXISTS CustomerPreferences (
    CustomerID INTEGER,
    FishName TEXT,
    PreferenceScore REAL,
    Rarity INTEGER,
    HasPurchased BOOLEAN,
    PRIMARY KEY (CustomerID, FishName),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);

CREATE TABLE IF NOT EXISTS ListingRejections (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    ListingID INTEGER NOT NULL,
    CustomerID INTEGER NOT NULL,
    Reason TEXT NOT NULL,
    RejectionTime DATETIME NOT NULL,
    FOREIGN KEY (ListingID) REFERENCES MarketListings(ListingID),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);

