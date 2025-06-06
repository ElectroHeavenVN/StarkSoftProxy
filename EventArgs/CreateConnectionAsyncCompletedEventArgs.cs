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
using System.Net.Sockets;

namespace Starksoft.Net.Proxy
{
    /// <summary>
    /// Event arguments class for the EncryptAsyncCompleted event.
    /// </summary>
    public class CreateConnectionAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// The proxy connection.
        /// </summary>
        public TcpClient ProxyConnection { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="error">Exception information generated by the event.</param>
        /// <param name="cancelled">Cancelled event flag. This flag is set to true if the event was cancelled.</param>
        /// <param name="proxyConnection">Proxy Connection. The initialized and open TcpClient proxy connection.</param>
        public CreateConnectionAsyncCompletedEventArgs(Exception error, bool cancelled, TcpClient proxyConnection) : base(error, cancelled, null) => ProxyConnection = proxyConnection;
    }
}
