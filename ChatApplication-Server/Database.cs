using System;
using System.Data.SQLite;

namespace ChatServer
{
    // Handles all I/O with the two local databases, which store messages and public keys respectively
    public static class Database
    {
        // Locations on disk of the two databases
        private const string MessagesDBLocation = @"URI=file:C:\ChatAppServer\Messages.db";
        private const string PubKeysDBLocation = @"URI=file:C:\ChatAppServer\PubKeys.db";

        // Adds the public key for a user to the public key database
        public static void AddPublicKey(string username, string key)
        {
            // Set up the connection
            SQLiteConnection connection = new SQLiteConnection(PubKeysDBLocation);
            connection.Open();
            
            // Set up the command and insert the username and key values
            string commandText = "INSERT INTO Keys(Username, Key) VALUES (@username,@key);";
            SQLiteCommand insertCommand = new SQLiteCommand(commandText, connection);
            insertCommand.Parameters.AddWithValue("@username", username);
            insertCommand.Parameters.AddWithValue("@key", key);

            // Execute the command and close the connection
            insertCommand.ExecuteNonQuery();
            connection.Close();
        }

        // Retrieve the public key for a user from the public key database
        // Returns NO_PUB_KEY_FOUND if the user does not have a pub key
        public static string GetPublicKey(string username)
        {
            // Set up the connection
            SQLiteConnection connection = new SQLiteConnection(PubKeysDBLocation);
            connection.Open();

            // Set up the query and insert the username value
            string commandText = "SELECT * FROM Keys WHERE Username = @username;";
            SQLiteCommand selectCommand = new SQLiteCommand(commandText, connection);
            selectCommand.Parameters.AddWithValue("@username", username);

            // Set up the reader we can use to pull the public key record
            SQLiteDataReader reader = selectCommand.ExecuteReader();

            string pubKey = "NO_PUB_KEY_FOUND";

            if (reader.HasRows)
            {
                // A pub key exists for this user in the DB

                reader.Read();
                pubKey = reader.GetString(1);
            }

            // Close up and return
            connection.Close();

            Console.WriteLine("[INFO] fjshfjghasf: " + pubKey);

            return pubKey;
        }

        // Check if a specific user exists in the public key database
        public static bool CheckUsernameKnown(string username)
        {
            // Set up the connection
            SQLiteConnection connection = new SQLiteConnection(PubKeysDBLocation);
            connection.Open();

            // Set up the query and insert the username value
            string queryText = "SELECT * FROM Keys WHERE Username = @username;";
            SQLiteCommand selectQuery = new SQLiteCommand(queryText, connection);
            selectQuery.Parameters.AddWithValue("@username", username);

            // Set up the reader
            SQLiteDataReader reader = selectQuery.ExecuteReader();

            // Check if the reader has any data associated with it since this is synonomous to the user having an entry in the public key database 
            return reader.HasRows;
        }

        // Creates a new entry in the messages DB with the given recipient and encrypted message object string
        public static void AddUserMessages(string recipient, string content)
        {
            // Set up the connection
            SQLiteConnection connection = new SQLiteConnection(MessagesDBLocation);
            connection.Open();

            // Set up the command and insert the username and content values
            string commandText = "INSERT INTO UserMessages(Recipient, Content) VALUES (@recipient,@content);";
            SQLiteCommand insertCommand = new SQLiteCommand(commandText, connection);
            insertCommand.Parameters.AddWithValue("@recipient", recipient);
            insertCommand.Parameters.AddWithValue("@content", content);

            // Execute the command and close the connection
            insertCommand.ExecuteNonQuery();
            connection.Close();
        }


        // Returns all encrypted message object strings from the messages DB, where a given user is the recipient
        // Message objects are separated by semicolons
        // Also deletes the message records from the DB so they aren't sent twice
        // Returns an empty string if there are no messages for the user
        public static string RetrieveAndDeleteUserMessages(string username)
        {
            // Set up the connection
            SQLiteConnection connection = new SQLiteConnection(MessagesDBLocation);
            connection.Open();

            // Set up the query and insert the username value
            string commandText = "SELECT * FROM UserMessages WHERE Recipient=@username;";
            SQLiteCommand selectQuery = new SQLiteCommand(commandText, connection);
            selectQuery.Parameters.AddWithValue("@username", username);

            // Set up the reader we can use to pull message records in turn
            SQLiteDataReader reader = selectQuery.ExecuteReader();

            string messagesString = "";

            // Return a blank string if there are no messages for the user
            if(!reader.HasRows)
            {
                return "";
            }

            // Continually read message records from the database to compile all the message objects into a string
            while (reader.Read())
            {
                messagesString += reader.GetString(2) + ";";
            }

            // Set up a new command to delete the sent message records from the database
            commandText = "DELETE FROM UserMessages WHERE Recipient=@username;";
            SQLiteCommand deleteCommand = new SQLiteCommand(commandText, connection);
            deleteCommand.Parameters.AddWithValue("@username", username);

            // Execute the command
            deleteCommand.ExecuteNonQuery();

            // Close up and return
            connection.Close();
            return messagesString;
        }
    }
}