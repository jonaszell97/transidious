using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Transidious
{
    public class EventManager : MonoBehaviour
    {
        /// The active event manager instance.
        public static EventManager current;

        /// The registered event listeners.
        // Dictionary<string, List<UnityAction>> eventListeners;
        List<object> listeners;

        /// Currently disabled listeners.
        HashSet<object> disabledListeners;

        /// The last assigned event listener ID.
        // int lastID;

        void Awake()
        {
            if (EventManager.current == null)
            {
                EventManager.current = this;
            }

            // eventListeners = new Dictionary<string, List<UnityAction>>();
            // disabledListeners = new HashSet<int>();
            // lastID = 0;

            listeners = new List<object>();
            disabledListeners = new HashSet<object>();
        }

        public void RegisterEventListener(object listener)
        {
            listeners.Add(listener);
        }

        public void DisableEventListener(object listener)
        {
            disabledListeners.Add(listener);
        }

        public void EnabledEventListener(object listener)
        {
            disabledListeners.Remove(listener);
        }

        public void TriggerEvent(string eventName)
        {
            foreach (var listener in listeners)
            {
                if (disabledListeners.Contains(listener))
                {
                    continue;
                }

                var method = listener.GetType().GetMethod("On" + eventName, 
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic);

                if (method != null)
                {
                    method.Invoke(listener, null);
                }
            }
        }
    }
}