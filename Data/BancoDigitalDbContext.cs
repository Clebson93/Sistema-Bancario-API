using Microsoft.EntityFrameworkCore;
using BancoAPI.Api.Models;
using BancoAPI.Models;

namespace BancoAPI.Api.Data
{
    public class BancoDigitalDbContext : DbContext
    {
        public BancoDigitalDbContext(DbContextOptions<BancoDigitalDbContext> options)
            : base(options) { }

        // ========================
        // DBSETS
        // ========================

        public DbSet<Agencia> Agencias { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Conta> Contas { get; set; }
        public DbSet<Cartao> Cartoes { get; set; }
        public DbSet<Transacao> Transacoes { get; set; }
        public DbSet<LogSistema> LogsSistema { get; set; }
        public DbSet<User> Users { get; set; }

        // ========================
        // RELACIONAMENTOS
        // ========================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================
            // MAPEAMENTO DE TABELAS (snake_case do MySQL)
            // ========================

            modelBuilder.Entity<Agencia>().ToTable("agencias");
            modelBuilder.Entity<Cliente>().ToTable("clientes");
            modelBuilder.Entity<Conta>().ToTable("contas");
            modelBuilder.Entity<Cartao>().ToTable("cartoes");
            modelBuilder.Entity<Transacao>().ToTable("transacoes");
            modelBuilder.Entity<LogSistema>().ToTable("logs_sistema");

            // ========================
            // MAPEAMENTO DE COLUNAS - Agencia
            // ========================

            modelBuilder.Entity<Agencia>(e => {
                e.Property(a => a.Id).HasColumnName("id_agencia");
                e.Property(a => a.NomeAgencia).HasColumnName("nome_agencia");
                e.Property(a => a.Endereco).HasColumnName("endereco");
                e.Property(a => a.Cidade).HasColumnName("cidade");
                e.Property(a => a.Estado).HasColumnName("estado");
            });

            // ========================
            // MAPEAMENTO DE COLUNAS - Cliente
            // ========================

            modelBuilder.Entity<Cliente>(e => {
                e.Property(c => c.Id).HasColumnName("id_cliente");
                e.Property(c => c.Nome).HasColumnName("nome");
                e.Property(c => c.Cpf).HasColumnName("cpf");
                e.Property(c => c.DataNascimento).HasColumnName("data_nascimento");
                e.Property(c => c.Telefone).HasColumnName("telefone");
                e.Property(c => c.Email).HasColumnName("email");
                e.Property(c => c.Cep).HasColumnName("cep");
                e.Property(c => c.Logradouro).HasColumnName("logradouro");
                e.Property(c => c.Numero).HasColumnName("numero");
                e.Property(c => c.Bairro).HasColumnName("bairro");
                e.Property(c => c.SenhaHash).HasColumnName("senha_hash");
            });

            // ========================
            // MAPEAMENTO DE COLUNAS - Conta
            // ========================

            modelBuilder.Entity<Conta>(e => {
                e.Property(c => c.Id).HasColumnName("id_conta");
                e.Property(c => c.NumeroConta).HasColumnName("numero_conta");
                e.Property(c => c.TipoConta).HasColumnName("tipo_conta");
                e.Property(c => c.Saldo).HasColumnName("saldo");
                e.Property(c => c.StatusConta).HasColumnName("status_conta");
                e.Property(c => c.DataAbertura).HasColumnName("data_abertura");
                e.Property(c => c.ClienteId).HasColumnName("id_cliente");
                e.Property(c => c.AgenciaId).HasColumnName("id_agencia");
            });

            // ========================
            // MAPEAMENTO DE COLUNAS - Transacao
            // ========================

            modelBuilder.Entity<Transacao>(e => {
                e.Property(t => t.Id).HasColumnName("id_transacao");
                e.Property(t => t.TipoTransacao).HasColumnName("tipo_transacao");
                e.Property(t => t.Valor).HasColumnName("valor");
                e.Property(t => t.DataHora).HasColumnName("data_hora");
                e.Property(t => t.ContaOrigemId).HasColumnName("id_conta_origem");
                e.Property(t => t.ContaDestinoId).HasColumnName("id_conta_destino");
            });

            // ========================
            // MAPEAMENTO DE COLUNAS - LogSistema
            // ========================

            modelBuilder.Entity<LogSistema>(e => {
                e.Property(l => l.Id).HasColumnName("id_log");
                e.Property(l => l.Mensagem).HasColumnName("mensagem");
                e.Property(l => l.DataHora).HasColumnName("data_hora");
            });

            // ========================
            // RELACIONAMENTOS
            // ========================

            modelBuilder.Entity<Transacao>()
                .HasOne(t => t.ContaDestino)
                .WithMany()
                .HasForeignKey(t => t.ContaDestinoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Transacao>()
                .HasOne(t => t.ContaOrigem)
                .WithMany(c => c.Transacoes)
                .HasForeignKey(t => t.ContaOrigemId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================
            // SEED ADMIN
            // ========================

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$mmit6UUVsaBKChTUf.xE1OQdUHim3e1VJEKlyjjgCvtWjJEMTO4ne",
                    Role = "Admin"
                }
            );
        }
    }
}