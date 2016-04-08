﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetBall.Helpers.Network
{
    public class NetworkClient
    {
        /** Constants **/
        private static int MESSAGE_SIZE = 1024;

        private int port;
        private String peerName;
        private bool connected;

        Socket sock;

        public NetworkClient(String peer, int p)
        {
            this.peerName = peer;
            this.port = p;
        }

        public void connectClient()
        {
            //Create a reference to a new thread
            ThreadStart networkThreadRef = new ThreadStart(connectedState);

            //create a new thread
            Thread networkThread = new Thread(networkThreadRef);

            //start the thread
            networkThread.Start();
        }

        private void connectedState()
        {
            //convert the peer's IP from a string and create the end point
            IPAddress ipAddr = IPAddress.Parse(peerName);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

            //create the socket
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                //connect to the server
                sock.Connect(ipEndPoint);

                Console.WriteLine("Socket connected to {0}", sock.RemoteEndPoint.ToString());

                connected = true;

                while (connected)
                {
                    String receivedMsg = readData();

                    // show the data on the console 
                    Console.WriteLine("Text Received: {0}", receivedMsg);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
          
        }


        private String readData()
        {
            string data = null;

            byte[] bytes = new byte[MESSAGE_SIZE];
            int bytesReceived = sock.Receive(bytes);

            data = Encoding.ASCII.GetString(bytes, 0, bytesReceived);
            return data;
        }

        private void sendData(String message)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);

            sock.Send(messageBytes);
        }

        private void closeConnection()
        {
            sock.Shutdown(SocketShutdown.Both);
            sock.Close();
        }
    }
}
