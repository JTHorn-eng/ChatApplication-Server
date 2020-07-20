using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace ChatServer
{
    class ServerDatabase
    {
        SQLiteConnection connection;
        Dictionary<int, List<string>> databaseObjects;
        private const string server_messages_location = @"URI=file:C:\Users\horn1\source\repos\Chat\Chat\dbfiles\users.db";

        public ServerDatabase()
        {
            Console.WriteLine("Establishing connection to database");
            connection = new SQLiteConnection(server_messages_location);
            connection.Open();
            loadClientMessages();
            connection.Close();
        }

        //find message objects for user in server database
        public void loadClientMessages()
        {
            SQLiteCommand select = new SQLiteCommand(connection);
            select.CommandText = "SELECT * FROM Server_Messages";
            SQLiteDataReader rdr = select.ExecuteReader();
            Console.WriteLine("Retrieving values from server_messages");

            databaseObjects = new Dictionary<int, List<string>>();
            while (rdr.Read())
            {
                List<string> databaseItems = new List<string>();
                for (int x = 1; x < rdr.FieldCount; x++)
                {
                    databaseItems.Add(rdr.GetString(x));

                }
                databaseObjects.Add(rdr.GetInt32(0), databaseItems);
            }
        }

        public void deleteMessages()
        {
            SQLiteCommand delete = new SQLiteCommand(connection);
            delete.CommandText = "";
        }


        public Dictionary<int, List<string>> getDatabaseObjects()
        {
            return databaseObjects;
        }




    }
}
