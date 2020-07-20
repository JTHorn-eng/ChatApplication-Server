using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServer
{
    class ServerApplication
    {

        ServerDatabase serverDB;
        Server server;
        public ServerApplication()
        { 

            serverDB = new ServerDatabase();
            server = new Server();
        }


        public static void Main(String[] args)
        {
            Console.WriteLine("Starting server");
            ServerApplication sa = new ServerApplication();


        }


    }
}
