using System.Collections.Generic;
using UnityEngine;

namespace Transidious
{
    public class ResourceManager : MonoBehaviour
    {
        public struct ResourceLimits
        {
            /// Maximum number of active paths.
            public int maxActivePaths;
        }

        /// Static reference to the singleton instance.
        public static ResourceManager instance;

        /// The resource limits.
        public ResourceLimits resourceLimits;

        /// Prefab for ActivePath.
        [SerializeField] private GameObject _activePathPrefab;

        /// Set of unused ActivePath instances.
        private Stack<ActivePath> _unusedActivePaths;

        /// Number of total ActivePath instances.
        private int _activePaths;

        private void Awake()
        {
            resourceLimits = new ResourceLimits
            {
                maxActivePaths = 10_000,
            };

            _unusedActivePaths = new Stack<ActivePath>();
            instance = this;
        }

        /// Get an available ActivePath or instantiate one if none are available.
        public ActivePath GetActivePath(bool force = false)
        {
            if (_unusedActivePaths.Count > 0)
            {
                return _unusedActivePaths.Pop();
            }

            if (!force && _activePaths >= resourceLimits.maxActivePaths)
            {
                return null;
            }

            ++_activePaths;

            var obj = Instantiate(_activePathPrefab);
            obj.SetActive(false);

            return obj.GetComponent<ActivePath>();
        }

        /// Reclaim an active path.
        public void Reclaim(ActivePath path)
        {
            path.gameObject.SetActive(false);
            _unusedActivePaths.Push(path);
        }
    }
}