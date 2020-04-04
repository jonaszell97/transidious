#if DEBUG

using System;
using Transidious.PathPlanning;
using UnityEngine;

namespace Transidious
{
    public class BenchmarkController : MonoBehaviour
    {
        [System.Flags]
        public enum Benchmark
        {
            None = 0x0,
            CarPathPlanning,
        }

        /// The benchmarks to run.
        public Benchmark benchmarks = Benchmark.None;

        /// The seed to use for RNG.
        public int seed = -1;

        /// The number of iterations to perform.
        public int iterations = 10_000;

        /// The random number generator to use.
        private System.Random _random;

        private void Awake()
        {
            _random = new System.Random(seed);
        }

        private void Start()
        {
            GameController.instance.onLoad.AddListener(ExecuteBenchmarks);
        }

        void ExecuteBenchmarks()
        {
            var Self = GetType();

            var types = Enum.GetValues(typeof(Benchmark));
            foreach (var value in types)
            {
                if (!benchmarks.HasFlag((Benchmark) value) || (Benchmark)value == Benchmark.None)
                    continue;

                var name = value.ToString();
                var method = Self.GetMethod($"Run{name}Benchmark", 
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);

                if (method == null)
                {
                    Debug.LogError($"Undefined benchmark {name}");
                    continue;
                }

                using (this.CreateTimer(value.ToString(), iterations))
                {
                    method.Invoke(this, null);
                }
            }
        }

        private void RunCarPathPlanningBenchmark()
        {
            var map = SaveManager.loadedMap;
            var options = new PathPlanningOptions
            {
                allowCar = true,
                allowWalk = false,
                maxWalkingDistance = 0,
            };

            var planner = new PathPlanner(options);
            for (var i = 0; i < iterations; ++i)
            {
                var from = _random.Vector2(map.minX, map.maxX, map.minY, map.maxY);
                var to = _random.Vector2(map.minX, map.maxX, map.minY, map.maxY);

                _ = planner.FindClosestDrive(map, from, to, true);
            }
        }
    }
}

#endif