using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
}

// Functions as an async socket listener
// Use with Server.Start()
public static class Server
{
    // MREs for signalling when threads may proceed
    private static ManualResetEvent connectionDone = new ManualResetEvent(false);
    private static ManualResetEvent receiveDone = new ManualResetEvent(false);
    private static ManualResetEvent sendDone = new ManualResetEvent(false);

    private const int ListenPort = 8182;
    private const int MaxClients = 100;

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
        StateObject state = new StateObject();
        state.workSocket = handler;

        // Connection all established now. Perform handshake to get client established
        ChatHandshake(state);

        // Keep listening for new messages from the client
        while (true)
        {
            string clientMessage = Receive(state);
            Console.WriteLine("[INFO] Client says:" + clientMessage);
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

        // Client responds with "IDENTIFICATION: dummy_name<EOF>"
        // We parse this string and store it in the client's state object
        state.userName = ((((clientIDResponse.Split(":"))[1]).Split("<"))[0]).Trim();

        Console.WriteLine("[INFO] Client username is " + state.userName);

        // TODO: IMPLEMENT PUB KEY CHECK
        bool pubKeyPresent = false;

        if (!pubKeyPresent)
        {
            Console.WriteLine("[INFO] Sending key request to client");

            // This is a new client. We haven't got a public key for it
            // Ask the client to generate a key pair and send us the public key
            Send(handler, "KEY_GEN_REQUEST: <EOF>");

            // Wait for pub key
            string pubKeyResponse = Receive(state);

            // Client responds with "PUBKEY: dummy_key<EOF>"
            // We parse this string and store it in the client's state object
            state.pubKey = ((((pubKeyResponse.Split(":"))[1]).Split("<"))[0]).Trim();

            // TODO: Add pub key to database
        }
        else
        {
            // We've seen this client before

            Console.WriteLine("[INFO] Retrieving public key for client from database");

            // TODO: Retrieve the pub key from the database and store it in the state object
            state.pubKey = "key_from_database";
        }

        Console.WriteLine("[INFO] Public key for client is " + state.pubKey);

        Console.WriteLine("[INFO] Sending new messages to client");

        // TODO: Fetch and send new messages for this user from the database
        Send(handler, "MESSAGES: Some juicy messages<EOF>");
    }

    // Receive data from a client. Blocks parent thread execution
    private static string Receive(StateObject state)
    {
        // Get the listener socket
        Socket handler = state.workSocket;

        // Reset the MRE so we pause until the client responds
        receiveDone.Reset();

        // Set up a callback for when the client begins sending data
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

        // Wait for the client to be received
        receiveDone.WaitOne();

        // Get the received data
        string data = state.sb.ToString();

        // Reset the client's buffer so we can receive more data from it
        state.sb = new StringBuilder();
        state.buffer = new byte[StateObject.BufferSize];

        return data;
    }

    // Callback to handle receiving data from client
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
                receiveDone.Set();

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
    private static void Send(Socket handler, String data)
    {
        // Convert the string to send into byte data using ASCII encoding
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Reset the MRE so we pause until sending is completed
        sendDone.Reset();

        // Begin sending the data to the client
        handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);

        // Wait for the data to be sent
        sendDone.WaitOne();
    }

    // Callback to handle sending data to the client
    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the handler socket 
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the client
            int bytesSent = handler.EndSend(ar);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        // We've sent the data so let the parent thread proceed
        sendDone.Set();
    }
}