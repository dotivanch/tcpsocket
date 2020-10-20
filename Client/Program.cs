using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;


public class SocketClient {
    private static ManualResetEvent connectDone = new ManualResetEvent(false);
    private static ManualResetEvent sendDone = new ManualResetEvent(false);
    private static ManualResetEvent receiveDone = new ManualResetEvent(false);

    public static int Main(String[] args) {
        StartClient();
        return 0;
    }

    public static void StartClient() {
        try {
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try {
                socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), socket);
                connectDone.WaitOne();
                Receive(socket);

                for (int i = 0; i < 500; i++) {
                    Send(socket, "ping;" + i + ";#<EOS>");
                    sendDone.WaitOne();
                    Thread.Sleep(1);
                }

                receiveDone.WaitOne();

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();

            } catch (ArgumentNullException e) {
                Console.WriteLine("ArgumentNullException: {0}", e.ToString());
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e.ToString());
            } catch (Exception e) {
                Console.WriteLine("Unexpected exception: {0}", e.ToString());
            }

        } catch (Exception e) {
            Console.WriteLine("Outer exception!");
            Console.WriteLine(e.ToString());
        }
    }

    private static void ConnectCallback(IAsyncResult ar) {
        Socket socket = (Socket) ar.AsyncState;
        socket.EndConnect(ar);

        Console.WriteLine("Connection made with {0}", socket.RemoteEndPoint.ToString());

        connectDone.Set();
    }

    private static void Receive(Socket client) {
        try {
            State state = new State();
            state.socket = client;
            client.BeginReceive(state.buffer, 0, State.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private static void ReceiveCallback(IAsyncResult ar) {
        try {
            State state = (State) ar.AsyncState;
            Socket client = state.socket;
            String data = "";

            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0) {
                data += Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                client.BeginReceive(state.buffer, 0, State.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
            }

            if (data.Length > 1) {
                data = data.Remove(data.IndexOf("<EOS>"), "<EOS>".Length);
                Console.WriteLine("=> Received: {0}", data);
            }
            receiveDone.Set();

        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private static void Send(Socket client, String data) {
        byte[] byteData = Encoding.ASCII.GetBytes(data);
        client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
    }

    private static void SendCallback(IAsyncResult ar) {
        try {
            Socket client = (Socket) ar.AsyncState;
            int bytesSent = client.EndSend(ar);
            sendDone.Set();
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

}