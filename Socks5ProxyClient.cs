/*
 * Authors: Benton Stark
 * 
 * Copyright (c) 2007-2009 Starksoft, LLC (http://www.starksoft.com) 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Starksoft.Net.Proxy
{
    /// <summary>
    /// Socks5 connection proxy class. This class implements the Socks5 standard proxy protocol.
    /// </summary>
    /// <remarks>
    /// This implementation supports TCP proxy connections with a Socks v5 server.
    /// </remarks>
    public class Socks5ProxyClient : IProxyClient
    {
        SocksAuthentication ProxyAuthMethod;

        const string PROXY_NAME = "SOCKS5";
        const int SOCKS5_DEFAULT_PORT = 1080;

        const byte SOCKS5_VERSION_NUMBER = 5;
        const byte SOCKS5_RESERVED = 0x00;
        const byte SOCKS5_AUTH_METHOD_NO_AUTHENTICATION_REQUIRED = 0x00;
        const byte SOCKS5_AUTH_METHOD_GSSAPI = 0x01;
        const byte SOCKS5_AUTH_METHOD_USERNAME_PASSWORD = 0x02;
        const byte SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_BEGIN = 0x03;
        const byte SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_END = 0x7f;
        const byte SOCKS5_AUTH_METHOD_RESERVED_RANGE_BEGIN = 0x80;
        const byte SOCKS5_AUTH_METHOD_RESERVED_RANGE_END = 0xfe;
        const byte SOCKS5_AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS = 0xff;
        const byte SOCKS5_CMD_CONNECT = 0x01;
        const byte SOCKS5_CMD_BIND = 0x02;
        const byte SOCKS5_CMD_UDP_ASSOCIATE = 0x03;
        const byte SOCKS5_CMD_REPLY_SUCCEEDED = 0x00;
        const byte SOCKS5_CMD_REPLY_GENERAL_SOCKS_SERVER_FAILURE = 0x01;
        const byte SOCKS5_CMD_REPLY_CONNECTION_NOT_ALLOWED_BY_RULESET = 0x02;
        const byte SOCKS5_CMD_REPLY_NETWORK_UNREACHABLE = 0x03;
        const byte SOCKS5_CMD_REPLY_HOST_UNREACHABLE = 0x04;
        const byte SOCKS5_CMD_REPLY_CONNECTION_REFUSED = 0x05;
        const byte SOCKS5_CMD_REPLY_TTL_EXPIRED = 0x06;
        const byte SOCKS5_CMD_REPLY_COMMAND_NOT_SUPPORTED = 0x07;
        const byte SOCKS5_CMD_REPLY_ADDRESS_TYPE_NOT_SUPPORTED = 0x08;
        const byte SOCKS5_ADDRTYPE_IPV4 = 0x01;
        const byte SOCKS5_ADDRTYPE_DOMAIN_NAME = 0x03;
        const byte SOCKS5_ADDRTYPE_IPV6 = 0x04;

        /// <summary>
        /// Authentication itemType.
        /// </summary>
        enum SocksAuthentication
        {
            /// <summary>
            /// No authentication used.
            /// </summary>
            None,
            /// <summary>
            /// Username and password authentication.
            /// </summary>
            UsernamePassword
        }

        /// <summary>
        /// Gets String representing the name of the proxy.
        /// </summary>
        /// <remarks>This property will always return the value 'SOCKS5'</remarks>
        public string ProxyName => PROXY_NAME;

        /// <summary>
        /// Gets or sets host name or IP address of the proxy server.
        /// </summary>
        public string ProxyHost { get; set; }

        /// <summary>
        /// Gets or sets port used to connect to proxy server.
        /// </summary>
        public ushort ProxyPort { get; set; }

        /// <summary>
        /// Gets or sets proxy authentication user name.
        /// </summary>
        public string ProxyUserName { get; set; }

        /// <summary>
        /// Gets or sets proxy authentication password.
        /// </summary>
        public string ProxyPassword { get; set; }

        /// <summary>
        /// Gets or sets the TcpClient object.
        /// This property can be set prior to executing CreateConnection to use an existing TcpClient connection.
        /// </summary>
        public TcpClient Client { get; set; }

        /// <summary>
        /// Create a Socks5 proxy client object.
        /// </summary>
        public Socks5ProxyClient() { }

        /// <summary>
        /// Creates a Socks5 proxy client object using the supplied TcpClient object connection.
        /// </summary>
        /// <param name="tcpClient">A TcpClient connection object.</param>
        public Socks5ProxyClient(TcpClient tcpClient) => Client = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));

        /// <summary>
        /// Create a Socks5 proxy client object. The default proxy port 1080 is used.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        public Socks5ProxyClient(string proxyHost)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            ProxyHost = proxyHost;
            ProxyPort = SOCKS5_DEFAULT_PORT;
        }

        /// <summary>
        /// Create a Socks5 proxy client object.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        public Socks5ProxyClient(string proxyHost, ushort proxyPort)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            ProxyHost = proxyHost;
            ProxyPort = proxyPort;
        }

        /// <summary>
        /// Create a Socks5 proxy client object. The default proxy port 1080 is used.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyUsername">Proxy authentication user name.</param>
        /// <param name="proxyPassword">Proxy authentication password.</param>
        public Socks5ProxyClient(string proxyHost, string proxyUsername, string proxyPassword)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));
            if (proxyUsername is null)
                throw new ArgumentNullException(nameof(proxyUsername));
            if (proxyPassword is null)
                throw new ArgumentNullException(nameof(proxyPassword));

            ProxyHost = proxyHost;
            ProxyPort = SOCKS5_DEFAULT_PORT;
            ProxyUserName = proxyUsername;
            ProxyPassword = proxyPassword;
        }

        /// <summary>
        /// Create a Socks5 proxy client object.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        /// <param name="proxyUsername">Proxy authentication user name.</param>
        /// <param name="proxyPassword">Proxy authentication password.</param>
        public Socks5ProxyClient(string proxyHost, ushort proxyPort, string proxyUsername, string proxyPassword)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));
            if (proxyUsername is null)
                throw new ArgumentNullException(nameof(proxyUsername));
            if (proxyPassword is null)
                throw new ArgumentNullException(nameof(proxyPassword));

            ProxyHost = proxyHost;
            ProxyPort = proxyPort;
            ProxyUserName = proxyUsername;
            ProxyPassword = proxyPassword;
        }

        /// <summary>
        /// Creates a remote TCP connection through a proxy server to the destination host on the destination port.
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address of the destination server.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// An open TcpClient object that can be used normally to communicate with the destination server.
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
                if (Client is null)
                {
                    if (string.IsNullOrEmpty(ProxyHost))
                        throw new ProxyException("ProxyHost property must contain a value.");
                    // create new tcp client object to the proxy server
                    Client = new TcpClient();
                    // attempt to open the connection
                    Client.Connect(ProxyHost, ProxyPort);
                }

                // determine which authentication method the client would like to use
                DetermineClientAuthMethod();
                // negotiate which authentication methods are supported / accepted by the server
                NegotiateServerAuthMethod();
                // send a connect command to the proxy server for destination host and port
                SendCommand(SOCKS5_CMD_CONNECT, destinationHost, destinationPort);

                // return the open proxied tcp client object to the caller for normal use
                return Client;
            }
            catch (Exception ex)
            {
                throw new ProxyException(string.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(Client), Utils.GetPort(Client)), ex);
            }
        }

        void DetermineClientAuthMethod()
        {
            // set the authentication itemType used based on values inputed by the user
            if (ProxyUserName != null && ProxyPassword != null)
                ProxyAuthMethod = SocksAuthentication.UsernamePassword;
            else
                ProxyAuthMethod = SocksAuthentication.None;
        }

        void NegotiateServerAuthMethod()
        {
            // get a reference to the network stream
            NetworkStream stream = Client.GetStream();

            // SERVER AUTHENTICATION REQUEST
            // The client connects to the server, and sends a version
            // identifier/method selection message:
            //
            //   +----+----------+----------+
            //   |VER | NMETHODS | METHODS |
            //   +----+----------+----------+
            //   | 1 |  1   | 1 to 255 |
            //   +----+----------+----------+

            bool haveUserPass = !string.IsNullOrEmpty(ProxyUserName) && !string.IsNullOrEmpty(ProxyPassword);
            List<byte> authRequest = new List<byte>
            {
                SOCKS5_VERSION_NUMBER
            };
            if (haveUserPass)
            {
                authRequest.Add(2);
                authRequest.Add(SOCKS5_AUTH_METHOD_NO_AUTHENTICATION_REQUIRED);
                authRequest.Add(SOCKS5_AUTH_METHOD_USERNAME_PASSWORD);
            }
            else
            {
                authRequest.Add(1);
                authRequest.Add(SOCKS5_AUTH_METHOD_NO_AUTHENTICATION_REQUIRED);
            }

            // send the request to the server specifying authentication types supported by the client.
            stream.Write(authRequest.ToArray(), 0, authRequest.Count);

            // SERVER AUTHENTICATION RESPONSE
            // The server selects from one of the methods given in METHODS, and
            // sends a METHOD selection message:
            //
            //   +----+--------+
            //   |VER | METHOD |
            //   +----+--------+
            //   | 1  |   1    |
            //   +----+--------+
            //
            // If the selected METHOD is X'FF', none of the methods listed by the
            // client are acceptable, and the client MUST close the connection.
            //
            // The values currently defined for METHOD are:
            //  * X'00' NO AUTHENTICATION REQUIRED
            //  * X'01' GSSAPI
            //  * X'02' USERNAME/PASSWORD
            //  * X'03' to X'7F' IANA ASSIGNED
            //  * X'80' to X'FE' RESERVED FOR METHODS
            //  * X'FF' NO ACCEPTABLE METHODS

            // receive the server response 
            byte[] response = new byte[2];
            stream.Read(response, 0, response.Length);

            // the first byte contains the socks version number (e.g. 5)
            // the second byte contains the auth method acceptable to the proxy server
            byte acceptedAuthMethod = response[1];

            // if the server does not accept any of our supported authenication methods then throw an error
            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS)
            {
                Client.Close();
                throw new ProxyException("The proxy destination does not accept the supported proxy client authentication methods.");
            }

            // if the server accepts a username and password authentication and none is provided by the user then throw an error
            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_USERNAME_PASSWORD && ProxyAuthMethod == SocksAuthentication.None)
            {
                Client.Close();
                throw new ProxyException("The proxy destination requires a username and password for authentication.");
            }

            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_USERNAME_PASSWORD)
            {
                // USERNAME / PASSWORD SERVER REQUEST
                // Once the SOCKS V5 server has started, and the client has selected the
                // Username/Password Authentication protocol, the Username/Password
                // subnegotiation begins. This begins with the client producing a
                // Username/Password request:
                //
                //    +----+------+----------+------+----------+
                //    |VER | ULEN | UNAME    | PLEN | PASSWD   |
                //    +----+------+----------+------+----------+
                //    | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
                //    +----+------+----------+------+----------+
                //
                byte[] credentials = new byte[ProxyUserName.Length + ProxyPassword.Length + 3];
                credentials[0] = 1;
                credentials[1] = (byte)ProxyUserName.Length;
                Array.Copy(Encoding.ASCII.GetBytes(ProxyUserName), 0, credentials, 2, ProxyUserName.Length);
                credentials[ProxyUserName.Length + 2] = (byte)ProxyPassword.Length;
                Array.Copy(Encoding.ASCII.GetBytes(ProxyPassword), 0, credentials, ProxyUserName.Length + 3, ProxyPassword.Length);

                // USERNAME / PASSWORD SERVER RESPONSE
                // The server verifies the supplied UNAME and PASSWD, and sends the
                // following response:
                //
                //  +----+--------+
                //  |VER | STATUS |
                //  +----+--------+
                //  | 1  |   1    |
                //  +----+--------+
                //
                // A STATUS field of X'00' indicates success. If the server returns a
                // `failure' (STATUS value other than X'00') status, it MUST close the
                // connection.
                stream.Write(credentials, 0, credentials.Length);
                byte[] crResponse = new byte[2];
                stream.Read(crResponse, 0, crResponse.Length);
                if (crResponse[1] != 0)
                {
                    Client.Close();
                    throw new ProxyException("Proxy authentification failure!");
                }
            }
        }

        byte GetDestAddressType(string host)
        {
            IPAddress ipAddr = null;
            bool result = IPAddress.TryParse(host, out ipAddr);
            if (!result)
                return SOCKS5_ADDRTYPE_DOMAIN_NAME;
            switch (ipAddr.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return SOCKS5_ADDRTYPE_IPV4;
                case AddressFamily.InterNetworkV6:
                    return SOCKS5_ADDRTYPE_IPV6;
            }
            throw new ProxyException(string.Format(CultureInfo.InvariantCulture, "The host addess {0} of type '{1}' is not a supported address type. The supported types are InterNetwork and InterNetworkV6.", host, Enum.GetName(typeof(AddressFamily), ipAddr.AddressFamily)));
        }

        byte[] GetDestAddressBytes(byte addressType, string host)
        {
            switch (addressType)
            {
                case SOCKS5_ADDRTYPE_IPV4:
                case SOCKS5_ADDRTYPE_IPV6:
                    return IPAddress.Parse(host).GetAddressBytes();
                case SOCKS5_ADDRTYPE_DOMAIN_NAME:
                    // create a byte array to hold the host name bytes plus one byte to store the length
                    byte[] bytes = new byte[host.Length + 1];
                    // if the address field contains a fully-qualified domain name. The first
                    // octet of the address field contains the number of octets of name that
                    // follow, there is no terminating NUL octet.
                    bytes[0] = Convert.ToByte(host.Length);
                    Encoding.ASCII.GetBytes(host).CopyTo(bytes, 1);
                    return bytes;
            }
            return null;
        }

        byte[] GetDestPortBytes(ushort value) => new byte[] { (byte)(value / 256), (byte)(value % 256) };

        void SendCommand(byte command, string destinationHost, ushort destinationPort)
        {
            NetworkStream stream = Client.GetStream();
            byte addressType = GetDestAddressType(destinationHost);
            byte[] destAddr = GetDestAddressBytes(addressType, destinationHost);
            byte[] destPort = GetDestPortBytes(destinationPort);

            // The connection request is made up of 6 bytes plus the
            // length of the variable address byte array
            //
            // +----+-----+-------+------+----------+----------+
            // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            //
            // * VER protocol version: X'05'
            // * CMD
            //  * CONNECT X'01'
            //  * BIND X'02'
            //  * UDP ASSOCIATE X'03'
            // * RSV RESERVED
            // * ATYP address itemType of following address
            //  * IP V4 address: X'01'
            //  * DOMAINNAME: X'03'
            //  * IP V6 address: X'04'
            // * DST.ADDR desired destination address
            // * DST.PORT desired destination port in network octet order      

            byte[] request = new byte[4 + destAddr.Length + 2];
            request[0] = SOCKS5_VERSION_NUMBER;
            request[1] = command;
            request[2] = SOCKS5_RESERVED;
            request[3] = addressType;
            destAddr.CopyTo(request, 4);
            destPort.CopyTo(request, 4 + destAddr.Length);

            // send connect request.
            stream.Write(request, 0, request.Length);

            // PROXY SERVER RESPONSE
            // +----+-----+-------+------+----------+----------+
            // |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            //
            // * VER protocol version: X'05'
            // * REP Reply field:
            //  * X'00' succeeded
            //  * X'01' general SOCKS server failure
            //  * X'02' connection not allowed by ruleset
            //  * X'03' Network unreachable
            //  * X'04' Host unreachable
            //  * X'05' Connection refused
            //  * X'06' TTL expired
            //  * X'07' Command not supported
            //  * X'08' Address itemType not supported
            //  * X'09' to X'FF' unassigned
            //* RSV RESERVED
            //* ATYP address itemType of following address

            byte[] response = new byte[255];
            // read proxy server response
            var responseSize = stream.Read(response, 0, response.Length);
            byte replyCode = response[1];
            // evaluate the reply code for an error condition
            if (responseSize < 2 || replyCode != SOCKS5_CMD_REPLY_SUCCEEDED)
                HandleProxyCommandError(response, destinationHost, destinationPort);
        }

        void HandleProxyCommandError(byte[] response, string destinationHost, ushort destinationPort)
        {
            string proxyErrorText;
            byte replyCode = response[1];
            byte addrType = response[3];
            string addr = "";
            ushort port = 0;

            switch (addrType)
            {
                case SOCKS5_ADDRTYPE_DOMAIN_NAME:
                    int addrLen = Convert.ToInt32(response[4]);
                    byte[] addrBytes = new byte[addrLen];
                    for (int i = 0; i < addrLen; i++)
                        addrBytes[i] = response[i + 5];
                    addr = Encoding.ASCII.GetString(addrBytes);
                    byte[] portBytesDomain = new byte[] { response[6 + addrLen], response[5 + addrLen] };
                    port = BitConverter.ToUInt16(portBytesDomain, 0);
                    break;
                case SOCKS5_ADDRTYPE_IPV4:
                    byte[] ipv4Bytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                        ipv4Bytes[i] = response[i + 4];
                    IPAddress ipv4 = new IPAddress(ipv4Bytes);
                    addr = ipv4.ToString();
                    byte[] portBytesIpv4 = new byte[] { response[9], response[8] };
                    port = BitConverter.ToUInt16(portBytesIpv4, 0);
                    break;
                case SOCKS5_ADDRTYPE_IPV6:
                    byte[] ipv6Bytes = new byte[16];
                    for (int i = 0; i < 16; i++)
                        ipv6Bytes[i] = response[i + 4];
                    IPAddress ipv6 = new IPAddress(ipv6Bytes);
                    addr = ipv6.ToString();
                    byte[] portBytesIpv6 = new byte[] { response[21], response[20] };
                    port = BitConverter.ToUInt16(portBytesIpv6, 0);
                    break;
            }

            switch (replyCode)
            {
                case SOCKS5_CMD_REPLY_GENERAL_SOCKS_SERVER_FAILURE:
                    proxyErrorText = "a general socks destination failure occurred";
                    break;
                case SOCKS5_CMD_REPLY_CONNECTION_NOT_ALLOWED_BY_RULESET:
                    proxyErrorText = "the connection is not allowed by proxy destination rule set";
                    break;
                case SOCKS5_CMD_REPLY_NETWORK_UNREACHABLE:
                    proxyErrorText = "the network was unreachable";
                    break;
                case SOCKS5_CMD_REPLY_HOST_UNREACHABLE:
                    proxyErrorText = "the host was unreachable";
                    break;
                case SOCKS5_CMD_REPLY_CONNECTION_REFUSED:
                    proxyErrorText = "the connection was refused by the remote network";
                    break;
                case SOCKS5_CMD_REPLY_TTL_EXPIRED:
                    proxyErrorText = "the time to live (TTL) has expired";
                    break;
                case SOCKS5_CMD_REPLY_COMMAND_NOT_SUPPORTED:
                    proxyErrorText = "the command issued by the proxy client is not supported by the proxy destination";
                    break;
                case SOCKS5_CMD_REPLY_ADDRESS_TYPE_NOT_SUPPORTED:
                    proxyErrorText = "the address type specified is not supported";
                    break;
                default:
                    proxyErrorText = string.Format(CultureInfo.InvariantCulture, "that an unknown reply with the code value '{0}' was received by the destination", replyCode.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            string exceptionMsg = string.Format(CultureInfo.InvariantCulture, "The {0} concerning destination host {1} port number {2}. The destination reported the host as {3} port {4}.", proxyErrorText, destinationHost, destinationPort, addr, port.ToString(CultureInfo.InvariantCulture));
            throw new ProxyException(exceptionMsg);
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
            if (_asyncWorker is null || _asyncWorker.CancellationPending || !_asyncWorker.IsBusy)
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
        /// Asynchronously creates a remote TCP connection through a proxy server to the destination host on the destination port.
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// Returns TcpClient object that can be used normally to communicate
        /// with the destination server.
        /// </returns>
        /// <remarks>
        /// This method instructs the proxy server
        /// to make a pass through connection to the specified destination host on the specified
        /// port. 
        /// </remarks>
        public void CreateConnectionAsync(string destinationHost, ushort destinationPort)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
                throw new InvalidOperationException("The Socks4 object is already busy executing another asynchronous operation. You can only execute one asychronous method at a time.");

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
