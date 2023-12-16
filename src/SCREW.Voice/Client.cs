using System;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;

namespace SCREW.Voice.Client
{
    public class ChatSettings
    {
        public string ipAddress { get; set; }
        public int port { get; set; }
        public string uid { get; set; }
        public DeviceSettings deviceSettings { get; set; }
    }

    public class DeviceSettings
    {
        private int _bufferMilliseconds;

        private int BufferMilliseconds
        {
            get { return _bufferMilliseconds; }
            set
            {
                if (value > 512)
                {
                    _bufferMilliseconds = 512;
                }
                else if (value < 16)
                {
                    _bufferMilliseconds = 16;
                }
                else
                {
                    _bufferMilliseconds = value;
                }
            }
        }

        public int GetBufferMilliseconds()
        {
            return _bufferMilliseconds;
        }

        public void SetBufferMilliseconds(int value)
        {
            BufferMilliseconds = value;
        }
    }
    public class Client
    {
        private UdpClient udpClient;
        private WaveInEvent waveIn;
        private WaveOut waveOut;
        private BufferedWaveProvider bufferedWaveProvider;
        private IPEndPoint serverReceiveEndPoint;

        public Client(ChatSettings chatSettings)
        {
            try
            {
                udpClient = new UdpClient();
                ConnectToServer(chatSettings.ipAddress, chatSettings.port);

                waveIn = new WaveInEvent();
                waveIn.BufferMilliseconds = chatSettings.deviceSettings.GetBufferMilliseconds();
                waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
                waveIn.DataAvailable += OnDataAvailable;

                waveOut = new WaveOut();
                bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
                waveOut.Init(bufferedWaveProvider);
                waveOut.Play();

                byte[] usernameBytes = System.Text.Encoding.Default.GetBytes(chatSettings.uid);
                udpClient.Send(usernameBytes, usernameBytes.Length);

                waveIn.StartRecording();
                StartListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] SCREW Voice: {ex.Message}");
            }
        }

        private void ConnectToServer(string ipAddress, int port)
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(ipAddress);
                bool connected = false;

                foreach (IPAddress address in addresses)
                {
                    try
                    {
                        serverReceiveEndPoint = new IPEndPoint(address, port);
                        udpClient.Connect(serverReceiveEndPoint);
                        connected = true;
                        Console.WriteLine($"Connected to {address}");
                        break;
                    }
                    catch
                    {
                        Console.WriteLine($"[Error] SCREW Voice: Failed to connect to {address}. Trying next address...");
                    }
                }

                if (!connected)
                {
                    Console.WriteLine("[Error] SCREW Voice: Failed to connect to the server.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] SCREW Voice: {ex.Message}");
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                udpClient.Send(e.Buffer, e.BytesRecorded);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] SCREW Voice: {ex.Message}");
            }
        }

        private void StartListening()
        {
            try
            {
                udpClient.BeginReceive(OnVoiceReceived, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] SCREW Voice: {ex.Message}");
            }
        }

        private void OnVoiceReceived(IAsyncResult result)
        {
            try
            {
                IPEndPoint serverReceiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpClient.EndReceive(result, ref serverReceiveEndPoint);

                if (receivedBytes.Length > 0 && waveOut != null)
                {
                    bufferedWaveProvider.AddSamples(receivedBytes, 0, receivedBytes.Length);
                }

                udpClient.BeginReceive(OnVoiceReceived, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] SCREW Voice: {ex.Message}");
            }
        }

        public int GetPing(int timeoutInMilliseconds)
        {
            try
            {
                udpClient.Client.ReceiveTimeout = timeoutInMilliseconds;

                DateTime startTime = DateTime.Now;
                byte[] pingMessage = new byte[1];
                udpClient.Send(pingMessage, pingMessage.Length);

                bool receivedResponse = udpClient.Client.Poll(timeoutInMilliseconds * 1000, SelectMode.SelectRead);

                if (receivedResponse)
                {
                    udpClient.Receive(ref serverReceiveEndPoint);
                    TimeSpan pingTime = DateTime.Now - startTime;
                    return (int)pingTime.TotalMilliseconds;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] SCREW Voice: {ex.Message}");
                return -1;
            }
        }
    }
}
