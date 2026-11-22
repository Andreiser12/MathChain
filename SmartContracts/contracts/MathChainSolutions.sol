// SPDX-License-Identifier: MIT
pragma solidity ^0.8.30;

import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import {SafeERC20} from "@openzeppelin/contracts/token/ERC20/utils/SafeERC20.sol";

interface IERC3009 {
    function receiveWithAuthorization(
        address from,
        address to,
        uint256 value,
        uint256 validAfter,
        uint256 validBefore,
        bytes32 nonce,
        uint8 v,
        bytes32 r,
        bytes32 s
    ) external;
}

contract MathChainSolutions is AccessControl {
    using SafeERC20 for IERC20;

    enum Tier {
        Easy,
        Medium,
        Hard
    }

    struct Authorization {
        address payer;
        uint256 value;
        uint256 validAfter;
        uint256 validBefore;
        uint8 v;
        bytes32 r;
        bytes32 s;
    }

    IERC20 public immutable paymentToken;
    address public treasury;

    mapping(Tier tier => uint256 price) private _prices;
    mapping(bytes32 purchaseKey => bool purchased) private _purchases;

    event SolutionPurchased(
        address indexed account,
        bytes32 indexed solutionId,
        address indexed payer,
        Tier tier,
        uint256 price
    );
    event PriceChanged(Tier indexed tier, uint256 price);
    event TreasuryChanged(address indexed treasury);

    error AdminRequired();
    error PaymentTokenRequired();
    error TreasuryRequired();
    error AccountRequired();
    error PriceRequired();
    error AlreadyPurchased(address account, bytes32 solutionId);
    error PriceMismatch(uint256 price, uint256 offered);

    constructor(
        address admin,
        IERC20 token,
        address initialTreasury,
        uint256 easyPrice,
        uint256 mediumPrice,
        uint256 hardPrice
    ) {
        if (admin == address(0)) {
            revert AdminRequired();
        }

        if (address(token) == address(0)) {
            revert PaymentTokenRequired();
        }

        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        paymentToken = token;
        _setTreasury(initialTreasury);
        _setPrice(Tier.Easy, easyPrice);
        _setPrice(Tier.Medium, mediumPrice);
        _setPrice(Tier.Hard, hardPrice);
    }

    function buy(
        address account,
        bytes32 solutionId,
        Tier tier,
        bytes32 salt,
        Authorization calldata authorization
    ) external {
        if (account == address(0)) {
            revert AccountRequired();
        }

        bytes32 key = purchaseKey(account, solutionId);

        if (_purchases[key]) {
            revert AlreadyPurchased(account, solutionId);
        }

        uint256 price = _prices[tier];

        if (authorization.value != price) {
            revert PriceMismatch(price, authorization.value);
        }

        _purchases[key] = true;

        IERC3009(address(paymentToken)).receiveWithAuthorization(
            authorization.payer,
            address(this),
            price,
            authorization.validAfter,
            authorization.validBefore,
            purchaseNonce(account, solutionId, tier, salt),
            authorization.v,
            authorization.r,
            authorization.s
        );

        paymentToken.safeTransfer(treasury, price);

        emit SolutionPurchased(account, solutionId, authorization.payer, tier, price);
    }

    function setPrice(Tier tier, uint256 price) external onlyRole(DEFAULT_ADMIN_ROLE) {
        _setPrice(tier, price);
    }

    function setTreasury(address newTreasury) external onlyRole(DEFAULT_ADMIN_ROLE) {
        _setTreasury(newTreasury);
    }

    function priceOf(Tier tier) external view returns (uint256) {
        return _prices[tier];
    }

    function prices() external view returns (uint256 easy, uint256 medium, uint256 hard) {
        return (_prices[Tier.Easy], _prices[Tier.Medium], _prices[Tier.Hard]);
    }

    function isPurchased(address account, bytes32 solutionId) external view returns (bool) {
        return _purchases[purchaseKey(account, solutionId)];
    }

    function purchaseKey(address account, bytes32 solutionId) public pure returns (bytes32) {
        return keccak256(abi.encode(account, solutionId));
    }

    function purchaseNonce(
        address account,
        bytes32 solutionId,
        Tier tier,
        bytes32 salt
    ) public view returns (bytes32) {
        return keccak256(abi.encode(block.chainid, address(this), account, solutionId, tier, salt));
    }

    function _setPrice(Tier tier, uint256 price) private {
        if (price == 0) {
            revert PriceRequired();
        }

        _prices[tier] = price;

        emit PriceChanged(tier, price);
    }

    function _setTreasury(address newTreasury) private {
        if (newTreasury == address(0)) {
            revert TreasuryRequired();
        }

        treasury = newTreasury;

        emit TreasuryChanged(newTreasury);
    }
}
