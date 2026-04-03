
async function connectMetaMask(){
    if(typeof window.ethereum === "undefined"){
    return null;
    }

    const accounts = await window.ethereum.request({
    method: 'eth_requestAccounts'
    });

    return accounts[0];
}

