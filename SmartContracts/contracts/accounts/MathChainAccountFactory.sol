// SPDX-License-Identifier: GPL-3.0
pragma solidity ^0.8.30;

import {Create2} from "@openzeppelin/contracts/utils/Create2.sol";
import {ERC1967Proxy} from "@openzeppelin/contracts/proxy/ERC1967/ERC1967Proxy.sol";
import {IEntryPoint} from "@account-abstraction/contracts/interfaces/IEntryPoint.sol";
import {SimpleAccount} from "@account-abstraction/contracts/samples/SimpleAccount.sol";
import {MathChainAccount} from "./MathChainAccount.sol";

contract MathChainAccountFactory {
    MathChainAccount public immutable accountImplementation;

    event AccountCreated(address indexed account, address indexed owner, uint256 salt);

    error EntryPointRequired();
    error OwnerRequired();

    constructor(IEntryPoint anEntryPoint) {
        if (address(anEntryPoint) == address(0)) {
            revert EntryPointRequired();
        }

        accountImplementation = new MathChainAccount(anEntryPoint);
    }

    function createAccount(address owner, uint256 salt) external returns (MathChainAccount account) {
        if (owner == address(0)) {
            revert OwnerRequired();
        }

        address predicted = accountAddress(owner, salt);

        if (predicted.code.length > 0) {
            return MathChainAccount(payable(predicted));
        }

        account = MathChainAccount(payable(new ERC1967Proxy{salt: bytes32(salt)}(
            address(accountImplementation),
            abi.encodeCall(SimpleAccount.initialize, (owner))
        )));

        emit AccountCreated(address(account), owner, salt);
    }

    function accountAddress(address owner, uint256 salt) public view returns (address) {
        return Create2.computeAddress(bytes32(salt), keccak256(abi.encodePacked(
            type(ERC1967Proxy).creationCode,
            abi.encode(address(accountImplementation), abi.encodeCall(SimpleAccount.initialize, (owner)))
        )));
    }
}
