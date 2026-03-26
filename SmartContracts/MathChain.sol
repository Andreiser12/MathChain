//SPDX-License-Identifier: MIT
pragma solidity ^0.8.30;

contract MathChain{

    address public owner;
    uint256 public viewSolutionFee;

    constructor(){
        owner = msg.sender;
        viewSolutionFee = 500000000000000;
    }

    struct Solution{
        address wallet;
        uint256 problemId;
        int256 scaledSolution;
        bool isPaid;
        uint256 timestamp;

    }

    mapping(uint256 => Solution) public solutions;

    event SolutionSubmitted(address wallet, uint256 problemId);
    event SolutionPaid(address wallet, uint256 problemId);

    function submitSolution(uint256 problemId, int256 scaledSolution) external{
        solutions[problemId] = Solution({
            wallet: msg.sender,
            problemId: problemId,
            scaledSolution: scaledSolution,
            isPaid: false,
            timestamp: block.timestamp
        });

        emit SolutionSubmitted(msg.sender, problemId);
    }

    function payForSolution(uint256 problemId) external payable {

        require(msg.value == viewSolutionFee, "Wrong fee!");
        (bool success, ) = payable(owner).call{value: msg.value}("");
        require(success, "Transfer failed");
        solutions[problemId].isPaid = true;
        emit SolutionPaid(msg.sender, problemId);
    }

    function getWalletBalance(address wallet) public view returns(uint256){
        return address(wallet).balance;
    }

}