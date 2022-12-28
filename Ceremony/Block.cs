using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;

namespace Notus.Ceremony
{
    public class Block : IDisposable
    {
        private int DefaultBlockGenerateInterval = 3000;

        public int PreviousId
        {
            get { return DefaultBlockGenerateInterval; }
        }
        public void Start()
        {

        }
        public Block(bool AutoStart = true)
        {
            if (AutoStart == true)
            {
                Start();
            }
        }
        public void Dispose()
        {

        }
        ~Block()
        {
            Dispose();
        }
    }
}
