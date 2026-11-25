require("@nomicfoundation/hardhat-ethers");
require("@nomicfoundation/hardhat-chai-matchers");

const optimizedForTests = {
  version: "0.8.30",
  settings: {
    optimizer: { enabled: true, runs: 1000000 },
    evmVersion: "prague"
  }
};

module.exports = {
  solidity: {
    compilers: [
      {
        version: "0.8.30",
        settings: {
          optimizer: { enabled: false, runs: 200 },
          evmVersion: "prague"
        }
      }
    ],
    overrides: {
      "@account-abstraction/contracts/core/EntryPoint.sol": optimizedForTests,
      "contracts/test/EntryPointForTests.sol": optimizedForTests
    }
  },
  networks: {
    hardhat: { hardfork: "prague" }
  }
};
