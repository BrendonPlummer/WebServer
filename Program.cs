using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

//using LiteDB;

namespace bDogWebAmmended
{
    class myWebServer
    {
        private TcpListener myListener;


//Constructor
        public myWebServer(int port = 5050)
        {
            /* Constructor for webServer, will take a port as arg but defaults to 5050
            Constructor starts listener on provided port, will set StartListen 
            method to another thread */

            try 
            {
                Console.WriteLine("Constructing Web Server...[19]");
                myListener = new TcpListener(port);
                myListener.Start() ;
                Console.WriteLine("Web Server Running...\n\n");

                Thread th = new Thread(new ThreadStart(StartListen));
                th.Start();

            }
            catch (Exception e)
            {
                Console.WriteLine("[42] - An exception occurred : "+e.ToString());
            }
        }
        

//Model   
        public string GetDefaultFileName(string sLocalDirectory)
        {
            /*Searches through Default.Dat file for the defined default documents,
            looping through all listed options, it will work through until a successful
            read occurs. E.G. if default.html doesnt exist in root, default.htm will be
            searched for next and so on.*/

            StreamReader sr;
            String sLine = "";
            try
            {
                Console.WriteLine("Retrieving Default File...[40]");
                sr = new StreamReader("data/Default.Dat");
                while((sLine = sr.ReadLine()) != null)
                {
                    if (File.Exists(sLocalDirectory + sLine) == true)
                        break;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("[66] - An exception occured : " + e.ToString());
            }
            if(File.Exists(sLocalDirectory + sLine) == true)
                return sLine;
            else 
                return "";
        }
        public string GetLocalPath(string sMyWebServerRoot, string sDirName)
        {
            /*In the instance an alias is used in the URL request, this function
            searches through the Virtual Directory for any matches. If any are found,
            the corresponding file path to the Alias is returned, e.g. URL requests 
            "GET /documents/test.html", the Virtual Directory will be searched for an alias
            matching 'documents', once found returning its corresponding filepath. */

            Console.WriteLine("Searching VirtualDir...[62]");
            StreamReader sr;
            String sLine = "",
                sVirtualDir = "",
                sRealDir = "";
            int iStartPos = 0;

            sDirName.Trim();
            sMyWebServerRoot = sMyWebServerRoot.ToLower();
            sDirName = sDirName.ToLower();

            try
            {
                sr = new StreamReader("data/VDirs.Dat");
                while((sLine = sr.ReadLine()) != null)
                {
                    sLine.Trim();
                    if(sLine.Length > 0)
                    {
                        sLine = sLine.ToLower();
                        iStartPos = sLine.IndexOf(";");
                        sVirtualDir = sLine.Substring(0,iStartPos);
                        sRealDir = sLine.Substring(iStartPos);

                        if(sVirtualDir == sDirName)
                            break;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("[112] - An exception occurred: " + e.ToString());
            }
            if (sVirtualDir == sDirName)
                return sRealDir;
            else
                return "";
        }
        public string GetMimeType(string sRequestedFile)
        {
            /*Verifies the requested file's extension is supported by the web Server.
            As is the same with the GetLocalPath() function, GetMimeType searches through
            the Mime.Dat file for supported extensions and returns the ext type if a 
            successful match is found*/
            Console.WriteLine("Confirming extension eligibility...[120]");

            StreamReader sr;
            String sLine = "",
                sMimeType = "",
                sMimeExt = "",
                sFileExt = "";
            
            sRequestedFile = sRequestedFile.ToLower();
            int iStartPos = sRequestedFile.IndexOf(".");
            sFileExt = sRequestedFile.Substring(iStartPos);

            try
            {
                sr = new StreamReader("data/Mime.Dat");
                while((sLine = sr.ReadLine()) != null)
                {
                    sLine.Trim();
                    if(sLine.Length > 0)
                    {
                        iStartPos = sLine.IndexOf(";");
                        sLine.ToLower();
                        sMimeExt = sLine.Substring(0, iStartPos);
                        sMimeType = sLine.Substring(iStartPos + 1);

                        if(sMimeExt == sFileExt)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[157] - An exception occured : " + e.ToString());
            }

            if(sMimeExt == sFileExt)
                return sMimeExt;
            else   
                return "";
        }


//Controller
        public void SendHeader(string sHttpVersion, string sMimeHeader, int iTotBytes, 
                                string sStatusCode, ref Socket mySocket)
        {
            /*Compiles information for HTTP header and then sends to browser using 
            SendToBrowser() function*/
            Console.WriteLine("Compiling Header... [167]");
            String sBuffer = "";

            if(sMimeHeader.Length == 0)
            {
                sMimeHeader = "text/html";
                //If no extension is specified in http request, defaults to Mime type: text/html
            }

            sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
            sBuffer = sBuffer + "Server: cx1193719-b\r\n";
            sBuffer = sBuffer + "Content-Type: " + sMimeHeader + "\r\n";
            sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            SendToBrowser(bSendData, ref mySocket);
            
            Console.WriteLine(sBuffer + "\n\nTotal Bytes sent : " + iTotBytes.ToString());
        }
        public void SendToBrowser(String sData, ref Socket mySocket)
        {
            /*Sends information back to the browser via connected socket*/
            Console.WriteLine("Sending to browser... [191]");
            SendToBrowser(Encoding.ASCII.GetBytes(sData), ref mySocket);
        }
        public void SendToBrowser(Byte[] bSendData, ref Socket mySocket)
        {
            Console.WriteLine("Sending to browser... [198]");
            int numBytes = 0;
            try
            {
                if(mySocket.Connected)
                {
                    if((numBytes = mySocket.Send(bSendData, bSendData.Length, 0)) == -1)
                        Console.WriteLine("Socket Error! Cannot send packet.");
                    else
                    {
                        Console.WriteLine("No. of bytes sent: {0}", numBytes);
                    }
                }
                else
                {
                    Console.WriteLine("Connection dropped...");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("[218] - An exception occured : " + e.ToString());
            }
        }
        public void StartListen()
        {
            /*The 'working' aspect of the class if you were, starts infinite loop and 
            begins listening for socket requests. Once connection is made and successful,
            it creates a byte array ready for client HTTP request. When recieved the request
            will be interpreted,*/

            int iStartPos = 0;
            String sRequest = "",
                sDirName = "",
                sRequestedFile = "",
                sErrorMessage = "",
                sLocalDir = "",
                sMyWebServerRoot = "/home/bdog/Sandbox/webServer_SecondTest/",
                sPhysicalFilePath = "",
                sFormattedMessage = "",
                sResponse = "",
                sHttpVersion = "";
            
            while (true)
            {
                Socket mySocket = myListener.AcceptSocket();
                if(mySocket.Connected)
                {
                    Console.WriteLine("\n\n===========!!Client Connected!!===========");
                    Byte[] bReceive = new Byte[1024];
                    int i = mySocket.Receive(bReceive, bReceive.Length, 0);

                    string sBuffer = Encoding.ASCII.GetString(bReceive);
                    Console.WriteLine("\nSocket Type: " + mySocket.SocketType);
                    Console.WriteLine("\n" + sBuffer + "\n");
                    
                    //////////////////////////////////////////////////////////////
                    //Only Handles Get, CHANGE ???
                    //////////////////////////////////////////////////////////////
                    if(sBuffer.Substring(0,3) != "GET")
                    {
                        Console.WriteLine("[255] - Only Get Method is supported..");
                        mySocket.Close();
                        return;
                    }

                    iStartPos = sBuffer.IndexOf("HTTP", 1);
                    sHttpVersion = sBuffer.Substring(iStartPos, 8);
                    sRequest = sBuffer.Substring(0, iStartPos - 1);
                    sRequest.Replace("\\", "/");
                    Console.WriteLine("\nHTTP request: " + sRequest);

                    if ((sRequest.IndexOf(".") < 1) && (!sRequest.EndsWith("/")))
                    {
                        sRequest = sRequest + "/";
                    }
                    //Extracting requested fileName from request
                    iStartPos = sRequest.LastIndexOf("/") + 1;
                    sRequestedFile = sRequest.Substring(iStartPos);
                    Console.WriteLine("File requested : " + sRequestedFile);
                    //Extracting directory name
                    sDirName = sRequest.Substring(sRequest.IndexOf("/"),
                               sRequest.LastIndexOf("/") - 3);
                    
                    /*Identifying physical Directory, if none given assumes root,
                    otherwise will use GetLocalPath() to translate alias to filePath.
                    If fails sends error code 404 to browser.*/ 
                    if (sDirName == "/")
                        sLocalDir = sMyWebServerRoot;
                    else
                    {
                        sLocalDir = GetLocalPath(sMyWebServerRoot, sDirName);
                    }
                    Console.WriteLine(sLocalDir);
                    Console.WriteLine("Directory requested : " + sLocalDir);
                    if (sLocalDir.Length == 0)
                    {
                        sErrorMessage = "<H2>Error!! Requested Directory does not exists</H2>";
                        sErrorMessage = sErrorMessage + "<p1>Please check data\\Vdirs.Dat</p1>";
                        SendHeader(sHttpVersion, "", sErrorMessage.Length,
                                   " 404 Not Found", ref mySocket);
                        SendToBrowser(sErrorMessage, ref mySocket);

                        mySocket.Close();
                        continue;
                    }

                    /*Interpreting and searching for requested file using
                    GetDefaultFileName(), if no file is povided returns error 404*/
                    if (sRequestedFile.Length == 0)
                    {
                        sRequestedFile = GetDefaultFileName(sLocalDir);
                        if (sRequestedFile == "")
                        {
                            sErrorMessage = "<H2>Error!! No Default File Name Specified</H2>";
                            SendHeader(sHttpVersion, "", sErrorMessage.Length,
                                       " 404 Not Found", ref mySocket);
                            SendToBrowser(sErrorMessage, ref mySocket);

                            mySocket.Close();
                            return;
                        }
                    }

                    /**/
                    String sMimeType = GetMimeType(sRequestedFile);
                    
                    sPhysicalFilePath = sLocalDir + sRequestedFile;
                    Console.WriteLine("File Requested : " + sPhysicalFilePath);
                    
                    if (File.Exists(sPhysicalFilePath) == false)
                    {
                        sErrorMessage = "<H2>404 Error! File Does Not Exists...</H2>";
                        SendHeader(sHttpVersion, "", sErrorMessage.Length,
                                   " 404 Not Found", ref mySocket);
                        SendToBrowser(sErrorMessage, ref mySocket);
                        Console.WriteLine("HTTP response : " + sFormattedMessage);
                    }
                    else
                    {
                        int iTotBytes = 0;

                        sResponse = "";

                        FileStream fs = new FileStream(sPhysicalFilePath,
                                        FileMode.Open, FileAccess.Read,
                          FileShare.Read);
                        // Create a reader that can read bytes from the FileStream.


                        BinaryReader reader = new BinaryReader(fs);
                        byte[] bytes = new byte[fs.Length];
                        int read;
                        while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Read from the file and write the data to the network
                            sResponse = sResponse + Encoding.ASCII.GetString(bytes, 0, read);

                            iTotBytes = iTotBytes + read;

                        }
                        reader.Close();
                        fs.Close();

                        SendHeader(sHttpVersion, sMimeType, iTotBytes, " 200 OK", ref mySocket);
                        SendToBrowser(bytes, ref mySocket);
                        //mySocket.Send(bytes, bytes.Length,0);

                    }
                    mySocket.Close();
                }
            }
        }
    
    //View

    }

    static class Program
    {
        static int Main()
        {
            myWebServer myServer = new myWebServer();
            myServer.StartListen();

            return 0;
        }
    }
}