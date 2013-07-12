﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web.Hosting;
using CassiniDev;
using CassiniDev.FluentSharp.CassiniDev;
using FluentSharp.BCL;
using FluentSharp.CassiniDev;
using FluentSharp.CoreLib;
using FluentSharp.CoreLib.API;
using NUnit.Framework;

namespace UnitTests.FluentSharp_AspNet_MVC
{

    [TestFixture]
    public class Test_Host 
    {
        [Test]
        public void Host()
        {            
            var server = new Server("_temp_CassiniSite".tempDir());

            var host = new Host();
            Assert.IsNotNull(host);            
            
            
            //check values before configure
            Assert.Throws<NullReferenceException>(()=> host.GetProcessToken());
            Assert.Throws<NullReferenceException>(()=> host.GetProcessUser());
            Assert.IsNotNull(host.AppDomain);
            Assert.IsFalse  (host.DisableDirectoryListing);
            Assert.IsNull   (host.NormalizedClientScriptPath);
            Assert.IsNull   (host.NormalizedVirtualPath);
            Assert.IsNull   (host.PhysicalClientScriptPath);
            Assert.IsNull   (host.PhysicalPath);
            Assert.AreEqual (0, host.Port);
            Assert.IsFalse  (host.RequireAuthentication);
            Assert.IsNull   (host.VirtualPath);

            host.Configure(server, server.Port, server.VirtualPath, server.PhysicalPath);

            //check values after configure
            Assert.AreNotEqual(host.GetProcessToken(),IntPtr.Zero);
            Assert.IsNotNull  (host.GetProcessUser());            
            Assert.IsFalse    (host.DisableDirectoryListing);
            Assert.IsNull     (host.InstallPath);
            Assert.IsNotNull  (host.NormalizedClientScriptPath);
            Assert.IsNotNull  (host.NormalizedVirtualPath);
            Assert.IsNotNull  (host.PhysicalClientScriptPath);
            Assert.IsNotNull  (host.PhysicalPath);
            Assert.AreNotEqual(0, host.Port);
            Assert.IsFalse(host.RequireAuthentication);
            Assert.IsNotNull  (host.VirtualPath);
            
            //removed tempDir
            server.PhysicalPath.delete_Folder();
        }

        [Test]
        public void GET_Request_Using_Start_And_TcpClient()
        {
            var server           = new Server("_temp_CassiniSite".tempDir());
            var fileName         = "test.aspx";
            var fileContents     = "<%=\"some text\"%>";
            var expectedResponse = "some text";
            var filePath         = server.PhysicalPath.pathCombine(fileName);
            fileContents.saveAs(filePath);            

            server.Start();
            
            var request = "GET /test.aspx HTTP/1.1".line().line();
            var tcpClient = server.Port.tcpClient();
            tcpClient.write(request);            
            var response = tcpClient.read_Response();
            Assert.IsTrue(response.valid());
            var responseBody = response.subString_After("".line().line());
            Assert.AreEqual(responseBody, expectedResponse);

            server.ShutDown();
            server.PhysicalPath.delete_Folder();
        }

        [Test]
        public void GET_Request_Using_GetHost_And_TcpClient()
        {            
            var server = new Server("_temp_CassiniSite".tempDir());

            var socket = Server.CreateSocketBindAndListen(AddressFamily.InterNetwork, server.IPAddress, server.Port);            
            
            var thread = O2Thread.mtaThread(
                ()=>{
                        Socket acceptedSocket = socket.Accept();
                        var connection = new Connection(server,acceptedSocket);
                        //var host = new Host();                    // we can't get the host directly, it needs to be created using the technique coded in  
                        var host =server.GetHost();                 // CreateWorkerAppDomainWithHost 
                        
                        host.Configure(server, server.Port, server.VirtualPath, server.PhysicalPath);
                        host.ProcessRequest(connection);            
                    });
            
            var request1 = "GET / HTTP/1.1".line().line();
                
            var tcpClient1 = server.Port.tcpClient();
            tcpClient1.write(request1);            
            var response1 = tcpClient1.read_Response();
            Assert.IsTrue(response1.valid());                     

            server.PhysicalPath.delete_Folder();
        }

        [Test]
        public void GET_Request_Using_GetHost_And_SimpleWorkerRequest()
        {
            var server           = new Server("_temp_CassiniSite".tempDir());
            var host             = server.GetHost();
            //host.Configure(server, server.Port, server.VirtualPath, server.PhysicalPath);
            var fileName         = "test.aspx";
            var query1            = "name=John Smith";
            var query2            = "name=OWASP";
            var query3            = "name=42";
            var query4            = "";
            var fileContents     = "<%=\"Hello \" + Request(\"name\") %>";
            var expectedResponse1 = "Hello John Smith";
            var expectedResponse2 = "Hello OWASP";
            var expectedResponse3 = "Hello 42";
            var expectedResponse4 = "Hello ";
            var filePath         = server.PhysicalPath.pathCombine(fileName);
            fileContents.saveAs(filePath);            
            
            var response1 = host.Process_SimpleWorkerRequest(fileName, query1);            
            var response2 = host.Process_SimpleWorkerRequest(fileName, query2);            
            var response3 = host.Process_SimpleWorkerRequest(fileName, query3);                        
            var response4 = host.Process_SimpleWorkerRequest(fileName, query4);                        
            Assert.AreEqual(response1, expectedResponse1);
            Assert.AreEqual(response2, expectedResponse2);
            Assert.AreEqual(response3, expectedResponse3);
            Assert.AreEqual(response4, expectedResponse4);            
            
            server.ShutDown();
            //delete temp server.PhysicalPath
            server.PhysicalPath.delete_Folder();
        }

    }


    [TestFixture]
    public class Test_Cassini_Host : Temp_Cassini_Site
    {
        [Test]
        public void Host_Object()
        {

            //var host = server.invoke("GetHost");
            //Server.script_Me();
         //   Assert.IsNotNull(host);
            //
        }

        [Test]
        public void Get_Html_From_Txt_and_Aspx_Files()
        {
            Action<string,string,string> checkFileViaHttp = 
                (fileName,fileContents, expectedResponse) =>
                    {
                        var filePath = webRoot.pathCombine(fileName);
                        Assert.IsFalse(filePath.fileExists());
                        if (fileContents.valid())
                        {
                            fileContents.saveAs(filePath);
                            Assert.IsTrue(filePath.fileExists());
                        }
                        var fileUrl = apiCassini.url() + fileName;
                        var html    = fileUrl.html();
                        Assert.AreEqual(expectedResponse, html);
                        filePath.file_Delete();
                        Assert.IsFalse(filePath.fileExists());                
                    };
            
            checkFileViaHttp("test_File1.txt" , ""                          , "");            
            checkFileViaHttp("test_File2.txt" , "Some contents ..."         , "Some contents ...");                        
            checkFileViaHttp("test_File2.txt" , "Some contents changed"     , "Some contents changed");                        
            checkFileViaHttp("test_ASPX1.aspx",  "<%=\"Hello from ASPX\"%>" , "Hello from ASPX");
            checkFileViaHttp("test_ASPX2.aspx",  "<%=\"Hello Again\"%>"     , "Hello Again");
        }
    }
}
