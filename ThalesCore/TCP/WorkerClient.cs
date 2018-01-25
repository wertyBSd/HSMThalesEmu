using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.TCP
{
    public class WorkerClient
    {
        private TcpClient MyClient;
        private const int PacketSize = 4096;
        private byte[] ReceiveData = new byte[PacketSize];
        private int m_ClientNum;
        private int m_len = -1;
        private byte[] recBytes = new byte[65536];
        private int recBytesOffset = 0;
        private bool connected = false;

        public delegate void DisconnectedMethod(WorkerClient sender);

        public event DisconnectedMethod Disconnected;

        public delegate void MessageArrivedMethod(WorkerClient sender, byte[] b, int len);

        public event MessageArrivedMethod MessageArrived;

        public bool IsConnected
        {
            get { return connected; }
        }

        public WorkerClient(TcpClient MyClient)
        {
            this.MyClient = MyClient;
        }

        public void InitOps()
        {
            connected = true;
            MyClient.GetStream().BeginRead(ReceiveData, 0, PacketSize, StreamReceive, null);
        }

        public void TermClient()
        {
            try
            {
                connected = false;
                MyClient.GetStream().Close();
                MyClient.Close();
            }
            catch (Exception ex)
            { }
        }

        public string ClientIP()
        {
            return MyClient.Client.RemoteEndPoint.ToString();
        }

        private void StreamReceive(IAsyncResult ar)
        {
            int ByteCount;

            try
            {
                lock (MyClient.GetStream())
                {
                    ByteCount = MyClient.GetStream().EndRead(ar);
                }

                if (ByteCount < 1)
                {
                    connected = false;
                    Disconnected(this);
                }

                MessageAssembler(ReceiveData, 0, ByteCount);

                lock (MyClient.GetStream())
                {
                    MyClient.GetStream().BeginRead(ReceiveData, 0, PacketSize, StreamReceive, null);
                }
            }
            catch (Exception ex)
            {
                connected = false;
                Disconnected(this);
            }
        }

        private void MessageAssembler(byte[] Bytes, int offset, int count)
        {
            int len = -1;
            int recBytesOffset = 0;
            byte[] recBytes = new byte[count];

            int ByteCount = 0;

            while (ByteCount != count)
            {
                if (len == -1)
                {
                    if (count - ByteCount < 2)
                    {
                        break;
                    }
                    len = Bytes[ByteCount] * 256 + Bytes[ByteCount + 1];
                    ByteCount += 2;
                    recBytes = new byte[len];
                }

                if (len == 0)
                {
                    MessageArrived(this, recBytes, 0);
                    recBytes = null;
                    len = -1;
                    recBytesOffset = 0;
                }
                else
                {
                    for (int i = ByteCount; i < count; i++)
                    {
                        recBytes[recBytesOffset] = Bytes[i];
                        recBytesOffset += 1;
                        ByteCount += 1;
                        if (recBytesOffset == len)
                        {
                            if (IsEBCDICEnabled())
                                recBytes = System.Text.Encoding.Convert(System.Text.Encoding.GetEncoding(37), System.Text.Encoding.ASCII, recBytes);
                            MessageArrived(this, recBytes, recBytesOffset);

                            recBytes = null;
                            len = -1;
                            recBytesOffset = 0;
                        }
                    }
                }
            }
        }

        public void send(string sendData)
        {
            byte[] Buffer;
            if (IsEBCDICEnabled())
                Buffer = Utility.GetBytesFromString("  " + sendData, System.Text.Encoding.GetEncoding(37));
            else
                Buffer = Utility.GetBytesFromString("  " + sendData);

            Buffer[0] = Convert.ToByte((int)(sendData.Length / 256));
            Buffer[1] = Convert.ToByte(sendData.Length % 256);

            lock (MyClient.GetStream())
            {
                MyClient.GetStream().BeginWrite(Buffer, 0, Buffer.Length, null, null);
            }
        }

        private void send(byte[] buffer)
        {
            byte[] b = new byte[buffer.GetLength(0) + 2];

            b[0] = Convert.ToByte((int)(buffer.GetLength(0) / 256));
            b[1] = Convert.ToByte(buffer.GetLength(0) % 256);
            Array.Copy(buffer, 0, b, 2, buffer.GetLength(0));

            lock (MyClient.GetStream())
            {
                MyClient.GetStream().BeginWrite(b, 0, b.Length, null, null);
                b = null;
            }
        }

        private bool IsEBCDICEnabled()
        {
            try
            {
                return Convert.ToBoolean(Resources.GetResource(Resources.EBCDIC));
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
