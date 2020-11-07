using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP
{
    class IOResponseMananger
    {
        private enum NextResponse
        {
            SaveFile = 2,
            PrintFile = 4,
            Print1KB = 8,
            DoNothing = 16
        }
        private NextResponse State = NextResponse.DoNothing;
        private StreamWriter stream;

        public void SaveIncomingFile(string FileName, bool PrintAlso = false)
        {
            State = NextResponse.SaveFile;
            if (PrintAlso) {
                State = NextResponse.SaveFile | NextResponse.Print1KB;
            }

            stream = new StreamWriter(FileName);
        }

        public void PrintIncomingFile()
        {
            State = NextResponse.PrintFile;
        }

        public void Wait()
        {
            while (State != NextResponse.DoNothing) { }
        }

        public void OnChunkDataRecieved(object sender, DataChunkArgs e)
        {
            if (!e.Finished)
            {
                var decoded = Encoding.ASCII.GetString(e.Buffer);
                
                if (State.HasFlag(NextResponse.PrintFile) || State.HasFlag(NextResponse.Print1KB)) {
                    if (State.HasFlag(NextResponse.Print1KB))
                    {
                        Console.Write(Encoding.ASCII.GetString(e.Buffer, 0, Math.Min(e.Buffer.Length, 1024)));
                    }
                    else
                    {
                        Console.Write(decoded);
                    }
                }
                

                if (State.HasFlag(NextResponse.SaveFile)) {
                    stream.Write(decoded);
                }

            } else
            {
                if (State == NextResponse.PrintFile)
                    Console.Write("\n");

                if (State == NextResponse.SaveFile)
                {
                    stream.Write("\n");
                    stream.Close();
                }
                
                State = NextResponse.DoNothing;
            }
        }
    }
}
