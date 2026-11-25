const { expect } = require("chai");
const { ethers } = require("hardhat");

const Kind = { Class: 0, Chapter: 1 };
const coder = ethers.AbiCoder.defaultAbiCoder();
const VERIFICATION_GAS = 600000n;
const CALL_GAS = 400000n;
const PAYMASTER_VERIFICATION_GAS = 150000n;
const PAYMASTER_POSTOP_GAS = 0n;
const PRE_VERIFICATION_GAS = 60000n;
const MAX_COST = ethers.parseEther("0.05");

function packUints(high, low) {
  return ethers.toBeHex((BigInt(high) << 128n) | BigInt(low), 32);
}

function subjectId(guid) {
  return ethers.zeroPadValue("0x" + guid.replace(/-/g, ""), 32);
}

describe("MathChain smart accounts", function () {
  const chapter = subjectId("70000000-0000-0000-0000-00000000c001");
  const otherChapter = subjectId("70000000-0000-0000-0000-00000000c002");
  const dataHash = ethers.id("chapter-certificate-data");

  let entryPoint, factory, paymaster, claims, certificates;
  let admin, platform, outsider, beneficiary, owner, otherOwner, chainId;

  beforeEach(async function () {
    [admin, platform, outsider, beneficiary] = await ethers.getSigners();
    owner = ethers.Wallet.createRandom();
    otherOwner = ethers.Wallet.createRandom();
    chainId = (await ethers.provider.getNetwork()).chainId;

    entryPoint = await (await ethers.getContractFactory("EntryPoint")).deploy();
    factory = await (await ethers.getContractFactory("MathChainAccountFactory")).deploy(await entryPoint.getAddress());
    certificates = await (await ethers.getContractFactory("MathChainCertificates")).deploy(admin.address);
    claims = await (await ethers.getContractFactory("CertificateClaims"))
      .deploy(admin.address, await certificates.getAddress(), platform.address);
    paymaster = await (await ethers.getContractFactory("MathChainPaymaster", outsider)).deploy(
      await entryPoint.getAddress(), admin.address, platform.address, await claims.getAddress(),
      claims.interface.getFunction("claim").selector, MAX_COST);

    await certificates.connect(admin).grantRole(await certificates.ISSUER_ROLE(), await claims.getAddress());
    await paymaster.connect(admin).deposit({ value: ethers.parseEther("1") });
  });

  async function voucher({ student, kind = Kind.Chapter, subject = chapter, hash = dataHash, deadline = null, signer = platform }) {
    const latest = await ethers.provider.getBlock("latest");
    const until = deadline ?? BigInt(latest.timestamp + 3600);
    const signature = await signer.signTypedData(
      { name: "MathChain Certificate Claims", version: "1", chainId, verifyingContract: await claims.getAddress() },
      { Claim: [
        { name: "student", type: "address" }, { name: "kind", type: "uint8" }, { name: "subjectId", type: "bytes32" },
        { name: "dataHash", type: "bytes32" }, { name: "deadline", type: "uint256" }] },
      { student, kind, subjectId: subject, dataHash: hash, deadline: until });
    return { kind, subject, hash, deadline: until, signature };
  }

  function claimCall(v) {
    return claims.interface.encodeFunctionData("claim", [v.kind, v.subject, v.hash, v.deadline, v.signature]);
  }

  async function executeCall(target, value, data) {
    const account = await ethers.getContractAt("MathChainAccount", ethers.ZeroAddress);
    return account.interface.encodeFunctionData("execute", [target, value, data]);
  }

  async function buildOperation({ accountOwner = owner, callData, deployed = null, sponsor = platform, validUntil = 0, validAfter = 0,
    callGas = CALL_GAS, signWith = null, mutate = null }) {
    const sender = await factory.accountAddress(accountOwner.address, 0);
    const isDeployed = deployed ?? (await ethers.provider.getCode(sender)) !== "0x";
    const fees = await ethers.provider.getFeeData();
    const op = {
      sender,
      nonce: await entryPoint.getNonce(sender, 0),
      initCode: isDeployed ? "0x" : ethers.concat([await factory.getAddress(),
        factory.interface.encodeFunctionData("createAccount", [accountOwner.address, 0])]),
      callData,
      accountGasLimits: packUints(VERIFICATION_GAS, callGas),
      preVerificationGas: PRE_VERIFICATION_GAS,
      gasFees: packUints(fees.maxPriorityFeePerGas, fees.maxFeePerGas),
      paymasterAndData: "0x",
      signature: "0x"
    };
    const paymasterPrefix = ethers.concat([await paymaster.getAddress(), ethers.toBeHex(PAYMASTER_VERIFICATION_GAS, 16),
      ethers.toBeHex(PAYMASTER_POSTOP_GAS, 16), coder.encode(["uint48", "uint48"], [validUntil, validAfter])]);
    op.paymasterAndData = ethers.concat([paymasterPrefix, new Uint8Array(65)]);
    const sponsorHash = await paymaster.getHash(op, validUntil, validAfter);
    op.paymasterAndData = ethers.concat([paymasterPrefix, await sponsor.signMessage(ethers.getBytes(sponsorHash))]);
    if (mutate) {
      mutate(op);
    }
    const userOpHash = await entryPoint.getUserOpHash(op);
    op.signature = await (signWith ?? accountOwner).signMessage(ethers.getBytes(userOpHash));
    return { op, userOpHash };
  }

  async function claimOperation(options = {}) {
    const accountOwner = options.accountOwner ?? owner;
    const sender = await factory.accountAddress(accountOwner.address, 0);
    const v = await voucher({ student: options.voucherStudent ?? sender, ...(options.voucher ?? {}) });
    return buildOperation({ ...options, accountOwner, callData: await executeCall(await claims.getAddress(), 0, claimCall(v)) });
  }

  function handle(op) {
    return entryPoint.connect(platform).handleOps([op], beneficiary.address);
  }

  describe("factory and account", function () {
    it("creates the account at the predicted address only once", async function () {
      const predicted = await factory.accountAddress(owner.address, 0);
      expect(await ethers.provider.getCode(predicted)).to.equal("0x");

      await expect(factory.createAccount(owner.address, 0)).to.emit(factory, "AccountCreated").withArgs(predicted, owner.address, 0);
      await expect(factory.createAccount(owner.address, 0)).to.not.emit(factory, "AccountCreated");

      const account = await ethers.getContractAt("MathChainAccount", predicted);
      expect(await account.owner()).to.equal(owner.address);
      expect(await account.entryPoint()).to.equal(await entryPoint.getAddress());
      expect(await factory.accountAddress(otherOwner.address, 0)).to.not.equal(predicted);
      expect(await factory.accountAddress(owner.address, 1)).to.not.equal(predicted);
    });

    it("rejects the zero owner and the zero entry point", async function () {
      await expect(factory.createAccount(ethers.ZeroAddress, 0)).to.be.revertedWithCustomError(factory, "OwnerRequired");
      const factoryFactory = await ethers.getContractFactory("MathChainAccountFactory");
      await expect(factoryFactory.deploy(ethers.ZeroAddress)).to.be.revertedWithCustomError(factoryFactory, "EntryPointRequired");
    });

    it("cannot reinitialize the account implementation", async function () {
      const implementation = await ethers.getContractAt("MathChainAccount", await factory.accountImplementation());
      await expect(implementation.initialize(outsider.address)).to.be.revertedWithCustomError(implementation, "InvalidInitialization");
    });
  });

  describe("sponsored certificate claim", function () {
    it("deploys the account, claims the certificate and lets the paymaster pay the gas", async function () {
      const { op, userOpHash } = await claimOperation();
      const depositBefore = await entryPoint.balanceOf(await paymaster.getAddress());
      const beneficiaryBefore = await ethers.provider.getBalance(beneficiary.address);

      await expect(handle(op))
        .to.emit(entryPoint, "UserOperationEvent")
        .withArgs(userOpHash, op.sender, await paymaster.getAddress(), 0, true, (cost) => cost > 0n, (gas) => gas > 0n)
        .and.to.emit(claims, "CertificateClaimed")
        .withArgs(1, op.sender, Kind.Chapter, chapter);

      const certificate = await certificates.getCertificate(1);
      expect(certificate.student).to.equal(op.sender);
      expect(certificate.issuer).to.equal(await claims.getAddress());
      expect(certificate.kind).to.equal(BigInt(Kind.Chapter));
      expect(certificate.dataHash).to.equal(dataHash);
      expect(await certificates.ownerOf(1)).to.equal(op.sender);
      expect(await ethers.provider.getBalance(op.sender)).to.equal(0n);
      expect(await entryPoint.balanceOf(await paymaster.getAddress())).to.be.lessThan(depositBefore);
      expect(await ethers.provider.getBalance(beneficiary.address)).to.be.greaterThan(beneficiaryBefore);
    });

    it("claims a second certificate from the already deployed account", async function () {
      await handle((await claimOperation()).op);
      const { op } = await claimOperation({ voucher: { subject: otherChapter } });

      expect(op.initCode).to.equal("0x");
      expect(op.nonce).to.equal(1n);
      await handle(op);
      expect((await certificates.getCertificate(2)).subjectId).to.equal(otherChapter);
    });

    it("rejects a sponsorship signed by someone else", async function () {
      const { op } = await claimOperation({ sponsor: outsider });

      await expect(handle(op)).to.be.revertedWithCustomError(entryPoint, "FailedOp").withArgs(0, "AA34 signature error");
    });

    it("rejects an operation signed by someone other than the owner", async function () {
      const { op } = await claimOperation({ signWith: otherOwner });

      await expect(handle(op)).to.be.revertedWithCustomError(entryPoint, "FailedOp").withArgs(0, "AA24 signature error");
    });

    it("rejects an operation changed after the sponsor signed it", async function () {
      const v = await voucher({ student: await factory.accountAddress(owner.address, 0), subject: otherChapter });
      const replacement = await executeCall(await claims.getAddress(), 0, claimCall(v));
      const { op } = await claimOperation({ mutate: (op) => { op.callData = replacement; } });

      await expect(handle(op)).to.be.revertedWithCustomError(entryPoint, "FailedOp").withArgs(0, "AA34 signature error");
    });

    it("rejects an expired sponsorship", async function () {
      const latest = await ethers.provider.getBlock("latest");
      const { op } = await claimOperation({ validUntil: latest.timestamp - 1 });

      await expect(handle(op)).to.be.revertedWithCustomError(entryPoint, "FailedOp").withArgs(0, "AA32 paymaster expired or not due");
    });

    for (const [label, makeCall] of [
      ["another contract", async () => executeCall(await certificates.getAddress(), 0, claimCall(await voucher({ student: outsider.address })))],
      ["another function of the claims contract", async () => executeCall(await claims.getAddress(), 0,
        claims.interface.encodeFunctionData("revoke", [1]))],
      ["a call that sends ether", async () => executeCall(await claims.getAddress(), 1,
        claimCall(await voucher({ student: await factory.accountAddress(owner.address, 0) })))],
      ["a batch call", async () => {
        const account = await ethers.getContractAt("MathChainAccount", ethers.ZeroAddress);
        return account.interface.encodeFunctionData("executeBatch", [[await claims.getAddress()], [], ["0x"]]);
      }],
      ["an empty call", async () => "0x"],
      ["an unknown function with the arguments of execute", async () => {
        const executeData = await executeCall(await claims.getAddress(), 0,
          claimCall(await voucher({ student: await factory.accountAddress(owner.address, 0) })));
        return ethers.concat(["0x12345678", ethers.dataSlice(executeData, 4)]);
      }]
    ]) {
      it(`does not sponsor ${label}`, async function () {
        const { op } = await buildOperation({ callData: await makeCall() });

        await expect(handle(op)).to.be.revertedWithCustomError(entryPoint, "FailedOpWithRevert")
          .withArgs(0, "AA33 reverted", paymaster.interface.encodeErrorResult("CallNotSponsored"));
      });
    }

    it("does not sponsor an operation that could cost more than the limit", async function () {
      await paymaster.connect(admin).setMaxCostPerOperation(1000n);
      const { op } = await claimOperation();

      await expect(handle(op)).to.be.revertedWithCustomError(entryPoint, "FailedOpWithRevert");
    });

    it("still charges the paymaster but issues nothing when the voucher is invalid", async function () {
      const { op, userOpHash } = await claimOperation({ voucher: { signer: outsider } });
      const depositBefore = await entryPoint.balanceOf(await paymaster.getAddress());

      await expect(handle(op))
        .to.emit(entryPoint, "UserOperationRevertReason")
        .withArgs(userOpHash, op.sender, 0, claims.interface.encodeErrorResult("InvalidVoucher"));
      expect(await certificates.totalIssued()).to.equal(0n);
      expect(await entryPoint.balanceOf(await paymaster.getAddress())).to.be.lessThan(depositBefore);
    });

    it("rejects a voucher written for another student", async function () {
      const { op, userOpHash } = await claimOperation({ voucherStudent: await factory.accountAddress(otherOwner.address, 0) });

      await expect(handle(op)).to.emit(entryPoint, "UserOperationRevertReason")
        .withArgs(userOpHash, op.sender, 0, claims.interface.encodeErrorResult("InvalidVoucher"));
    });
  });

  describe("certificate claims", function () {
    it("lets a wallet claim directly with its own voucher", async function () {
      const v = await voucher({ student: outsider.address });

      await expect(claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline, v.signature))
        .to.emit(certificates, "CertificateIssued");
      expect(await certificates.ownerOf(1)).to.equal(outsider.address);
    });

    it("rejects an expired voucher", async function () {
      const latest = await ethers.provider.getBlock("latest");
      const v = await voucher({ student: outsider.address, deadline: BigInt(latest.timestamp - 1) });

      await expect(claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline, v.signature))
        .to.be.revertedWithCustomError(claims, "VoucherExpired");
    });

    it("rejects using the same voucher twice, even after a revocation", async function () {
      const v = await voucher({ student: outsider.address });
      await claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline, v.signature);
      await claims.connect(platform).revoke(1);

      await expect(claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline, v.signature))
        .to.be.revertedWithCustomError(claims, "VoucherAlreadyUsed");
      expect(await claims.isVoucherUsed(await claims.voucherDigest(outsider.address, v.kind, v.subject, v.hash, v.deadline))).to.equal(true);
    });

    it("rejects a voucher whose data was changed", async function () {
      const v = await voucher({ student: outsider.address });

      await expect(claims.connect(outsider).claim(v.kind, otherChapter, v.hash, v.deadline, v.signature))
        .to.be.revertedWithCustomError(claims, "InvalidVoucher");
      await expect(claims.connect(outsider).claim(Kind.Class, v.subject, v.hash, v.deadline, v.signature))
        .to.be.revertedWithCustomError(claims, "InvalidVoucher");
      await expect(claims.connect(outsider).claim(v.kind, v.subject, ethers.id("other"), v.deadline, v.signature))
        .to.be.revertedWithCustomError(claims, "InvalidVoucher");
      await expect(claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline + 1n, v.signature))
        .to.be.revertedWithCustomError(claims, "InvalidVoucher");
    });

    it("rejects a malformed signature", async function () {
      const v = await voucher({ student: outsider.address });

      await expect(claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline, "0x1234"))
        .to.be.revertedWithCustomError(claims, "InvalidVoucher");
    });

    it("cannot issue without the issuer role", async function () {
      await certificates.connect(admin).revokeRole(await certificates.ISSUER_ROLE(), await claims.getAddress());
      const v = await voucher({ student: outsider.address });

      await expect(claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline, v.signature))
        .to.be.revertedWithCustomError(certificates, "AccessControlUnauthorizedAccount");
    });

    it("lets only the operator revoke and only the admin change the voucher signer", async function () {
      const v = await voucher({ student: outsider.address });
      await claims.connect(outsider).claim(v.kind, v.subject, v.hash, v.deadline, v.signature);

      await expect(claims.connect(outsider).revoke(1)).to.be.revertedWithCustomError(claims, "AccessControlUnauthorizedAccount");
      await expect(claims.connect(platform).setVoucherSigner(outsider.address))
        .to.be.revertedWithCustomError(claims, "AccessControlUnauthorizedAccount");
      await expect(claims.connect(admin).setVoucherSigner(ethers.ZeroAddress))
        .to.be.revertedWithCustomError(claims, "VoucherSignerRequired");
      await expect(claims.connect(admin).setVoucherSigner(outsider.address))
        .to.emit(claims, "VoucherSignerChanged").withArgs(outsider.address);
      await expect(claims.connect(platform).revoke(1)).to.emit(certificates, "CertificateRevoked");
    });

    it("rejects missing constructor values", async function () {
      const claimsFactory = await ethers.getContractFactory("CertificateClaims");
      const certificatesAddress = await certificates.getAddress();
      await expect(claimsFactory.deploy(ethers.ZeroAddress, certificatesAddress, platform.address))
        .to.be.revertedWithCustomError(claimsFactory, "AdminRequired");
      await expect(claimsFactory.deploy(admin.address, ethers.ZeroAddress, platform.address))
        .to.be.revertedWithCustomError(claimsFactory, "CertificatesRequired");
      await expect(claimsFactory.deploy(admin.address, certificatesAddress, ethers.ZeroAddress))
        .to.be.revertedWithCustomError(claimsFactory, "VoucherSignerRequired");
    });
  });

  describe("paymaster administration", function () {
    it("belongs to the admin, not to the deployer", async function () {
      expect(await paymaster.owner()).to.equal(admin.address);
      expect(await paymaster.sponsorSigner()).to.equal(platform.address);
      expect(await paymaster.sponsoredTarget()).to.equal(await claims.getAddress());
      expect(await paymaster.maxCostPerOperation()).to.equal(MAX_COST);
    });

    it("lets only the owner change the policy and withdraw the deposit", async function () {
      for (const signer of [platform, outsider]) {
        await expect(paymaster.connect(signer).setSponsorSigner(signer.address)).to.be.revertedWithCustomError(paymaster, "OwnableUnauthorizedAccount");
        await expect(paymaster.connect(signer).setSponsoredCall(signer.address, "0x12345678")).to.be.revertedWithCustomError(paymaster, "OwnableUnauthorizedAccount");
        await expect(paymaster.connect(signer).setMaxCostPerOperation(1n)).to.be.revertedWithCustomError(paymaster, "OwnableUnauthorizedAccount");
        await expect(paymaster.connect(signer).withdrawTo(signer.address, 1n)).to.be.revertedWithCustomError(paymaster, "OwnableUnauthorizedAccount");
      }

      await expect(paymaster.connect(admin).setSponsorSigner(outsider.address)).to.emit(paymaster, "SponsorSignerChanged");
      await expect(paymaster.connect(admin).setMaxCostPerOperation(5n)).to.emit(paymaster, "MaxCostPerOperationChanged").withArgs(5n);
      await expect(paymaster.connect(admin).withdrawTo(outsider.address, ethers.parseEther("0.5")))
        .to.changeEtherBalance(outsider, ethers.parseEther("0.5"));
    });

    it("rejects invalid policy values", async function () {
      await expect(paymaster.connect(admin).setSponsorSigner(ethers.ZeroAddress)).to.be.revertedWithCustomError(paymaster, "SponsorSignerRequired");
      await expect(paymaster.connect(admin).setSponsoredCall(ethers.ZeroAddress, "0x12345678")).to.be.revertedWithCustomError(paymaster, "SponsoredTargetRequired");
      await expect(paymaster.connect(admin).setMaxCostPerOperation(0n)).to.be.revertedWithCustomError(paymaster, "MaxCostRequired");
      const paymasterFactory = await ethers.getContractFactory("MathChainPaymaster");
      await expect(paymasterFactory.deploy(await entryPoint.getAddress(), ethers.ZeroAddress, platform.address,
        await claims.getAddress(), "0x12345678", 1n)).to.be.revertedWithCustomError(paymasterFactory, "OwnerRequired");
    });

    it("rejects paymaster data of the wrong length", async function () {
      await expect(paymaster.parsePaymasterAndData("0x1234")).to.be.revertedWithCustomError(paymaster, "InvalidPaymasterData");
    });
  });
});
