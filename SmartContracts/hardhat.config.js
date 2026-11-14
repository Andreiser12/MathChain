require("@nomicfoundation/hardhat-ethers");
require("@nomicfoundation/hardhat-chai-matchers");

module.exports = {
  solidity: {
    version: "0.8.30",
    settings: {
      optimizer: { enabled: false, runs: 200 },
      evmVersion: "prague"
    }
  },
  networks: {
    hardhat: { hardfork: "prague" }
  }
};
