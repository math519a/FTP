using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SimpleFTP.UserInterpreter;

namespace SimpleFTP
{
    public class FTPClient
    {
        public int DATA_PORT
        {
            get
            {
                return _DATA_PORT;
            }
            set
            {
                _DATA_PORT = clamp(value,1025,64000);

            }
        }
        private int _DATA_PORT = 1025;

        public const int CONTROL_PORT  = 21;
        
        public event EventHandler OnConnected;
        public event EventHandler<DataEventArgs> OnDataRecieved;
        public event EventHandler<DataChunkArgs> DataChunkUpdated;

        private Socket controlConnectionSocket;         // P21    - Control Port ------- SENDS COMMAND TO FTP
        private Socket dataConnectionSocket;            // P1025+ - File Stream Port ------- ONLY TRANSFERS DATA (UPLOAD/DOWNLOAD)

        private Dictionary<string, List<byte>> buffers;
        private object mutex = new object();

        public FTPClient()
        {
            buffers = new Dictionary<string, List<byte>>();

            OnDataRecieved += (s, e) => {
                // Invokes the AwaitResponse()
                switch (e.Name) {
                    case "CONTROL":
                        tcsControlResponse?.TrySetResult(e);
                        break;

                    case "DATA":
                        tcsDataResponse?.TrySetResult(e);
                        break;
                }
            };
        }

        public void Connect(string Host)
        {
            controlConnectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            createThread("CONTROL", CONTROL_PORT, controlConnectionSocket);
     
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(Host), CONTROL_PORT);
            print("Connecting to remote end point {0}", remoteEndPoint);
            controlConnectionSocket.Connect(remoteEndPoint);

            print("Connected successfully.");
            OnConnected?.Invoke(null, EventArgs.Empty);
        }

        public async Task ConnectPassiveDataSocket()
        {
            lock (mutex)
            {
                dataConnectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                dataConnectionSocket.Bind(new IPEndPoint(IPAddress.Any, DATA_PORT));
            }

            SendCommand(FTPCommand.SetPortPassive, DATA_PORT++);

        reawait:
            DataEventArgs passiveResponse = await AwaitResponse();

            /*
                The values h1 to h4 are the IP addresses that the server is listening on. 
                The values p1 to p2 are used to calculate the port that the server is listening on using the following formula: 

                PASV port = (p1 * 256) + p2 

                Example:
                    227 Entering Passive Mode (h1, h2, h3, h4, p1, p2)
            */

            if (passiveResponse.FtpResponseCode == 227)
            {
                // h1, h2, h3, h4, p1, p2
                var responseDataMsg = passiveResponse.Message.Split('(')[1].Split(')')[0];
                var responseDataParts = responseDataMsg.Split(',');

                string hostIP = string.Format("{0}.{1}.{2}.{3}", responseDataParts[0], responseDataParts[1], responseDataParts[2], responseDataParts[3]);

                int p1 = int.Parse(responseDataParts[4]);
                int p2 = int.Parse(responseDataParts[5]);

                int port = (p1 * 256) + p2;

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(hostIP), port);
                dataConnectionSocket.Connect(remoteEndPoint);

                new Thread(() =>
                { // Reads every chunk of data until connection is closed from server.
                    while (SocketConnected(dataConnectionSocket))
                    {
                        while (dataConnectionSocket.Available > 0)
                        {
                            byte[] chunkBuffer = new byte[dataConnectionSocket.Available];
                            dataConnectionSocket.Receive(chunkBuffer);
                            DataChunkUpdated?.Invoke(null, new DataChunkArgs(chunkBuffer, false));
                        }
                    }

                    DataChunkUpdated?.Invoke(null, new DataChunkArgs(null, true));

                    lock (mutex)
                    {
                        dataConnectionSocket.Close();
                        dataConnectionSocket = null;
                    }
                }).Start();
            }
            else goto reawait;
        }

        public async Task WaitForDataConnectionClose()
        {
            while (true)
            {
                await Task.Delay(5);

                lock (mutex)
                {
                    if (dataConnectionSocket == null || !SocketConnected(dataConnectionSocket))
                    {
                        break;
                    }
                }

            }
        }

        public void SendCommand(FTPCommand command, params object[] args)
        {
            string cmd = InterpretCommand(command, args);
            print(">> " + cmd);
            controlConnectionSocket.Send(ToASCII(cmd));
        }

        public void SendCommand(FTPCommand command, bool shouldPrint, params object[] args)
        {
            string cmd = InterpretCommand(command, args);
            if (shouldPrint) print(">> " + cmd);
            controlConnectionSocket.Send(ToASCII(cmd));
        }

        public void SendBufferOnData(byte[] Buffer)
        {
            lock (mutex)
            {
                if (SocketConnected(dataConnectionSocket))
                {
                    dataConnectionSocket.Send(Buffer);
                }
            }
        }

        public async void Disconnect()
        {
            SendCommand(FTPCommand.Disconnect);
            await AwaitResponse();

            lock (mutex)
            {
                controlConnectionSocket?.Close();
                dataConnectionSocket?.Close();
            }
        }

        TaskCompletionSource<DataEventArgs> tcsControlResponse = null;
        public async Task<DataEventArgs> AwaitResponse()
        {
            tcsControlResponse = new TaskCompletionSource<DataEventArgs>();
            var args = await tcsControlResponse.Task;
            return args;
        }

        TaskCompletionSource<DataEventArgs> tcsDataResponse = null;
        public async Task<DataEventArgs> AwaitDataResponse()
        {
            tcsDataResponse = new TaskCompletionSource<DataEventArgs>();
            var args = await tcsDataResponse.Task;
            return args;
        }

        private void print(string text, params object[] args)
        {
            Console.WriteLine(text, args);
        }
        private void createThread(string Name, int Port, Socket socket, bool silent = false)
        {
            new Thread(() => {
                print("[Thread] Listening for incoming data on port {0}", Port);
                
                while (true)
                {
                    try
                    {
                        Thread.Sleep(25);

                        lock (mutex)
                        {
                            if (socket.Available > 0)
                            {
                                byte[] buffer = new byte[socket.Available];
                                socket.Receive(buffer);
                                //print("[{0}] Recieved\n: {1}", Name,Encoding.ASCII.GetString(buffer));
                                string decodedBuffer = Encoding.ASCII.GetString(buffer);
                                int ftpResponseId = 0;
                                int.TryParse(decodedBuffer.Substring(0, 3), out ftpResponseId);

                                OnDataRecieved?.Invoke(socket, new DataEventArgs(Name, ftpResponseId, decodedBuffer));
                            }
                        }
                    } catch (Exception)
                    {
                        break;
                    }
                }
            }).Start();
        }

        private int clamp(int value, int min, int max)
        {
            if (value < min) { return clamp(value + 1, min, max); }
            else if (value > max) { return clamp(value - 1, min, max); }
            else return value;
        }

        private bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }
    }
}
