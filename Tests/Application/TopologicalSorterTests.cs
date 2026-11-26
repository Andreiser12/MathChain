using MathChain.Application.Curriculum;

namespace MathChain.Tests.Application
{
    public class TopologicalSorterTests
    {
        private static Func<string, IReadOnlyList<string>> Arcs(params (string From, string To)[] arcs)
        {
            var lookup = arcs.ToLookup(a => a.From, a => a.To);
            return node => lookup[node].ToList();
        }

        private static void AssertTopological(IReadOnlyList<string> order, params (string From, string To)[] arcs)
        {
            var position = order.Select((node, index) => (node, index)).ToDictionary(p => p.node, p => p.index);

            foreach (var (from, to) in arcs)
            {
                Assert.True(position[from] < position[to], $"{from} must come before {to} in {string.Join(", ", order)}");
            }
        }

        [Fact]
        public void TopologicalSort_WithoutArcs_PutsTheLastSourceFirst()
        {
            var result = TopologicalSorter.TopologicalSort(new[] { "a", "b", "c" }, Arcs());

            Assert.False(result.HasCycle);
            Assert.Equal(new[] { "c", "b", "a" }, result.Order);
        }

        [Fact]
        public void TopologicalSort_Chain_KeepsTheChainOrder()
        {
            var arcs = new[] { ("a", "b"), ("b", "c"), ("c", "d") };

            var result = TopologicalSorter.TopologicalSort(new[] { "d", "c", "b", "a" }, Arcs(arcs));

            Assert.Equal(new[] { "a", "b", "c", "d" }, result.Order);
        }

        [Fact]
        public void TopologicalSort_Diamond_RespectsEveryArc()
        {
            var arcs = new[] { ("a", "b"), ("a", "c"), ("b", "d"), ("c", "d") };

            var result = TopologicalSorter.TopologicalSort(new[] { "a", "b", "c", "d" }, Arcs(arcs));

            Assert.False(result.HasCycle);
            AssertTopological(result.Order, arcs);
            Assert.Equal(4, result.Order.Count);
        }

        [Fact]
        public void TopologicalSort_SeveralComponents_VisitsEveryNode()
        {
            var arcs = new[] { ("a", "b"), ("c", "d"), ("e", "d") };

            var result = TopologicalSorter.TopologicalSort(new[] { "a", "b", "c", "d", "e", "f" }, Arcs(arcs));

            Assert.Equal(6, result.Order.Distinct().Count());
            AssertTopological(result.Order, arcs);
        }

        [Fact]
        public void TopologicalSort_OrderIsTheDecreasingOrderOfFinishTimes()
        {
            var arcs = new[] { ("a", "b"), ("a", "c"), ("c", "b"), ("d", "a") };

            var result = TopologicalSorter.TopologicalSort(new[] { "a", "b", "c", "d" }, Arcs(arcs));

            var finishTimes = result.Order.Select(node => result.FinishTimes[node]).ToList();
            Assert.Equal(finishTimes.OrderByDescending(t => t), finishTimes);
        }

        [Fact]
        public void TopologicalSort_TimesFollowTheDepthFirstTraversal()
        {
            var result = TopologicalSorter.TopologicalSort(new[] { "a", "b", "c" }, Arcs(("a", "b"), ("b", "c")));

            Assert.Equal(1, result.DiscoveryTimes["a"]);
            Assert.Equal(2, result.DiscoveryTimes["b"]);
            Assert.Equal(3, result.DiscoveryTimes["c"]);
            Assert.Equal(4, result.FinishTimes["c"]);
            Assert.Equal(5, result.FinishTimes["b"]);
            Assert.Equal(6, result.FinishTimes["a"]);
            Assert.All(result.Order, node => Assert.True(result.DiscoveryTimes[node] < result.FinishTimes[node]));
        }

        [Fact]
        public void TopologicalSort_Circuit_ReturnsTheCircuitFoundThroughTheBackArc()
        {
            var result = TopologicalSorter.TopologicalSort(new[] { "a", "b", "c", "d" },
                Arcs(("a", "b"), ("b", "c"), ("c", "a"), ("c", "d")));

            Assert.True(result.HasCycle);
            Assert.Empty(result.Order);
            Assert.Equal(new[] { "a", "b", "c", "a" }, result.Cycle);
        }

        [Fact]
        public void TopologicalSort_CircuitReachedFromAnotherNode_ReturnsOnlyTheCircuit()
        {
            var result = TopologicalSorter.TopologicalSort(new[] { "s", "a", "b" },
                Arcs(("s", "a"), ("a", "b"), ("b", "a")));

            Assert.Equal(new[] { "a", "b", "a" }, result.Cycle);
        }

        [Fact]
        public void TopologicalSort_Loop_IsACircuit()
        {
            var result = TopologicalSorter.TopologicalSort(new[] { "a" }, Arcs(("a", "a")));

            Assert.Equal(new[] { "a", "a" }, result.Cycle);
        }

        [Fact]
        public void TopologicalSort_ArcToAFinishedNode_IsNotACircuit()
        {
            var result = TopologicalSorter.TopologicalSort(new[] { "a", "b", "c" }, Arcs(("a", "b"), ("c", "b")));

            Assert.False(result.HasCycle);
        }

        [Fact]
        public void TopologicalSort_EmptyGraph_ReturnsAnEmptyOrder()
        {
            var result = TopologicalSorter.TopologicalSort(Array.Empty<string>(), Arcs());

            Assert.False(result.HasCycle);
            Assert.Empty(result.Order);
        }

        [Fact]
        public void TopologicalSort_DuplicateNode_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => TopologicalSorter.TopologicalSort(new[] { "a", "a" }, Arcs()));
        }

        [Fact]
        public void TopologicalSort_ArcToAnUnknownNode_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => TopologicalSorter.TopologicalSort(new[] { "a" }, Arcs(("a", "x"))));
        }

        [Fact]
        public void TopologicalSort_ReadsTheArcsOfEveryNodeOnlyOnce()
        {
            var calls = new Dictionary<string, int>();
            var arcs = Arcs(("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"), ("d", "e"));

            TopologicalSorter.TopologicalSort(new[] { "a", "b", "c", "d", "e" }, node =>
            {
                calls[node] = calls.GetValueOrDefault(node) + 1;
                return arcs(node);
            });

            Assert.All(calls.Values, count => Assert.Equal(1, count));
            Assert.Equal(5, calls.Count);
        }

        [Fact]
        public void TopologicalSort_RandomGraphs_AgreeWithAnIndependentAcyclicityTest()
        {
            var random = new Random(20261126);

            for (var round = 0; round < 500; round++)
            {
                var count = random.Next(1, 9);
                var nodes = Enumerable.Range(0, count).ToList();
                var arcs = new List<(int From, int To)>();

                foreach (var from in nodes)
                {
                    foreach (var to in nodes)
                    {
                        if (random.NextDouble() < 0.2)
                        {
                            arcs.Add((from, to));
                        }
                    }
                }

                var lookup = arcs.ToLookup(a => a.From, a => a.To);
                var result = TopologicalSorter.TopologicalSort(nodes, node => lookup[node].ToList());
                var acyclic = IsAcyclicByRemovingSources(nodes, arcs);

                Assert.Equal(acyclic, !result.HasCycle);

                if (acyclic)
                {
                    var position = result.Order.Select((node, index) => (node, index)).ToDictionary(p => p.node, p => p.index);
                    Assert.Equal(count, position.Count);
                    Assert.All(arcs, arc => Assert.True(position[arc.From] < position[arc.To]));
                }
                else
                {
                    var cycle = result.Cycle!;
                    Assert.Equal(cycle[0], cycle[^1]);
                    Assert.All(cycle.Zip(cycle.Skip(1)), step => Assert.Contains((step.First, step.Second), arcs));
                }
            }
        }

        private static bool IsAcyclicByRemovingSources(List<int> nodes, List<(int From, int To)> arcs)
        {
            var remaining = new HashSet<int>(nodes);
            var removed = true;

            while (removed)
            {
                removed = false;

                foreach (var node in remaining.ToList())
                {
                    if (!arcs.Any(a => a.To == node && remaining.Contains(a.From)))
                    {
                        remaining.Remove(node);
                        removed = true;
                    }
                }
            }

            return remaining.Count == 0;
        }
    }
}
