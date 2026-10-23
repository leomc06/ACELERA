# SIGA-CEAN

Sistema de controle de estoque do **CEAN – Centro de Apoio aos Animais** (Cruzeiro-SP).
Projeto acadêmico do curso de Análise e Desenvolvimento de Sistemas (FATEC).

Aplicativo desktop feito em **C# com .NET MAUI**, para **Windows**.
Os dados ficam **somente em memória**: ao fechar o programa, tudo volta ao estado inicial
(o estoque real do CEAN carregado pelo código).

## Funcionalidades

- Painel inicial com o que precisa de atenção (sem estoque, abaixo do mínimo, vencidos, validade próxima); cada quadro
  abre o Estoque já filtrado
- Cadastro de produtos e consulta do estoque, com busca por nome (ignora maiúsculas e acentos), filtro por categoria
  e botões de situação com a contagem de cada uma (sem estoque, crítico, abaixo do mínimo, validade próxima, vencido)
- Ficha do produto: situação, saldos por validade na ordem em que saem (FEFO) e últimas movimentações; o
  Veterinário só consulta, e o Administrativo tem atalhos de entrada, saída e edição
- Categorias: Medicamento, Insumo, Alimentar, Equipamento hospitalar, Higiene e Microchip
- Registro de entradas (com documento: nota fiscal ou pedido, e validade opcional) e de saídas (com destino e
  quem retirou, ambos obrigatórios); a saída usa primeiro o que vence antes e a tela mostra de qual validade
  o produto vai sair antes de confirmar
- Ajuste de balanço
- Corrigir ou cancelar uma movimentação (só o perfil Administrativo, com motivo), desfazendo o efeito no estoque
- Histórico de movimentações, com busca por produto, retirante, destino ou documento e filtros por tipo,
  situação e período (mostra 100 por vez, com "Mostrar mais")
- Relatório de Consumo mensal, trimestral, anual ou por período, com exportação em PDF
- Gráficos por categoria (barras, pizza ou rosca): estoque atual, uso no período ou número de produtos, mais o
  produto mais e o menos usado dentro de cada categoria (só o perfil Administrativo)
- Relatório por Categoria, na tela e em PDF: uso de cada categoria e seus produtos mais e menos usados
- Confirmação antes de gravar e faixa de aviso depois de cada operação
- Controle de acesso por usuário, com botão Sair para trocar de usuário
- Teclado: Enter envia os formulários e o login; no login há "Mostrar senha"

## Requisitos

- Windows 10 (versão 19041 ou mais nova) ou Windows 11
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Carga de trabalho do MAUI para Windows
- Internet no primeiro build, para baixar os pacotes NuGet

O Visual Studio é opcional. Se for usá-lo, a carga de trabalho
"Desenvolvimento de interface do usuário multiplataforma do .NET" já inclui o MAUI.

## Como rodar

```bash
# 1. conferir o SDK
dotnet --version

# 2. instalar a carga de trabalho do MAUI (uma vez só)
dotnet workload install maui-windows

# 3. clonar e compilar
git clone https://github.com/leomc06/ACELERA.git
cd ACELERA
dotnet build SigaCean.Maui\SigaCean.csproj

# 4. abrir o programa
SigaCean.Maui\bin\Debug\net10.0-windows10.0.19041.0\win-x64\SigaCean.exe
```

Também é possível abrir `SigaCean.slnx` no Visual Studio e executar com F5.

Se o build reclamar da carga de trabalho, rode o terminal como administrador e repita o passo 2.

## Login de teste

Usuários fictícios, criados só para demonstração:

| Usuário | Senha      |
|---------|------------|
| `admin` | `admin123` |
| `vet`   | `vet123`   |

## Testes automatizados

```bash
dotnet test SigaCean.Tests
```

São 214 testes das regras de estoque, validade, estoque mínimo, movimentações (inclusive correção e
cancelamento), busca, histórico, relatório, PDF e login. Os Services também são testados sozinhos (a tela não é a única
barreira): saída sem "retirado por", produto duplicado e ids inexistentes são recusados sem alterar nada.

## Dados de exemplo

Os 134 produtos e as quantidades vêm da planilha real do CEAN. A planilha não informa estoque mínimo nem validade,
então os alertas de mínimo só aparecem depois que o mínimo de um produto é cadastrado. Produto com quantidade zero
aparece como "SEM ESTOQUE". Os dados ficam só em memória e voltam ao estado inicial ao fechar o programa.

## Estrutura

| Pasta            | Conteúdo                                                        |
|------------------|-----------------------------------------------------------------|
| `SigaCean.Maui/` | Aplicativo: domínio, serviços, dados em memória e telas (MAUI)  |
| `SigaCean.Tests/`| Testes automatizados (xUnit) das regras de negócio e do PDF     |

## Dependência externa

O PDF do relatório usa o [PDFsharp-MigraDoc](https://github.com/empira/PDFsharp) 6.2.4 (licença MIT),
restaurado automaticamente pelo `dotnet build`.
