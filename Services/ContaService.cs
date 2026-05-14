using BancoAPI.Api.Data;
using BancoAPI.Api.Models;
using BancoAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BancoAPI.Api.Services
{
    public class ContaService
    {
        private readonly BancoDigitalDbContext _context;

        public ContaService(BancoDigitalDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Depositar(int contaId, decimal valor)
        {
            try
            {
                var conta = await _context.Contas.FindAsync(contaId);
                if (conta == null || conta.StatusConta != "Ativa") return false;

                // CORREÇÃO: Atualiza o saldo na memória
                conta.Saldo += valor;

                var transacao = new Transacao
                {
                    TipoTransacao = "Deposito",
                    Valor = valor,
                    DataHora = DateTime.Now,
                    ContaOrigemId = contaId
                };

                _context.Transacoes.Add(transacao);
                _context.LogsSistema.Add(new LogSistema
                {
                    Mensagem = $"Depósito de {valor:C} na conta {conta.NumeroConta}",
                    DataHora = DateTime.Now
                });

                await _context.SaveChangesAsync(); // O EF agora vê que o Saldo mudou e faz o UPDATE
                return true;
            }
            catch { return false; }
        }

        public async Task<string?> Sacar(int contaId, decimal valor)
        {
            try
            {
                var conta = await _context.Contas.FindAsync(contaId);
                if (conta == null || conta.StatusConta != "Ativa") return "Conta inexistente ou inativa";

                decimal taxa = conta.TipoConta switch
                {
                    "Corrente" => 5,
                    "Poupanca" => 2,
                    "Empresarial" => 10,
                    _ => 0
                };

                if (conta.Saldo < valor + taxa) return "Saldo insuficiente (incluindo taxa)";

                // CORREÇÃO: Deduz o valor e a taxa do saldo
                conta.Saldo -= (valor + taxa);

                var transacao = new Transacao
                {
                    TipoTransacao = "Saque",
                    Valor = valor + taxa,
                    DataHora = DateTime.Now,
                    ContaOrigemId = contaId
                };

                _context.Transacoes.Add(transacao);
                _context.LogsSistema.Add(new LogSistema
                {
                    Mensagem = $"Saque de {valor:C} (Taxa: {taxa:C}) na conta {conta.NumeroConta}",
                    DataHora = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return null;
            }
            catch (Exception ex) { return ex.InnerException?.Message ?? ex.Message; }
        }

        public async Task<string?> Transferir(int contaOrigemId, string cpfDestino, decimal valor)
        {
            try
            {
                var cpfLimpo = new string(cpfDestino.Where(char.IsDigit).ToArray());
                var clienteDestino = await _context.Clientes
                    .Include(c => c.Contas)
                    .FirstOrDefaultAsync(c => c.Cpf == cpfLimpo);

                if (clienteDestino == null) return "CPF destinatário não encontrado";

                var contaDestino = clienteDestino.Contas?.FirstOrDefault(c => c.StatusConta == "Ativa");
                if (contaDestino == null) return "Destinatário não possui conta ativa";

                if (contaOrigemId == contaDestino.Id) return "Você não pode transferir para sua própria conta";

                var origem = await _context.Contas.FindAsync(contaOrigemId);
                if (origem == null || origem.StatusConta != "Ativa") return "Conta de origem inexistente ou inativa";

                if (origem.Saldo < valor) return "Saldo insuficiente";

                // ============================================================
                // CORREÇÃO CRÍTICA: A MÁGICA DA TRANSFERÊNCIA ACONTECE AQUI
                // ============================================================
                origem.Saldo -= valor;       // Tira da conta de quem envia
                contaDestino.Saldo += valor; // Coloca na conta de quem recebe
                // ============================================================

                var transacao = new Transacao
                {
                    TipoTransacao = "Transferencia",
                    Valor = valor,
                    DataHora = DateTime.Now,
                    ContaOrigemId = contaOrigemId,
                    ContaDestinoId = contaDestino.Id
                };

                _context.Transacoes.Add(transacao);
                _context.LogsSistema.Add(new LogSistema
                {
                    Mensagem = $"Transferência de {valor:C} da conta {origem.NumeroConta} para {contaDestino.NumeroConta} ({clienteDestino.Nome})",
                    DataHora = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return null;
            }
            catch (Exception ex) { return ex.InnerException?.Message ?? ex.Message; }
        }
    }
}