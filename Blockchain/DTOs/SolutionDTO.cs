using Nethereum.ABI.FunctionEncoding.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;


namespace MathChain.Blockchain.DTOs
{
    public class SolutionDTO
    {
        [Parameter("address", "wallet", 1)]
        public /*required*/ string Wallet { get; set; }

        [Parameter("uint256", "problemId", 2)]
        public BigInteger ProblemId { get; set; }

        [Parameter("int256", "scaledSolution", 3)]
        public BigInteger ScaledSolution { get; set; }

        [Parameter("bool", "isPaid", 4)]
        public bool IsPaid { get; set; }

        [Parameter("uint256", "timestamp", 5)]
        public BigInteger Timestamp { get; set; }

    }
}
