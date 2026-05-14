using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BancoAPI.Api.Models
{
    [Table("clientes")]
    public class Cliente
    {
        [Key]
        [Column("id_cliente")]
        public int Id { get; set; }

        [Required]
        [Column("nome")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Column("cpf")]
        public string Cpf { get; set; } = string.Empty;

        [Column("data_nascimento")]
        public DateTime? DataNascimento { get; set; }

        [Column("telefone")]
        public string? Telefone { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("cep")]
        public string? Cep { get; set; }

        [Column("logradouro")]
        public string? Logradouro { get; set; }

        [Column("numero")]
        public string? Numero { get; set; }

        [Column("bairro")]
        public string? Bairro { get; set; }

        [Column("senha_hash")]
        public string? SenhaHash { get; set; }

        // 🔥 RELACIONAMENTO
        public ICollection<Conta>? Contas { get; set; }
    }
}