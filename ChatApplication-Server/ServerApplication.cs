using System;

namespace ChatServer
{
    class ServerApplication
    {

        ServerDatabase serverDB;
        public ServerApplication()
        { 

            serverDB = new ServerDatabase();
            Server.Start();
        }

        public static void Main(String[] args)
        {
            ServerApplication sa = new ServerApplication();
        }
    }
}