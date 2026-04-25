#  MathChain: Web3 Educational SaaS

![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white)
![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![C#](https://img.shields.io/badge/C%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![Blazor](https://img.shields.io/badge/blazor-%235C2D91.svg?style=for-the-badge&logo=blazor&logoColor=white)
![WebAssembly](https://img.shields.io/badge/webassembly-%23654FF0.svg?style=for-the-badge&logo=webassembly&logoColor=white)
![HTML5](https://img.shields.io/badge/html5-%23E34F26.svg?style=for-the-badge&logo=html5&logoColor=white)
![CSS](https://img.shields.io/badge/css-%23663399.svg?style=for-the-badge&logo=css&logoColor=white)
![JavaScript](https://img.shields.io/badge/javascript-%23323330.svg?style=for-the-badge&logo=javascript&logoColor=%23F7DF1E)
![Swagger](https://img.shields.io/badge/-Swagger-%23Clojure?style=for-the-badge&logo=swagger&logoColor=white)
![LaTeX](https://img.shields.io/badge/latex-%23008080.svg?style=for-the-badge&logo=latex&logoColor=white)
![Remix](https://img.shields.io/badge/remix-%23000.svg?style=for-the-badge&logo=remix&logoColor=white)
![Solidity](https://img.shields.io/badge/Solidity-%23363636.svg?style=for-the-badge&logo=solidity&logoColor=white)
![Ethereum](https://img.shields.io/badge/Ethereum-3C3C3D?style=for-the-badge&logo=Ethereum&logoColor=white)
![Blender](https://img.shields.io/badge/blender-%23F5792A.svg?style=for-the-badge&logo=blender&logoColor=white)


##  Overview
  MathChain is a decentralized educational platform that bridges 
advanced mathematics, 3D visualization, and Web3 technology. 
It transforms traditional 2D mathematical formulas into interactive 
3D meshes and leverages a decentralized pay-per-view SaaS model 
for step-by-step problem-solving.It was initially conceived as a **desktop application using 
WPF**, where core concepts like 
OOP architecture, MVVM pattern, Helix Toolkit 3D rendering, and 
Nethereum blockchain integration were designed and implemented. 

  During development, it became clear that a **Web3 application 
belongs in the browser**. This insight led to a architectural pivot 
toward **Blazor WebAssembly**, replacing the desktop frontend while 
preserving the entire backend, blockchain layer, and domain logic.

  The WPF prototype remains in the repository as a testament to the 
iterative development process and demonstrates proficiency in both 
desktop and web C# development paradigms.

##  Key Features
- **Interactive 3D Math Visualization**: Renders mathematical 
  formulas as interactive 3D meshes.
- **Algorithmic Problem Generation**: Integrates Wolfram Alpha API 
  to dynamically generate, compute, and validate calculus integrals 
  off-chain.
- **Web3 Micro-Transactions**: Custom Solidity Smart Contract on 
  Ethereum Sepolia testnet handles payments for unlocking 
  step-by-step solutions.
- **Decentralized Authentication**: MetaMask wallet login replaces 
  vulnerable centralized authentication systems.

## Tech Stack

### Frontend
- **Blazor WebAssembly** — primary web interface
- **MudBlazor** — UI component library
- **WPF + Helix Toolkit** — desktop prototype with 3D rendering
- **WpfMath** — LaTeX formula rendering
- **JavaScript Interop** — MetaMask integration


### Backend
- **ASP.NET Core Web API** — REST API layer
- **Wolfram Alpha API** — mathematical validation
- **C# / .NET 8**

### Blockchain / Web3
- **Solidity** — smart contract language
- **Remix IDE** — contract development and deployment
- **Ethereum Sepolia Testnet** — blockchain network
- **Nethereum** — C# Web3 integration
- **WalletConnect v2** — wallet connectivity

## Architecture
MathChain uses a hybrid architecture to maximize efficiency 
and minimize gas fees:

1. **Off-Chain Validation**: Mathematical validation is handled 
   by Wolfram Alpha for zero-latency feedback.
2. **On-Chain SaaS Logic**: Premium content unlocking is handled 
   by a Smart Contract — a trustless, automated vending machine 
   for educational content.
3. **Decentralized Identity**: User identity is established through 
   MetaMask wallet signatures — no passwords, no database.

## Prerequisites
- Visual Studio 2022
- .NET 8 SDK
- MetaMask browser extension
- A Wolfram Alpha Developer AppID
- An Infura/Alchemy RPC URL for Sepolia
- A MetaMask wallet with Sepolia Test ETH
- A WalletConnect Cloud Project ID
- A .env file in the root of the WPF project containing: CONTRACT_ADDRESS, INFURA_RPC_URL, METAMASK_PRIVATE_KEY, WOLFRAM_API_KEY and PROJECT_ID
  
## Licence
* This project is licensed under the MIT License.
