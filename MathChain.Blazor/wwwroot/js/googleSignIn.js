window.renderGoogleSignInButton = async function (elementId, clientId, dotNetReference) {
    const deadline = Date.now() + 10000;

    while (!(window.google && window.google.accounts && window.google.accounts.id)) {
        if (Date.now() > deadline) {
            console.warn("[Google] The Google Identity Services script did not load.");
            return false;
        }

        await new Promise(resolve => setTimeout(resolve, 100));
    }

    const element = document.getElementById(elementId);
    if (!element) {
        return false;
    }

    window.google.accounts.id.initialize({
        client_id: clientId,
        callback: response => dotNetReference.invokeMethodAsync("OnGoogleCredential", response.credential),
        auto_select: false
    });

    window.google.accounts.id.renderButton(element, {
        type: "standard",
        theme: "filled_blue",
        size: "large",
        text: "signin_with",
        shape: "rectangular",
        width: 320
    });

    return true;
};
