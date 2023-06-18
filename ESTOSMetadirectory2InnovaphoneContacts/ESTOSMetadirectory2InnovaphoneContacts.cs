﻿using CsvHelper;
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

            Timer timer = new Timer();
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
