/*
 *  Authors:  Benton Stark
 * 
 *  Copyright (c) 2007-2009 Starksoft, LLC (http://www.starksoft.com) 
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 */

using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Starksoft.Net.Proxy
{
    /// <summary>
    /// Socks4 connection proxy class. This class implements the Socks4 standard proxy protocol.
    /// </summary>
    /// <remarks>
    /// This class implements the Socks4 proxy protocol standard for TCP communciations.
    /// </remarks>
    public class Socks4ProxyClient : IProxyClient
    {
        static readonly int WAIT_FOR_DATA_INTERVAL = 50;   // 50 ms
        static readonly int WAIT_FOR_DATA_TIMEOUT = 15000; // 15 seconds
        static readonly string PROXY_NAME = "SOCKS4";

        /// <summary>
        /// Default Socks4 proxy port.
        /// </summary>
        internal static readonly ushort SOCKS_PROXY_DEFAULT_PORT = 1080;
        /// <summary>
        /// Socks4 version number.
        /// </summary>
        internal static readonly byte SOCKS4_VERSION_NUMBER = 4;
        /// <summary>
        /// Socks4 connection command value.
        /// </summary>
        internal static readonly byte SOCKS4_CMD_CONNECT = 0x01;
        /// <summary>
        /// Socks4 bind command value.
        /// </summary>
        internal static readonly byte SOCKS4_CMD_BIND = 0x02;
        /// <summary>
        /// Socks4 reply request grant response value.
        /// </summary>
        internal static readonly byte SOCKS4_CMD_REPLY_REQUEST_GRANTED = 90;
        /// <summary>
        /// Socks4 reply request rejected or failed response value.
        /// </summary>
        internal static readonly byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_OR_FAILED = 91;
        /// <summary>
        /// Socks4 reply request rejected becauase the proxy server can not connect to the IDENTD server value.
        /// </summary>
        internal static readonly byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_CANNOT_CONNECT_TO_IDENTD = 92;
        /// <summary>
        /// Socks4 reply request rejected because of a different IDENTD server.
        /// </summary>
        internal static readonly byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_DIFFERENT_IDENTD = 93;

        /// <summary>
        /// Gets String representing the name of the proxy. 
        /// </summary>
        /// <remarks>This property will always return the value 'SOCKS4'</remarks>
        public virtual string ProxyName => PROXY_NAME;

        /// <summary>
        /// Gets or sets host name or IP address of the proxy server.
        /// </summary>
        public string ProxyHost { get; set; }

        /// <summary>
        /// Gets or sets port used to connect to proxy server.
        /// </summary>
        public ushort ProxyPort { get; set; }

        /// <summary>
        /// Gets or sets proxy user identification information.
        /// </summary>
        public string ProxyUserId { get; set; }

        /// <summary>
        /// Gets or sets the TcpClient object. 
        /// This property can be set prior to executing CreateConnection to use an existing TcpClient connection.
        /// </summary>
        public TcpClient Client { get; set; }

        /// <summary>
        /// Create a Socks4 proxy client object. The default proxy port 1080 is used.
        /// </summary>
        public Socks4ProxyClient() { }

        /// <summary>
        /// Creates a Socks4 proxy client object using the supplied TcpClient object connection.
        /// </summary>
        /// <param name="tcpClient">A TcpClient connection object.</param>
        public Socks4ProxyClient(TcpClient tcpClient) => Client = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));

        /// <summary>
        /// Create a Socks4 proxy client object. The default proxy port 1080 is used.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyUserId">Proxy user identification information.</param>
        public Socks4ProxyClient(string proxyHost, string proxyUserId)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));
            if (proxyUserId is null)
                throw new ArgumentNullException(nameof(proxyUserId));

            ProxyHost = proxyHost;
            ProxyPort = SOCKS_PROXY_DEFAULT_PORT;
            ProxyUserId = proxyUserId;
        }

        /// <summary>
        /// Create a Socks4 proxy client object.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        /// <param name="proxyUserId">Proxy user identification information.</param>
        public Socks4ProxyClient(string proxyHost, ushort proxyPort, string proxyUserId)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));
            if (proxyUserId is null)
                throw new ArgumentNullException(nameof(proxyUserId));

            ProxyHost = proxyHost;
            ProxyPort = proxyPort;
            ProxyUserId = proxyUserId;
        }

        /// <summary>
        /// Create a Socks4 proxy client object. The default proxy port 1080 is used.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        public Socks4ProxyClient(string proxyHost)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            ProxyHost = proxyHost;
            ProxyPort = SOCKS_PROXY_DEFAULT_PORT;
        }

        /// <summary>
        /// Create a Socks4 proxy client object.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        public Socks4ProxyClient(string proxyHost, ushort proxyPort)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            ProxyHost = proxyHost;
            ProxyPort = proxyPort;
        }

        /// <summary>
        /// Creates a TCP connection to the destination host through the proxy server
        /// host.
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address of the destination server.</param>
        /// <param name="destinationPort">Port number to connect to on the destination server.</param>
        /// <returns>
        /// Returns an open TcpClient object that can be used normally to communicate
        /// with the destination server
        /// </returns>
        /// <remarks>
        /// This method creates a connection to the proxy server and instructs the proxy server
        /// to make a pass through connection to the specified destination host on the specified
        /// port. 
        /// </remarks>
        public TcpClient CreateConnection(string destinationHost, ushort destinationPort)
        {
            if (string.IsNullOrEmpty(destinationHost))
                throw new ArgumentNullException(nameof(destinationHost));
            try
            {
                // if we have no connection, create one
                if (Client == null)
                {
                    if (string.IsNullOrEmpty(ProxyHost))
                        throw new ProxyException("ProxyHost property must contain a value.");
                    //  create new tcp client object to the proxy server
                    Client = new TcpClient();
                    // attempt to open the connection
                    Client.Connect(ProxyHost, ProxyPort);
                }
                // send connection command to proxy host for the specified destination host and port
                SendCommand(Client.GetStream(), SOCKS4_CMD_CONNECT, destinationHost, destinationPort, ProxyUserId);
                // return the open proxied tcp client object to the caller for normal use
                return Client;
            }
            catch (Exception ex)
            {
                throw new ProxyException(string.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(Client), Utils.GetPort(Client)), ex);
            }
        }

        /// <summary>
        /// Sends a command to the proxy server.
        /// </summary>
        /// <param name="proxy">Proxy server data stream.</param>
        /// <param name="command">Proxy byte command to execute.</param>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Destination port number</param>
        /// <param name="userId">IDENTD user ID value.</param>
        internal virtual void SendCommand(NetworkStream proxy, byte command, string destinationHost, ushort destinationPort, string userId)
        {
            // PROXY SERVER REQUEST
            // The client connects to the SOCKS server and sends a CONNECT request when
            // it wants to establish a connection to an application server. The client
            // includes in the request packet the IP address and the port number of the
            // destination host, and userid, in the following format.
            //
            //               +----+----+----+----+----+----+----+----+----+----+....+----+
            //               | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
            //               +----+----+----+----+----+----+----+----+----+----+....+----+
            // # of bytes:	   1    1       2              4          variable        1
            //
            // VN is the SOCKS protocol version number and should be 4. CD is the
            // SOCKS command code and should be 1 for CONNECT request. NULL is a byte
            // of all zero bits.        

            //  userId needs to be a zero length string so that the GetBytes method
            //  works properly
            if (userId is null)
                userId = "";
            byte[] destIp = GetIPAddressBytes(destinationHost);
            byte[] destPort = GetDestinationPortBytes(destinationPort);
            byte[] userIdBytes = Encoding.ASCII.GetBytes(userId);
            byte[] request = new byte[9 + userIdBytes.Length];

            // set the bits on the request byte array
            request[0] = SOCKS4_VERSION_NUMBER;
            request[1] = command;
            destPort.CopyTo(request, 2);
            destIp.CopyTo(request, 4);
            userIdBytes.CopyTo(request, 8);
            request[8 + userIdBytes.Length] = 0x00;  // null (byte with all zeros) terminator for userId

            // send the connect request
            proxy.Write(request, 0, request.Length);
            // wait for the proxy server to respond
            WaitForData(proxy);

            // PROXY SERVER RESPONSE
            // The SOCKS server checks to see whether such a request should be granted
            // based on any combination of source IP address, destination IP address,
            // destination port number, the userid, and information it may obtain by
            // consulting IDENT, cf. RFC 1413. If the request is granted, the SOCKS
            // server makes a connection to the specified port of the destination host.
            // A reply packet is sent to the client when this connection is established,
            // or when the request is rejected or the operation fails. 
            //
            //              +----+----+----+----+----+----+----+----+
            //              | VN | CD | DSTPORT |      DSTIP        |
            //              +----+----+----+----+----+----+----+----+
            // # of bytes:	   1    1      2              4
            //
            // VN is the version of the reply code and should be 0. CD is the result
            // code with one of the following values:
            //
            //    90: request granted
            //    91: request rejected or failed
            //    92: request rejected becuase SOCKS server cannot connect to
            //        identd on the client
            //    93: request rejected because the client program and identd
            //        report different user-ids
            //
            // The remaining fields are ignored.
            //
            // The SOCKS server closes its connection immediately after notifying
            // the client of a failed or rejected request. For a successful request,
            // the SOCKS server gets ready to relay traffic on both directions. This
            // enables the client to do I/O on its connection as if it were directly
            // connected to the application server.

            // create an 8 byte response array  
            byte[] response = new byte[8];
            // read the resonse from the network stream
            proxy.Read(response, 0, 8);
            //  evaluate the reply code for an error condition
            if (response[1] != SOCKS4_CMD_REPLY_REQUEST_GRANTED)
                HandleProxyCommandError(response, destinationHost, destinationPort);
        }

        /// <summary>
        /// Translate the host name or IP address to a byte array.
        /// </summary>
        /// <param name="destinationHost">Host name or IP address.</param>
        /// <returns>Byte array representing IP address in bytes.</returns>
        internal byte[] GetIPAddressBytes(string destinationHost)
        {
            // if the address doesn't parse then try to resolve with dns
            if (!IPAddress.TryParse(destinationHost, out IPAddress ipAddr))
            {
                try
                {
                    ipAddr = Dns.GetHostEntry(destinationHost).AddressList[0];
                }
                catch (Exception ex)
                {
                    throw new ProxyException(string.Format(CultureInfo.InvariantCulture, "A error occurred while attempting to DNS resolve the host name {0}.", destinationHost), ex);
                }
            }
            // return address bytes
            return ipAddr.GetAddressBytes();
        }

        /// <summary>
        /// Translate the destination port value to a byte array.
        /// </summary>
        /// <param name="value">Destination port.</param>
        /// <returns>Byte array representing an 16 bit port number as two bytes.</returns>
        internal byte[] GetDestinationPortBytes(ushort value) => new byte[] { (byte)(value / 256), (byte)(value % 256) };

        /// <summary>
        /// Receive a byte array from the proxy server and determine and handle and errors that may have occurred.
        /// </summary>
        /// <param name="response">Proxy server command response as a byte array.</param>
        /// <param name="destinationHost">Destination host.</param>
        /// <param name="destinationPort">Destination port number.</param>
        internal void HandleProxyCommandError(byte[] response, string destinationHost, ushort destinationPort)
        {
            if (response == null)
                throw new ArgumentNullException("response");
            //  extract the reply code
            byte replyCode = response[1];
            //  extract the ip v4 address (4 bytes)
            byte[] ipBytes = new byte[4];
            for (int i = 0; i < 4; i++)
                ipBytes[i] = response[i + 4];
            //  convert the ip address to an IPAddress object
            IPAddress ipAddr = new IPAddress(ipBytes);
            //  extract the port number big endian (2 bytes)
            byte[] portBytes = new byte[] { response[3], response[2] };
            ushort port = BitConverter.ToUInt16(portBytes, 0);
            // translate the reply code error number to human readable text
            string proxyErrorText;
            switch (replyCode)
            {
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_OR_FAILED:
                    proxyErrorText = "connection request was rejected or failed";
                    break;
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_CANNOT_CONNECT_TO_IDENTD:
                    proxyErrorText = "connection request was rejected because SOCKS destination cannot connect to identd on the client";
                    break;
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_DIFFERENT_IDENTD:
                    proxyErrorText = "connection request rejected because the client program and identd report different user-ids";
                    break;
                default:
                    proxyErrorText = string.Format(CultureInfo.InvariantCulture, "proxy client received an unknown reply with the code value '{0}' from the proxy destination", replyCode.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            // build the exeception message string
            string exceptionMsg = string.Format(CultureInfo.InvariantCulture, "The {0} concerning destination host {1} port number {2}. The destination reported the host as {3} port {4}.", proxyErrorText, destinationHost, destinationPort, ipAddr.ToString(), port.ToString(CultureInfo.InvariantCulture));
            // throw a new application exception 
            throw new ProxyException(exceptionMsg);
        }

        internal void WaitForData(NetworkStream stream)
        {
            int sleepTime = 0;
            while (!stream.DataAvailable)
            {
                Thread.Sleep(WAIT_FOR_DATA_INTERVAL);
                sleepTime += WAIT_FOR_DATA_INTERVAL;
                if (sleepTime > WAIT_FOR_DATA_TIMEOUT)
                    throw new ProxyException("A timeout while waiting for the proxy destination to respond.");
            }
        }

        #region "Async Methods"

        BackgroundWorker _asyncWorker;
        Exception _asyncException;
        bool _asyncCancelled;

        /// <summary>
        /// Gets a value indicating whether an asynchronous operation is running.
        /// </summary>
        /// <remarks>Returns true if an asynchronous operation is running; otherwise, false.
        /// </remarks>
        public bool IsBusy => _asyncWorker != null && _asyncWorker.IsBusy;

        /// <summary>
        /// Gets a value indicating whether an asynchronous operation is cancelled.
        /// </summary>
        /// <remarks>Returns true if an asynchronous operation is cancelled; otherwise, false.
        /// </remarks>
        public bool IsAsyncCancelled => _asyncCancelled;

        /// <summary>
        /// Cancels any asychronous operation that is currently active.
        /// </summary>
        public void CancelAsync()
        {
            if (_asyncWorker == null || _asyncWorker.CancellationPending || !_asyncWorker.IsBusy)
                return;
            _asyncCancelled = true;
            _asyncWorker.CancelAsync();
        }

        void CreateAsyncWorker()
        {
            _asyncWorker?.Dispose();
            _asyncException = null;
            _asyncWorker = null;
            _asyncCancelled = false;
            _asyncWorker = new BackgroundWorker();
        }

        /// <summary>
        /// Event handler for CreateConnectionAsync method completed.
        /// </summary>
        public event EventHandler<CreateConnectionAsyncCompletedEventArgs> CreateConnectionAsyncCompleted;

        /// <summary>
        /// Asynchronously creates a remote TCP connection through a proxy server to the destination host on the destination port
        /// using the supplied open TcpClient object with an open connection to proxy server.
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// Returns TcpClient object that can be used normally to communicate
        /// with the destination server. 
        /// </returns>
        /// <remarks>
        /// This instructs the proxy server to make a pass through connection to the specified destination host on the specified
        /// port. 
        /// </remarks>
        public void CreateConnectionAsync(string destinationHost, ushort destinationPort)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
                throw new InvalidOperationException("The Socks4/4a object is already busy executing another asynchronous operation. You can only execute one asychronous method at a time.");

            CreateAsyncWorker();
            _asyncWorker.WorkerSupportsCancellation = true;
            _asyncWorker.DoWork += new DoWorkEventHandler(CreateConnectionAsync_DoWork);
            _asyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CreateConnectionAsync_RunWorkerCompleted);
            _asyncWorker.RunWorkerAsync(new object[] { destinationHost, destinationPort });
        }

        void CreateConnectionAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                object[] args = (object[])e.Argument;
                e.Result = CreateConnection((string)args[0], (ushort)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        void CreateConnectionAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) => CreateConnectionAsyncCompleted?.Invoke(this, new CreateConnectionAsyncCompletedEventArgs(_asyncException, _asyncCancelled, (TcpClient)e.Result));

        #endregion
    }

}
