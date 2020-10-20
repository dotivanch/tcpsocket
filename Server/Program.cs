using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;


public class SocketListener
{

    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public static int Main(String[] args) {
        StartServer();
        return 0;
    }

    public static void StartServer() {
        IPHostEntry host = Dns.GetHostEntry("localhost");
        IPAddress ipAddress = host.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        try {
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);

            while(true) {
                allDone.Reset();
                Console.WriteLine("Waiting for a connection...");

                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                allDone.WaitOne();
            }
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    public static void AcceptCallback(IAsyncResult ar) {
        allDone.Set();

        Socket listener = (Socket) ar.AsyncState;
        Socket socket = listener.EndAccept(ar);
        Console.WriteLine("Connection made");

        State state = new State();
        state.socket = socket;
        socket.BeginReceive(state.buffer, 0, State.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
    }

    private static void ReadCallback(IAsyncResult ar) {
        String data = "";
        State state = (State) ar.AsyncState;
        Socket socket = state.socket;

        try {
            int bytesRead = socket.EndReceive(ar);

            if (bytesRead > 0) {
                data += Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                if (data.IndexOf("<EOS>") > -1) {
                    data = data.Remove(data.IndexOf("<EOS>"), "<EOS>".Length);
                    Console.WriteLine("=> Received: {0}", data);

                    int index = Utils.getIndexFromPing(data);

                    Send(socket, "pong;"+index+";#<EOS>");
                } else {
                    socket.BeginReceive(state.buffer, 0, State.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                }
            }

            socket.BeginReceive(state.buffer, 0, State.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
        }
        catch (Exception e)
        {
            Console.WriteLine("Connection closed!");
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
    private static void Send(Socket handler, String data) {
        byte[] byteData = Encoding.ASCII.GetBytes(data);
        handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
    }

    private static void SendCallback(IAsyncResult ar) {
        try {
            Socket handler = (Socket) ar.AsyncState;
            int bytesSent = handler.EndSend(ar);
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }
}
