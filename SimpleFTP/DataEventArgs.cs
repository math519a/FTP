using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP
{
    public class DataEventArgs : EventArgs
    {
        public DataEventArgs(string Name, int FtpResponseCode, string Message)
        {
            this.Name = Name;
            this.Message = Message;
            this.FtpResponseCode = FtpResponseCode;
        }

        public string Message { get; private set; }
        public string Name { get; private set; }
        public int FtpResponseCode { get; private set; }
    }
}
