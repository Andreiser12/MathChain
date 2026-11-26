const { expect } = require("chai");
const { ethers } = require("hardhat");

const rawCid = (seed) => ethers.concat(["0x01551220", ethers.sha256(ethers.toUtf8Bytes(seed))]);
const dagCid = (seed) => ethers.concat(["0x01701220", ethers.sha256(ethers.toUtf8Bytes(seed))]);

describe("CurriculumRegistry", function () {
  let registry, admin, publisher, outsider;

  beforeEach(async function () {
    [admin, publisher, outsider] = await ethers.getSigners();
    registry = await (await ethers.getContractFactory("CurriculumRegistry")).deploy(admin.address, publisher.address);
  });

  it("gives the admin and publisher roles from the constructor", async function () {
    expect(await registry.hasRole(await registry.DEFAULT_ADMIN_ROLE(), admin.address)).to.equal(true);
    expect(await registry.hasRole(await registry.PUBLISHER_ROLE(), publisher.address)).to.equal(true);
    expect(await registry.hasRole(await registry.PUBLISHER_ROLE(), admin.address)).to.equal(false);
  });

  it("rejects a zero admin", async function () {
    const factory = await ethers.getContractFactory("CurriculumRegistry");
    await expect(factory.deploy(ethers.ZeroAddress, publisher.address)).to.be.revertedWithCustomError(factory, "AdminRequired");
  });

  it("can be deployed without a publisher", async function () {
    const empty = await (await ethers.getContractFactory("CurriculumRegistry")).deploy(admin.address, ethers.ZeroAddress);
    expect(await empty.hasRole(await empty.PUBLISHER_ROLE(), ethers.ZeroAddress)).to.equal(false);
  });

  it("publishes versions in order and emits an event", async function () {
    const first = rawCid("v1");
    const second = dagCid("v2");

    const tx = await registry.connect(publisher).publish(first);
    const block = await ethers.provider.getBlock((await tx.wait()).blockNumber);
    await expect(tx).to.emit(registry, "CurriculumPublished").withArgs(1, first, publisher.address, block.timestamp);
    await expect(registry.connect(publisher).publish(second)).to.emit(registry, "CurriculumPublished");

    expect(await registry.publicationCount()).to.equal(2);
    const [version, latest] = await registry.latest();
    expect(version).to.equal(2);
    expect(latest.cid).to.equal(second);
    expect(latest.publisher).to.equal(publisher.address);
    expect((await registry.publicationAt(1)).cid).to.equal(first);
  });

  it("returns the version from publish", async function () {
    expect(await registry.connect(publisher).publish.staticCall(rawCid("v1"))).to.equal(1);
  });

  it("refuses to publish the latest content identifier again", async function () {
    await registry.connect(publisher).publish(rawCid("v1"));

    await expect(registry.connect(publisher).publish(rawCid("v1")))
      .to.be.revertedWithCustomError(registry, "AlreadyPublished").withArgs(1);
  });

  it("allows going back to an older curriculum as a new version", async function () {
    await registry.connect(publisher).publish(rawCid("v1"));
    await registry.connect(publisher).publish(rawCid("v2"));

    await registry.connect(publisher).publish(rawCid("v1"));

    expect((await registry.latest())[0]).to.equal(3);
  });

  it("only lets publishers publish", async function () {
    await expect(registry.connect(outsider).publish(rawCid("v1")))
      .to.be.revertedWithCustomError(registry, "AccessControlUnauthorizedAccount");
    await expect(registry.connect(admin).publish(rawCid("v1")))
      .to.be.revertedWithCustomError(registry, "AccessControlUnauthorizedAccount");
  });

  it("lets the admin change the publisher", async function () {
    const role = await registry.PUBLISHER_ROLE();
    await registry.connect(admin).revokeRole(role, publisher.address);
    await registry.connect(admin).grantRole(role, outsider.address);

    await expect(registry.connect(publisher).publish(rawCid("v1"))).to.be.revertedWithCustomError(registry, "AccessControlUnauthorizedAccount");
    await expect(registry.connect(outsider).publish(rawCid("v1"))).to.emit(registry, "CurriculumPublished");
    await expect(registry.connect(publisher).grantRole(role, publisher.address)).to.be.revertedWithCustomError(registry, "AccessControlUnauthorizedAccount");
  });

  describe("content identifier validation", function () {
    const digest = ethers.sha256(ethers.toUtf8Bytes("x"));
    const cases = {
      "empty": "0x",
      "CIDv0 multihash": ethers.concat(["0x1220", digest]),
      "unknown codec": ethers.concat(["0x01711220", digest]),
      "other hash function": ethers.concat(["0x01551320", digest]),
      "wrong digest length": ethers.concat(["0x01551219", digest]),
      "version two": ethers.concat(["0x02551220", digest]),
      "too short": ethers.concat(["0x01551220", ethers.dataSlice(digest, 1)]),
      "too long": ethers.concat(["0x01551220", digest, "0x00"])
    };

    for (const [name, value] of Object.entries(cases)) {
      it(`rejects ${name}`, async function () {
        expect(await registry.isValidContentIdentifier(value)).to.equal(false);
        await expect(registry.connect(publisher).publish(value)).to.be.revertedWithCustomError(registry, "InvalidContentIdentifier");
      });
    }

    it("accepts raw and dag-pb CIDv1 with sha2-256", async function () {
      expect(await registry.isValidContentIdentifier(rawCid("a"))).to.equal(true);
      expect(await registry.isValidContentIdentifier(dagCid("a"))).to.equal(true);
    });
  });

  it("has no latest publication before the first one", async function () {
    await expect(registry.latest()).to.be.revertedWithCustomError(registry, "NoPublication");
    await expect(registry.publicationAt(0)).to.be.revertedWithCustomError(registry, "UnknownVersion").withArgs(0);
    await expect(registry.publicationAt(1)).to.be.revertedWithCustomError(registry, "UnknownVersion").withArgs(1);
  });
});
