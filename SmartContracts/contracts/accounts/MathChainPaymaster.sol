// SPDX-License-Identifier: GPL-3.0
pragma solidity ^0.8.30;

import {ECDSA} from "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import {MessageHashUtils} from "@openzeppelin/contracts/utils/cryptography/MessageHashUtils.sol";
import {BasePaymaster} from "@account-abstraction/contracts/core/BasePaymaster.sol";
import {UserOperationLib} from "@account-abstraction/contracts/core/UserOperationLib.sol";
import {_packValidationData} from "@account-abstraction/contracts/core/Helpers.sol";
import {IEntryPoint} from "@account-abstraction/contracts/interfaces/IEntryPoint.sol";
import {PackedUserOperation} from "@account-abstraction/contracts/interfaces/PackedUserOperation.sol";
import {SimpleAccount} from "@account-abstraction/contracts/samples/SimpleAccount.sol";

contract MathChainPaymaster is BasePaymaster {
    using UserOperationLib for PackedUserOperation;

    uint256 private constant VALID_TIMESTAMP_OFFSET = PAYMASTER_DATA_OFFSET;
    uint256 private constant SIGNATURE_OFFSET = VALID_TIMESTAMP_OFFSET + 64;
    uint256 private constant SIGNATURE_LENGTH = 65;

    address public sponsorSigner;
    address public sponsoredTarget;
    bytes4 public sponsoredSelector;
    uint256 public maxCostPerOperation;

    event SponsorSignerChanged(address indexed sponsorSigner);
    event SponsoredCallChanged(address indexed target, bytes4 selector);
    event MaxCostPerOperationChanged(uint256 maxCost);

    error OwnerRequired();
    error SponsorSignerRequired();
    error SponsoredTargetRequired();
    error MaxCostRequired();
    error CallNotSponsored();
    error CostTooHigh(uint256 maxCost, uint256 limit);
    error InvalidPaymasterData();

    constructor(
        IEntryPoint anEntryPoint,
        address initialOwner,
        address initialSponsorSigner,
        address target,
        bytes4 selector,
        uint256 maxCost
    ) BasePaymaster(anEntryPoint) {
        if (initialOwner == address(0)) {
            revert OwnerRequired();
        }

        _transferOwnership(initialOwner);
        _setSponsorSigner(initialSponsorSigner);
        _setSponsoredCall(target, selector);
        _setMaxCostPerOperation(maxCost);
    }

    function getHash(PackedUserOperation calldata userOp, uint48 validUntil, uint48 validAfter) public view returns (bytes32) {
        return keccak256(abi.encode(
            userOp.getSender(),
            userOp.nonce,
            keccak256(userOp.initCode),
            keccak256(userOp.callData),
            userOp.accountGasLimits,
            uint256(bytes32(userOp.paymasterAndData[PAYMASTER_VALIDATION_GAS_OFFSET:PAYMASTER_DATA_OFFSET])),
            userOp.preVerificationGas,
            userOp.gasFees,
            block.chainid,
            address(this),
            validUntil,
            validAfter
        ));
    }

    function parsePaymasterAndData(bytes calldata paymasterAndData)
        public
        pure
        returns (uint48 validUntil, uint48 validAfter, bytes calldata signature)
    {
        if (paymasterAndData.length != SIGNATURE_OFFSET + SIGNATURE_LENGTH) {
            revert InvalidPaymasterData();
        }

        (validUntil, validAfter) = abi.decode(paymasterAndData[VALID_TIMESTAMP_OFFSET:SIGNATURE_OFFSET], (uint48, uint48));
        signature = paymasterAndData[SIGNATURE_OFFSET:];
    }

    function setSponsorSigner(address newSponsorSigner) external onlyOwner {
        _setSponsorSigner(newSponsorSigner);
    }

    function setSponsoredCall(address target, bytes4 selector) external onlyOwner {
        _setSponsoredCall(target, selector);
    }

    function setMaxCostPerOperation(uint256 maxCost) external onlyOwner {
        _setMaxCostPerOperation(maxCost);
    }

    function _validatePaymasterUserOp(PackedUserOperation calldata userOp, bytes32, uint256 maxCost)
        internal
        view
        override
        returns (bytes memory context, uint256 validationData)
    {
        if (maxCost > maxCostPerOperation) {
            revert CostTooHigh(maxCost, maxCostPerOperation);
        }

        _requireSponsoredCall(userOp.callData);

        (uint48 validUntil, uint48 validAfter, bytes calldata signature) = parsePaymasterAndData(userOp.paymasterAndData);
        bytes32 hash = MessageHashUtils.toEthSignedMessageHash(getHash(userOp, validUntil, validAfter));
        (address signer, ECDSA.RecoverError error, ) = ECDSA.tryRecover(hash, signature);
        bool signatureFailed = error != ECDSA.RecoverError.NoError || signer != sponsorSigner;

        return ("", _packValidationData(signatureFailed, validUntil, validAfter));
    }

    function _requireSponsoredCall(bytes calldata callData) private view {
        if (callData.length < 4 || bytes4(callData[:4]) != SimpleAccount.execute.selector) {
            revert CallNotSponsored();
        }

        (address target, uint256 value, bytes memory innerCall) = abi.decode(callData[4:], (address, uint256, bytes));

        if (target != sponsoredTarget || value != 0 || innerCall.length < 4 || bytes4(innerCall) != sponsoredSelector) {
            revert CallNotSponsored();
        }
    }

    function _setSponsorSigner(address newSponsorSigner) private {
        if (newSponsorSigner == address(0)) {
            revert SponsorSignerRequired();
        }

        sponsorSigner = newSponsorSigner;

        emit SponsorSignerChanged(newSponsorSigner);
    }

    function _setSponsoredCall(address target, bytes4 selector) private {
        if (target == address(0)) {
            revert SponsoredTargetRequired();
        }

        sponsoredTarget = target;
        sponsoredSelector = selector;

        emit SponsoredCallChanged(target, selector);
    }

    function _setMaxCostPerOperation(uint256 maxCost) private {
        if (maxCost == 0) {
            revert MaxCostRequired();
        }

        maxCostPerOperation = maxCost;

        emit MaxCostPerOperationChanged(maxCost);
    }
}
