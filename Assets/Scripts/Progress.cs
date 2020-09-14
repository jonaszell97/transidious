using System;
using System.Collections.Generic;
using UnityEngine;

namespace Transidious
{
    public class Progress
    {
        /// Types of unlockable items.
        public enum Unlockable
        {
            /// The small loan.
            SmallLoan,
            /// The medium loan.
            MediumLoan,
            /// The big loan.
            BigLoan,

            /// The bus transit system.
            Bus,
            /// The tram transit system.
            Tram,
            /// The subway transit system.
            Subway,
            /// The intercity transit system.
            Intercity,
            /// The ferry transit system.
            Ferry,
            
            /// Marker.
            _Last,
        }

        /// Set of unlocked items.
        private readonly HashSet<Unlockable> _unlockStatus;

        /// C'tor.
        public Progress()
        {
            _unlockStatus = new HashSet<Unlockable>();
        }

        /// Unlock everything.
        public void UnlockAll()
        {
            for (var i = 0; i < (int) Unlockable._Last; ++i)
            {
                _unlockStatus.Add((Unlockable) i);
            }
        }

        /// Unlock an item.
        public bool Unlock(Unlockable item)
        {
            return _unlockStatus.Add(item);
        }

        /// Whether or not an item is unlocked.
        public bool IsUnlocked(Unlockable item)
        {
            return _unlockStatus.Contains(item);
        }
    }
}