using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace MSG_SERVER
{
    /// <summary>
    /// A class for creating a chat server over TCP 
    /// </summary>
    public class MsgServer
    {
        #region Variables
        public Boolean Active { get; private set; }
        private bool AllowingConnections = false;
        private bool ConnectingAborted = false;
        private bool AllowingMessages = false;

        private TcpListener listener;
        public List<Client> Clients { get; private set; } = new List<Client>();
        public List<IPAddress> BlackIP = new List<IPAddress>();

        private StreamWriter globalWriter;
        private StreamReader globalReader;

        private Thread connectionWatcher;
        private Thread messageWatcher;

        public int Port { get; private set; }
        public int ClientTimeout { get; private set; }
        public IPAddress ServerIP { get { return IPAddress.Parse(Misc.RemoteIP()); } }
        public Boolean EncryptTraffic { get; private set; }
        public DateTime StartedServerTime { get; private set; }
        public TimeSpan CurrentServerTime { get { return DateTime.Now.Subtract(StartedServerTime); } }
        private SecureString Password;
        public Boolean SuppressPM { get; set; } = true;
        #endregion

        #region ServerEvents
        public delegate void ChatUpdatedEventHandler(String _message, Client _cli);
        public event ChatUpdatedEventHandler MessageReceived;

        public delegate void NameChangedEventHandler(String _oldName, Client _cli);
        public event NameChangedEventHandler NameChanged;

        public delegate void NewClientEventHandler(Client _cli);
        public event NewClientEventHandler NewClient;

        public delegate void ClientDisconnectedEventHandler(Client _cli);
        public event ClientDisconnectedEventHandler ClientDisconnected;

        public delegate void CustomCommandReceived(String _comm);
        public event CustomCommandReceived CustomCommand;
        #endregion

        public MsgServer(int _port, SecureString _pass)
        {
            Port = _port;
            EncryptTraffic = true;
            Password = _pass;
        }
        public MsgServer(int _port, SecureString _pass, IPAddress[] _blacklist)
        {
            Port = _port;
            EncryptTraffic = true;
            BlackIP = _blacklist.ToList();
            Password = _pass;
        }

        /// <summary>
        /// Starts listening on the specified port for incoming traffic from clients
        /// </summary>
        public void Bind()
        {
            if (Misc.HasConnection())
            {
                AllowingConnections = true;
                AllowingMessages = true;
                connectionWatcher = new Thread(() => watchForConnections());
                connectionWatcher.Start();
                Active = true;
            }
            else
            {
                throw new NoInternet(MachineType.Server);
            }
            MessageReceived("=> Server started successfully at: " + DateTime.Now.ToString("HH:mm:ss"), null);
        }
        /// <summary>
        /// Stops listening for incoming traffic and resets variables
        /// </summary>
        public void UnBind()
        {
            RelayMessage("SERVER_CLOSING");
            Thread.Sleep(1250);
            try
            {
                if (AllowingConnections == true)
                {
                    AllowingConnections = false;
                }
            }
            catch { }
            try
            {
                if (AllowingMessages == true)
                {
                    AllowingMessages = false;
                }
            }
            catch { }
            try
            {
                if (connectionWatcher.ThreadState == ThreadState.Running)
                {
                    connectionWatcher.Interrupt();
                    while (ConnectingAborted == false)
                        Thread.Sleep(50);
                }
            }
            catch { }
            Active = false;
        }

        /// <summary>
        /// Stops allowing new connections, but will still function as a chat server for clients already connected
        /// </summary>
        public void StopConnections()
        {
            if (AllowingConnections == true)
            {
                AllowingConnections = false;
                Thread.Sleep(50);
                while (ConnectingAborted == false)
                    Thread.Sleep(50);
            }
        }
        /// <summary>
        /// Startes watching for incoming connections if previously stopped
        /// </summary>
        public void StartConnections()
        {
            if (AllowingConnections == false && ConnectingAborted == true)
            {
                AllowingConnections = true;
                connectionWatcher = new Thread(() => watchForConnections());
                connectionWatcher.Start();
            }
        }

        /// <summary>
        /// Bans the specified client from the server and does not allow re-connection
        /// </summary>
        /// <param name="_cli">The client to ban</param>
        public void Ban(Client _cli)
        {
            NDNSW stream = new NDNSW(_cli.Connection.GetStream());
            StreamWriter writeMsg = new StreamWriter(stream);
            if (EncryptTraffic)
            {
                writeMsg.WriteLine(("SERVER_BAN").TripleDES_Encrypt(Password, false));
                writeMsg.Flush();
            }
            else
            {
                writeMsg.WriteLine("SERVER_BAN");
                writeMsg.Flush();
            }
            writeMsg.Dispose();
            RelayMessage("SERVER_CLIENT_BAN_" + _cli.IP.ToString() + "_" + DateTime.Now.ToString("HH:mm:ss"));
        }
        /// <summary>
        /// Writes a custom command to the MSG stream
        /// </summary>
        /// <param name="_comm">The custom command and attributes</param>
        public void Custom(string _comm)
        {
            RelayMessage("SERVER_CUSTOM_" + _comm);
        }
        /// <summary>
        /// Kicks the specified client from the server but allows them to re-connect
        /// </summary>
        /// <param name="_cli">The client to kick</param>
        public void Kick(Client _cli)
        {
            NDNSW stream = new NDNSW(_cli.Connection.GetStream());
            StreamWriter writeMsg = new StreamWriter(stream);
            if (EncryptTraffic)
            {
                writeMsg.WriteLine(("SERVER_KICK").TripleDES_Encrypt(Password, false));
                writeMsg.Flush();
            }
            else
            {
                writeMsg.WriteLine("SERVER_KICK");
                writeMsg.Flush();
            }
        }
        /// <summary>
        /// Sends a personal message to the specified client
        /// </summary>
        /// <param name="_message">Message to send</param>
        /// <param name="_target">Client to send personal message to</param>
        public void PM(String _message, Client _target)
        {
            globalWriter = new StreamWriter(new NDNSW(_target.Connection.GetStream()));
            if (EncryptTraffic)
            {
                globalWriter.WriteLine(("CLIENT_PM_" + _target.IP + "_" + _message).TripleDES_Encrypt(Password, false));
                globalWriter.Flush();
            }
            else
            {
                globalWriter.WriteLine("CLIENT_PM_" + _target.IP + "_" + _message);
                globalWriter.Flush();
            }
        }
        /// <summary>
        /// Sends a ping message to the specified client and returns true if you're connected.
        /// </summary>
        /// <param name="_cli">Client to ping</param>
        /// <param name="_latency">How long the ping took to complete (if completed)</param>
        /// <returns></returns>
        public Boolean Ping(Client _cli, out string _latency)
        {
            if (_cli != null)
            {
                NDNSW strm = new NDNSW(_cli.Connection.GetStream());
                globalReader = new StreamReader(strm);
                globalWriter = new StreamWriter(strm);
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                string pong = string.Empty;
                if (EncryptTraffic)
                {
                    globalWriter.WriteLine(("SERVER_PING").TripleDES_Encrypt(Password, false));
                    globalWriter.Flush();
                    pong = globalReader.ReadLine().TripleDES_Decrypt(Password, false);
                }
                else
                {
                    globalWriter.WriteLine("SERVER_PING");
                    globalWriter.Flush();
                    pong = globalReader.ReadLine();
                }
                if (pong.StartsWith("CLIENT_PONG"))
                {
                    timer.Stop();
                    _latency = timer.Elapsed.ToString("ffffff");
                    return true;
                }
                else
                {
                    timer.Stop();
                    _latency = "n/a";
                    return false;
                }
            }
            else
                throw new MsgException("The client to ping cannot be null.");
        }
        /// <summary>
        /// Sends a message from the server to all clients
        /// </summary>
        /// <param name="_message">The message to send</param>
        public void Write(String _message)
        {
            if (AllowingMessages == true)
            {
                RelayMessage("Server: " + _message);
            }
        }

        private void watchForConnections()
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            TcpClient client = new TcpClient();
            ConnectingAborted = false;
            try
            {
                do
                {
                    if (!listener.Pending())
                    {
                        Thread.Sleep(500);
                        if (AllowingConnections)
                            continue;
                        else
                            break;
                    }
                    client = listener.AcceptTcpClient();
                    StreamReader reader = new StreamReader(new NDNSW(client.GetStream()));
                    StreamWriter writeMsg = new StreamWriter(new NDNSW(client.GetStream()));
                    string clientInfo = reader.ReadLine();
                    string tempIP = clientInfo.Replace("CLIENT_NEW_", "").Substring(0, clientInfo.Replace("CLIENT_NEW_", "").IndexOf("_"));
                    string tempName = clientInfo.Replace("CLIENT_NEW_" + tempIP + "_", "").Substring(0, clientInfo.Replace("CLIENT_NEW_" + tempIP + "_", "").IndexOf("_"));
                    DateTime tempConnected = DateTime.Parse((clientInfo.Replace("CLIENT_NEW_" + tempIP + "_" + tempName + "_", "").Substring(0, clientInfo.Replace("CLIENT_NEW_" + tempIP + "_" + tempName + "_", "").Length)));
                    writeMsg.WriteLine("SERVER_STARTED_" + ServerIP + "_" + StartedServerTime.ToString("HH:mm:ss"));
                    writeMsg.Flush();
                    Client c = new Client(tempIP, tempName, tempConnected, (TcpClient)Clone(client));
                    if (Clients.Contains(c) == false && BlackIP.Contains(c.IP) == false)
                    {
                        messageWatcher = new Thread(() => handleMessages(c.Connection));
                        messageWatcher.Start();
                        c.State = ClientState.Connected;
                        Clients.Add(c);
                        RelayMessage("SERVER_CLIENT_NEW_" + c.IP + "_" + c.Name + "_" + c.ConnectedAt.ToString());
                    }
                    else
                    {
                        c.State = ClientState.Banned;
                        if (EncryptTraffic)
                        {
                            writeMsg.WriteLine(("SERVER_CLIENT_BAN_" + c.IP.ToString() + "_" + DateTime.Now.ToString("HH:mm:ss")).TripleDES_Encrypt(Password, false));
                            writeMsg.Flush();
                        }
                        else
                        {
                            writeMsg.WriteLine("SERVER_CLIENT_BAN_" + c.IP.ToString() + "_" + DateTime.Now.ToString("HH:mm:ss"));
                            writeMsg.Flush();
                        }
                        writeMsg.Dispose();
                    }
                    reader.Dispose();
                } while (AllowingConnections == true);
            }
            catch (ThreadAbortException) { }
            catch (ThreadInterruptedException) { }
            ConnectingAborted = true;
            listener.Stop();
        }

        private void handleMessages(TcpClient _client)
        {
            NDNSW stream = new NDNSW(_client.GetStream());
            StreamReader readMsg = new StreamReader(stream);
            StreamWriter writeMsg = new StreamWriter(stream);
            do
            {
                if (!stream.DataAvailable())
                {
                    Thread.Sleep(500);
                    continue;
                }
                string message = readMsg.ReadLine();
                if (message != null)
                {
                    if (EncryptTraffic == true)
                    {
                        message = message.TripleDES_Decrypt(Password, false);
                    }
                    if (message.StartsWith("CLIENT_"))
                    {
                        string finalMsg = message.Replace("CLIENT_", "");
                        if (finalMsg.StartsWith("DISCONNECT_"))
                        {
                            string cliIP = finalMsg.Replace("DISCONNECT_", "").Substring(0, finalMsg.Replace("DISCONNECT_", "").IndexOf("_"));
                            string cliTime = finalMsg.Replace("DISCONNECT_" + cliIP + "_", "");
                            Client d = Clients.Find(c => c.IP.ToString() == cliIP);
                            MessageReceived("=> " + d.Name + " has disconnected at: " + cliTime + ".", d);
                            ClientDisconnected(d);
                            Clients.Remove(d);
                            goto close;
                        }
                        else if (finalMsg.StartsWith("RENAME_"))
                        {
                            string cliIP = finalMsg.Replace("RENAME_", "").Substring(0, finalMsg.Replace("RENAME_", "").IndexOf("_"));
                            string cliName = finalMsg.Replace("RENAME_" + cliIP + "_", "");
                            Client sender = Clients.Find(c => c.IP.ToString() == cliIP);
                            NameChanged(sender.Name, sender);
                            MessageReceived("=> " + sender.Name + " (" + cliIP + ") has changed their name to: " + cliName + ".", sender);
                            Clients.Remove(sender);
                            Clients.Add(new Client(sender.IP.ToString(), cliName, sender.ConnectedAt.ToString(), sender.State));
                        }
                        else if (finalMsg.StartsWith("PM_"))
                        {
                            string sender = finalMsg.Replace("PM_", "").Substring(0, finalMsg.Replace("PM_", "").IndexOf("_"));
                            string target = finalMsg.Replace("PM_" + sender + "_", "").Substring(0, finalMsg.Replace("PM_" + sender + "_", "").IndexOf("_"));
                            string pm = finalMsg.Replace("PM_" + sender + "_" + target + "_", "");
                            Client tmpC = Clients.Find(cl => cl.IP.ToString() == sender);
                            if (target != ServerIP.ToString())
                            {
                                if (!SuppressPM)
                                    MessageReceived("PM from " + Clients.Find(c => c.IP.ToString() == sender).Name + " to " + Clients.Find(c => c.IP.ToString() == target).Name + ".", tmpC);
                                globalWriter = new StreamWriter(new NDNSW(tmpC.Connection.GetStream()));
                                if (EncryptTraffic)
                                {
                                    globalWriter.WriteLine(("CLIENT_PM_" + tmpC.IP + "_" + pm).TripleDES_Encrypt(Password, false));
                                    globalWriter.Flush();
                                }
                                else
                                {
                                    globalWriter.WriteLine("CLIENT_PM_" + tmpC.IP + "_" + pm);
                                    globalWriter.Flush();
                                }
                            }
                            else
                                MessageReceived("PM from " + Clients.Find(c => c.IP.ToString() == sender).Name + ": " + pm, tmpC);
                        }
                        else if (finalMsg.StartsWith("PING"))
                        {
                            if (EncryptTraffic)
                            {
                                writeMsg.WriteLine(("SERVER_PONG").TripleDES_Encrypt(Password, false));
                                writeMsg.Flush();
                            }
                            else
                            {
                                writeMsg.WriteLine("SERVER_PONG");
                                writeMsg.Flush();
                            }
                        }
                        else if (finalMsg.StartsWith("CUSTOM_"))
                        {
                            CustomCommand(finalMsg.Replace("CUSTOM_", ""));
                        }
                    }
                    else if (message.StartsWith("CONTENT_"))
                    {
                        string content = message.Replace("CONTENT_", "");
                        if (content.StartsWith("IMAGE"))
                        {

                        }
                        else if (content.StartsWith("FILE"))
                        {

                        }
                    }
                    else { RelayMessage((String)Clone(message)); }
                }
            } while (AllowingMessages == true);
            close:
            if (EncryptTraffic == true)
            { 
                writeMsg.WriteLine(("SERVER_CLOSING").TripleDES_Encrypt(Password, false));
                writeMsg.Flush();
            }
            else
            {
                writeMsg.WriteLine("SERVER_CLOSING");
                writeMsg.Flush();
            }
            //Remove this instance from clients and addresses.
            Clients.Remove(Clients.Find(cl => cl.Connection == _client));
            try { _client.Close(); }
            catch { }
        }

        private void RelayMessage(String _message)
        {
            try
            {
                Client banCli = null;
                if (_message.StartsWith("SERVER_CLIENT_"))
                {
                    string _message2 = _message.Replace("SERVER_CLIENT_", "");
                    if (_message2.StartsWith("NEW_"))
                    {
                        string tempIP = _message2.Replace("NEW_", "").Substring(0, _message2.Replace("NEW_", "").IndexOf("_"));
                        string tempName = _message2.Replace("NEW_" + tempIP + "_", "").Substring(0, _message2.Replace("NEW_" + tempIP + "_", "").IndexOf("_"));
                        DateTime tempTime = DateTime.Parse(_message2.Replace("NEW_" + tempIP + "_" + tempName + "_", ""));
                        string _msg = "=> " + tempName + " (" + tempIP + ")  has joined the session. Time: " + tempTime.ToLongTimeString();
                        Client c = Clients.Find(d => d.IP.ToString() == tempIP);
                        NewClient(c);
                        MessageReceived(_msg, c);
                    }
                    else if (_message2.StartsWith("BAN_"))
                    {
                        string tempIP = _message2.Replace("BAN_", "").Substring(0, _message2.Replace("BAN_", "").IndexOf("_"));
                        DateTime tempTime = DateTime.Parse(_message2.Replace("BAN_" + tempIP + "_", ""));
                        Client c = Clients.Find(d => d.IP.ToString() == tempIP);
                        MessageReceived("=> " + c.Name + " has been banned. Time: " + tempTime + ".", c);
                        ClientDisconnected(c);
                        banCli = c;
                        BlackIP.Add(c.IP);
                    }
                }
                else if (_message.StartsWith("Server:"))
                {
                    MessageReceived(_message.Replace("Server", "You"), null);
                }
                else { MessageReceived(_message, null); }

                foreach (Client tempClient in Clients)
                {
                    if (tempClient.State == ClientState.Connected)
                    {
                        using (StreamWriter writer = new StreamWriter(new NDNSW(tempClient.Connection.GetStream())))
                        {
                            if (EncryptTraffic == true)
                            {
                                writer.WriteLine(_message.TripleDES_Encrypt(Password, false));
                                writer.Flush();
                            }
                            else
                            {
                                writer.WriteLine(_message);
                                writer.Flush();
                            }
                        }
                    }
                }
                if (banCli != null)
                        Clients.Remove(banCli);
            }
            catch (Exception ex) { }
        }

        public static object Clone<T>(T _itemToCopy)
        {
            return _itemToCopy;
        }
    }

    public class NDNSW : Stream
    {
        private NetworkStream wrappedStream;
        public NDNSW(NetworkStream wrappedStream) { this.wrappedStream = wrappedStream; }
        public override void Flush() { wrappedStream.Flush(); }
        public override long Seek(long offset, SeekOrigin origin) { return wrappedStream.Seek(offset, origin); }
        public override void SetLength(long value) { wrappedStream.SetLength(value); }
        public override int Read(byte[] buffer, int offset, int count) { return wrappedStream.Read(buffer, offset, count); }
        public override void Write(byte[] buffer, int offset, int count) { try { wrappedStream.Write(buffer, offset, count); } catch { } }
        public override bool CanRead { get { return wrappedStream.CanRead; } }
        public override bool CanSeek { get { return wrappedStream.CanSeek; } }
        public override bool CanWrite { get { return wrappedStream.CanWrite; } }
        public override long Length { get { return wrappedStream.Length; } }
        public override long Position { get { return wrappedStream.Position; } set { wrappedStream.Position = value; } }
        public Boolean DataAvailable() { return wrappedStream.DataAvailable; }
    }

    #region Exceptions
    class NoInternet : Exception
    {
        public String ErrorMessage { get; private set; }
        public NoInternet(MachineType _type)
        {
            switch (_type)
            {
                case MachineType.Client:
                    {
                        ErrorMessage = "WARNING: You are not connected to the internet. Please connect and try again."; break;
                    }
                case MachineType.Server:
                    {
                        ErrorMessage = "WARNING: You are not connected to the internet. Please connect and try again."; break;
                    }
            }
        }
    }
    class SendFailed : Exception
    {
        public String ErrorMessage { get; private set; }
        public SendFailed(MachineType _type, ClientState _state)
        {
            if (_type == MachineType.Client)
            {
                if (_state == ClientState.Banned)
                    ;
                else if (_state == ClientState.Muted)
                    ;
            }
            switch (_type)
            {
                case MachineType.Client:
                    {

                        ErrorMessage = ""; break;
                    }
                case MachineType.Server:
                    {
                        ErrorMessage = ""; break;
                    }
            }
        }
    }
    class NotConnected : Exception
    {
        public String ErrorMessage { get; private set; }
        public NotConnected(MachineType _type)
        {
            switch (_type)
            {
                case MachineType.Client:
                    {
                        ErrorMessage = ""; break;
                    }
                case MachineType.Server:
                    {
                        ErrorMessage = ""; break;
                    }
            }
        }
    }
    class ConnectionFailed : Exception
    {
        public String ErrorMessage { get; private set; }
        public ConnectionFailed(MachineType _type)
        {
            switch (_type)
            {
                case MachineType.Client:
                    {
                        ErrorMessage = "Failed to connect to server/peer."; break;
                    }
                case MachineType.Server:
                    {
                        ErrorMessage = "Failed to connect for an unknown reason."; break;
                    }
            }
        }
    }
    class MsgException : Exception
    {
        public string Reason { get; private set; }

        public MsgException(string _reason)
        {
            Reason = _reason;
        }
    }
    #endregion
}