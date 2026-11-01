document.addEventListener("DOMContentLoaded", () => {
    const token = new URLSearchParams(window.location.search).get("token");
    const requestForm = document.getElementById("requestForm");
    const resetForm = document.getElementById("resetForm");
    const title = document.getElementById("pageTitle");
    const description = document.getElementById("pageDescription");

    if (token) {
        requestForm.hidden = true;
        resetForm.hidden = false;
        title.textContent = "Criar nova senha";
        description.textContent = "Escolha uma senha com pelo menos 8 caracteres.";
    }

    requestForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        const button = requestForm.querySelector("button[type='submit']");
        const message = document.getElementById("requestMessage");
        button.disabled = true;
        message.textContent = "Enviando...";

        try {
            const response = await fetch("/api/v1/auth/recuperar-senha", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email: document.getElementById("email").value.trim() })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result?.detail || result?.mensagem || "Não foi possível enviar as instruções.");
            message.textContent = result.mensagem || "Se houver uma conta ativa com esse e-mail, enviaremos as instruções de recuperação.";
            requestForm.reset();
        } catch (error) {
            message.textContent = error instanceof TypeError
                ? "Não foi possível conectar ao servidor. Tente novamente."
                : error.message;
        } finally {
            button.disabled = false;
        }
    });

    resetForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        const password = document.getElementById("newPassword").value;
        const confirmation = document.getElementById("confirmPassword").value;
        const button = resetForm.querySelector("button[type='submit']");
        const message = document.getElementById("resetMessage");

        if (password !== confirmation) {
            message.textContent = "As senhas não coincidem.";
            return;
        }

        button.disabled = true;
        message.textContent = "Salvando...";
        try {
            const response = await fetch("/api/v1/auth/redefinir-senha", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ token, novaSenha: password, confirmacaoSenha: confirmation })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result?.detail || result?.mensagem || "O link é inválido ou expirou.");
            message.textContent = "Senha atualizada. Você já pode entrar com a nova senha.";
            resetForm.reset();
            button.disabled = true;
            window.history.replaceState({}, document.title, window.location.pathname);
        } catch (error) {
            message.textContent = error instanceof TypeError
                ? "Não foi possível conectar ao servidor. Tente novamente."
                : error.message;
            button.disabled = false;
        }
    });
});
