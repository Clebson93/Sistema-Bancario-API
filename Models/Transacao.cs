using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BancoAPI.Api.Models
{
    [Table("transacoes")]
    public class Transacao
    {
        [Key]
        [Column("id_transacao")]
        public int Id { get; set; }

        [Column("tipo_transacao")]
        public string TipoTransacao { get; set; } = string.Empty;

        [Column("valor")]
        public decimal Valor { get; set; }

        [Column("data_hora")]
        public DateTime DataHora { get; set; }

        [Column("id_conta_origem")]
        public int ContaOrigemId { get; set; }

        [Column("id_conta_destino")]
        public int? ContaDestinoId { get; set; }

        // Conta Origem
        [ForeignKey("ContaOrigemId")]
        public Conta ContaOrigem { get; set; } = null!;

        // Conta Destino
        [ForeignKey("ContaDestinoId")]
        public Conta? ContaDestino { get; set; }
    }
}