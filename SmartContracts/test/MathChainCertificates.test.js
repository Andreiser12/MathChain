const { expect } = require("chai");
const { ethers } = require("hardhat");

const Kind = { Class: 0, Chapter: 1 };
const ERC165_ID = "0x01ffc9a7";
const ERC721_ID = "0x80ac58cd";
const ERC5192_ID = "0xb45a3c0e";
const ACCESS_CONTROL_ID = "0x7965db0b";

function subjectIdFromGuid(guid) {
  return ethers.zeroPadValue("0x" + guid.replace(/-/g, ""), 32);
}

function commitment(student, kind, subjectId, grade, salt) {
  return ethers.keccak256(
    ethers.AbiCoder.defaultAbiCoder().encode(
      ["address", "uint8", "bytes32", "uint16", "bytes32"],
      [student, kind, subjectId, grade, salt]
    )
  );
}

describe("MathChainCertificates", function () {
  const classId = subjectIdFromGuid("b3ba5880-baaa-4172-8f9e-2bb240efddfd");
  const chapterId = subjectIdFromGuid("70000000-0000-0000-0000-000000000002");
  const dataHash = ethers.id("certificate-data");

  let certificates, admin, teacher, otherTeacher, student, otherStudent, outsider, ISSUER_ROLE, DEFAULT_ADMIN_ROLE;

  beforeEach(async function () {
    [admin, teacher, otherTeacher, student, otherStudent, outsider] = await ethers.getSigners();
    const factory = await ethers.getContractFactory("MathChainCertificates", outsider);
    certificates = await factory.deploy(admin.address);
    ISSUER_ROLE = await certificates.ISSUER_ROLE();
    DEFAULT_ADMIN_ROLE = await certificates.DEFAULT_ADMIN_ROLE();
    await certificates.connect(admin).grantRole(ISSUER_ROLE, teacher.address);
    await certificates.connect(admin).grantRole(ISSUER_ROLE, otherTeacher.address);
  });

  async function issueClassCertificate(to = student, issuer = teacher) {
    await certificates.connect(issuer).issue(to.address, Kind.Class, classId, dataHash);
    return certificates.totalIssued();
  }

  describe("deployment", function () {
    it("sets the token name, symbol and admin", async function () {
      expect(await certificates.name()).to.equal("MathChain Certificate");
      expect(await certificates.symbol()).to.equal("MCC");
      expect(await certificates.hasRole(DEFAULT_ADMIN_ROLE, admin.address)).to.equal(true);
      expect(await certificates.totalIssued()).to.equal(0n);
    });

    it("does not give the deployer any role", async function () {
      expect(await certificates.hasRole(DEFAULT_ADMIN_ROLE, outsider.address)).to.equal(false);
      expect(await certificates.hasRole(ISSUER_ROLE, outsider.address)).to.equal(false);
    });

    it("rejects the zero address as admin", async function () {
      const factory = await ethers.getContractFactory("MathChainCertificates");

      await expect(factory.deploy(ethers.ZeroAddress)).to.be.revertedWithCustomError(certificates, "AdminRequired");
    });

    it("declares the ERC-165, ERC-721, ERC-5192 and AccessControl interfaces", async function () {
      for (const interfaceId of [ERC165_ID, ERC721_ID, ERC5192_ID, ACCESS_CONTROL_ID]) {
        expect(await certificates.supportsInterface(interfaceId)).to.equal(true);
      }

      expect(await certificates.supportsInterface("0xffffffff")).to.equal(false);
    });
  });

  describe("roles", function () {
    it("lets only the admin grant and revoke the issuer role", async function () {
      await expect(certificates.connect(teacher).grantRole(ISSUER_ROLE, outsider.address))
        .to.be.revertedWithCustomError(certificates, "AccessControlUnauthorizedAccount")
        .withArgs(teacher.address, DEFAULT_ADMIN_ROLE);

      await certificates.connect(admin).revokeRole(ISSUER_ROLE, teacher.address);

      await expect(certificates.connect(teacher).issue(student.address, Kind.Class, classId, dataHash))
        .to.be.revertedWithCustomError(certificates, "AccessControlUnauthorizedAccount")
        .withArgs(teacher.address, ISSUER_ROLE);
    });
  });

  describe("issue", function () {
    it("mints a locked certificate to the student and stores its data", async function () {
      const transaction = await certificates.connect(teacher).issue(student.address, Kind.Chapter, chapterId, dataHash);
      const block = await ethers.provider.getBlock((await transaction.wait()).blockNumber);

      await expect(transaction).to.emit(certificates, "Transfer").withArgs(ethers.ZeroAddress, student.address, 1n);
      await expect(transaction).to.emit(certificates, "Locked").withArgs(1n);
      await expect(transaction)
        .to.emit(certificates, "CertificateIssued")
        .withArgs(1n, student.address, teacher.address, Kind.Chapter, chapterId, dataHash);

      expect(await certificates.ownerOf(1n)).to.equal(student.address);
      expect(await certificates.balanceOf(student.address)).to.equal(1n);
      expect(await certificates.locked(1n)).to.equal(true);
      expect(await certificates.totalIssued()).to.equal(1n);
      expect(await certificates.findCertificate(student.address, Kind.Chapter, chapterId)).to.equal(1n);

      const certificate = await certificates.getCertificate(1n);
      expect(certificate.student).to.equal(student.address);
      expect(certificate.issuer).to.equal(teacher.address);
      expect(certificate.kind).to.equal(BigInt(Kind.Chapter));
      expect(certificate.subjectId).to.equal(chapterId);
      expect(certificate.dataHash).to.equal(dataHash);
      expect(certificate.issuedAt).to.equal(BigInt(block.timestamp));
      expect(certificate.revoked).to.equal(false);
    });

    it("rejects callers without the issuer role", async function () {
      for (const caller of [student, outsider, admin]) {
        await expect(certificates.connect(caller).issue(student.address, Kind.Class, classId, dataHash))
          .to.be.revertedWithCustomError(certificates, "AccessControlUnauthorizedAccount")
          .withArgs(caller.address, ISSUER_ROLE);
      }

      expect(await certificates.totalIssued()).to.equal(0n);
    });

    it("rejects a second active certificate for the same student, kind and subject", async function () {
      await issueClassCertificate();

      await expect(certificates.connect(otherTeacher).issue(student.address, Kind.Class, classId, ethers.id("other")))
        .to.be.revertedWithCustomError(certificates, "CertificateAlreadyIssued")
        .withArgs(1n);
    });

    it("treats class and chapter certificates, subjects and students separately", async function () {
      await certificates.connect(teacher).issue(student.address, Kind.Class, classId, dataHash);
      await certificates.connect(teacher).issue(student.address, Kind.Chapter, classId, dataHash);
      await certificates.connect(teacher).issue(student.address, Kind.Class, chapterId, dataHash);
      await certificates.connect(teacher).issue(otherStudent.address, Kind.Class, classId, dataHash);

      expect(await certificates.totalIssued()).to.equal(4n);
      expect(await certificates.balanceOf(student.address)).to.equal(3n);
      expect(await certificates.findCertificate(otherStudent.address, Kind.Class, classId)).to.equal(4n);
      expect(await certificates.findCertificate(otherStudent.address, Kind.Chapter, classId)).to.equal(0n);
    });

    it("rejects the zero address as student", async function () {
      await expect(
        certificates.connect(teacher).issue(ethers.ZeroAddress, Kind.Class, classId, dataHash)
      ).to.be.revertedWithCustomError(certificates, "StudentRequired");
    });

    it("rejects an unknown certificate kind", async function () {
      const data = certificates.interface
        .encodeFunctionData("issue", [student.address, Kind.Chapter, classId, dataHash])
        .replace(/0{63}1(?=[0-9a-f]{128}$)/, "0".repeat(63) + "2");

      await expect(teacher.sendTransaction({ to: await certificates.getAddress(), data })).to.be.reverted;
      expect(await certificates.totalIssued()).to.equal(0n);
    });
  });

  describe("soulbound", function () {
    beforeEach(async function () {
      await issueClassCertificate();
    });

    it("blocks every kind of transfer by the student", async function () {
      const from = student.address;
      const to = otherStudent.address;

      await expect(certificates.connect(student).transferFrom(from, to, 1n))
        .to.be.revertedWithCustomError(certificates, "CertificateNotTransferable");
      await expect(certificates.connect(student)["safeTransferFrom(address,address,uint256)"](from, to, 1n))
        .to.be.revertedWithCustomError(certificates, "CertificateNotTransferable");
      await expect(certificates.connect(student)["safeTransferFrom(address,address,uint256,bytes)"](from, to, 1n, "0x"))
        .to.be.revertedWithCustomError(certificates, "CertificateNotTransferable");

      expect(await certificates.ownerOf(1n)).to.equal(from);
    });

    it("blocks transfers by an approved operator", async function () {
      await certificates.connect(student).approve(outsider.address, 1n);
      await certificates.connect(student).setApprovalForAll(otherStudent.address, true);

      await expect(certificates.connect(outsider).transferFrom(student.address, outsider.address, 1n))
        .to.be.revertedWithCustomError(certificates, "CertificateNotTransferable");
      await expect(certificates.connect(otherStudent).transferFrom(student.address, otherStudent.address, 1n))
        .to.be.revertedWithCustomError(certificates, "CertificateNotTransferable");
    });

    it("blocks transfers by the issuer and the admin", async function () {
      for (const caller of [teacher, admin]) {
        await expect(certificates.connect(caller).transferFrom(student.address, caller.address, 1n)).to.be.reverted;
      }

      expect(await certificates.ownerOf(1n)).to.equal(student.address);
    });

    it("reports locked only for existing certificates", async function () {
      await expect(certificates.locked(2n))
        .to.be.revertedWithCustomError(certificates, "ERC721NonexistentToken")
        .withArgs(2n);
    });
  });

  describe("isValid", function () {
    it("is true only for the stored data hash of an active certificate", async function () {
      await issueClassCertificate();

      expect(await certificates.isValid(1n, dataHash)).to.equal(true);
      expect(await certificates.isValid(1n, ethers.id("tampered"))).to.equal(false);
      expect(await certificates.isValid(2n, dataHash)).to.equal(false);
      expect(await certificates.isValid(0n, ethers.ZeroHash)).to.equal(false);
    });

    it("detects a grade changed after the certificate was issued", async function () {
      const salt = ethers.hexlify(ethers.randomBytes(32));
      const issuedHash = commitment(student.address, Kind.Class, classId, 9, salt);
      await certificates.connect(teacher).issue(student.address, Kind.Class, classId, issuedHash);

      expect(await certificates.isValid(1n, commitment(student.address, Kind.Class, classId, 9, salt))).to.equal(true);
      expect(await certificates.isValid(1n, commitment(student.address, Kind.Class, classId, 10, salt))).to.equal(false);
      expect(await certificates.isValid(1n, commitment(otherStudent.address, Kind.Class, classId, 9, salt))).to.equal(false);
      expect(await certificates.isValid(1n, commitment(student.address, Kind.Class, classId, 9, ethers.ZeroHash))).to.equal(false);
    });
  });

  describe("revoke", function () {
    beforeEach(async function () {
      await issueClassCertificate();
    });

    it("lets the issuer revoke, burns the token and keeps the record", async function () {
      const transaction = await certificates.connect(teacher).revoke(1n);

      await expect(transaction).to.emit(certificates, "CertificateRevoked").withArgs(1n, teacher.address);
      await expect(transaction).to.emit(certificates, "Transfer").withArgs(student.address, ethers.ZeroAddress, 1n);
      await expect(certificates.ownerOf(1n)).to.be.revertedWithCustomError(certificates, "ERC721NonexistentToken");

      expect(await certificates.balanceOf(student.address)).to.equal(0n);
      expect(await certificates.isValid(1n, dataHash)).to.equal(false);
      expect(await certificates.findCertificate(student.address, Kind.Class, classId)).to.equal(0n);

      const certificate = await certificates.getCertificate(1n);
      expect(certificate.revoked).to.equal(true);
      expect(certificate.student).to.equal(student.address);
      expect(certificate.issuer).to.equal(teacher.address);
    });

    it("lets the admin revoke", async function () {
      await expect(certificates.connect(admin).revoke(1n))
        .to.emit(certificates, "CertificateRevoked")
        .withArgs(1n, admin.address);
    });

    it("rejects the student, another issuer and outsiders", async function () {
      for (const caller of [student, otherTeacher, outsider]) {
        await expect(certificates.connect(caller).revoke(1n))
          .to.be.revertedWithCustomError(certificates, "NotAllowedToRevoke")
          .withArgs(caller.address);
      }

      expect(await certificates.isValid(1n, dataHash)).to.equal(true);
    });

    it("rejects an issuer whose role was removed", async function () {
      await certificates.connect(admin).revokeRole(ISSUER_ROLE, teacher.address);

      await expect(certificates.connect(teacher).revoke(1n))
        .to.be.revertedWithCustomError(certificates, "NotAllowedToRevoke")
        .withArgs(teacher.address);
    });

    it("rejects unknown and already revoked certificates", async function () {
      await expect(certificates.connect(admin).revoke(7n))
        .to.be.revertedWithCustomError(certificates, "CertificateNotFound")
        .withArgs(7n);

      await certificates.connect(teacher).revoke(1n);

      await expect(certificates.connect(teacher).revoke(1n))
        .to.be.revertedWithCustomError(certificates, "CertificateAlreadyRevoked")
        .withArgs(1n);
      await expect(certificates.getCertificate(7n))
        .to.be.revertedWithCustomError(certificates, "CertificateNotFound")
        .withArgs(7n);
    });

    it("allows a corrected certificate to be issued after revocation", async function () {
      const correctedHash = ethers.id("corrected");
      await certificates.connect(teacher).revoke(1n);

      await certificates.connect(teacher).issue(student.address, Kind.Class, classId, correctedHash);

      expect(await certificates.findCertificate(student.address, Kind.Class, classId)).to.equal(2n);
      expect(await certificates.ownerOf(2n)).to.equal(student.address);
      expect(await certificates.isValid(2n, correctedHash)).to.equal(true);
      expect(await certificates.isValid(1n, dataHash)).to.equal(false);
      expect(await certificates.totalIssued()).to.equal(2n);
    });
  });

  describe("issueBatch", function () {
    it("issues one certificate per student and returns the token ids", async function () {
      const students = [student.address, otherStudent.address, outsider.address];
      const hashes = students.map((_, index) => ethers.id(`hash-${index}`));

      expect(await certificates.connect(teacher).issueBatch.staticCall(students, Kind.Class, classId, hashes)).to.deep.equal([1n, 2n, 3n]);

      await certificates.connect(teacher).issueBatch(students, Kind.Class, classId, hashes);

      for (let index = 0; index < students.length; index++) {
        expect(await certificates.ownerOf(BigInt(index + 1))).to.equal(students[index]);
        expect(await certificates.isValid(BigInt(index + 1), hashes[index])).to.equal(true);
      }
    });

    it("issues nothing when one entry of the batch is invalid", async function () {
      const students = [student.address, otherStudent.address, student.address];
      const hashes = students.map(() => dataHash);

      await expect(certificates.connect(teacher).issueBatch(students, Kind.Class, classId, hashes))
        .to.be.revertedWithCustomError(certificates, "CertificateAlreadyIssued")
        .withArgs(1n);

      expect(await certificates.totalIssued()).to.equal(0n);
      expect(await certificates.balanceOf(student.address)).to.equal(0n);
      expect(await certificates.balanceOf(otherStudent.address)).to.equal(0n);
    });

    it("rejects arrays of different lengths and callers without the issuer role", async function () {
      await expect(certificates.connect(teacher).issueBatch([student.address, otherStudent.address], Kind.Class, classId, [dataHash]))
        .to.be.revertedWithCustomError(certificates, "BatchLengthMismatch")
        .withArgs(2n, 1n);
      await expect(certificates.connect(outsider).issueBatch([student.address], Kind.Class, classId, [dataHash]))
        .to.be.revertedWithCustomError(certificates, "AccessControlUnauthorizedAccount")
        .withArgs(outsider.address, ISSUER_ROLE);
    });

    it("fits a class of thirty students in one transaction", async function () {
      const students = Array.from({ length: 30 }, () => ethers.Wallet.createRandom().address);
      const hashes = students.map(address => ethers.id(address));

      const receipt = await (await certificates.connect(teacher).issueBatch(students, Kind.Class, classId, hashes)).wait();

      expect(await certificates.totalIssued()).to.equal(30n);
      expect(receipt.gasUsed).to.be.lessThan(6_000_000n);
    });
  });

  describe("subject identifiers", function () {
    it("keeps the full application identifier in the bytes32 subject", async function () {
      expect(classId).to.equal("0x00000000000000000000000000000000b3ba5880baaa41728f9e2bb240efddfd");
      expect(subjectIdFromGuid("b3ba5880-baaa-4172-8f9e-2bb240efddfe")).to.not.equal(classId);
    });
  });
});
