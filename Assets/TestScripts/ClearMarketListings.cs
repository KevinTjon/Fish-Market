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
}