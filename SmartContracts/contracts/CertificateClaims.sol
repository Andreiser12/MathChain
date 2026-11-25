// SPDX-License-Identifier: MIT
pragma solidity ^0.8.30;

import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";
import {ECDSA} from "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import {EIP712} from "@openzeppelin/contracts/utils/cryptography/EIP712.sol";

interface IMathChainCertificates {
    function issue(address student, uint8 kind, bytes32 subjectId, bytes32 dataHash) external returns (uint256);

    function revoke(uint256 tokenId) external;
}

contract CertificateClaims is AccessControl, EIP712 {
    bytes32 public constant OPERATOR_ROLE = keccak256("OPERATOR_ROLE");
    bytes32 public constant CLAIM_TYPEHASH =
        keccak256("Claim(address student,uint8 kind,bytes32 subjectId,bytes32 dataHash,uint256 deadline)");

    IMathChainCertificates public immutable certificates;
    address public voucherSigner;

    mapping(bytes32 digest => bool used) private _usedVouchers;

    event VoucherSignerChanged(address indexed voucherSigner);
    event CertificateClaimed(uint256 indexed tokenId, address indexed student, uint8 kind, bytes32 subjectId);

    error AdminRequired();
    error CertificatesRequired();
    error VoucherSignerRequired();
    error VoucherExpired(uint256 deadline);
    error VoucherAlreadyUsed();
    error InvalidVoucher();

    constructor(address admin, IMathChainCertificates certificatesContract, address initialVoucherSigner)
        EIP712("MathChain Certificate Claims", "1")
    {
        if (admin == address(0)) {
            revert AdminRequired();
        }

        if (address(certificatesContract) == address(0)) {
            revert CertificatesRequired();
        }

        certificates = certificatesContract;
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _setVoucherSigner(initialVoucherSigner);
        _grantRole(OPERATOR_ROLE, initialVoucherSigner);
    }

    function claim(uint8 kind, bytes32 subjectId, bytes32 dataHash, uint256 deadline, bytes calldata signature)
        external
        returns (uint256 tokenId)
    {
        if (block.timestamp > deadline) {
            revert VoucherExpired(deadline);
        }

        bytes32 digest = voucherDigest(msg.sender, kind, subjectId, dataHash, deadline);

        if (_usedVouchers[digest]) {
            revert VoucherAlreadyUsed();
        }

        (address signer, ECDSA.RecoverError error, ) = ECDSA.tryRecover(digest, signature);

        if (error != ECDSA.RecoverError.NoError || signer != voucherSigner) {
            revert InvalidVoucher();
        }

        _usedVouchers[digest] = true;
        tokenId = certificates.issue(msg.sender, kind, subjectId, dataHash);

        emit CertificateClaimed(tokenId, msg.sender, kind, subjectId);
    }

    function revoke(uint256 tokenId) external onlyRole(OPERATOR_ROLE) {
        certificates.revoke(tokenId);
    }

    function setVoucherSigner(address newVoucherSigner) external onlyRole(DEFAULT_ADMIN_ROLE) {
        _setVoucherSigner(newVoucherSigner);
    }

    function isVoucherUsed(bytes32 digest) external view returns (bool) {
        return _usedVouchers[digest];
    }

    function voucherDigest(address student, uint8 kind, bytes32 subjectId, bytes32 dataHash, uint256 deadline)
        public
        view
        returns (bytes32)
    {
        return _hashTypedDataV4(keccak256(abi.encode(CLAIM_TYPEHASH, student, kind, subjectId, dataHash, deadline)));
    }

    function _setVoucherSigner(address newVoucherSigner) private {
        if (newVoucherSigner == address(0)) {
            revert VoucherSignerRequired();
        }

        voucherSigner = newVoucherSigner;

        emit VoucherSignerChanged(newVoucherSigner);
    }
}
