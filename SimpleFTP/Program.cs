using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SimpleFTP.UserInterpreter;

namespace SimpleFTP
{
    class Program
    {
        static IOResponseMananger ResponseMgr;

        static void Main(string[] args)
        {
            // Setup
            FTPClient client = new FTPClient();
            ResponseMgr = new IOResponseMananger();
            client.DataChunkUpdated += ResponseMgr.OnChunkDataRecieved;

            // Test Client (2x download & print, 1x upload) and a few commands.
            TestFTPClient(client);
        }

        static async void TestFTPClient(FTPClient client)
        {
            // Connects to server..
            client.Connect("128.148.32.111");
            var response = await client.AwaitResponse();
            Console.WriteLine(response.Message);

            // Login to server
            client.SendCommand(FTPCommand.User, "anonymous");
            response = await client.AwaitResponse();
            Console.WriteLine(response.Message);

            // Set mode to STREAM
            client.SendCommand(FTPCommand.SetTransferMode, "S");
            response = await client.AwaitResponse();
            Console.WriteLine(response.Message);

            // Set type to ASCII
            client.SendCommand(FTPCommand.SetTransferType, "A");
            response = await client.AwaitResponse();
            Console.WriteLine(response.Message);

            // Change Directory to /pub/ 
            client.SendCommand(FTPCommand.ChangeDirectory, "/pub");
            response = await client.AwaitResponse();
            Console.WriteLine(response.Message);
            
            // Establish passive connection
            await client.ConnectPassiveDataSocket();

            // Try list files in /pub/ on our passive connection
            ResponseMgr.PrintIncomingFile(); // print incoming DATA to console
            client.SendCommand(FTPCommand.ListFiles);
            response = await client.AwaitResponse();

            ResponseMgr.Wait();
            await client.WaitForDataConnectionClose();

            Console.WriteLine(response.Message);

            // Download README file (9KB)
            await client.ConnectPassiveDataSocket();
            Console.WriteLine("PRINTING FIRST 1KB OF 'README' ");
            Console.WriteLine("-------------------------------------------------");
            ResponseMgr.SaveIncomingFile("README.txt", PrintAlso: true); // save incoming DATA to README.txt file
            client.SendCommand(FTPCommand.RetrieveFile, false, "README");
            response = await client.AwaitResponse();
            ResponseMgr.Wait();
            Console.WriteLine("\n-------------------------------------------------");
            await client.WaitForDataConnectionClose();
            Console.WriteLine(response.Message);


            // Download another text file
            await client.ConnectPassiveDataSocket();
            Console.WriteLine("PRINTING FIRST 1KB OF 'Effective_C++_errata.txt' ");
            Console.WriteLine("-------------------------------------------------");
            ResponseMgr.SaveIncomingFile("Effective_C++_errata.txt", PrintAlso: true); // save incoming DATA to Effective_C++_errata.txt file
            client.SendCommand(FTPCommand.RetrieveFile, false, "Effective_C++_errata.txt");
            response = await client.AwaitResponse();
            ResponseMgr.Wait();
            Console.WriteLine("\n-------------------------------------------------");
            await client.WaitForDataConnectionClose();
            Console.WriteLine(response.Message);
            
            // Reconnect our data socket. Use PASSIVE ftp to upload a test txt file.
            await client.ConnectPassiveDataSocket();
            client.SendCommand(FTPCommand.Put, "TestFile.txt");
            response = await client.AwaitResponse();
            client.SendBufferOnData(ToASCII("Test content of file ..."));
            Console.WriteLine(response.Message); // ... Access Denied because we don't have access to this FTP server .. 
            
            // Wait for user input.
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;

            Console.WriteLine("Press any key to close connection and end program.");
            Console.ReadKey();

            // Disconnect app..
            client.Disconnect();
            Console.WriteLine("Disconnected.");
        }
        
    }
}
