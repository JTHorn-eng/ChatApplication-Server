using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer
{

    /**
     * FOR SOME GENERAL UNDERSTANDING
     * 
     * 
     * ManualResetEvent - Used for send signals between two or more threads
     * maintains a boolean variable in memory
     * false - blocks all threads
     * true unblocks all threads
     * 
     * I think this is how it works...
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
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
    class Server
    {

        private static Dictionary<string, StateObject> connectedClients; //list of connected clients


        private const int PORT_NUMBER = 25565;
        IPAddress a;
        IPHostEntry ipHostInfo;
        IPEndPoint localEP;
        Socket listener;
        //manual thread synchronisation events
        //could use one thread for all of this... but no
        public static ManualResetEvent AcceptingConnectionsThread; //thread for accepting connections
        public static ManualResetEvent SendingThread; //thread for handling each connection

         
        public Server()
        {
            //get remote endpoint for listener socket
            //ipHostInfo = Dns.GetHostByName("localhost");
            a = IPAddress.Parse("127.0.0.1");
            localEP =  new IPEndPoint(a, PORT_NUMBER);
            AcceptingConnectionsThread = new ManualResetEvent(false);
            SendingThread = new ManualResetEvent(false);
            connectedClients = new Dictionary<string, StateObject>();
            StartListening();
        }

        public void StartListening()
        {
            //establish thread for accepting connections
            //create a new asynchronous socket
            listener = new Socket(a.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine(localEP.Port);
            listener.Bind(localEP);
            listener.Listen(10); //max no. of clients the server will accept before returning "server too busy"
            Console.WriteLine("Listening on: " + localEP.ToString());   
            while (true)
                {
                    AcceptingConnectionsThread.Reset();
                    Console.WriteLine("Waiting for a connection");
                    //begin accpeting client connections
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                //Console.WriteLine("Accepted connection from: {0}",listener.RemoteEndPoint.ToString());
                
                AcceptingConnectionsThread.WaitOne();
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

            Console.WriteLine("trying to contact a client");
            //start asynchronous read of data from client
            //Requires state object to read data from a client socket (created above)
            StateObject state = new StateObject();

            state.workSocket = handler;
            //get data from client and store in state object using ReadCallback
            handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);


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

                content = state.sb.ToString();

                //add state object to dicitonary of handled clients

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
                handler.Close();
            }
        }

        //
        private static void SendMessageToClient(Socket handler, String data)
        {

            SendingThread.Reset();

            //Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);


            //Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }



        //send message objects to selected clients
        private static void SendDataBaseObejctsToClient()
        {

        }

        private static void SendCallback(IAsyncResult ar)
        {
            SendingThread.Set();
            try
            {
                //Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Send {0} bytes to client.", bytesSent);

                SendingThread.WaitOne();

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
