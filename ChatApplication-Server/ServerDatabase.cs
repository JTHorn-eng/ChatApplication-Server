using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ChatServer
{
    class ServerDatabase
    {
        SQLiteConnection connection;
        Dictionary<int, List<string>> databaseObjects;
        private const string server_messages_location = @"URI=file:C:\Users\horn1\source\repos\ChatApplication-Server\ChatApplication-Server\database\Users.db";

        public ServerDatabase()
        {

        }

        //find message objects for user in server database
        public string retrieveClientMessages(string name = "PersonA")
        {

            connection = new SQLiteConnection(server_messages_location);
            connection.Open();
            string commandText = "SELECT * FROM UserMessages WHERE Sender='" + name + "'";
            SQLiteCommand select = new SQLiteCommand(commandText, connection);
            SQLiteDataReader rdr = select.ExecuteReader();
            Console.WriteLine("Retrieving values from server_messages");
            string message = "";
            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    message += rdr.GetInt32(0) + ":" //ID
                        + rdr.GetString(1) + ":"     //Sender
                        + rdr.GetString(2) + ":"     //Receiver
                        + rdr.GetString(3) + ":"     //Content
                        + rdr.GetString(4);          //Timestamp
                }
            }
            connection.Close();
            return message;
            }
        }
    
}