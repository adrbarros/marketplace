﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeuCardapioAi.Domain
{
    public class item
    {
        public int id { get; set; }
        public string nome { get; set; }
        public decimal qtde { get; set; }
        public decimal valor { get; set; }
        public decimal total { get; set; }
        public item_produto produto { get; set; }
        public string unidade { get; set; }
        //adicionais
    }

    public class item_produto
    {
        public int id { get; set; }
        public string nome { get; set; }
        public decimal preco { get; set; }
        public string descricao { get; set; }
        public string mensagemPedido { get; set; }
        public string imagens { get; set; }
        public string empresa { get; set; }
        public bool exibirNoSite { get; set; }
        public int disponibilidade { get; set; }
        public bool exibirPrecoSite { get; set; }
        public item_produto_categoria categoria { get; set; }
        public string tipoDeVenda { get; set; }
        public string unidadeMedida { get; set; }
        public decimal? valorInicial { get; set; }
        public string incremento { get; set; }
        public bool disponivelParaDelivery { get; set; }
        public bool disponivelNaMesa { get; set; }
        public int qtdeMinima { get; set; }
        //"camposAdicionais": [],
        //"horarios": [],
        public string tipo { get; set; }
        public bool temEstoque { get; set; }
        public int ordem { get; set; }
    }

    public class item_produto_categoria
    {
        public int id { get; set; }
        public string nome { get; set; }
        //"impressoras": [],
        public int posicao { get; set; }
        public bool disponivel { get; set; }
    }
}
