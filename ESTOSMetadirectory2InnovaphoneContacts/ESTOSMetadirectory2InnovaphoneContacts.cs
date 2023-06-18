using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

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
        }

        protected override void OnStop()
        {
        }
    }
}
