// SPDX-License-Identifier: MIT
pragma solidity ^0.8.30;

import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";

contract CurriculumRegistry is AccessControl {
    bytes32 public constant PUBLISHER_ROLE = keccak256("PUBLISHER_ROLE");

    uint256 private constant CID_LENGTH = 36;
    bytes1 private constant CID_VERSION = 0x01;
    bytes1 private constant RAW_CODEC = 0x55;
    bytes1 private constant DAG_PB_CODEC = 0x70;
    bytes1 private constant SHA2_256 = 0x12;
    bytes1 private constant DIGEST_LENGTH = 0x20;

    struct Publication {
        bytes cid;
        uint64 publishedAt;
        address publisher;
    }

    Publication[] private _publications;

    event CurriculumPublished(uint256 indexed version, bytes cid, address indexed publisher, uint64 publishedAt);

    error AdminRequired();
    error InvalidContentIdentifier();
    error AlreadyPublished(uint256 version);
    error NoPublication();
    error UnknownVersion(uint256 version);

    constructor(address admin, address publisher) {
        if (admin == address(0)) {
            revert AdminRequired();
        }

        _grantRole(DEFAULT_ADMIN_ROLE, admin);

        if (publisher != address(0)) {
            _grantRole(PUBLISHER_ROLE, publisher);
        }
    }

    function publish(bytes calldata cid) external onlyRole(PUBLISHER_ROLE) returns (uint256 version) {
        if (!isValidContentIdentifier(cid)) {
            revert InvalidContentIdentifier();
        }

        uint256 count = _publications.length;

        if (count > 0 && keccak256(_publications[count - 1].cid) == keccak256(cid)) {
            revert AlreadyPublished(count);
        }

        uint64 publishedAt = uint64(block.timestamp);
        _publications.push(Publication({cid: cid, publishedAt: publishedAt, publisher: msg.sender}));
        version = count + 1;

        emit CurriculumPublished(version, cid, msg.sender, publishedAt);
    }

    function latest() external view returns (uint256 version, Publication memory publication) {
        uint256 count = _publications.length;

        if (count == 0) {
            revert NoPublication();
        }

        return (count, _publications[count - 1]);
    }

    function publicationAt(uint256 version) external view returns (Publication memory) {
        if (version == 0 || version > _publications.length) {
            revert UnknownVersion(version);
        }

        return _publications[version - 1];
    }

    function publicationCount() external view returns (uint256) {
        return _publications.length;
    }

    function isValidContentIdentifier(bytes calldata cid) public pure returns (bool) {
        return cid.length == CID_LENGTH
            && cid[0] == CID_VERSION
            && (cid[1] == RAW_CODEC || cid[1] == DAG_PB_CODEC)
            && cid[2] == SHA2_256
            && cid[3] == DIGEST_LENGTH;
    }
}
