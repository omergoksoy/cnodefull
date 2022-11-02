﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;
namespace Notus.Message
{
    //socket-exception
    public class Publisher : IDisposable
    {
        private bool closeSocket = false;
        private bool readyForDispose = false;
        private System.Net.Sockets.Socket? listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public void Start()
        {
            subStart(0, "");
        }
        public void Start(int portNo, string ipAddress)
        {
            subStart(portNo, ipAddress);
        }
        public void Start(int portNo)
        {
            subStart(portNo, "");
        }
        public void Start(string ipAddress)
        {
            subStart(0, ipAddress);
        }
        private void subStart(int portNo, string ipAddress)
        {
            if (portNo == 0)
            {
                portNo = Notus.Network.Node.GetNetworkPort()+10;
            }
            listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            byte[] byteArr = new byte[8192];

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, portNo);
            if (ipAddress.Length > 0)
            {
                localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), portNo);
            }

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1000);
                NP.Basic(NVG.Settings, "Message Listener Has Started");
                while (closeSocket == false)
                {
                    System.Net.Sockets.Socket handler = listener.Accept();
                    /*
                    string replyData = string.Empty;
                    while (closeSocket == false)
                    {
                    */
                    int byteArraySize = handler.Receive(byteArr);
                    string contentText = Encoding.ASCII.GetString(byteArr, 0, byteArraySize);
                    Console.WriteLine("Private Socket Server : " + contentText);
                    Console.WriteLine("Private Socket Server : " + contentText);
                    /*
                    }
                    */
                    handler.Send(System.Text.Encoding.ASCII.GetBytes("ok"));
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception ex)
            {

            }
            readyForDispose = true;
        }
        public Publisher()
        {
            closeSocket = false;
        }
        public Publisher(int portNo)
        {
            closeSocket = false;
            Start(portNo);
        }
        public Publisher(string ipAddress)
        {
            closeSocket = false;
            Start(ipAddress);
        }
        public Publisher(int portNo,string ipAddress)
        {
            closeSocket = false;
            Start(ipAddress);
        }
        ~Publisher()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (listener != null)
            {
                closeSocket = true;
                while (readyForDispose == false)
                {
                    Thread.Sleep(5);
                }
                listener.Shutdown(SocketShutdown.Both);
                listener.Close();
                listener.Dispose();
            }
        }

    }
}
