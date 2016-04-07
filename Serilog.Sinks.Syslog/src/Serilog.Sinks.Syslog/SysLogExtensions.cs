using System;
using Serilog;
using Serilog.Configuration;
using System.Net.Sockets;

namespace Serilog.Sinks.Syslog
{
    public static class SysLogExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sysLogServer">the syslog server where messages should be sent</param>
        /// <param name="port">syslog server port</param>
        /// <param name="protocol">supports Udp and TCP</param>
        /// <param name="useSSL">should use encypted transport (works with TCP only)</param>
        /// <param name="certificateCollection">X509Certificates for the transport (works with TCP only)</param>
        /// <param name="appName">appName field for syslog message</param>
        /// <param name="batchSize">sink batch size</param>
        /// <param name="batchPeriodInseconds">sink flush time in seconds</param>
        /// <returns></returns>
        public static LoggerConfiguration Syslog(this LoggerSinkConfiguration config, string sysLogServer, int port, ProtocolType protocol, bool useSSL=false, System.Security.Cryptography.X509Certificates.X509CertificateCollection certificateCollection=null, string appName = null, int batchSize = 10, int batchPeriodInseconds = 1)
        {
            var sink = new SyslogSink(batchSize, TimeSpan.FromSeconds(batchPeriodInseconds), sysLogServer, port, protocol, useSSL, certificateCollection, appName);
            return config.Sink(sink);
        }
    }
}