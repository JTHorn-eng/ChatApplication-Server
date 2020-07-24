using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace ChatServer
{
    class ServerDatabase
    {
        Dictionary<int, List<string>> databaseObjects;
        private const string server_messages_location = @"URI=file:C:\Users\horn1\source\repos\ChatApplication-Server\ChatApplication-Server\database\Users.db";
        private const string server_keys_location = @"URI=file:C:\Users\horn1\source\repos\ChatApplication-Server\ChatApplication-Server\database\SymmetricKeys.db";

        public ServerDatabase()
        {

        }

        //find message objects for user in server database
        public string retrieveClientMessages(string name = "Person A")
        {

            SQLiteConnection connection = new SQLiteConnection(server_messages_location);
            connection.Open();
            string commandText = "SELECT * FROM UserMessages WHERE Sender='" + name + "'";
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


        //find AES symmetric key for a client pair
        public string RetrieveAESSymmetricKey(string name = "Person A")
        {
            SQLiteConnection connection = new SQLiteConnection(server_keys_location);
            connection.Open();
            string commandText = "SELECT * FROM Keys WHERE Username = '" + name + "'";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);
            SQLiteDataReader rdr = select.ExecuteReader();
            Console.WriteLine("Retrieving AES symmetric key for user: " + name);
            string message = "";

            while (rdr.Read())
            {
                message += rdr.GetInt32(0) + ":" //ID
                    + rdr.GetString(1) + ":"     //Username
                    + rdr.GetString(2) + ":";     //Key

            }

            Console.WriteLine("Message:" + message);
            connection.Close();
            return message;


        }
    }
}
