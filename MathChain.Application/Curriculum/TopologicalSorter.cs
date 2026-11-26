namespace MathChain.Application.Curriculum
{
    public sealed class TopologicalSortResult<TNode> where TNode : notnull
    {
        private TopologicalSortResult(IReadOnlyList<TNode> order, IReadOnlyList<TNode>? cycle,
            IReadOnlyDictionary<TNode, int> discoveryTimes, IReadOnlyDictionary<TNode, int> finishTimes)
        {
            Order = order;
            Cycle = cycle;
            DiscoveryTimes = discoveryTimes;
            FinishTimes = finishTimes;
        }

        public IReadOnlyList<TNode> Order { get; }
        public IReadOnlyList<TNode>? Cycle { get; }
        public bool HasCycle => Cycle != null;
        public IReadOnlyDictionary<TNode, int> DiscoveryTimes { get; }
        public IReadOnlyDictionary<TNode, int> FinishTimes { get; }

        internal static TopologicalSortResult<TNode> Sorted(IReadOnlyList<TNode> order,
            IReadOnlyDictionary<TNode, int> discoveryTimes, IReadOnlyDictionary<TNode, int> finishTimes)
        {
            return new TopologicalSortResult<TNode>(order, null, discoveryTimes, finishTimes);
        }

        internal static TopologicalSortResult<TNode> WithCycle(IReadOnlyList<TNode> cycle,
            IReadOnlyDictionary<TNode, int> discoveryTimes, IReadOnlyDictionary<TNode, int> finishTimes)
        {
            return new TopologicalSortResult<TNode>(Array.Empty<TNode>(), cycle, discoveryTimes, finishTimes);
        }
    }

    public static class TopologicalSorter
    {
        public static TopologicalSortResult<TNode> TopologicalSort<TNode>(IReadOnlyList<TNode> nodes,
            Func<TNode, IReadOnlyList<TNode>> successors) where TNode : notnull
        {
            ArgumentNullException.ThrowIfNull(nodes);
            ArgumentNullException.ThrowIfNull(successors);

            var unvisited = new HashSet<TNode>(nodes);

            if (unvisited.Count != nodes.Count)
            {
                throw new ArgumentException("Every node must appear only once.", nameof(nodes));
            }

            var known = new HashSet<TNode>(nodes);
            var open = new Stack<TNode>();
            var isOpen = new HashSet<TNode>();
            var parent = new Dictionary<TNode, TNode>();
            var discoveryTimes = new Dictionary<TNode, int>();
            var finishTimes = new Dictionary<TNode, int>();
            var arcs = new Dictionary<TNode, IReadOnlyList<TNode>>();
            var nextArc = new Dictionary<TNode, int>();
            var order = new LinkedList<TNode>();
            var time = 0;

            foreach (var source in nodes)
            {
                if (!unvisited.Remove(source))
                {
                    continue;
                }

                Open(source);

                while (open.Count > 0)
                {
                    var x = open.Peek();
                    var targets = arcs[x];
                    var index = nextArc[x];

                    if (index < targets.Count)
                    {
                        nextArc[x] = index + 1;
                        var y = targets[index];

                        if (!known.Contains(y))
                        {
                            throw new ArgumentException($"The arc ({x}, {y}) points to a node that is not in the graph.", nameof(successors));
                        }

                        if (isOpen.Contains(y))
                        {
                            return TopologicalSortResult<TNode>.WithCycle(BuildCycle(x, y, parent), discoveryTimes, finishTimes);
                        }

                        if (unvisited.Remove(y))
                        {
                            parent[y] = x;
                            Open(y);
                        }
                    }
                    else
                    {
                        open.Pop();
                        isOpen.Remove(x);
                        finishTimes[x] = ++time;
                        order.AddFirst(x);
                    }
                }
            }

            return TopologicalSortResult<TNode>.Sorted(order.ToList(), discoveryTimes, finishTimes);

            void Open(TNode node)
            {
                discoveryTimes[node] = ++time;
                arcs[node] = successors(node) ?? Array.Empty<TNode>();
                nextArc[node] = 0;
                open.Push(node);
                isOpen.Add(node);
            }
        }

        private static List<TNode> BuildCycle<TNode>(TNode last, TNode first, IReadOnlyDictionary<TNode, TNode> parent) where TNode : notnull
        {
            var comparer = EqualityComparer<TNode>.Default;
            var path = new List<TNode> { last };
            var current = last;

            while (!comparer.Equals(current, first))
            {
                current = parent[current];
                path.Add(current);
            }

            path.Reverse();
            path.Add(first);

            return path;
        }
    }
}
