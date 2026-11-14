using System.Numerics;
using MathChain.Blockchain.Certificates;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;

namespace MathChain.Tests.Blockchain
{
    public class CertificateEncodingTests
    {
        private const string Student = "0x52908400098527886E0F7030069857D2E4169EE7";
        private const string Teacher = "0x8617E340B3D01FA5F11F306F4090FD50E238070D";
        private const string Issuer = "0xde709f2102306220921060314715629080e2fb77";
        private const string Salt = "0x1111111111111111111111111111111111111111111111111111111111111111";
        private const string SubjectHex = "0x00000000000000000000000000000000b3ba5880baaa41728f9e2bb240efddfd";
        private const string ClassDataHash = "0xc7d2dd8bcf611a33b0f238f7dc42a3156865e6ce248e34132552810227926bfc";
        private const string ChapterDataHash = "0xa01216faf13ff1014d5a2855a3f1f583fab79fc32bb0fb4e04ea2b33a0ee239d";
        private const string IssuerRoleHex = "0x114e74f6ea3bd819998f78687bfcb11b140da08e9b7d222fa9c1f1ba1f2aa122";
        private const string GrantRoleCallData = "0x2f2ff15d114e74f6ea3bd819998f78687bfcb11b140da08e9b7d222fa9c1f1ba1f2aa122000000000000000000000000de709f2102306220921060314715629080e2fb77";
        private const string IssueBatchCallData = "0x768adf580000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000b3ba5880baaa41728f9e2bb240efddfd00000000000000000000000000000000000000000000000000000000000000e0000000000000000000000000000000000000000000000000000000000000000200000000000000000000000052908400098527886e0f7030069857d2e4169ee70000000000000000000000008617e340b3d01fa5f11f306f4090fd50e238070d0000000000000000000000000000000000000000000000000000000000000002c7d2dd8bcf611a33b0f238f7dc42a3156865e6ce248e34132552810227926bfc45318970bfff215a328f56895f3a97d4f276a44c24c135c12c37867a1f667b8a";
        private const string RevokeCallData = "0x20c5429b0000000000000000000000000000000000000000000000000000000000000007";
        private const string FindCertificateCallData = "0x5463451800000000000000000000000052908400098527886e0f7030069857d2e4169ee7000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000b3ba5880baaa41728f9e2bb240efddfd";
        private const string GetCertificateCallData = "0x51640fee0000000000000000000000000000000000000000000000000000000000000007";
        private const string GetCertificateResult = "0x00000000000000000000000052908400098527886e0f7030069857d2e4169ee7000000000000000000000000000000000000000000000000000000006af82f9000000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000001000000000000000000000000de709f2102306220921060314715629080e2fb7700000000000000000000000000000000b3ba5880baaa41728f9e2bb240efddfdc7d2dd8bcf611a33b0f238f7dc42a3156865e6ce248e34132552810227926bfc";
        private const string CertificateIssuedTopic = "0xf7cd537a75849d589ff4830e697d0ca628976ca041e8f8407d2ca6d14338f1ac";

        private static readonly Guid ClassId = Guid.Parse("b3ba5880-baaa-4172-8f9e-2bb240efddfd");

        private static CertificateCommitment ClassCommitment()
        {
            return new CertificateCommitment
            {
                StudentAddress = Student,
                Kind = CertificateKind.Class,
                SubjectId = ClassId,
                Grade = 9,
                TeacherAddress = Teacher,
                Salt = Salt
            };
        }

        private static string CallData(FunctionMessage message)
        {
            return CertificateEncoding.ToHex(message switch
            {
                IssueBatchFunction issueBatch => issueBatch.GetCallData(),
                RevokeFunction revoke => revoke.GetCallData(),
                FindCertificateFunction findCertificate => findCertificate.GetCallData(),
                GetCertificateFunction getCertificate => getCertificate.GetCallData(),
                _ => throw new ArgumentOutOfRangeException(nameof(message))
            });
        }

        [Fact]
        public void ToSubjectBytes_KeepsTheWholeIdentifierAndRoundTrips()
        {
            var bytes = CertificateEncoding.ToSubjectBytes(ClassId);

            Assert.Equal(SubjectHex, CertificateEncoding.ToHex(bytes));
            Assert.Equal(ClassId, CertificateEncoding.ToSubjectGuid(bytes));
            Assert.NotEqual(bytes, CertificateEncoding.ToSubjectBytes(Guid.Parse("b3ba5880-baaa-4172-8f9e-2bb240efddfe")));
        }

        [Fact]
        public void ComputeDataHash_MatchesTheSolidityEncoding()
        {
            Assert.Equal(ClassDataHash, CertificateEncoding.ComputeDataHash(ClassCommitment()));
        }

        [Fact]
        public void ComputeDataHash_ChapterCertificateWithoutTeacher_MatchesTheSolidityEncoding()
        {
            var commitment = ClassCommitment();
            commitment.Kind = CertificateKind.Chapter;
            commitment.Grade = 0;
            commitment.TeacherAddress = "0x0000000000000000000000000000000000000000";
            commitment.StudentAddress = Student.ToLowerInvariant();

            Assert.Equal(ChapterDataHash, CertificateEncoding.ComputeDataHash(commitment));
        }

        [Fact]
        public void ComputeDataHash_ChangesWhenAnyFieldChanges()
        {
            var changes = new Action<CertificateCommitment>[]
            {
                c => c.Grade = 10,
                c => c.StudentAddress = Teacher,
                c => c.TeacherAddress = Student,
                c => c.Kind = CertificateKind.Chapter,
                c => c.SubjectId = Guid.NewGuid(),
                c => c.Salt = "0x" + new string('2', 64)
            };

            foreach (var change in changes)
            {
                var commitment = ClassCommitment();
                change(commitment);

                Assert.NotEqual(ClassDataHash, CertificateEncoding.ComputeDataHash(commitment));
            }
        }

        [Theory]
        [InlineData("0x1234", 9, Student)]
        [InlineData(Salt, -1, Student)]
        [InlineData(Salt, 65536, Student)]
        [InlineData(Salt, 9, "not-an-address")]
        public void ComputeDataHash_InvalidInput_Throws(string salt, int grade, string student)
        {
            var commitment = ClassCommitment();
            commitment.Salt = salt;
            commitment.Grade = grade;
            commitment.StudentAddress = student;

            Assert.Throws<ArgumentException>(() => CertificateEncoding.ComputeDataHash(commitment));
        }

        [Fact]
        public void CreateSalt_ReturnsDifferent32ByteValues()
        {
            var salts = Enumerable.Range(0, 50).Select(_ => CertificateEncoding.CreateSalt()).ToList();

            Assert.All(salts, salt => Assert.True(CertificateEncoding.IsBytes32(salt)));
            Assert.Equal(50, salts.Distinct().Count());
        }

        [Fact]
        public void IssuerRole_MatchesTheContractConstant()
        {
            Assert.Equal(IssuerRoleHex, CertificateEncoding.ToHex(CertificateEncoding.IssuerRole));
        }

        [Fact]
        public void BuildGrantIssuerRoleCallData_MatchesTheContractAbi()
        {
            Assert.Equal(GrantRoleCallData, CertificateEncoding.BuildGrantIssuerRoleCallData(Issuer));
        }

        [Fact]
        public void IssueBatchFunction_EncodesLikeTheContractAbi()
        {
            var message = new IssueBatchFunction
            {
                Students = new List<string> { Student, Teacher },
                Kind = (byte)CertificateKind.Class,
                SubjectId = CertificateEncoding.ToSubjectBytes(ClassId),
                DataHashes = new List<byte[]> { ClassDataHash.HexToByteArray(), "0x45318970bfff215a328f56895f3a97d4f276a44c24c135c12c37867a1f667b8a".HexToByteArray() }
            };

            Assert.Equal(IssueBatchCallData, CallData(message));
        }

        [Fact]
        public void RevokeFindAndGetFunctions_EncodeLikeTheContractAbi()
        {
            Assert.Equal(RevokeCallData, CallData(new RevokeFunction { TokenId = new BigInteger(7) }));
            Assert.Equal(GetCertificateCallData, CallData(new GetCertificateFunction { TokenId = new BigInteger(7) }));
            Assert.Equal(FindCertificateCallData, CallData(new FindCertificateFunction
            {
                Student = Student,
                Kind = (byte)CertificateKind.Chapter,
                SubjectId = CertificateEncoding.ToSubjectBytes(ClassId)
            }));
        }

        [Fact]
        public void GetCertificateOutput_DecodesTheContractStruct()
        {
            var certificate = new FunctionCallDecoder().DecodeFunctionOutput<GetCertificateOutput>(GetCertificateResult).Certificate;

            Assert.Equal(Student, certificate.Student, ignoreCase: true);
            Assert.Equal(1794650000UL, certificate.IssuedAt);
            Assert.Equal((byte)CertificateKind.Chapter, certificate.Kind);
            Assert.True(certificate.Revoked);
            Assert.Equal(Issuer, certificate.Issuer, ignoreCase: true);
            Assert.Equal(ClassId, CertificateEncoding.ToSubjectGuid(certificate.SubjectId));
            Assert.Equal(ClassDataHash, CertificateEncoding.ToHex(certificate.DataHash));
        }

        [Fact]
        public void CertificateIssuedEvent_HasTheContractEventSignature()
        {
            Assert.Equal(CertificateIssuedTopic, "0x" + EventExtensions.GetEventABI<CertificateIssuedEvent>().Sha3Signature);
        }

        [Theory]
        [InlineData("AccessControlUnauthorizedAccount(address,bytes32)", "0xe2517d3f")]
        [InlineData("CertificateAlreadyIssued(uint256)", "0x62d90dde")]
        [InlineData("CertificateAlreadyRevoked(uint256)", "0xe3092565")]
        public void ErrorSelector_MatchesTheContractErrors(string signature, string expectedSelector)
        {
            Assert.Equal(expectedSelector, CertificateEncoding.ToHex(CertificateEncoding.ErrorSelector(signature)));
        }
    }
}
