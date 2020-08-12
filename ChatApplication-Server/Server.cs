using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer
{

    // State object for storing client info, particularly data received
    public class StateObject
    {
        // Client socket
        public Socket workSocket = null;

        // Chat username for client
        public string userName = "";

        // Public key for client
        public string pubKey = "";

        // Size of receive buffer
        public const int BufferSize = 1024;

        // Receive buffer
        public byte[] buffer = new byte[BufferSize];

        // Received data string
        public StringBuilder sb = new StringBuilder();

        // MREs for signalling when threads may proceed
        public readonly ManualResetEvent receiveDone = new ManualResetEvent(false);
        public readonly ManualResetEvent sendDone = new ManualResetEvent(false);
    }

    // Functions as an async socket listener
    // Use with Server.Start()
    public static class Server
    {
        // Basic settings
        private const int ListenPort = 8182;
        private const int MaxClients = 100;

        // MRE for signalling when connection to a client is done
        private static readonly ManualResetEvent connectionDone = new ManualResetEvent(false);

        // Dictionary to store StateObjects for all connected clients, referenced by username
        private static Dictionary<string, StateObject> connectedClients = new Dictionary<string, StateObject>();

        // Initialises connection to client then keeps listening for data from client
        public static void Start()
        {
            // Initialises endpoint (IP + port) to create a socket on
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, ListenPort);

            // Create a TCP/IP socket
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the endpoint and listen for incoming connections
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(MaxClients);

                while (true)
                {
                    // Await connection
                    Console.WriteLine("[INFO] Waiting for connection");

                    // When a client connects, AcceptCallback runs in a new thread to handle the connection
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // Pause until client connects
                    connectionDone.WaitOne();

                    Console.WriteLine("[INFO] Client connected");

                    // Client connected. Now reset the MRE so we pause next loop while waiting for another client
                    connectionDone.Reset();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Handle a client connection
        private static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue so more clients can be dealt with
            connectionDone.Set();

            // Get the listener socket
            Socket listener = (Socket)ar.AsyncState;

            // Fetch the new socket that will handle this client specifcally
            Socket handler = listener.EndAccept(ar);

            // Create the state object for this client
            StateObject state = new StateObject
            {
                workSocket = handler
            };

            // Connection all established now. Perform handshake to get client established
            ChatHandshake(state);

            bool connectionClosed = false;

            // Runs while the client remains connected. Does the following regularly:
            // - Receives and handles incoming messages from the client
            // - Sends recipient public key for encryption
            // - Checks for and sends new messages in the DB for the client
            // - Checks whether the client is still connected and closes up if not
            while (!connectionClosed)
            {
                // Set up a callback for when the client begins sending data
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

                // Reset the MRE so we can detect when we've finished receiving data
                state.receiveDone.Reset();

                bool dataReceived = false;

                // Runs repeatedly while we wait for and receieve data from the client. Does the following:
                // - Checks for and sends new messages in the DB for the client
                // - Checks whether the client is still connected and closes up if not
                while (!dataReceived)
                {
                    // Check if the client has disconnected
                    if (handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0)
                    {
                        Console.WriteLine("[INFO] Client disconnected. Closing up...");

                        // Shutdown and close the socket
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();

                        // Remove the client from the dictionary
                        connectedClients.Remove(state.userName);

                        // Signal the outer loop to exit so the thread for this client terminates
                        connectionClosed = true;

                        // Break out of the inner loop
                        break;
                    }

                    // Retrieve new messages for the user from the DB and then delete them from the DB
                    string newMessages = Database.RetrieveAndDeleteUserMessages(state.userName);

                    // If we have new messages
                    if(newMessages != "")
                    {
                        // Send the new messages to the client
                        Send(state, "MESSAGES:<SOR>" + state.userName + "<EOR><SOT>" + newMessages + "<EOT><EOF>");
                    }

                    // Check whether we've received data from the client (but do not wait)
                    dataReceived = state.receiveDone.WaitOne(0);

                    // Repeat these checks every 100ms
                    Thread.Sleep(100);
                }

                // If we've got data from the client (i.e. the connection wasn't closed)
                if (dataReceived)
                {

                    // Get the received message
                    string message = state.sb.ToString();

                    // Reset the client's buffer so we can receive more data from it
                    state.sb = new StringBuilder();
                    state.buffer = new byte[StateObject.BufferSize];

                    //If client message is a request for recipient public key instead of a normal message
                    if (message.LastIndexOf("<SOT>") == -1)
                    {
                        Console.WriteLine("[INFO] Sending public key for recipient ");
                        Console.WriteLine("Message: " + message);

                        Console.WriteLine("User: " + message.Split(":")[0]);
                        //Get recipient's public key
                        string recPub = Database.GetPublicKey(message.Split(':')[0]);

                        Console.WriteLine("[INFO] Key Obtained: "+ recPub);


                        //Send public key
                        Send(state, recPub + "<EOF>");
                    }

                    // Client sends messages in the following format: "MESSAGES<SOR>recipient<EOR><SOT>content<EOT><EOF>"
                    // Parse out the recipient and content and add the message to the DB

                    Console.WriteLine("Message: " + message);

                    string recipient = "";
                    for (int x = message.IndexOf("<SOR>") + "<SOR>".Length; x < message.LastIndexOf("<EOR>"); x++)
                    {
                        recipient += message[x];
                    }

                    Console.WriteLine(message);
                    string content = "";
                    for (int x = message.IndexOf("<SOT>") + "<SOT>".Length; x < message.LastIndexOf("<EOT>"); x++)
                    {
                        content += message[x];
                    }

                    Console.WriteLine("[INFO] New message received for " + recipient + ". Message contents: " + content + ". Adding to DB...");
                    Database.AddUserMessages(recipient, content);
                }
            }
        }

        // Run through a set process to get a client established after connection
        // Gets the client's chat username, its public key (if needed) and hands over new messages for the client
        private static void ChatHandshake(StateObject state)
        {
            // Get the handler socket for this client
            Socket handler = state.workSocket;

            // Receive chat username from client
            string clientIDResponse = Receive(state);

            // Client responds with "IDENTIFICATION:dummy_name<EOF>"
            // We parse this string and store it in the client's state object
            state.userName = clientIDResponse.Split(":")[1].Replace("<EOF>", "");

            Console.WriteLine("[INFO] Client username is " + state.userName);

            // Check if the client's username exists in the public key database (and so whether they have a public key)
            bool pubKeyPresent = Database.CheckUsernameKnown(state.userName);

            Console.WriteLine("ASDASDASD" + pubKeyPresent);

            if (!pubKeyPresent)
            {
                Console.WriteLine("[INFO] Sending key request to client");

                // This is a new client. We haven't got a public key for it
                // Ask the client to generate a key pair and send us the public key
                Send(state, "KEY_GEN_REQUEST:<EOF>");

                // Wait for pub key
                string pubKeyResponse = Receive(state);

                // Client responds with "PUBKEY: dummy_key<EOF>"
                // We parse this string and store it in the client's state object
                state.pubKey = (pubKeyResponse.Replace("PUBKEY:", "").Replace("<EOF>", ""));
                Console.WriteLine("[INFO] Public key for client is " + state.pubKey);

                Console.WriteLine("[INFO] Adding public key to the database");

                // Add public key to database
                Database.AddPublicKey(state.userName, state.pubKey);
            }
            else
            {
                // We've seen this client before

                Console.WriteLine("[INFO] Retrieving public key for client from database");

                // Retrieve the pub key from the database and store it in the state object
                state.pubKey = Database.GetPublicKey(state.userName);
            }

            Console.WriteLine("[INFO] Sending new messages to client");

            // Fetch and send new messages for this user from the database
            Send(state, "MESSAGES:" + Database.RetrieveAndDeleteUserMessages(state.userName) + "<EOF>");

            // Add client's username and state object to dictionary
            connectedClients.Add(state.userName, state);

            //Attempt to receive recipient public key from database
            string recipientPubKeyRequest = Receive(state);


            if (recipientPubKeyRequest.IndexOf("KEY_REQUEST") > -1)
            {
                Console.WriteLine("[INFO] Received recipient key request from client: " + recipientPubKeyRequest);

                //If a key request has been made by the client 
                //Send the recipient key for RSA encryption
                Send(state, Database.GetPublicKey(recipientPubKeyRequest) + "<EOF>");
            }


        }

        // Receive data from a client. Blocks parent thread execution
        private static string Receive(StateObject state)
        {
            // Get the listener socket
            Socket handler = state.workSocket;

            // Reset the MRE so we pause until the client responds
            state.receiveDone.Reset();

            // Set up a callback for when the client begins sending data
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

            // Wait for the client to be received
            state.receiveDone.WaitOne();

            // Get the received data
            string data = state.sb.ToString();

            // Reset the client's buffer so we can receive more data from it
            state.sb = new StringBuilder();
            state.buffer = new byte[StateObject.BufferSize];

            return data;
        }

        // Callback to handle receiving data from the client
        public static void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // Append the data we've just got to the string builder
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Check for end-of-file tag
                if (state.sb.ToString().IndexOf("<EOF>") > -1)
                {
                    // We've got all the data so let the parent thread proceed if it's waiting for the data
                    state.receiveDone.Set();

                    // The parent thread can get the data receieved with state.sb.ToString();
                }
                else
                {
                    // Get some more data
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
        }

        // Send data to a client. Blocks thread execution
        private static void Send(StateObject state, String data)
        {
            // Get the listener socket
            Socket handler = state.workSocket;

            // Convert the string to send into byte data using ASCII encoding
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Reset the MRE so we pause until sending is completed
            state.sendDone.Reset();

            // Begin sending the data to the client
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), state);

            // Wait for the data to be sent
            state.sendDone.WaitOne();
        }

        // Callback to handle sending data to the client
        private static void SendCallback(IAsyncResult ar)
        {
            // Retrieve the state object and handler socket
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            try
            {
                // Complete sending the data to the client
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            // We've sent the data so let the parent thread proceed
            state.sendDone.Set();
        }
    }
}