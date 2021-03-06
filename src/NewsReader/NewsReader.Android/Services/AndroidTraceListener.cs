﻿using System.Diagnostics;
using System.Globalization;

namespace Waf.NewsReader.Android.Services
{
    internal class AndroidTraceListener : TraceListener
    {
        public override void Write(string message) { }

        public override void WriteLine(string message) { }
        
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            Log(eventType, "");
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            Log(eventType, message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            Log(eventType, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        private static void Log(TraceEventType eventType, string message)
        {
            const string tag = "App";
            switch (eventType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    global::Android.Util.Log.Error(tag, message);
                    break;
                case TraceEventType.Warning:
                    global::Android.Util.Log.Warn(tag, message);
                    break;
                case TraceEventType.Information:
                    global::Android.Util.Log.Info(tag, message);
                    break;
                default:
                    global::Android.Util.Log.Verbose(tag, message);
                    break;
            }
        }
    }
}