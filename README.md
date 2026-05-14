# 🏦 Sistema de Banco Digital — Banco Único

Este projeto foi desenvolvido como parte da formação tecnológica no programa **Ford-Enter** em parceria com o **SENAI CIMATEC**. Trata-se de uma plataforma de Internet Banking robusta, focada em segurança e experiência do usuário.

## 🚀 Proposta do Sistema
A plataforma conta com dois níveis de acesso distintos, garantindo uma gestão eficiente e operações seguras para os clientes.
* **Autenticação:** Utiliza **JWT (JSON Web Token)** para sessões seguras.
* **Segurança:** Criptografia de senhas para proteção de dados sensíveis.

---

## 🔑 Credenciais de Acesso (Para Testes)

### 👨‍💼 Painel Administrativo (Gestão)
* **Usuário:** `admin`
* **Senha:** `admin123`

### 👤 Painel do Cliente
| Nome | CPF | Senha |
| :--- | :--- | :--- |
| Gabriel Cunha | `11122233344` | `12345` |
| Marcelo Dias | `55566677788` | `12345` |

> **Nota:** Você também pode utilizar a opção **“Cadastre-se”** para criar uma conta nova e testar o fluxo completo de registro.

---

## 🛠️ Funcionalidades

### ⚙️ Painel Administrativo
O administrador possui visão global do sistema, permitindo:
* Visualizar todos os clientes cadastrados.
* Consultar CPFs e acompanhar contas ativas.
* Monitorar todas as transações realizadas no ecossistema bancário.

### 💰 Painel do Cliente
Interface intuitiva com exibição de saldo em tempo real e histórico de transações.
* **Depositar:** Adição de valores ao saldo.
* **Sacar:** Retiradas com aplicação automática de taxa fixa de **R$ 5,00**.
* **Transferir:** Envio de valores entre contas via CPF, com validação de nome do favorecido para maior segurança.

---

## ⚙️ Configuração Técnica e Instalação

Para que o sistema conecte corretamente ao seu banco de dados local durante os testes, siga os passos abaixo:

1. Abra o arquivo `appsettings.json` na raiz do projeto.
2. Localize a seção `ConnectionStrings`.
3. No campo **Password**, substitua pelo valor da senha do seu servidor SQL Server local.
   
```json
"DefaultConnection": "Server=...; Database=BancoUnico; User Id=sa; Password=SUA_SENHA_AQUI;"
