using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP
{
    public class DataChunkArgs
    {
        public DataChunkArgs(byte[] Buffer, bool Finished)
        {
            this.Buffer = Buffer;
            this.Finished = Finished;
        }

        public byte[] Buffer { get; private set; }
        public bool Finished { get; private set; }
    }
}
