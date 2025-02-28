using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Data.Sqlite;

public class ConnectionPool
{
    private static ConnectionPool _instance;
    public static ConnectionPool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ConnectionPool();
            }
            return _instance;
        }
    }

    private readonly Queue<SqliteConnection> availableConnections;
    private readonly HashSet<SqliteConnection> inUseConnections;
    private readonly object lockObject = new object();
    private readonly int maxPoolSize;
    private readonly string connectionString;
    private int totalConnections;

    private ConnectionPool()
    {
        maxPoolSize = 10; // Adjust based on your needs
        availableConnections = new Queue<SqliteConnection>();
        inUseConnections = new HashSet<SqliteConnection>();
        string dbName = "FishDB.db";
        string dbFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, dbName);
        connectionString = $"URI=file:{dbFilePath}";
        totalConnections = 0;
    }

    public SqliteConnection GetConnection()
    {
        lock (lockObject)
        {
            SqliteConnection connection = null;

            // Try to get an available connection
            if (availableConnections.Count > 0)
            {
                connection = availableConnections.Dequeue();
                
                // Test the connection
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }
                }
                catch
                {
                    // If connection is broken, create a new one
                    connection.Dispose();
                    connection = CreateNewConnection();
                }
            }
            // Create new connection if pool isn't full
            else if (totalConnections < maxPoolSize)
            {
                connection = CreateNewConnection();
            }
            // Wait for a connection to become available
            else
            {
                Debug.LogWarning("Connection pool is full. Waiting for available connection...");
                Monitor.Wait(lockObject);
                return GetConnection(); // Recursive call after being notified
            }

            if (connection != null)
            {
                inUseConnections.Add(connection);
            }

            return connection;
        }
    }

    public void ReleaseConnection(SqliteConnection connection)
    {
        if (connection == null) return;

        lock (lockObject)
        {
            if (inUseConnections.Remove(connection))
            {
                // Only return good connections to the pool
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    availableConnections.Enqueue(connection);
                }
                else
                {
                    try
                    {
                        connection.Open();
                        availableConnections.Enqueue(connection);
                    }
                    catch
                    {
                        // If connection is bad, dispose it and reduce count
                        connection.Dispose();
                        totalConnections--;
                    }
                }

                // Notify any waiting threads
                Monitor.Pulse(lockObject);
            }
        }
    }

    private SqliteConnection CreateNewConnection()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        totalConnections++;
        return connection;
    }

    public void CloseAllConnections()
    {
        lock (lockObject)
        {
            foreach (var connection in availableConnections)
            {
                try
                {
                    connection.Close();
                    connection.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error closing connection: {e.Message}");
                }
            }

            foreach (var connection in inUseConnections)
            {
                try
                {
                    connection.Close();
                    connection.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error closing connection: {e.Message}");
                }
            }

            availableConnections.Clear();
            inUseConnections.Clear();
            totalConnections = 0;
        }
    }
} 