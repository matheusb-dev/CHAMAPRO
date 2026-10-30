
/* =========================================
   CHAMAPRO - AUTENTICAÇÃO
   Arquivo: wwwroot/js/pages/autenticacao.js
========================================= */

document.addEventListener("DOMContentLoaded", () => {

    // ELEMENTOS DA TELA
    const loginForm = document.getElementById("loginForm");
    const emailInput = document.getElementById("email");
    const passwordInput = document.getElementById("password");
    const togglePassword = document.getElementById("togglePassword");
    const authMessage = document.getElementById("authMessage");
    const forgotPassword = document.getElementById("forgotPassword");

    // API ASP.NET CORE
    const API_LOGIN = "/api/v1/auth/login";

    // PÁGINAS POR PERFIL
    const paginasPorPerfil = {
        CLIENTE: "../cliente/painel.html",
        PROFISSIONAL: "../trabalhador/fila-chamados.html",
        ADMIN: "../admin/gestao.html"
    };

    // =====================================
    // MOSTRAR / ESCONDER SENHA
    // =====================================

    if (togglePassword && passwordInput) {
        togglePassword.addEventListener("click", () => {

            const mostrarSenha = passwordInput.type === "password";

            passwordInput.type = mostrarSenha ? "text" : "password";

            togglePassword.setAttribute(
                "aria-label",
                mostrarSenha ? "Ocultar senha" : "Mostrar senha"
            );
        });
    }

    // =====================================
    // MENSAGENS DO FORMULÁRIO
    // =====================================

    function showMessage(message) {
        if (!authMessage) return;

        authMessage.textContent = message;
        authMessage.classList.add("visible");
    }

    function clearMessage() {
        if (!authMessage) return;

        authMessage.textContent = "";
        authMessage.classList.remove("visible");
    }

    // =====================================
    // IDENTIFICAR DESTINO DO USUÁRIO
    // =====================================

    function obterPaginaPorPerfil(perfilOriginal) {

        const perfil = String(perfilOriginal || "")
            .trim()
            .toUpperCase();

        return paginasPorPerfil[perfil] || null;
    }

    // =====================================
    // REALIZAR LOGIN
    // =====================================

    if (loginForm) {

        loginForm.addEventListener("submit", async (event) => {

            event.preventDefault();
            clearMessage();

            const email = emailInput?.value.trim() ?? "";
            const senha = passwordInput?.value ?? "";

            if (!email || !senha) {
                showMessage("Preencha seu e-mail e sua senha.");
                return;
            }

            const submitButton = loginForm.querySelector(
                'button[type="submit"]'
            );

            const textoOriginalBotao = submitButton?.textContent;

            if (submitButton) {
                submitButton.disabled = true;
                submitButton.textContent = "Entrando...";
            }

            try {

                // ENVIA CREDENCIAIS PARA A API
                const response = await fetch(API_LOGIN, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        email: email,
                        senha: senha
                    })
                });

                // LÊ A RESPOSTA DO SERVIDOR
                const resultado = await response.json();

                // TRATAMENTO DE ERROS
                if (!response.ok) {

                    if (
                        response.status === 400 ||
                        response.status === 401 ||
                        response.status === 403
                    ) {
                        throw new Error(
                            "E-mail ou senha inválidos."
                        );
                    }

                    if (response.status === 429) {
                        throw new Error(
                            "Muitas tentativas de login. Aguarde e tente novamente."
                        );
                    }

                    throw new Error(
                        resultado?.message ||
                        resultado?.mensagem ||
                        "Não foi possível realizar o login."
                    );
                }

                // VERIFICA DADOS RETORNADOS
                if (!resultado.token || !resultado.usuario) {
                    throw new Error(
                        "O servidor retornou uma resposta de login inválida."
                    );
                }

                // IDENTIFICA PERFIL
                const perfil = String(
                    resultado.usuario.perfil || ""
                ).trim().toUpperCase();

                // DEFINE PÁGINA DE DESTINO
                const destino = obterPaginaPorPerfil(perfil);

                if (!destino) {
                    throw new Error(
                        "Perfil de usuário não reconhecido: " + perfil
                    );
                }

                // =====================================
                // SALVAR SESSÃO
                // =====================================

                sessionStorage.setItem(
                    "chamapro_token",
                    resultado.token
                );

                sessionStorage.setItem(
                    "chamapro_usuario",
                    JSON.stringify(resultado.usuario)
                );

                sessionStorage.setItem(
                    "chamapro_expira_em",
                    resultado.expiraEm || ""
                );

                // =====================================
                // REDIRECIONAR USUÁRIO
                // =====================================

                window.location.replace(destino);

            } catch (error) {

                console.error(
                    "Erro ao realizar login:",
                    error.message
                );

                if (error instanceof TypeError) {
                    showMessage(
                        "Não foi possível conectar ao servidor. Verifique se a API está funcionando."
                    );
                } else {
                    showMessage(
                        error.message ||
                        "Ocorreu um erro inesperado ao entrar."
                    );
                }

            } finally {

                if (submitButton) {
                    submitButton.disabled = false;
                    submitButton.textContent =
                        textoOriginalBotao || "Entrar";
                }
            }
        });
    }

    // =====================================
    // RECUPERAÇÃO DE SENHA
    // =====================================

    if (forgotPassword) {

        forgotPassword.addEventListener("click", (event) => {

            event.preventDefault();

            showMessage(
                "A recuperação de senha ainda não está disponível."
            );
        });
    }

});
