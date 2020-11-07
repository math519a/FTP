using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFTP
{
    public class UserInterpreter
    {
        public enum FTPCommand
        {
            ChangeToParent,
            StructureMount,
            StoreUnique,
            RemoveDirectory,
            MakeDirectory,
            ChangeDirectory,
            PrintDirectory,
            ListFiles,
            System,
            SetPortActive,
            SetPortPassive,
            SetTransferMode,
            SetTransferType,
            User,
            RetrieveFile,
            AppendFile,
            Put,
            Disconnect
        }

        public static string InterpretCommand(FTPCommand cmd, params object[] args)
        {
            switch (cmd)
            {
                case FTPCommand.ChangeToParent: return "CDUP";
                case FTPCommand.StructureMount: return "SMNT";
                case FTPCommand.StoreUnique: return "STOU";
                case FTPCommand.RemoveDirectory: return "RMD";
                case FTPCommand.MakeDirectory: return "MKD";
                case FTPCommand.PrintDirectory: return "PWD";
                case FTPCommand.ChangeDirectory: return string.Format("CWD {0}", args);
                case FTPCommand.System: return "SYST";
                case FTPCommand.User: return string.Format("USER {0}", args);
                case FTPCommand.ListFiles: return "NLST";
                case FTPCommand.SetPortActive: return string.Format("PORT {0}", args);
                case FTPCommand.SetPortPassive: return string.Format("PASV {0}", args);
                case FTPCommand.RetrieveFile:  return string.Format("RETR {0}", args);
                case FTPCommand.SetTransferMode: return string.Format("MODE {0}", args);
                case FTPCommand.SetTransferType: return string.Format("TYPE {0}", args);
                case FTPCommand.AppendFile: return string.Format("APPE {0}", args);
                case FTPCommand.Put: return string.Format("STOR {0}", args);
                case FTPCommand.Disconnect: return "QUIT";
                default:
                    throw new Exception("Can not interpret this command");
            }
        }

        public static byte[] ToASCII(string Ascii_String)
        {
            return Encoding.ASCII.GetBytes(Ascii_String + "\r\n");
        }
    }
}
