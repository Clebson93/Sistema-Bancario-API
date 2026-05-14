-- Banco e estrutura adaptados para BANCO_DIGITAL_PREMIUM
DROP DATABASE IF EXISTS `BANCO_DIGITAL_PREMIUM`;
CREATE DATABASE `BANCO_DIGITAL_PREMIUM` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `BANCO_DIGITAL_PREMIUM`;

-- Tabela de Agências
CREATE TABLE agencias (
    id_agencia INT AUTO_INCREMENT PRIMARY KEY,
    nome_agencia VARCHAR(100) NOT NULL,
    endereco VARCHAR(255),
    cidade VARCHAR(100),
    estado CHAR(2)
);

-- Tabela de Clientes
CREATE TABLE clientes (
    id_cliente INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(150) NOT NULL,
    cpf CHAR(11) NOT NULL UNIQUE,
    data_nascimento DATE,
    telefone VARCHAR(15),
    email VARCHAR(100) UNIQUE,
    cep CHAR(8),
    logradouro VARCHAR(150),
    numero VARCHAR(10),
    bairro VARCHAR(100)
);

-- Tabela de Contas com Status e Trava de Segurança (CHECK)
CREATE TABLE contas (
    id_conta INT AUTO_INCREMENT PRIMARY KEY,
    numero_conta VARCHAR(20) NOT NULL UNIQUE,
    tipo_conta ENUM('Corrente', 'Poupanca') NOT NULL,
    saldo DECIMAL(15, 2) DEFAULT 0.00,
    status_conta ENUM('Ativa', 'Bloqueada', 'Inativa') DEFAULT 'Ativa',
    data_abertura DATE NOT NULL,
    id_cliente INT NOT NULL,
    id_agencia INT NOT NULL,
    CONSTRAINT fk_cliente FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente) ON DELETE CASCADE,
    CONSTRAINT fk_agencia FOREIGN KEY (id_agencia) REFERENCES agencias(id_agencia) ON DELETE CASCADE,
    CONSTRAINT chk_saldo_positivo CHECK (saldo >= 0)
);

-- Tabela de Cartões
CREATE TABLE cartoes (
    id_cartao INT AUTO_INCREMENT PRIMARY KEY,
    numero_cartao VARCHAR(16) NOT NULL UNIQUE,
    bandeira ENUM('Mastercard', 'Visa', 'Elo') NOT NULL,
    tipo_cartao ENUM('Debito', 'Credito') NOT NULL,
    limite_credito DECIMAL(15, 2) DEFAULT 0.00,
    id_conta INT NOT NULL,
    CONSTRAINT fk_conta_cartao FOREIGN KEY (id_conta) REFERENCES contas(id_conta) ON DELETE CASCADE
);

-- Tabela de Transações
CREATE TABLE transacoes (
    id_transacao INT AUTO_INCREMENT PRIMARY KEY,
    tipo_transacao ENUM('Deposito', 'Saque', 'Transferencia') NOT NULL,
    valor DECIMAL(15, 2) NOT NULL,
    data_hora DATETIME DEFAULT CURRENT_TIMESTAMP,
    id_conta_origem INT NOT NULL,
    id_conta_destino INT,
    CONSTRAINT fk_origem FOREIGN KEY (id_conta_origem) REFERENCES contas(id_conta) ON DELETE CASCADE
);

-- Tabela de Auditoria (Logs)
CREATE TABLE logs_sistema (
    id_log INT AUTO_INCREMENT PRIMARY KEY,
    mensagem TEXT,
    data_hora DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Índice para buscas rápidas por CPF
CREATE INDEX idx_cliente_cpf ON clientes(cpf);

DELIMITER //

-- Trigger: Processa Transação, Atualiza Saldo e Bloqueia se Conta estiver Bloqueada
CREATE TRIGGER trg_processa_transacao BEFORE INSERT ON transacoes
FOR EACH ROW
BEGIN
    DECLARE v_status VARCHAR(20);
    SELECT status_conta INTO v_status FROM contas WHERE id_conta = NEW.id_conta_origem;
    
    -- Bloqueio de Segurança
    IF v_status != 'Ativa' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'ERRO: Conta Bloqueada ou Inativa!';
    END IF;

    -- Atualização Automática de Saldo
    IF NEW.tipo_transacao = 'Deposito' THEN
        UPDATE contas SET saldo = saldo + NEW.valor WHERE id_conta = NEW.id_conta_origem;
    ELSEIF NEW.tipo_transacao = 'Saque' THEN
        UPDATE contas SET saldo = saldo - NEW.valor WHERE id_conta = NEW.id_conta_origem;
    ELSEIF NEW.tipo_transacao = 'Transferencia' THEN
        UPDATE contas SET saldo = saldo - NEW.valor WHERE id_conta = NEW.id_conta_origem;
        UPDATE contas SET saldo = saldo + NEW.valor WHERE id_conta = NEW.id_conta_destino;
    END IF;
    
    -- Registro de Auditoria (Log)
    INSERT INTO logs_sistema (mensagem) VALUES (CONCAT('Transação de ', NEW.tipo_transacao, ' no valor de ', NEW.valor, ' realizada.'));
END //
DELIMITER ;

-- Dados iniciais (amostras)
INSERT INTO agencias (nome_agencia, endereco, cidade, estado) VALUES ('Agência Central','Rua das Flores, 123','São Paulo','SP');

INSERT INTO clientes (nome, cpf, cep, logradouro, numero, bairro) VALUES 
('João Silva','12345678901','01001000','Rua Direita','10','Centro'),
('Maria Oliveira','23456789012','11010000','Av. Ana Costa','500','Gonzaga');

-- Inserção de Contas mínimas (associações)
INSERT INTO contas (numero_conta, tipo_conta, saldo, data_abertura, id_cliente, id_agencia) VALUES 
('1001-X','Corrente',1500.00,CURDATE(),1,1),
('2002-Y','Poupanca',5000.00,CURDATE(),2,1);

-- Inserção de Cartões amostra
INSERT INTO cartoes (numero_cartao, bandeira, tipo_cartao, limite_credito, id_conta) VALUES 
('4433221100998877','Visa','Debito',0.00,1);

-- View de Perfil Completo
CREATE OR REPLACE VIEW view_perfil_cliente_completo AS
SELECT 
    c.nome AS Cliente,
    ct.numero_conta AS Conta,
    CONCAT('R$ ', REPLACE(REPLACE(REPLACE(FORMAT(ct.saldo, 2), '.', '|'), ',', '.'), '|', ',')) AS Saldo,
    COALESCE(car.bandeira, 'Sem Cartão') AS Bandeira,
    COALESCE(car.tipo_cartao, 'N/A') AS Tipo,
    CONCAT('R$ ', REPLACE(REPLACE(REPLACE(FORMAT(COALESCE(car.limite_credito, 0.00), 2), '.', '|'), ',', '.'), '|', ',')) AS Limite,
    CONCAT(SUBSTRING(c.cep, 1, 2), '.', SUBSTRING(c.cep, 3, 3), '-', SUBSTRING(c.cep, 6, 3)) AS CEP,
    CASE 
        WHEN ct.saldo >= 1000000 THEN 'BLACK'
        WHEN ct.saldo >= 20000 THEN 'OURO'
        WHEN ct.saldo >= 5000 THEN 'PRATA'
        ELSE 'BRONZE'
    END AS Score
FROM clientes c
JOIN contas ct ON c.id_cliente = ct.id_cliente
LEFT JOIN cartoes car ON ct.id_conta = car.id_conta;

-- Teste final: selecionar da view
SELECT * FROM view_perfil_cliente_completo;
