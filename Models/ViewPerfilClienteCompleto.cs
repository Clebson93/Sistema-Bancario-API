using System;
using System.Collections.Generic;

namespace BancoAPI.Api.Models;

public partial class ViewPerfilClienteCompleto
{
    public string Cliente { get; set; } = null!;

    public string Conta { get; set; } = null!;

    public string? Saldo { get; set; }

    public string Bandeira { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Limite { get; set; } = null!;

    public string? Cep { get; set; }

    public string Score { get; set; } = null!;
}
