using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;

namespace ChatServer
{

    /**
     * FOR SOME GENERAL UNDERSTANDING
     * 
     * 1) Setup a remote endpoint connection to the server device
     * 2) Use this for a listening socket for client connections
     * 3) Use an AcceptCallback method to resume acceptionThread, establish client connection and read data from client
     * 4) Use ReadCallback to continuously get bytes of information until
     * 5) store read data from client via ReadCallback into a state object
     * 6) Have another Send method to send data to client
     *  
     *  
     *  USEFUL WEBSITE FOR UNDERSTANDING CONNECTION FOR MULTIPLE CLIENTS
     *  https://stackoverflow.com/questions/5815872/asynchronous-server-socket-multiple-clients
     *  https://www.winsocketdotnetworkprogramming.com/serverlisteningnetworksocketdotnet9.html
     * https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example
     * summary - one listener socket for accepting client requests, use callbacks to handle multiple clients
     */



    class StateObject
    {
        // Client initial name
        public string name = "";
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();


        public StateObject(Socket handler)
        {
            workSocket = handler;
        }


    }
    class Server
    {

        private static List<StateObject> connectedClients; //list of connected clients

        private const int PORT_NUMBER = 25565;
        IPAddress a;
        IPHostEntry ipHostInfo;
        IPEndPoint localEP;
        Socket listener;
        ServerDatabase serverDatabase;
        public static ManualResetEvent AcceptingConnectionsThread; //thread for accepting connections
        public static ManualResetEvent ReceivingThread;
        public static ManualResetEvent SendingThread; //thread for handling each connection

        private static int clientSocketIndex = 0;

         
        public Server()
        {
            //init server
            a = IPAddress.Parse("127.0.0.1");
            localEP =  new IPEndPoint(a, PORT_NUMBER);
            AcceptingConnectionsThread = new ManualResetEvent(false);
            SendingThread = new ManualResetEvent(false);
            ReceivingThread = new ManualResetEvent(false);
            connectedClients = new List<StateObject>();
            listener = new Socket(a.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            serverDatabase = new ServerDatabase();
            ServerCycle();
        }

        public void ServerCycle()
        {
            Console.WriteLine("Server endpoint, listening on: " + localEP.Port);
            listener.Bind(localEP);
            listener.Listen(10); //max no. of clients the server will accept before returning "server too busy"
            while (true)
                {
                    //handle connections
                    AcceptingConnectionsThread.Reset();
                    Console.WriteLine("Waiting for a client connection");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    AcceptingConnectionsThread.WaitOne();

                    //receive and process client data
                    ReceivingThread.Set();
                    ReceiveClientData();
                    ReceivingThread.WaitOne();


                    //send data to clients
                    SendingThread.Reset();
                    HandleServerMessageDownloads();
                    SendingThread.WaitOne();

                }
            listener.Close();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            Console.WriteLine("Callback");
            AcceptingConnectionsThread.Set(); //set thread to true, signal accepting connections thread to continue

            //establish connection to client
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            connectedClients.Add(new StateObject(handler));
        }

        private static void ReceiveClientData()
        {
            for (int x = 0; x < connectedClients.Count; x++) 
            {
                //get data from client and store in state object using ReadCallback
                connectedClients[x].workSocket.BeginReceive(connectedClients[x].buffer, 0, connectedClients[x].buffer.Length, 0, new AsyncCallback(ReadCallback), connectedClients[x]);
                clientSocketIndex = x;
            }
           
        }

        //returns data sent by client
        private static void ReadCallback(IAsyncResult ar)
        {

            //get state object and the handler socket
            string content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            //read data from the client socket
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                //get strings from client, store in state object
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
                content = state.sb.ToString();


            }


            else
            {
                if (state.sb.Length > 1)
                {
                    //All the data has been read from the client
                    //display it on the console.
                    string stuff = state.sb.ToString();
                    Console.WriteLine($"Read {stuff.Length} bytes from worker socket");
                }
                //add state object to dicitonary of handled clients
                connectedClients[clientSocketIndex] = state;
                handler.Close();
            }

            
        }

        //
        private void HandleServerMessageDownloads()
        {
            SendingThread.Set();
            for (int x = 0; x < connectedClients.Count; x++)
            {
                //Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(serverDatabase.retrieveClientMessages());


                //Begin sending the data to the remote device.
                connectedClients[x].workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), connectedClients[x].workSocket);
            }   
        }
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                //Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Send {0} bytes to client.", bytesSent);


                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }










    }
}
