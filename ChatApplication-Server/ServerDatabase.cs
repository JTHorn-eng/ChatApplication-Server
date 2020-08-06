using System;
using System.Data.SQLite;

namespace ChatServer
{
    public static class ServerDatabase
    {
     
        private const string MessagesDBLocation = @"URI=file:C:\ChatAppServer\Messages.db";
        private const string PubKeysDBLocation = @"URI=file:C:\ChatAppServer\PubKeys.db";

        // Adds the public key for a user to the public key database
        public static void AddPublicKey(string username, string key)
        {
            if (username != null || !(username.Equals("")))
            {
                SQLiteConnection connection = new SQLiteConnection(PubKeysDBLocation);
                connection.Open();
                string commandText = "INSERT INTO Keys(Username, Key) VALUES ('" + username + "','" + key + "');";
                SQLiteCommand insert = new SQLiteCommand(commandText, connection);
                insert.ExecuteNonQuery();
                connection.Close();
            }
        }

        // Check if a user exists in the public key database
        public static bool UsernameExists(string name = "")
        {
            // Set up the connection and query
            SQLiteConnection connection = new SQLiteConnection(PubKeysDBLocation);
            connection.Open();
            string commandText = "SELECT * FROM Keys WHERE Username = '" + name + "';";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);

            // Set up the reader
            SQLiteDataReader rdr = select.ExecuteReader();

            // Check if the reader has any data associated with it since this is synonomous to the user having an entry in the public key database 
            return rdr.HasRows;
        }

        // Returns all encrypted message object strings from the messages DB, where a given user is the recipient
        // Message objects are separated by semicolons
        // Also deletes the message records from the DB so they aren't sent twice
        // Returns an empty string if there are no messages for the user
        public static string RetrieveUserMessages(string username = "")
        {
            // Set up the connection and query
            SQLiteConnection connection = new SQLiteConnection(MessagesDBLocation);
            connection.Open();
            string commandText = "SELECT * FROM UserMessages WHERE Recipient='" + username + "';";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);

            // Set up the reader we can use to pull message records in turn
            SQLiteDataReader rdr = select.ExecuteReader();

            string messagesString = "";

            // Return a blank string if there are no messages for the user
            if(!rdr.HasRows)
            {
                return "";
            }

            // Continually read message records from the database to compile all the message objects into a string
            while (rdr.Read())
            {
                messagesString += rdr.GetString(2) + ";";
            }

            // Delete the sent message records from the database
            commandText = "DELETE FROM UserMessages WHERE Recipient='" + username + "';";
            SQLiteCommand delete = new SQLiteCommand(commandText, connection);
            delete.ExecuteNonQuery();

            // Close up and return
            connection.Close();
            return messagesString;
        }

        public static void AddUserMessages(string recipient, string content)
        {
            SQLiteConnection connection = new SQLiteConnection(MessagesDBLocation);
            connection.Open();
            string commandText = "INSERT INTO UserMessages(Recipient, Content) VALUES ('" + recipient + "','" + content + "');";
            SQLiteCommand insert = new SQLiteCommand(commandText, connection);
            insert.ExecuteNonQuery();
            connection.Close();
        }

        // Retrieve the public key for a user from the public key DB
        public static string RetrievePublicKey(string username = "")
        {

            // Set up the connection and query
            SQLiteConnection connection = new SQLiteConnection(PubKeysDBLocation);
            connection.Open();
            string commandText = "SELECT * FROM Keys WHERE Username = '" + username + "';";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);

            // Set up the reader we can use to pull the public key record
            SQLiteDataReader rdr = select.ExecuteReader();

            string message = "NO_KEY_PAIR_FOUND";

            if (rdr.HasRows)
            {
                message = "";
                while (rdr.Read())
                {
                    message += rdr.GetString(1);
                }
            }

            // Close up and return
            connection.Close();
            return message;
        }


    }
}