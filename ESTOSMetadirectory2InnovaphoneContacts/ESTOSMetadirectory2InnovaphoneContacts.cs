using CredentialManagement;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;

namespace ESTOSMetadirectory2InnovaphoneContacts
{
    public partial class ESTOSMetadirectory2InnovaphoneContacts : ServiceBase
    {
        public EventLog eventLog1;
        public string httpEndpoint;
        public string httpUserName;
        public string httpPassword;
        public string innovaphoneDomain;
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

            eventLog1 = new EventLog();
            eventLog1.Source = serviceName;
        }

        protected override void OnStart(string[] args)
        {
            var serviceDirectory = Path.GetPathRoot(Environment.SystemDirectory) + serviceName;
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

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 20000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTick);
            timer.Start();
        }

        protected override void OnStop()
        {
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
            }
        }
    }
}
