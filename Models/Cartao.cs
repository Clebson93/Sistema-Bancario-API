using System;

namespace BancoAPI.Api.Models
{
    public class Cartao
    {
        public int Id { get; set; }
        public string NumeroCartao { get; set; } = null!;
        public string Bandeira { get; set; } = null!;
        public string TipoCartao { get; set; } = null!;
        public decimal LimiteCredito { get; set; }

        public int ContaId { get; set; }
        public virtual Conta Conta { get; set; } = null!;
    }
}
