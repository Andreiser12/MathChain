// SPDX-License-Identifier: MIT
pragma solidity ^0.8.30;

import {ERC721} from "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";

interface IERC5192 {
    event Locked(uint256 tokenId);
    event Unlocked(uint256 tokenId);

    function locked(uint256 tokenId) external view returns (bool);
}

contract MathChainCertificates is ERC721, AccessControl, IERC5192 {
    enum CertificateKind {
        Class,
        Chapter
    }

    struct Certificate {
        address student;
        uint64 issuedAt;
        CertificateKind kind;
        bool revoked;
        address issuer;
        bytes32 subjectId;
        bytes32 dataHash;
    }

    bytes32 public constant ISSUER_ROLE = keccak256("ISSUER_ROLE");

    uint256 private _lastTokenId;
    mapping(uint256 tokenId => Certificate) private _certificates;
    mapping(bytes32 certificateKey => uint256 tokenId) private _activeTokenIds;

    event CertificateIssued(
        uint256 indexed tokenId,
        address indexed student,
        address indexed issuer,
        CertificateKind kind,
        bytes32 subjectId,
        bytes32 dataHash
    );
    event CertificateRevoked(uint256 indexed tokenId, address indexed revokedBy);

    error AdminRequired();
    error StudentRequired();
    error BatchLengthMismatch(uint256 students, uint256 dataHashes);
    error CertificateAlreadyIssued(uint256 tokenId);
    error CertificateNotFound(uint256 tokenId);
    error CertificateAlreadyRevoked(uint256 tokenId);
    error CertificateNotTransferable();
    error NotAllowedToRevoke(address account);

    constructor(address admin) ERC721("MathChain Certificate", "MCC") {
        if (admin == address(0)) {
            revert AdminRequired();
        }

        _grantRole(DEFAULT_ADMIN_ROLE, admin);
    }

    function issue(
        address student,
        CertificateKind kind,
        bytes32 subjectId,
        bytes32 dataHash
    ) external onlyRole(ISSUER_ROLE) returns (uint256) {
        return _issue(student, kind, subjectId, dataHash);
    }

    function issueBatch(
        address[] calldata students,
        CertificateKind kind,
        bytes32 subjectId,
        bytes32[] calldata dataHashes
    ) external onlyRole(ISSUER_ROLE) returns (uint256[] memory tokenIds) {
        if (students.length != dataHashes.length) {
            revert BatchLengthMismatch(students.length, dataHashes.length);
        }

        tokenIds = new uint256[](students.length);

        for (uint256 i = 0; i < students.length; i++) {
            tokenIds[i] = _issue(students[i], kind, subjectId, dataHashes[i]);
        }
    }

    function revoke(uint256 tokenId) external {
        Certificate storage certificate = _certificates[tokenId];

        if (certificate.student == address(0)) {
            revert CertificateNotFound(tokenId);
        }

        if (certificate.revoked) {
            revert CertificateAlreadyRevoked(tokenId);
        }

        bool isActiveIssuer = msg.sender == certificate.issuer && hasRole(ISSUER_ROLE, msg.sender);

        if (!isActiveIssuer && !hasRole(DEFAULT_ADMIN_ROLE, msg.sender)) {
            revert NotAllowedToRevoke(msg.sender);
        }

        certificate.revoked = true;
        delete _activeTokenIds[_certificateKey(certificate.student, certificate.kind, certificate.subjectId)];
        _burn(tokenId);

        emit CertificateRevoked(tokenId, msg.sender);
    }

    function getCertificate(uint256 tokenId) external view returns (Certificate memory) {
        Certificate memory certificate = _certificates[tokenId];

        if (certificate.student == address(0)) {
            revert CertificateNotFound(tokenId);
        }

        return certificate;
    }

    function findCertificate(
        address student,
        CertificateKind kind,
        bytes32 subjectId
    ) external view returns (uint256) {
        return _activeTokenIds[_certificateKey(student, kind, subjectId)];
    }

    function isValid(uint256 tokenId, bytes32 dataHash) external view returns (bool) {
        Certificate storage certificate = _certificates[tokenId];

        return certificate.student != address(0) && !certificate.revoked && certificate.dataHash == dataHash;
    }

    function totalIssued() external view returns (uint256) {
        return _lastTokenId;
    }

    function locked(uint256 tokenId) external view returns (bool) {
        _requireOwned(tokenId);

        return true;
    }

    function supportsInterface(bytes4 interfaceId) public view override(ERC721, AccessControl) returns (bool) {
        return interfaceId == type(IERC5192).interfaceId || super.supportsInterface(interfaceId);
    }

    function _update(address to, uint256 tokenId, address auth) internal override returns (address) {
        address from = _ownerOf(tokenId);

        if (from != address(0) && to != address(0)) {
            revert CertificateNotTransferable();
        }

        return super._update(to, tokenId, auth);
    }

    function _issue(
        address student,
        CertificateKind kind,
        bytes32 subjectId,
        bytes32 dataHash
    ) private returns (uint256 tokenId) {
        if (student == address(0)) {
            revert StudentRequired();
        }

        bytes32 certificateKey = _certificateKey(student, kind, subjectId);
        uint256 activeTokenId = _activeTokenIds[certificateKey];

        if (activeTokenId != 0) {
            revert CertificateAlreadyIssued(activeTokenId);
        }

        tokenId = ++_lastTokenId;
        _activeTokenIds[certificateKey] = tokenId;
        _certificates[tokenId] = Certificate({
            student: student,
            issuedAt: uint64(block.timestamp),
            kind: kind,
            revoked: false,
            issuer: msg.sender,
            subjectId: subjectId,
            dataHash: dataHash
        });

        _mint(student, tokenId);

        emit Locked(tokenId);
        emit CertificateIssued(tokenId, student, msg.sender, kind, subjectId, dataHash);
    }

    function _certificateKey(
        address student,
        CertificateKind kind,
        bytes32 subjectId
    ) private pure returns (bytes32) {
        return keccak256(abi.encode(student, kind, subjectId));
    }
}
