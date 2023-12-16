using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SCREW.Voice.Server
{
    public class ClientActivityTracker
    {
        private readonly Dictionary<IPEndPoint, DateTime> clientActivity;
        private readonly TimeSpan timeoutDuration;

        public ClientActivityTracker(TimeSpan timeout)
        {
            clientActivity = new Dictionary<IPEndPoint, DateTime>();
            timeoutDuration = timeout;
        }

        public void UpdateActivity(IPEndPoint clientEndPoint)
        {
            clientActivity[clientEndPoint] = DateTime.Now;
        }

        public List<IPEndPoint> GetInactiveClients()
        {
            var inactiveClients = clientActivity.Where(pair => (DateTime.Now - pair.Value) > timeoutDuration)
                                                .Select(pair => pair.Key)
                                                .ToList();

            foreach (var client in inactiveClients)
            {
                clientActivity.Remove(client);
            }

            return inactiveClients;
        }
    }

    public class Server
    {
        private UdpClient udpServer;
        private Dictionary<IPEndPoint, string> clientUids;
        private readonly ClientActivityTracker activityTracker;

        public event Action<string, IPEndPoint> ClientConnected;
        public event Action<string, IPEndPoint> ClientDisconnected;

        public Server(TimeSpan timeout, int port)
        {
            udpServer = new UdpClient(port);
            clientUids = new Dictionary<IPEndPoint, string>();
            activityTracker = new ClientActivityTracker(timeout);
        }

        public void Start()
        {
            Console.WriteLine("[Server] SCREW Voice: Server started.");
            Console.WriteLine("[Server] SCREW Voice: Waiting for clients...");
            while (true)
            {
                try
                {
                    IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpServer.Receive(ref senderEndPoint);

                    if (!clientUids.ContainsKey(senderEndPoint))
                    {
                        string uid = System.Text.Encoding.Default.GetString(receivedBytes);
                        clientUids.Add(senderEndPoint, uid);

                        ClientConnected?.Invoke(uid, senderEndPoint);
                    }

                    activityTracker.UpdateActivity(senderEndPoint);

                    foreach (var client in clientUids.Keys.ToList())
                    {
                        if (client.Equals(senderEndPoint)) continue;

                        try
                        {
                            udpServer.Send(receivedBytes, receivedBytes.Length, client);
                        }
                        catch (SocketException)
                        {
                            ClientDisconnected?.Invoke(clientUids[client], client);
                            clientUids.Remove(client);
                        }
                    }

                    var inactiveClients = activityTracker.GetInactiveClients();
                    foreach (var client in inactiveClients)
                    {
                        ClientDisconnected?.Invoke(clientUids[client], client);
                        clientUids.Remove(client);
                    }
                }
                catch { }
            }
        }
    }
}
