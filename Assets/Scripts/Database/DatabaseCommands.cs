using UnityEngine;
using Mono.Data.Sqlite;
using System;
using System.Data;

namespace Database
{
    public static class DatabaseCommands
    {
        // Links to the Asset folder
        private static readonly string basePath = "URI=file:" + Application.dataPath;


        // Opens connection with a database starting from the /Assets/ folder
        // The extension is the path from basePath e.g. "/StreamingAssets/FishDB.db"
        public static IDbConnection GetConnection(string extension)
        {
            string dbPath = basePath + extension;
            using IDbConnection connection = new SqliteConnection(dbPath);
            connection.Open();
            return connection;
        }

        // Closes the connection with the database
        public static void CloseConnection(IDbConnection connection)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }

        // Executes a reader on the database and returns the result as an IDataReader
        public static IDataReader ExecuteReader(IDbConnection connection, string commandText)
        {
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = commandText;
            return command.ExecuteReader();
        }

        private static IDbCommand CreateCommand(IDbConnection connection, string commandText)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = commandText;
            return command;
        }


    }
}