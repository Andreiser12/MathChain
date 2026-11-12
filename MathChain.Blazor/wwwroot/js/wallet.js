window.getMetaMaskProvider = function() {
    if (typeof window.ethereum === "undefined") return null;
    if (window.ethereum.providers && window.ethereum.providers.length > 0) {
        const found = window.ethereum.providers.find(p => p.isMetaMask && !p.isPhantom);
        if (found) return found;
    }
    if (window.ethereum.isMetaMask && !window.ethereum.isPhantom) {
        return window.ethereum;
    }
    return window.ethereum;
};

window.connectMetaMask = async function() {
    try {
        console.log("[MetaMask] Inițiere conectare MetaMask...");
        const provider = window.getMetaMaskProvider();
        if (!provider) {
            alert("Extensia MetaMask nu a fost găsită în browser! Asigură-te că este instalată și activată.");
            return null;
        }

        try {
            await provider.request({
                method: 'wallet_requestPermissions',
                params: [{ eth_accounts: {} }]
            });
        } catch (e) {
            console.log("[MetaMask] wallet_requestPermissions info:", e);
        }

        const accounts = await provider.request({
            method: 'eth_requestAccounts'
        });

        console.log("[MetaMask] Conturi primite:", accounts);
        if (!accounts || accounts.length === 0) return null;
        window.mathchainProvider = provider;
        return accounts[0];
    } catch (err) {
        console.error("[MetaMask] Eroare connectMetaMask:", err);
        return null;
    }
};

window.connectPhantom = async function() {
    try {
        const evmProvider = window.phantom?.ethereum ||
            (window.ethereum?.isPhantom ? window.ethereum : null) ||
            (window.ethereum?.providers ? window.ethereum.providers.find(p => p.isPhantom) : null);

        if (!evmProvider) {
            alert("Phantom was not found or its Ethereum support is disabled. Install Phantom and enable Ethereum in its settings.");
            return null;
        }

        const accounts = await evmProvider.request({ method: 'eth_requestAccounts' });
        if (!accounts || accounts.length === 0) return null;

        window.mathchainProvider = evmProvider;
        return accounts[0];
    } catch (err) {
        console.error("[Phantom] connect failed:", err);
        return null;
    }
};

window.mathchainProvider = null;

window.getWalletChainId = async function() {
    const provider = window.mathchainProvider;
    if (!provider) {
        throw new Error("No wallet is connected.");
    }

    const chainIdHex = await provider.request({ method: 'eth_chainId' });
    return parseInt(chainIdHex, 16);
};

window.signLoginMessage = async function(address, message) {
    const provider = window.mathchainProvider;
    if (!provider) {
        throw new Error("No wallet is connected.");
    }

    const bytes = new TextEncoder().encode(message);
    const hexMessage = "0x" + Array.from(bytes, b => b.toString(16).padStart(2, "0")).join("");

    return await provider.request({
        method: 'personal_sign',
        params: [hexMessage, address]
    });
};
