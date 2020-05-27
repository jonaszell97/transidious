
// #define ENABLE_LOGS

using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Transidious
{
    public class Logger : MonoBehaviour
    {
        /// Types of logs.
        [System.Flags] public enum LogType
        {
            /// No logs enabled.
            None,

            /// Log traffic simulation.
            TrafficSim,
        }

        /// The logger instance.
        private static Logger _instance;

        /// The enabled log types.
        public LogType EnabledLogs = LogType.None;

        /// MonoBehavior impl.
        void Awake()
        {
            _instance = this;
        }

        /// Log a string of the given log type.
        [Conditional("ENABLE_LOGS")]
        public static void Log(LogType type, string message)
        {
            if (_instance == null || !_instance.EnabledLogs.HasFlag(type))
            {
                return;
            }
            
            Debug.Log(message);
        }

        /// Log a warning of the given log type.
        [Conditional("ENABLE_LOGS")]
        public static void LogWarning(LogType type, string message)
        {
            if (_instance == null || !_instance.EnabledLogs.HasFlag(type))
            {
                return;
            }
            
            Debug.LogWarning(message);
        }

        /// Log an error of the given log type.
        [Conditional("ENABLE_LOGS")]
        public static void LogError(LogType type, string message)
        {
            if (_instance == null || !_instance.EnabledLogs.HasFlag(type))
            {
                return;
            }

            Debug.LogError(message);
        }
    }
}