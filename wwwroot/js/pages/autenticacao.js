
/* =========================================
   CHAMAPRO - AUTENTICAÇÃO
   Arquivo: wwwroot/js/pages/autenticacao.js

   Funcionalidades:
   - Login com API ASP.NET Core
   - Autenticação JWT
   - Redirecionamento por perfil
   - Cadastro de Cliente
   - Cadastro de Profissional
   - Validação de formulário
   - Mostrar / esconder senha
========================================= */

document.addEventListener("DOMContentLoaded", () => {

    // =====================================
    // CONFIGURAÇÃO DA API
    // =====================================

    const API_AUTH = "/api/v1/auth";
    const API_LOGIN = `${API_AUTH}/login`;

    const API_CADASTRO_CLIENTE =
        `${API_AUTH}/cadastro/cliente`;

    const API_CADASTRO_PROFISSIONAL =
        `${API_AUTH}/cadastro/profissional`;

    // =====================================
    // PÁGINAS POR PERFIL
    // =====================================

    const paginasPorPerfil = {
        CLIENTE: "../cliente/painel.html",
        PROFISSIONAL: "../trabalhador/fila-chamados.html",
        ADMIN: "../admin/gestao.html"
    };

    // =====================================
    // ELEMENTOS DA TELA DE LOGIN
    // =====================================

    const loginForm = document.getElementById("loginForm");
    const emailInput = document.getElementById("email");
    const passwordInput = document.getElementById("password");
    const togglePassword = document.getElementById("togglePassword");
    const authMessage = document.getElementById("authMessage");
    const forgotPassword = document.getElementById("forgotPassword");

    // =====================================
    // FUNÇÕES DE MENSAGEM DO LOGIN
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
    // MOSTRAR / ESCONDER SENHA DO LOGIN
    // =====================================

    if (togglePassword && passwordInput) {

        togglePassword.addEventListener("click", () => {

            const mostrarSenha =
                passwordInput.type === "password";

            passwordInput.type =
                mostrarSenha ? "text" : "password";

            togglePassword.setAttribute(
                "aria-label",
                mostrarSenha ? "Ocultar senha" : "Mostrar senha"
            );
        });
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

    // =====================================
    // ELEMENTOS DA TELA DE CADASTRO
    // =====================================

    const cadastroForm =
        document.getElementById("cadastroForm");

    if (cadastroForm) {

        const nomeCadastro =
            document.getElementById("nome");

        const emailCadastro =
            document.getElementById("email");

        const senhaCadastro =
            document.getElementById("senha");

        const confirmarSenha =
            document.getElementById("confirmarSenha");

        const cadastroMensagem =
            document.getElementById("cadastroMensagem");

        const cadastroSubmit =
            document.getElementById("cadastroSubmit");

        // =====================================
        // MENSAGENS DO CADASTRO
        // =====================================

        function mostrarMensagemCadastro(
            mensagem,
            tipo = "erro"
        ) {
            if (!cadastroMensagem) return;

            cadastroMensagem.textContent = mensagem;

            cadastroMensagem.classList.remove(
                "erro",
                "sucesso",
                "visible"
            );

            if (mensagem) {
                cadastroMensagem.classList.add(
                    tipo,
                    "visible"
                );
            }
        }

        // =====================================
        // TRATAMENTO DE ERROS DA API
        // =====================================

        function obterMensagemErroCadastro(
            response,
            resultado
        ) {

            if (
                resultado &&
                typeof resultado === "object"
            ) {

                // Erros de validação do ASP.NET Core
                if (resultado.errors) {

                    const erros = Object.values(
                        resultado.errors
                    ).flat();

                    if (erros.length > 0) {
                        return erros.join(" ");
                    }
                }

                return resultado.mensagem ||
                    resultado.message ||
                    resultado.detail ||
                    resultado.title ||
                    `Erro ao cadastrar (${response.status}).`;
            }

            if (
                typeof resultado === "string" &&
                resultado.trim()
            ) {
                return resultado;
            }

            if (response.status === 409) {
                return "Este e-mail já está cadastrado.";
            }

            if (response.status === 429) {
                return "Muitas tentativas. Aguarde e tente novamente.";
            }

            return `Não foi possível realizar o cadastro (${response.status}).`;
        }

        // =====================================
        // REALIZAR CADASTRO
        // =====================================

        cadastroForm.addEventListener(
            "submit",
            async (event) => {

                event.preventDefault();

                mostrarMensagemCadastro("");

                // VALIDAÇÃO NATIVA DO FORMULÁRIO
                if (!cadastroForm.checkValidity()) {
                    cadastroForm.reportValidity();
                    return;
                }

                // OBTÉM OS DADOS
                const tipo = cadastroForm.querySelector(
                    'input[name="tipoConta"]:checked'
                )?.value || "";

                const nome =
                    nomeCadastro?.value.trim() || "";

                const email =
                    emailCadastro?.value.trim() || "";

                const senha =
                    senhaCadastro?.value || "";

                const confirmacao =
                    confirmarSenha?.value || "";

                // =====================================
                // VALIDAR TIPO DE CONTA
                // =====================================

                if (
                    tipo !== "cliente" &&
                    tipo !== "profissional"
                ) {

                    mostrarMensagemCadastro(
                        "Selecione um tipo de conta válido."
                    );

                    return;
                }

                // =====================================
                // VALIDAR NOME
                // =====================================

                if (nome.length < 3) {

                    mostrarMensagemCadastro(
                        "Informe seu nome completo."
                    );

                    return;
                }

                // =====================================
                // VALIDAR E-MAIL
                // =====================================

                if (!email) {

                    mostrarMensagemCadastro(
                        "Informe seu e-mail."
                    );

                    return;
                }

                // =====================================
                // VALIDAR SENHA
                // =====================================

                if (senha.length < 8) {

                    mostrarMensagemCadastro(
                        "A senha deve ter pelo menos 8 caracteres."
                    );

                    return;
                }

                if (senha !== confirmacao) {

                    mostrarMensagemCadastro(
                        "As senhas não coincidem."
                    );

                    return;
                }

                // =====================================
                // DEFINIR ENDPOINT
                // =====================================

                const endpoint =
                    tipo === "profissional"
                        ? API_CADASTRO_PROFISSIONAL
                        : API_CADASTRO_CLIENTE;

                // =====================================
                // PREPARAR DADOS PARA A API
                // =====================================

                const dadosCadastro = {
                    nome: nome,
                    email: email,
                    senha: senha
                };

                // =====================================
                // BLOQUEAR BOTÃO DURANTE ENVIO
                // =====================================

                const textoOriginalBotao =
                    cadastroSubmit?.textContent;

                if (cadastroSubmit) {
                    cadastroSubmit.disabled = true;
                    cadastroSubmit.textContent =
                        "Criando conta...";
                }

                try {

                    // =====================================
                    // ENVIAR CADASTRO PARA A API
                    // =====================================

                    const response = await fetch(
                        endpoint,
                        {
                            method: "POST",
                            headers: {
                                "Content-Type": "application/json"
                            },
                            body: JSON.stringify(
                                dadosCadastro
                            )
                        }
                    );

                    // =====================================
                    // LER RESPOSTA DO SERVIDOR
                    // =====================================

                    const textoResposta =
                        await response.text();

                    let resultado = null;

                    if (textoResposta) {

                        try {
                            resultado = JSON.parse(
                                textoResposta
                            );
                        } catch {
                            resultado = textoResposta;
                        }
                    }

                    // =====================================
                    // VERIFICAR ERROS
                    // =====================================

                    if (!response.ok) {

                        mostrarMensagemCadastro(
                            obterMensagemErroCadastro(
                                response,
                                resultado
                            )
                        );

                        return;
                    }

                    // =====================================
                    // CADASTRO REALIZADO
                    // =====================================

                    mostrarMensagemCadastro(
                        "✓ Conta criada com sucesso! Você será redirecionado para o login.",
                        "sucesso"
                    );

                    cadastroForm.reset();

                    // Aguarda 3 segundos antes de redirecionar
                    setTimeout(() => {
                        window.location.href = "./login.html";
                    }, 3000);

                } catch (error) {

                    console.error(
                        "Erro ao realizar cadastro:",
                        error
                    );

                    mostrarMensagemCadastro(
                        "Não foi possível conectar ao servidor. Verifique se a API está funcionando."
                    );

                } finally {

                    if (cadastroSubmit) {

                        cadastroSubmit.disabled = false;

                        cadastroSubmit.textContent =
                            textoOriginalBotao ||
                            "Criar minha conta";
                    }
                }
            }
        );
    }

});
