using UnityEngine;
using Mono.Data.Sqlite;

public class ClearMarketListings : MonoBehaviour
{
    public void ClearTables()
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Clear MarketListings table
                    command.CommandText = "DELETE FROM MarketListings";
                    command.ExecuteNonQuery();
                    //Debug.Log("MarketListings table cleared successfully");

                    // Clear MarketPrices table
                    command.CommandText = "DELETE FROM MarketPrices";
                    command.ExecuteNonQuery();
                    //Debug.Log("MarketPrices table cleared successfully");

                    // Clear Customers table
                    command.CommandText = "DELETE FROM Customers";
                    command.ExecuteNonQuery();
                    //Debug.Log("Customers table cleared successfully");

                    // Clear CustomerShoppingLists table
                    command.CommandText = "DELETE FROM CustomerShoppingList";
                    command.ExecuteNonQuery();
                    //Debug.Log("CustomerShoppingLists table cleared successfully");

                    // Clear CustomerBiases table
                    command.CommandText = "DELETE FROM CustomerBiases";
                    command.ExecuteNonQuery();
                    //Debug.Log("CustomerBiases table cleared successfully");

                    // Clear ListingRejections table
                    command.CommandText = "DELETE FROM ListingRejections";
                    command.ExecuteNonQuery();
                    
                    // Clear CustomerPreferences table
                    command.CommandText = "DELETE FROM CustomerPreferences";
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing tables: {e.Message}");
        }
    }

    public void ClearDailyTables()
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Clear only daily transaction tables, preserve customer data
                    
                    // Clear MarketListings table (daily fish listings)
                    command.CommandText = "DELETE FROM MarketListings";
                    command.ExecuteNonQuery();
                    Debug.Log("MarketListings cleared for next day");

                    // Clear CustomerShoppingLists table (daily shopping lists)
                    command.CommandText = "DELETE FROM CustomerShoppingList";
                    command.ExecuteNonQuery();
                    Debug.Log("CustomerShoppingLists cleared for next day");

                    // Clear ListingRejections table (daily rejection records)
                    command.CommandText = "DELETE FROM ListingRejections";
                    command.ExecuteNonQuery();
                    Debug.Log("ListingRejections cleared for next day");

                    // Note: We do NOT clear these tables as they contain persistent data:
                    // - Customers (persistent customer records)
                    // - CustomerPreferences (persistent customer preferences)
                    // - CustomerBiases (persistent customer biases)
                    // - MarketPrices (historical price data)
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing daily tables: {e.Message}");
        }
    }
}