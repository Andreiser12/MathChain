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
        return accounts && accounts.length > 0 ? accounts[0] : null;
    } catch (err) {
        console.error("[MetaMask] Eroare connectMetaMask:", err);
        return null;
    }
};

window.connectPhantom = async function() {
    try {
        console.log("[Phantom] Inițiere conectare Phantom...");

        const hasPhantom = (typeof window.phantom !== "undefined") || 
                           (typeof window.solana !== "undefined" && window.solana.isPhantom);

        if (!hasPhantom) {
            alert("Extensia Phantom nu a fost găsită în browser! Asigură-te că extensia Phantom este instalată din Chrome Web Store.");
            return null;
        }

        // 1. Încercăm întâi provider-ul nativ Phantom Solana (cel mai răspândit)
        const solanaProvider = window.phantom?.solana || (window.solana?.isPhantom ? window.solana : null);
        if (solanaProvider) {
            try {
                console.log("[Phantom] Apelăm solanaProvider.connect()...");
                const resp = await solanaProvider.connect();
                if (resp && resp.publicKey) {
                    const address = resp.publicKey.toString();
                    console.log("[Phantom] Conectat cu succes (Solana):", address);
                    return address;
                }
            } catch (solErr) {
                console.warn("[Phantom] Solana connect nu a reușit sau a fost anulat:", solErr);
            }
        }

        // 2. Încercăm provider-ul EVM (Ethereum / Polygon) din Phantom
        const evmProvider = window.phantom?.ethereum || 
            (window.ethereum?.isPhantom ? window.ethereum : null) ||
            (window.ethereum?.providers ? window.ethereum.providers.find(p => p.isPhantom) : null);

        if (evmProvider) {
            try {
                console.log("[Phantom] Apelăm evmProvider eth_requestAccounts...");
                const accounts = await evmProvider.request({
                    method: 'eth_requestAccounts'
                });
                if (accounts && accounts.length > 0) {
                    console.log("[Phantom] Conectat cu succes (EVM):", accounts[0]);
                    return accounts[0];
                }
            } catch (evmErr) {
                console.error("[Phantom] EVM connect nu a reușit:", evmErr);
            }
        }

        return null;
    } catch (err) {
        console.error("[Phantom] Eroare connectPhantom:", err);
        return null;
    }
};
