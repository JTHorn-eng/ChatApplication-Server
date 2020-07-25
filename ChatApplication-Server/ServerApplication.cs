using System;

namespace ChatServer
{
    class ServerApplication
    {
        public ServerApplication()
        { 
            Server.Start();
        }

        public static void Main(String[] args)
        {
            ServerApplication sa = new ServerApplication();
        }
    }
}