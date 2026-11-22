const { expect } = require("chai");
const { ethers } = require("hardhat");

const Tier = { Easy: 0, Medium: 1, Hard: 2 };
const USDC = 1_000_000n;
const PRICES = [1n * USDC, 2n * USDC, 3n * USDC];

function solutionIdFor(formulaGuid, index) {
  return ethers.keccak256(
    ethers.AbiCoder.defaultAbiCoder().encode(
      ["bytes16", "uint8"],
      ["0x" + formulaGuid.replace(/-/g, ""), index]
    )
  );
}

describe("MathChainSolutions", function () {
  const solutionId = solutionIdFor("b3ba5880-baaa-4172-8f9e-2bb240efddfd", 4);
  const otherSolutionId = solutionIdFor("b3ba5880-baaa-4172-8f9e-2bb240efddfd", 5);

  let token, solutions, admin, treasury, student, otherStudent, relayer, outsider, chainId, DEFAULT_ADMIN_ROLE;

  beforeEach(async function () {
    [admin, treasury, student, otherStudent, relayer, outsider] = await ethers.getSigners();
    token = await (await ethers.getContractFactory("MockAuthorizationToken")).deploy();
    const factory = await ethers.getContractFactory("MathChainSolutions", outsider);
    solutions = await factory.deploy(admin.address, await token.getAddress(), treasury.address, ...PRICES);
    DEFAULT_ADMIN_ROLE = await solutions.DEFAULT_ADMIN_ROLE();
    chainId = (await ethers.provider.getNetwork()).chainId;
    await token.mint(student.address, 100n * USDC);
  });

  async function authorize({ payer = student, account = student.address, id = solutionId, tier = Tier.Medium,
    salt = ethers.id("salt-1"), value = PRICES[tier], validAfter = 0n, validBefore = null, nonceFor = null } = {}) {
    const latest = await ethers.provider.getBlock("latest");
    const before = validBefore ?? BigInt(latest.timestamp + 3600);
    const nonce = nonceFor ?? (await solutions.purchaseNonce(account, id, tier, salt));
    const signature = await payer.signTypedData(
      { name: "USDC", version: "2", chainId, verifyingContract: await token.getAddress() },
      {
        ReceiveWithAuthorization: [
          { name: "from", type: "address" },
          { name: "to", type: "address" },
          { name: "value", type: "uint256" },
          { name: "validAfter", type: "uint256" },
          { name: "validBefore", type: "uint256" },
          { name: "nonce", type: "bytes32" }
        ]
      },
      { from: payer.address, to: await solutions.getAddress(), value, validAfter, validBefore: before, nonce }
    );
    const { v, r, s } = ethers.Signature.from(signature);
    return { account, id, tier, salt, nonce, authorization: { payer: payer.address, value, validAfter, validBefore: before, v, r, s } };
  }

  function submit(purchase, by = relayer) {
    return solutions.connect(by).buy(purchase.account, purchase.id, purchase.tier, purchase.salt, purchase.authorization);
  }

  describe("deployment", function () {
    it("stores the admin, token, treasury and prices", async function () {
      expect(await solutions.hasRole(DEFAULT_ADMIN_ROLE, admin.address)).to.equal(true);
      expect(await solutions.paymentToken()).to.equal(await token.getAddress());
      expect(await solutions.treasury()).to.equal(treasury.address);
      expect(await solutions.prices()).to.deep.equal(PRICES);
      expect(await solutions.priceOf(Tier.Hard)).to.equal(PRICES[2]);
    });

    it("does not give the deployer any role", async function () {
      expect(await solutions.hasRole(DEFAULT_ADMIN_ROLE, outsider.address)).to.equal(false);
    });

    it("emits the initial configuration", async function () {
      const factory = await ethers.getContractFactory("MathChainSolutions");
      const deployed = await factory.deploy(admin.address, await token.getAddress(), treasury.address, ...PRICES);
      const receipt = await deployed.deploymentTransaction().wait();
      const names = receipt.logs.map((log) => deployed.interface.parseLog(log)?.name).filter(Boolean);
      expect(names).to.deep.equal(["RoleGranted", "TreasuryChanged", "PriceChanged", "PriceChanged", "PriceChanged"]);
    });

    it("rejects missing constructor values", async function () {
      const factory = await ethers.getContractFactory("MathChainSolutions");
      const tokenAddress = await token.getAddress();
      await expect(factory.deploy(ethers.ZeroAddress, tokenAddress, treasury.address, ...PRICES))
        .to.be.revertedWithCustomError(factory, "AdminRequired");
      await expect(factory.deploy(admin.address, ethers.ZeroAddress, treasury.address, ...PRICES))
        .to.be.revertedWithCustomError(factory, "PaymentTokenRequired");
      await expect(factory.deploy(admin.address, tokenAddress, ethers.ZeroAddress, ...PRICES))
        .to.be.revertedWithCustomError(factory, "TreasuryRequired");
      await expect(factory.deploy(admin.address, tokenAddress, treasury.address, PRICES[0], 0n, PRICES[2]))
        .to.be.revertedWithCustomError(factory, "PriceRequired");
    });
  });

  describe("nonce", function () {
    it("binds the chain, the contract, the account, the solution, the tier and the salt", async function () {
      const salt = ethers.id("salt");
      const expected = ethers.keccak256(
        ethers.AbiCoder.defaultAbiCoder().encode(
          ["uint256", "address", "address", "bytes32", "uint8", "bytes32"],
          [chainId, await solutions.getAddress(), student.address, solutionId, Tier.Hard, salt]
        )
      );
      expect(await solutions.purchaseNonce(student.address, solutionId, Tier.Hard, salt)).to.equal(expected);
      expect(await solutions.purchaseNonce(student.address, solutionId, Tier.Easy, salt)).to.not.equal(expected);
    });

    it("computes the purchase key from the account and the solution", async function () {
      const expected = ethers.keccak256(
        ethers.AbiCoder.defaultAbiCoder().encode(["address", "bytes32"], [student.address, solutionId])
      );
      expect(await solutions.purchaseKey(student.address, solutionId)).to.equal(expected);
    });
  });

  describe("buy", function () {
    it("moves the price from the payer to the treasury and records the purchase", async function () {
      const purchase = await authorize({ tier: Tier.Hard });

      await expect(submit(purchase))
        .to.emit(solutions, "SolutionPurchased")
        .withArgs(student.address, solutionId, student.address, Tier.Hard, PRICES[2])
        .and.to.emit(token, "AuthorizationUsed")
        .withArgs(student.address, purchase.nonce);

      expect(await token.balanceOf(student.address)).to.equal(97n * USDC);
      expect(await token.balanceOf(treasury.address)).to.equal(PRICES[2]);
      expect(await token.balanceOf(await solutions.getAddress())).to.equal(0n);
      expect(await solutions.isPurchased(student.address, solutionId)).to.equal(true);
      expect(await solutions.isPurchased(student.address, otherSolutionId)).to.equal(false);
    });

    it("charges the price of each tier", async function () {
      for (const tier of [Tier.Easy, Tier.Medium, Tier.Hard]) {
        const id = solutionIdFor("70000000-0000-0000-0000-000000000002", tier);
        await submit(await authorize({ id, tier, salt: ethers.id("tier-" + tier) }));
      }

      expect(await token.balanceOf(treasury.address)).to.equal(6n * USDC);
    });

    it("lets a wallet pay for another account", async function () {
      const purchase = await authorize({ account: otherStudent.address });

      await expect(submit(purchase))
        .to.emit(solutions, "SolutionPurchased")
        .withArgs(otherStudent.address, solutionId, student.address, Tier.Medium, PRICES[1]);

      expect(await solutions.isPurchased(otherStudent.address, solutionId)).to.equal(true);
      expect(await solutions.isPurchased(student.address, solutionId)).to.equal(false);
    });

    it("can be submitted by the payer itself", async function () {
      await expect(submit(await authorize(), student)).to.emit(solutions, "SolutionPurchased");
    });

    it("rejects a second purchase of the same solution for the same account", async function () {
      await submit(await authorize());
      const again = await authorize({ salt: ethers.id("salt-2") });

      await expect(submit(again))
        .to.be.revertedWithCustomError(solutions, "AlreadyPurchased")
        .withArgs(student.address, solutionId);
      expect(await token.balanceOf(treasury.address)).to.equal(PRICES[1]);
    });

    it("rejects replaying the same signature", async function () {
      const purchase = await authorize();
      await submit(purchase);

      await expect(submit(purchase)).to.be.revertedWithCustomError(solutions, "AlreadyPurchased");
    });

    it("rejects an amount that differs from the current price", async function () {
      const purchase = await authorize({ tier: Tier.Easy, value: PRICES[0] - 1n });

      await expect(submit(purchase))
        .to.be.revertedWithCustomError(solutions, "PriceMismatch")
        .withArgs(PRICES[0], PRICES[0] - 1n);
    });

    it("rejects a signature made before the price changed", async function () {
      const purchase = await authorize({ tier: Tier.Medium });
      await solutions.connect(admin).setPrice(Tier.Medium, 5n * USDC);

      await expect(submit(purchase))
        .to.be.revertedWithCustomError(solutions, "PriceMismatch")
        .withArgs(5n * USDC, PRICES[1]);
    });

    it("rejects a relayer that changes the tier", async function () {
      const purchase = await authorize({ tier: Tier.Hard });

      await expect(submit({ ...purchase, tier: Tier.Easy }))
        .to.be.revertedWithCustomError(solutions, "PriceMismatch");
    });

    for (const [field, value] of [
      ["solution", { id: otherSolutionId }],
      ["account", { account: "0x000000000000000000000000000000000000dEaD" }],
      ["salt", { salt: ethers.id("other-salt") }]
    ]) {
      it(`rejects a relayer that changes the ${field}`, async function () {
        const purchase = await authorize();

        await expect(submit({ ...purchase, ...value })).to.be.revertedWith("FiatTokenV2: invalid signature");
        expect(await token.balanceOf(student.address)).to.equal(100n * USDC);
      });
    }

    it("rejects a relayer that changes the payer", async function () {
      await token.mint(otherStudent.address, 100n * USDC);
      const purchase = await authorize();

      await expect(submit({ ...purchase, authorization: { ...purchase.authorization, payer: otherStudent.address } }))
        .to.be.revertedWith("FiatTokenV2: invalid signature");
      expect(await token.balanceOf(otherStudent.address)).to.equal(100n * USDC);
    });

    it("rejects a signature over a nonce that the contract did not build", async function () {
      const purchase = await authorize({ nonceFor: ethers.id("foreign nonce") });

      await expect(submit(purchase)).to.be.revertedWith("FiatTokenV2: invalid signature");
    });

    it("rejects an expired authorization", async function () {
      const latest = await ethers.provider.getBlock("latest");
      const purchase = await authorize({ validBefore: BigInt(latest.timestamp) });

      await expect(submit(purchase)).to.be.revertedWith("FiatTokenV2: authorization is expired");
      expect(await solutions.isPurchased(student.address, solutionId)).to.equal(false);
    });

    it("keeps nothing when the payer cannot afford the price", async function () {
      const purchase = await authorize({ payer: otherStudent, account: otherStudent.address });

      await expect(submit(purchase)).to.be.revertedWithCustomError(token, "ERC20InsufficientBalance");
      expect(await solutions.isPurchased(otherStudent.address, solutionId)).to.equal(false);
    });

    it("rejects the zero account", async function () {
      const purchase = await authorize({ account: ethers.ZeroAddress });

      await expect(submit(purchase)).to.be.revertedWithCustomError(solutions, "AccountRequired");
    });

    it("rejects an unknown tier", async function () {
      const purchase = await authorize();

      await expect(submit({ ...purchase, tier: 3 })).to.be.reverted;
    });
  });

  describe("administration", function () {
    it("lets the admin change a price", async function () {
      await expect(solutions.connect(admin).setPrice(Tier.Easy, 1_500_000n))
        .to.emit(solutions, "PriceChanged")
        .withArgs(Tier.Easy, 1_500_000n);

      expect(await solutions.prices()).to.deep.equal([1_500_000n, PRICES[1], PRICES[2]]);
    });

    it("rejects a zero price", async function () {
      await expect(solutions.connect(admin).setPrice(Tier.Easy, 0n))
        .to.be.revertedWithCustomError(solutions, "PriceRequired");
    });

    it("lets only the admin change prices and the treasury", async function () {
      for (const signer of [outsider, treasury, student]) {
        await expect(solutions.connect(signer).setPrice(Tier.Easy, 1n))
          .to.be.revertedWithCustomError(solutions, "AccessControlUnauthorizedAccount")
          .withArgs(signer.address, DEFAULT_ADMIN_ROLE);
        await expect(solutions.connect(signer).setTreasury(signer.address))
          .to.be.revertedWithCustomError(solutions, "AccessControlUnauthorizedAccount");
      }
    });

    it("sends later payments to the new treasury", async function () {
      await expect(solutions.connect(admin).setTreasury(outsider.address))
        .to.emit(solutions, "TreasuryChanged")
        .withArgs(outsider.address);

      await submit(await authorize());

      expect(await token.balanceOf(outsider.address)).to.equal(PRICES[1]);
      expect(await token.balanceOf(treasury.address)).to.equal(0n);
    });

    it("rejects the zero treasury", async function () {
      await expect(solutions.connect(admin).setTreasury(ethers.ZeroAddress))
        .to.be.revertedWithCustomError(solutions, "TreasuryRequired");
    });
  });
});
