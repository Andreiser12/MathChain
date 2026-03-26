#  MathChain: Web3 Educational SaaS

![C#](https://img.shields.io/badge/C%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-%23512BD4.svg?style=for-the-badge&logo=dotnet&logoColor=white)
![Solidity](https://img.shields.io/badge/Solidity-%23363636.svg?style=for-the-badge&logo=solidity&logoColor=white)

##  Overview
**MathChain** is a decentralized desktop educational platform that bridges advanced mathematics, 3D visualization, and Web3 technology. It transforms traditional 2D mathematical formulas into interactive 3D meshes and leverages a decentralized pay-per-view SaaS model for step-by-step problem-solving.

##  Key Features
* **Interactive 3D Math Arena:** Renders complex mathematical formulas as 3D interactive meshes using the Helix Toolkit.
* **Algorithmic Problem Generation:** Integrates the Wolfram Alpha API to dynamically generate, compute, and rigorously validate random calculus integrals and equations off-chain.
* **Web3 Micro-Transactions:** Features a custom Solidity Smart Contract deployed on the Ethereum Sepolia testnet to handle payments for unlocking step-by-step solutions.
* **Decentralized Authentication:** Replaces vulnerable centralized databases with secure, anonymous wallet-based logins.
* **Modern Dark UI:** Built entirely from scratch using WPF control templates for a sleek, responsive user experience.

##  Tech Stack
* **Frontend/UI:** C#, WPF, Helix Toolkit (3D Rendering), WpfMath (LaTeX rendering).
* **Backend/Logic:** .NET, Wolfram Alpha REST API.
* **Blockchain/Web3:** Solidity, Remix IDE, Ethereum (Sepolia Testnet), Nethereum (C# Web3 integration).

##  Architecture
MathChain uses a hybrid architecture to maximize efficiency and minimize gas fees:
1. **Off-Chain Validation:** User inputs and floating-point math validations are processed locally via C# and Wolfram Alpha to ensure zero-latency feedback.
2. **On-Chain SaaS Logic:** The unlocking of premium content, step-by-step solutions, is handled via a Smart Contract, acting as an automated, trustless vending machine for educational content.

## Prerequisites
* Visual Studio 2022
* A Wolfram Alpha Developer AppID
* An Infura/Alchemy RPC URL for Sepolia
* A MetaMask wallet with Sepolia Test ETH
* A .env file in the root of the WPF project containing: CONTRACT_ADDRESS, INFURA_RPC_URL, METAMASK_PRIVATE_KEY, WOLFRAM_API_KEY
  
## Licence
* This project is licensed under the MIT License.
