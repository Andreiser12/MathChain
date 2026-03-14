using MathChain.Domain.Interfaces;
using Nethereum.Util;
using Nethereum.Web3;
using System;
using System.Threading.Tasks;
using Nethereum.Web3.Accounts;
using System.IO;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using MathChain.Blockchain.DTOs;

namespace MathChain.Blockchain.Services
{
    public class BlockchainService : IBlockchainService
    {
        private readonly Web3 _web3;
        private readonly BlockchainConfig _config;
        private readonly string _abi;

        public BlockchainService(BlockchainConfig config)
        {
            _config = config;
            var account = new Account(config.PrivateKey);
            _web3 = new Web3(account, config.RpcUrl);
            _abi = File.ReadAllText(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Contracts", "MathChainABI.json"));
        }

        public async Task<decimal> GetWalletBalanceAsync(string walletAddress)
        {
            var balanceWei = await _web3.Eth.GetBalance.SendRequestAsync(walletAddress);
            decimal balanceEth = Web3.Convert.FromWei(balanceWei.Value);
            return balanceEth;
        }

        public async Task<string> PayForSolutionAsync(Guid problemId, string walletAddress)
        {
            var contract = _web3.Eth.GetContract(_abi, _config.ContractAddress);
            var function = contract.GetFunction("payForSolution");
            var problemIdInt = new BigInteger(Math.Abs(problemId.GetHashCode()));

            var transactionHash = await function.SendTransactionAsync(
                from: walletAddress,
                gas: new HexBigInteger(100000),
                value: new HexBigInteger(500000000000000),
                functionInput: problemIdInt
            );

            return transactionHash;
        }

        public async Task<string> SubmitSolutionAsync(Guid problemId, string walletAddress, double solution)
        {
            var contract = _web3.Eth.GetContract(_abi, _config.ContractAddress);
            var function = contract.GetFunction("submitSolution");
            var problemIdInt = new BigInteger(Math.Abs(problemId.GetHashCode()));
            var scaledSolution = new BigInteger((int)(solution * 1000));

            var transactionHash = await function.SendTransactionAsync(
                from: walletAddress,
                gas: new HexBigInteger(100000),
                value: new HexBigInteger(0),
                functionInput: new object[] { problemIdInt, scaledSolution }
            );

            return transactionHash;
        }

        public async Task<bool> VerifySolutionAsync(Guid problemId, double solution)
        {
            const int epsilon = 10;

            var contract = _web3.Eth.GetContract(_abi, _config.ContractAddress);
            var function = contract.GetFunction("solutions");
            var problemIdInt = new BigInteger(Math.Abs(problemId.GetHashCode()));
            var userSolution = new BigInteger((int)(solution * 1000));

            var result = await function.CallDeserializingToObjectAsync<SolutionDTO>(
                problemIdInt
                );

            return BigInteger.Abs(result.ScaledSolution - userSolution) <= epsilon;
        }
    }
}
