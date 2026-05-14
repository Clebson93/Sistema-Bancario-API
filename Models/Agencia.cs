using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BancoAPI.Api.Models
{
    [Table("agencias")]
    public class Agencia
    {
        [Key]
        [Column("id_agencia")]
        public int Id { get; set; }

        [Column("nome_agencia")]
        public required string NomeAgencia { get; set; }

        [Column("endereco")]
        public required string Endereco { get; set; }

        [Column("cidade")]
        public required string Cidade { get; set; }

        [Column("estado")]
        public required string Estado { get; set; }
    }
}