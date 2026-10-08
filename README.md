# SIGA-CEAN

Sistema de controle de estoque do **CEAN – Centro de Apoio aos Animais** (Cruzeiro-SP).
Projeto acadêmico do curso de Análise e Desenvolvimento de Sistemas (FATEC).

Aplicativo desktop feito em **C# com .NET MAUI**, para **Windows**.
Os dados ficam **somente em memória**: ao fechar o programa, tudo volta ao estado inicial
(o estoque real do CEAN carregado pelo código).

## Funcionalidades

- Cadastro de produtos e consulta do estoque, com busca por nome (ignora maiúsculas e acentos) e filtros por
  categoria e situação (sem estoque, crítico, abaixo do mínimo, validade próxima, vencido)
- Categorias: Medicamento, Insumo, Alimentar, Equipamento hospitalar, Higiene e Microchip
- Registro de entradas (com documento: nota fiscal ou pedido, e validade opcional) e de saídas (com destino e
  quem retirou); a saída usa primeiro o que vence antes
- Ajuste de balanço
- Corrigir ou cancelar uma movimentação (só o perfil Administrativo, com motivo), desfazendo o efeito no estoque
- Histórico de movimentações, com busca por produto, retirante, destino ou documento e filtros por tipo,
  situação e período
- Relatório de Consumo mensal, trimestral, anual ou por período, com exportação em PDF
- Confirmação antes de gravar e faixa de aviso depois de cada operação
- Controle de acesso por usuário, com botão Sair para trocar de usuário

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

São 157 testes das regras de estoque, validade, estoque mínimo, movimentações (inclusive correção e
cancelamento), busca, histórico, relatório, PDF e login.

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
