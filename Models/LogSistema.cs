using System;

namespace BancoAPI.Models
{
    public class LogSistema
    {
        public int Id { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public DateTime DataHora { get; set; }
    }
}