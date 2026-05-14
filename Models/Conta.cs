using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BancoAPI.Api.Models
{
    [Table("contas")]
    public class Conta
    {
        [Key]
        [Column("id_conta")]
        public int Id { get; set; }

        [Column("numero_conta")]
        public string NumeroConta { get; set; } = string.Empty;

        [Column("tipo_conta")]
        public string TipoConta { get; set; } = string.Empty;

        [Column("saldo")]
        public decimal Saldo { get; set; }

        [Column("status_conta")]
        public string StatusConta { get; set; } = string.Empty;

        [Column("data_abertura")]
        public DateTime DataAbertura { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        [Column("id_agencia")]
        public int AgenciaId { get; set; }

        // 🔥 RELACIONAMENTO COM CLIENTE

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; } = null!;

        // 🔥 RELACIONAMENTO COM AGENCIA

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; } = null!;

        // 🔥 RELACIONAMENTO COM TRANSACOES (CORRIGIDO)

        public ICollection<Transacao> Transacoes { get; set; }
            = new List<Transacao>();
    }
}