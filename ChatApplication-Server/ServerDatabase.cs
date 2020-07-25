using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Runtime.CompilerServices;

namespace ChatServer
{
    public static class ServerDatabase
    {
        private static Dictionary<int, List<string>> databaseObjects;
        private const string server_messages_location = @"URI=file:C:\Users\horn1\source\repos\ChatApplication-Server\ChatApplication-Server\database\Users.db";
        private const string server_keys_location = @"URI=file:C:\Users\horn1\source\repos\ChatApplication-Server\ChatApplication-Server\database\SymmetricKeys.db";

        private static String GetTimestamp(this DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssfff");
        }



        //add public key to database
        public static void addPublicKey(string username, string key)
        {
            if (username != null || username != "")
            {
                SQLiteConnection connection = new SQLiteConnection(server_keys_location);
                connection.Open();
                string commandText = "INSERT INTO Keys(Username, Key) VALUES (" + username + ", " + key + ");";
                SQLiteCommand insert = new SQLiteCommand(commandText, connection);
                connection.Close();
            }
        }

        //check if username exists
        public static bool usernameExists(string name = "")
        {
            SQLiteConnection connection = new SQLiteConnection(server_keys_location);
            connection.Open();
            string commandText = "SELECT * FROM Keys WHERE Username = '" + name + "';";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);
            SQLiteDataReader rdr = select.ExecuteReader();
            connection.Close();

            return rdr.HasRows;
        }


        //find message objects for user in server database
        public static string retrieveClientMessages(string name = "")
        {
            //retrieve current timestamp
            string currentTimestamp = ServerDatabase.GetTimestamp(new DateTime());
            SQLiteConnection connection = new SQLiteConnection(server_messages_location);
            connection.Open();
            string commandText = "SELECT * FROM UserMessages WHERE Sender='" + name + "' AND Timestamp < + '" + currentTimestamp + "';";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);
            SQLiteDataReader rdr = select.ExecuteReader();
            Console.WriteLine("Retrieving values from server_messages");
            string message = "";

            while (rdr.Read())
            {
                message += rdr.GetInt32(0) + ":" //ID
                    + rdr.GetString(1) + ":"     //Sender
                    + rdr.GetString(2) + ":"     //Receiver
                    + rdr.GetString(3) + ":"     //Content
                    + rdr.GetString(4);          //Timestamp
            }

            Console.WriteLine("Message:" + message);
            connection.Close();
            return message;
        }



        //find RSA public key for a client pair
        public static string RetrieveRSAPublicKey(string name = "")
        {
            SQLiteConnection connection = new SQLiteConnection(server_keys_location);
            connection.Open();
            string commandText = "SELECT * FROM Keys WHERE Username = '" + name + "';";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);
            SQLiteDataReader rdr = select.ExecuteReader();
            Console.WriteLine("Retrieving AES symmetric key for user: " + name);
            string message = "NO_KEY_PAIR_FOUND";
            if (rdr.HasRows)
            {
                message = "";
                while (rdr.Read())
                {
                    message += rdr.GetString(1) + ":"     //Username
                             + rdr.GetString(2) + ":";     //Key

                }
            }
            connection.Close();
            return message;
        }

        public static Dictionary<int, List<string>> getUserMessageObjects()
        {
            return databaseObjects;
        }
    }
}
    

