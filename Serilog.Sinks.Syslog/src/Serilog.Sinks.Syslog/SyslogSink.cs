using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Events;
using System.IO;
using Serilog.Formatting.Json;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace Serilog.Sinks.Syslog
{
    public class SyslogSink : PeriodicBatchingSink
    {
        string SyslogHost { get; set; }
        int SyslogPort { get; set; }
        ProtocolType SyslogProtocol { get; set; }
        bool UseSSL { get; set; }
        string AppName { get; set; }
        X509CertificateCollection CertificateCollection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchSizeLimit">batch size</param>
        /// <param name="period">period</param>
        /// <param name="host">syslog server host</param>
        /// <param name="port">port of syslog server</param>
        /// <param name="protocol">protocol to use for syslog</param>
        /// <param name="useSSL">applicable onyl when using TCP</param>
        /// <param name="appName">appName field part of syslog message</param>
        /// <param name="certificateCollection">X509Certificates, only applicable when using TCP and SSL</param>
        public SyslogSink(int batchSizeLimit, TimeSpan period, string host, int port, ProtocolType protocol, bool useSSL, X509CertificateCollection certificateCollection=null, string appName=null) : base(batchSizeLimit, period)
        {
            this.SyslogHost = host;
            this.SyslogPort = port;
            this.SyslogProtocol = protocol;
            this.UseSSL = useSSL;
            this.AppName = appName;
            this.CertificateCollection = certificateCollection;
        }

        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            using (var sender = this.GetSender())
            {
                foreach (var logEvent in events)
                {
                    LogSingleEvent(logEvent, sender);
                }
            }
        }

        private SyslogNet.Client.Transport.ISyslogMessageSender GetSender()
        {
            if (this.SyslogProtocol == ProtocolType.Tcp && UseSSL)
                return new SyslogNet.Client.Transport.SyslogEncryptedTcpSender(this.SyslogHost, this.SyslogPort, this.CertificateCollection);
            else if (this.SyslogProtocol == ProtocolType.Tcp)
                return new SyslogNet.Client.Transport.SyslogTcpSender(this.SyslogHost, this.SyslogPort);

            if(this.SyslogProtocol == ProtocolType.Udp)
                return new SyslogNet.Client.Transport.SyslogUdpSender(this.SyslogHost, this.SyslogPort);

            throw new Exception(string.Format("Protocol {0} is not valid.", this.SyslogProtocol.ToString()));
        }

        private void LogSingleEvent(LogEvent logEvent, SyslogNet.Client.Transport.ISyslogMessageSender sender)
        {
            var message = this.BuildSyslogMessage(logEvent);

            sender.Send(message, new SyslogNet.Client.Serialization.SyslogRfc5424MessageSerializer());
        }

        private SyslogNet.Client.SyslogMessage BuildSyslogMessage(LogEvent logEvent)
        {
            var formattedMessage = this.GetFormattedMessage(logEvent);
            var severity = GetSyslogSeverity(logEvent.Level);

            DateTimeOffset dtOffset = new DateTimeOffset(DateTime.Now.ToUniversalTime());
            var facility = SyslogNet.Client.Facility.LocalUse1;
            string procId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            string msgId = null;
            string hostName = Dns.GetHostName();

            Dictionary<string, string> properties = new Dictionary<string, string>();
            foreach (var property in logEvent.Properties)
            {
                properties.Add(property.Key, property.Value.ToString());
            }
            SyslogNet.Client.StructuredDataElement sdElement = new SyslogNet.Client.StructuredDataElement("1", properties);

            SyslogNet.Client.SyslogMessage message = new SyslogNet.Client.SyslogMessage(dtOffset, facility, severity, hostName, this.AppName, procId, msgId, formattedMessage, sdElement);

            return message;
        }

        private static SyslogNet.Client.Severity GetSyslogSeverity(LogEventLevel logLevel)
        {
            if (logLevel == LogEventLevel.Fatal)
            {
                return SyslogNet.Client.Severity.Emergency;
            }

            if (logLevel >= LogEventLevel.Error)
            {
                return SyslogNet.Client.Severity.Error;
            }

            if (logLevel >= LogEventLevel.Warning)
            {
                return SyslogNet.Client.Severity.Warning;
            }

            if (logLevel >= LogEventLevel.Information)
            {
                return SyslogNet.Client.Severity.Informational;
            }

            if (logLevel >= LogEventLevel.Debug)
            {
                return SyslogNet.Client.Severity.Debug;
            }

            return SyslogNet.Client.Severity.Notice;
        }

        private string GetFormattedMessage(LogEvent logEvent)
        {
            return logEvent.RenderMessage(new CultureInfo("en-US"));
            //using (StringWriter sr = new StringWriter())
            //{
            //    new JsonFormatter().Format(logEvent, sr);

            //    return sr.ToString();
            //}
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
