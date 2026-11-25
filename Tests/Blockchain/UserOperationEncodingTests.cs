using System.Numerics;
using MathChain.Blockchain.Accounts;
using MathChain.Blockchain.Certificates;
using MathChain.Domain.Enums;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;

namespace MathChain.Tests.Blockchain
{
    public class UserOperationEncodingTests
    {
        private const string EntryPoint = "0x5FbDB2315678afecb367f032d93F642f64180aa3";
        private const string Factory = "0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512";
        private const string Claims = "0xCf7Ed3AccA5a467e9e704C703E8D87F634fB0Fc9";
        private const string Paymaster = "0xDc64a140Aa3E981100a9becA4E685f962f0cF6C9";
        private const string Certificates = "0x9fE46736679d2D9a65F0992F2272dE9f3c7fa6e0";
        private const string Platform = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8";
        private const string PlatformKey = "0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d";
        private const string OwnerKey = "0x2222222222222222222222222222222222222222222222222222222222222222";
        private const string Owner = "0x1563915e194D8CfBA1943570603F7606A3115508";
        private const string Sender = "0x5F6d3e6600D8e1ccDE3AEE58Eb86A7EAf947E14c";
        private const string Subject = "0x0000000000000000000000000000000070000000000000000000000000000000";
        private const string DataHash = "0x8f54f1c2d0eb5771cd5bf67a6689fcd6eed9444d91a39e5ef32a9b4ae5ca14ff";
        private const string VoucherDigest = "0xaaf766ee8d0dc3990b940abf336be470bcaae51067479631b98d72371ce591ce";
        private const string VoucherSignature = "0x390e3f814a12843ba34c6b843bf11fb5ae9b16457e575b0578ad605de47fd7c80b01c5ea285f0d9e3a91fa3932560c76344ef1ad364b480ac944e1f60c550ab41b";
        private const string Inner = "0xde3fd6de000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000700000000000000000000000000000008f54f1c2d0eb5771cd5bf67a6689fcd6eed9444d91a39e5ef32a9b4ae5ca14ff000000000000000000000000000000000000000000000000000000006b49d20000000000000000000000000000000000000000000000000000000000000000a00000000000000000000000000000000000000000000000000000000000000041390e3f814a12843ba34c6b843bf11fb5ae9b16457e575b0578ad605de47fd7c80b01c5ea285f0d9e3a91fa3932560c76344ef1ad364b480ac944e1f60c550ab41b00000000000000000000000000000000000000000000000000000000000000";
        private const string CallData = "0xb61d27f6000000000000000000000000cf7ed3acca5a467e9e704c703e8d87f634fb0fc9000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000600000000000000000000000000000000000000000000000000000000000000124de3fd6de000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000700000000000000000000000000000008f54f1c2d0eb5771cd5bf67a6689fcd6eed9444d91a39e5ef32a9b4ae5ca14ff000000000000000000000000000000000000000000000000000000006b49d20000000000000000000000000000000000000000000000000000000000000000a00000000000000000000000000000000000000000000000000000000000000041390e3f814a12843ba34c6b843bf11fb5ae9b16457e575b0578ad605de47fd7c80b01c5ea285f0d9e3a91fa3932560c76344ef1ad364b480ac944e1f60c550ab41b0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        private const string InitCode = "0xe7f1725e7734ce288f8367e1bb143e90bb3f05125fbfb9cf0000000000000000000000001563915e194d8cfba1943570603f7606a31155080000000000000000000000000000000000000000000000000000000000000000";
        private const string SponsorHash = "0x1d4ed1df357cd0e03ae152f7a625067bd636c00508edab7c2a8120512cd82cf1";
        private const string SponsorSignature = "0xbf44288e029d75810f01c6d5546e67325deadb6d86f8c542ebb2d4042c3be91377de252fcdc33e968efbaf517bbfe947ae63a519bdf85860def6a90be0120e191b";
        private const string PaymasterAndData = "0xdc64a140aa3e981100a9beca4e685f962f0cf6c9000000000000000000000000000249f000000000000000000000000000000000000000000000000000000000000000000000000000000000000000006b49d4580000000000000000000000000000000000000000000000000000000000000000bf44288e029d75810f01c6d5546e67325deadb6d86f8c542ebb2d4042c3be91377de252fcdc33e968efbaf517bbfe947ae63a519bdf85860def6a90be0120e191b";
        private const string UserOpHash = "0xd34b3f8c8c0888285a4a2b26fae2a9ec091deeaa7ec98b4b134fa19cc94b82ed";
        private const string OwnerSignature = "0x6fa84a4b6b9745e24aad1fc51f0ffc38fd896b27449101560cf61b84c847899639d91868d6c0fd778bdb6e523a1c246adf400962dc39d6db8c3edb777a4e40091b";
        private const string HandleOps = "0x765e827f000000000000000000000000000000000000000000000000000000000000004000000000000000000000000070997970c51812dc3a010c7d01b50e0d17dc79c8000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000200000000000000000000000005f6d3e6600d8e1ccde3aee58eb86a7eaf947e14c0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000012000000000000000000000000000000000000000000000000000000000000001a00000000000000000000000000007a120000000000000000000000000000493e0000000000000000000000000000000000000000000000000000000000000c350000000000000000000000000000f4240000000000000000000000000001e848000000000000000000000000000000000000000000000000000000000000003a000000000000000000000000000000000000000000000000000000000000004800000000000000000000000000000000000000000000000000000000000000058e7f1725e7734ce288f8367e1bb143e90bb3f05125fbfb9cf0000000000000000000000001563915e194d8cfba1943570603f7606a31155080000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001c4b61d27f6000000000000000000000000cf7ed3acca5a467e9e704c703e8d87f634fb0fc9000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000600000000000000000000000000000000000000000000000000000000000000124de3fd6de000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000700000000000000000000000000000008f54f1c2d0eb5771cd5bf67a6689fcd6eed9444d91a39e5ef32a9b4ae5ca14ff000000000000000000000000000000000000000000000000000000006b49d20000000000000000000000000000000000000000000000000000000000000000a00000000000000000000000000000000000000000000000000000000000000041390e3f814a12843ba34c6b843bf11fb5ae9b16457e575b0578ad605de47fd7c80b01c5ea285f0d9e3a91fa3932560c76344ef1ad364b480ac944e1f60c550ab41b00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000b5dc64a140aa3e981100a9beca4e685f962f0cf6c9000000000000000000000000000249f000000000000000000000000000000000000000000000000000000000000000000000000000000000000000006b49d4580000000000000000000000000000000000000000000000000000000000000000bf44288e029d75810f01c6d5546e67325deadb6d86f8c542ebb2d4042c3be91377de252fcdc33e968efbaf517bbfe947ae63a519bdf85860def6a90be0120e191b000000000000000000000000000000000000000000000000000000000000000000000000000000000000416fa84a4b6b9745e24aad1fc51f0ffc38fd896b27449101560cf61b84c847899639d91868d6c0fd778bdb6e523a1c246adf400962dc39d6db8c3edb777a4e40091b00000000000000000000000000000000000000000000000000000000000000";
        private const string CreateAccount = "0x5fbfb9cf0000000000000000000000001563915e194d8cfba1943570603f7606a31155080000000000000000000000000000000000000000000000000000000000000000";
        private const string AccountAddressCall = "0x58df3a3d0000000000000000000000001563915e194d8cfba1943570603f7606a31155080000000000000000000000000000000000000000000000000000000000000000";
        private const string GrantRole = "0x2f2ff15d114e74f6ea3bd819998f78687bfcb11b140da08e9b7d222fa9c1f1ba1f2aa122000000000000000000000000cf7ed3acca5a467e9e704c703e8d87f634fb0fc9";
        private const string Deposit = "0xd0e30db0";
        private const string UserOperationEventTopic = "0x49628fd1471006c1482da88028e9ce4dbb080b815c9b0344d39e5a8e6ec1419f";
        private const string RevertReasonTopic = "0x1c4fada7374c0a9ee8841fc38afe82932dc0f8e69012e927f061a8bae611a201";
        private const long ChainId = 31337;
        private const long Deadline = 1800000000;
        private const long ValidUntil = 1800000600;

        private static string Hex(byte[] bytes) => CertificateEncoding.ToHex(bytes);

        private static PackedUserOperation Operation(bool withPaymasterSignature)
        {
            return new PackedUserOperation
            {
                Sender = Sender,
                Nonce = BigInteger.Zero,
                InitCode = InitCode.HexToByteArray(),
                CallData = CallData.HexToByteArray(),
                AccountGasLimits = UserOperationEncoding.PackUints(500000, 300000),
                PreVerificationGas = 50000,
                GasFees = UserOperationEncoding.PackUints(1000000, 2000000),
                PaymasterAndData = UserOperationEncoding.BuildPaymasterData(Paymaster, 150000, 0, ValidUntil, 0,
                    withPaymasterSignature ? SponsorSignature.HexToByteArray() : new byte[65])
            };
        }

        [Fact]
        public void ComputeVoucherDigest_MatchesTheClaimsContract()
        {
            var digest = UserOperationEncoding.ComputeVoucherDigest(Claims, ChainId, Sender, CertificateKind.Chapter,
                Subject.HexToByteArray(), DataHash.HexToByteArray(), Deadline);

            Assert.Equal(VoucherDigest, Hex(digest));
            Assert.Equal(VoucherSignature, Hex(UserOperationEncoding.SignDigest(new EthECKey(PlatformKey), digest)));
        }

        [Fact]
        public void ClaimAndExecuteCalls_MatchTheContractAbi()
        {
            var claim = new ClaimCertificateFunction
            {
                Kind = (byte)CertificateKind.Chapter,
                SubjectId = Subject.HexToByteArray(),
                DataHash = DataHash.HexToByteArray(),
                Deadline = Deadline,
                Signature = VoucherSignature.HexToByteArray()
            };
            var execute = new ExecuteFunction { Destination = Claims, Value = 0, Data = claim.GetCallData() };

            Assert.Equal(Inner, Hex(claim.GetCallData()));
            Assert.Equal(CallData, Hex(execute.GetCallData()));
        }

        [Fact]
        public void InitCode_AndFactoryCalls_MatchTheFactory()
        {
            var create = new CreateAccountFunction { Owner = Owner, Salt = 0 };

            Assert.Equal(CreateAccount, Hex(create.GetCallData()));
            Assert.Equal(InitCode, Hex(UserOperationEncoding.BuildInitCode(Factory, create.GetCallData())));
            Assert.Equal(AccountAddressCall, Hex(new AccountAddressFunction { Owner = Owner, Salt = 0 }.GetCallData()));
        }

        [Fact]
        public void ComputeSponsorHash_MatchesThePaymaster_AndIgnoresThePaymasterSignature()
        {
            Assert.Equal(SponsorHash, Hex(UserOperationEncoding.ComputeSponsorHash(Operation(false), Paymaster, ChainId, ValidUntil, 0)));
            Assert.Equal(SponsorHash, Hex(UserOperationEncoding.ComputeSponsorHash(Operation(true), Paymaster, ChainId, ValidUntil, 0)));
            Assert.NotEqual(SponsorHash, Hex(UserOperationEncoding.ComputeSponsorHash(Operation(true), Paymaster, ChainId, ValidUntil + 1, 0)));
            Assert.Equal(SponsorSignature, Hex(UserOperationEncoding.SignPersonal(new EthECKey(PlatformKey), SponsorHash.HexToByteArray())));
        }

        [Fact]
        public void PaymasterData_MatchesTheSignedLayout()
        {
            Assert.Equal(PaymasterAndData, Hex(Operation(true).PaymasterAndData));
        }

        [Fact]
        public void ComputeUserOpHash_MatchesTheEntryPoint()
        {
            var operation = Operation(true);

            Assert.Equal(UserOpHash, Hex(UserOperationEncoding.ComputeUserOpHash(operation, EntryPoint, ChainId)));
            Assert.NotEqual(UserOpHash, Hex(UserOperationEncoding.ComputeUserOpHash(operation, EntryPoint, 1)));
            Assert.Equal(OwnerSignature, Hex(UserOperationEncoding.SignPersonal(new EthECKey(OwnerKey), UserOpHash.HexToByteArray())));
        }

        [Fact]
        public void HandleOps_EncodesThePackedOperation()
        {
            var operation = Operation(true);
            operation.Signature = OwnerSignature.HexToByteArray();

            var call = new HandleOpsFunction { Ops = new List<PackedUserOperation> { operation }, Beneficiary = Platform };

            Assert.Equal(HandleOps, Hex(call.GetCallData()));
        }

        [Fact]
        public void AdminCalls_MatchTheContracts()
        {
            Assert.Equal(Deposit, Hex(new PaymasterDepositFunction().GetCallData()));
            Assert.Equal(GrantRole, Hex(new GrantRoleFunction { Role = CertificateEncoding.IssuerRole, Account = Claims }.GetCallData()));
        }

        [Fact]
        public void Events_HaveTheEntryPointSignatures()
        {
            Assert.Equal(UserOperationEventTopic, "0x" + EventExtensions.GetEventABI<UserOperationEvent>().Sha3Signature);
            Assert.Equal(RevertReasonTopic, "0x" + EventExtensions.GetEventABI<UserOperationRevertReasonEvent>().Sha3Signature);
        }

        [Fact]
        public void PackUints_RejectsValuesThatDoNotFit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UserOperationEncoding.PackUints(BigInteger.One << 128, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => UserOperationEncoding.PackUints(0, -1));
        }
    }
}
