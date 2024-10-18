using CredentialManagement;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Timers;

namespace ESTOSMetadirectory2InnovaphoneContacts
{
    public partial class ESTOSMetadirectory2InnovaphoneContacts : ServiceBase
    {
        public EventLog eventLog1;
        public bool allowInsecureConnection = false;
        public string httpEndpoint;
        public string httpUserName;
        public string httpPassword;
        public string innovaphoneDomain;
        public string serviceDirectory;
        public const string serviceName = "ESTOSMetadirectory2InnovaphoneContacts";
        public ESTOSMetadirectory2InnovaphoneContacts()
        {
            InitializeComponent();
            if (!EventLog.SourceExists(serviceName))
            {
                EventLog.CreateEventSource(
                    serviceName,
                    "Application"
                );
            }

            eventLog1 = new EventLog
            {
                Source = serviceName
            };
        }

        protected override async void OnStart(string[] args)
        {
            
            string[] allArguments = args.Concat(Environment.GetCommandLineArgs()).ToArray();
            foreach (string argument in allArguments)
            {
                if (argument.Contains("="))
                {
                    string[] argumentParts = argument.Split('=');

                    switch (argumentParts[0].Replace("/", ""))
                    {
                        case "allowInsecureConnection":
                            allowInsecureConnection = bool.Parse(argumentParts[1]);
                            if (allowInsecureConnection)
                            {
                                eventLog1.WriteEntry(
                                    "Connection to insecure endpoints allowed. (i.e. invalid certifcate)",
                                    EventLogEntryType.Warning
                                );
                            }

                            break;
                    }
                }
            }

            serviceDirectory = Path.GetPathRoot(Environment.SystemDirectory) + serviceName;
            if (!Directory.Exists(serviceDirectory)) {
                eventLog1.WriteEntry(
                    String.Format(
                        "Could not find service related directory '{0}'",
                        serviceDirectory
                    ),
                    EventLogEntryType.Warning
                );

                Directory.CreateDirectory(serviceDirectory);

                if (Directory.Exists(serviceDirectory))
                {
                    eventLog1.WriteEntry("Created service directory successful.", EventLogEntryType.Information);
                }
            }

            using (var credential = new Credential())
            {
                credential.Target = ServiceName;
                credential.Load();

                if (String.IsNullOrEmpty(credential.Password))
                {
                    eventLog1.WriteEntry(
                        "Please set the username and password in the windows credential manager.",
                        EventLogEntryType.Warning
                    );

                    Stop();
                }
                else
                {
                    if (credential.Username.Split('|').Length < 2) {
                        eventLog1.WriteEntry(string.Format(
                                "Username of the '{0}' credential does not contain the two required parts of endpoint base url and the username splitted by '-'.",
                                serviceName
                            ),
                            EventLogEntryType.Error
                        );

                        Stop();
                    }

                    string[] credentialParts = credential.Username.Split('|');
                    if (!Uri.IsWellFormedUriString(credentialParts[0], UriKind.Absolute) )
                    {
                        eventLog1.WriteEntry(string.Format(
                                "Endpoint url '{0}' does not seems to be a valid url.",
                                credentialParts[0]
                            ),
                            EventLogEntryType.Error
                        );

                        Stop();
                    }

                    httpEndpoint = credentialParts[0];
                    httpPassword = credential.Password;
                    httpUserName = credentialParts[1];

                    if (credentialParts.Length == 3)
                    {
                        innovaphoneDomain = credentialParts[2];
                    }
                    else
                    {
                        innovaphoneDomain = new Uri(httpEndpoint).Host;
                    }

                    if (innovaphoneDomain.Split('.').Length >= 3)
                    {
                        string[] parts = innovaphoneDomain.Split('.');
                        Array.Reverse(parts);
                        innovaphoneDomain = parts[1] + "." + parts[0];
                    }

                    eventLog1.WriteEntry(string.Format(
                            "Read '{0}' as innovaphone PBX-endpoint url, " +
                            "'{1}' as innovaphone PBX-domain " +
                            "and username '{2}' from windows credential manager.",
                            httpEndpoint,
                            innovaphoneDomain,
                            httpUserName
                        ),
                        EventLogEntryType.Information
                    );
                }
            }
            
            if (File.Exists(Environment.SystemDirectory + Path.DirectorySeparatorChar + "curl.exe"))
            {
                Process process = new Process();
                process.StartInfo.FileName = Environment.SystemDirectory + Path.DirectorySeparatorChar + "curl.exe";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                eventLog1.WriteEntry(string.Format(
                        "Dectected curl with the following build:" + Environment.NewLine + "{0}",
                        process.StandardOutput.ReadLine().ToString()
                    )
                );

                process.WaitForExit();

                // check for required '--digest'-auth support
                process.StartInfo.Arguments = "--help auth";
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                if (String.IsNullOrEmpty(output))
                {
                    eventLog1.WriteEntry(
                        "Unexcepted empty output while reading supported auth-mechanism of curl.",
                        EventLogEntryType.Error
                    );

                    process.WaitForExit();
                    Stop();
                }
                else if (!output.Contains("--digest"))
                {
                    eventLog1.WriteEntry(string.Format(
                            "Installed curl does not support required digest-auth."
                        ),
                        EventLogEntryType.Error
                    );
                    process.WaitForExit();
                    Stop();
                }
            }
            else
            {
                eventLog1.WriteEntry(string.Format(
                        "Unable to locate 'curl.exe' at '{0}'",
                        Environment.SystemDirectory
                    ),
                    EventLogEntryType.Error
                );

                Stop();
            }

            try
            {
                HttpClient httpClient = new HttpClient();
                if (allowInsecureConnection)
                {
                    var httpClientHandler = new HttpClientHandler();
                    httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    httpClientHandler.ServerCertificateCustomValidationCallback = (HttpRequestMessage, cert, certChain, policyErrors) =>
                    {
                        return true;
                    };

                    httpClient = new HttpClient(httpClientHandler);
                }
                
                var response = await httpClient.GetAsync(httpEndpoint + "/" + innovaphoneDomain + "/contacts/post/");
                if (response.Headers.Contains("WWW-Authenticate"))
                {
                    if (!response.Headers.GetValues("WWW-Authenticate").First().Contains("realm=\"Innovaphone\""))
                    {
                        eventLog1.WriteEntry(string.Format(
                                "Unable to locate an Innovaphone PBX behind the endpoint address. " +
                                "Expected 'realm=\"Innovaphone\"' within 'WWW-Authenticate' header " +
                                "but got '{0}'",
                                response.Headers.GetValues("WWW-Authenticate").First().ToString()
                            ),
                            EventLogEntryType.Error
                        );

                        Stop();
                    }
                }
                else
                {
                    eventLog1.WriteEntry(
                        "Unable to locate an Innovaphone PBX behind the endpoint address. " +
                        "Expected 'WWW-Authenticate' header is missing.",
                        EventLogEntryType.Error
                    );

                    Stop();
                }
            }
            catch (HttpRequestException e)
            {
                eventLog1.WriteEntry(string.Format(
                        "Unexcepected exception while checking for the existance of a Innovaphone PBX behind endpoint address." +
                        Environment.NewLine +
                        "Exception: " + Environment.NewLine + "{0}",
                        e.Message
                    ),
                    EventLogEntryType.Error
                );

                Stop();
            }

            System.Timers.Timer timer = new System.Timers.Timer
            {
                Interval = 60000 * 5 // 5 minutes
            };
            timer.Elapsed += new ElapsedEventHandler(this.OnTick);
            timer.Start();
        }

        public void OnTick(object sender, ElapsedEventArgs args)
        {
            eventLog1.WriteEntry(
                "Searching for convertable files in service directory",
                EventLogEntryType.Information
            );

            string[] files = Directory.GetFiles(
                Path.GetPathRoot(Environment.SystemDirectory) + serviceName,
                "*.csv"
            );

            eventLog1.WriteEntry(
                string.Format("Found {0} files for conversion", files.Count()),
                EventLogEntryType.Information
            );

            foreach (string item in files)
            {
                eventLog1.WriteEntry(
                    string.Format("Begin conversion of file '{0}'", item),
                    EventLogEntryType.Information
                );

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    HasHeaderRecord = true
                };
                
                using (var reader = new StreamReader(item))
                using (var csv = new CsvReader(reader, config))
                {
                    csv.Context.RegisterClassMap<ESTOSMetadirectory2InnovaphoneMap>();
                    var records = csv.GetRecords<InnovaphoneContact>().ToList();

                    eventLog1.WriteEntry(
                        string.Format("Read {0} records from file '{1}' and mapped it successfully.",
                            records.Count(),
                            item
                        )
                    );

                    var outputFile = item.ToString();
                    using (var writer = new StreamWriter(outputFile.Replace(".csv", "_converted.csv")))
                    using (var csvWriter = new CsvWriter(writer, config))
                    {
                        csvWriter.WriteRecords(records);

                        eventLog1.WriteEntry(
                            string.Format("Converted file '{0}' successful. Output file written to '{1}'. Remove source file.",
                                item,
                                item.Replace(".csv", "_converted.csv")
                            )
                        );
                    }

                    reader.Close();
                }
                File.Delete(item);

                eventLog1.WriteEntry(string.Format(
                    "Start upload of file '{0}'",
                    item.Replace(".csv", "_converted.csv")
                    )
                );

                string command = "/C {5}";
                if (allowInsecureConnection)
                {
                    command += " --insecure";
                }
                command += " --digest" +
                             " -s" +
                             " -S" +
                             " -i" +
                             " -u {0}:{1}" +
                             " -H \"Content-Type:application/octet-stream\"" +
                             " -H \"Expect: 100-continue\"" +
                             " -X POST" +
                             " --data-binary @\"{2}\"" +
                             " \"" + "{3}\"" +
                             " >> {4}";
                Process process = Process.Start(
                    "cmd.exe",
                    string.Format(
                        command,
                        httpUserName,
                        httpPassword,
                        item.Replace(".csv", "_converted.csv"),
                        httpEndpoint + "/" + innovaphoneDomain + "/contacts/post/" + Path.GetFileNameWithoutExtension(item) + "?op=csv",
                        serviceDirectory.Replace(@"\", @"\\") + "\\debug_" + Path.GetFileNameWithoutExtension(item) + ".log",
                        Environment.SystemDirectory + Path.DirectorySeparatorChar + "curl.exe"
                    )
                );

                while (!process.HasExited && process.Responding)
                {
                    eventLog1.WriteEntry(string.Format(
                        "Waiting while upload file '{0}'",
                        item.Replace(".csv", "_converted.csv")
                        )
                    );

                    // wait for process being completed; re-check in 15 seconds
                    Thread.Sleep(15000);
                }

                File.Delete(item.Replace(".csv", "_converted.csv"));
                eventLog1.WriteEntry(string.Format(
                        "Finished upload of file '{0}'" + Environment.NewLine + Environment.NewLine + "{1}",
                        item.Replace(".csv", "_converted.csv"),
                        File.ReadAllText(serviceDirectory.Replace(@"\", @"\\") + "\\debug_" + Path.GetFileNameWithoutExtension(item) + ".log")
                    )
                );

                File.Delete(serviceDirectory.Replace(@"\", @"\\") + "\\debug_" + Path.GetFileNameWithoutExtension(item) + ".log");

                // sleep for 30 seconds to give the innovaphone PBX time to handle the latest upload
                Thread.Sleep(30000);
            }
        }
    }
}
