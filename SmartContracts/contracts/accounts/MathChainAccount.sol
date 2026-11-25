// SPDX-License-Identifier: GPL-3.0
pragma solidity ^0.8.30;

import {IEntryPoint} from "@account-abstraction/contracts/interfaces/IEntryPoint.sol";
import {SimpleAccount} from "@account-abstraction/contracts/samples/SimpleAccount.sol";

contract MathChainAccount is SimpleAccount {
    constructor(IEntryPoint anEntryPoint) SimpleAccount(anEntryPoint) {
    }
}
