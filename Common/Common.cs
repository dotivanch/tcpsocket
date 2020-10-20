using System;
using System.Net.Sockets;

namespace Common
{
    public class State
    {
        public const int BUFFER_SIZE = 1024;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public Socket socket = null;
    }

    public class Utils {

        private const int PING_INDEX_ID = 1;

        public static int getIndexFromPing(string data) {
            return Convert.ToInt32(data.Split(';')[PING_INDEX_ID]);
        }

    }
}
