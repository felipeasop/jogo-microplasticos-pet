# Caça aos Resíduos: do Macro ao Micro

Um jogo educativo sobre coleta de resíduos feito para o PET Biologia da UENP CLM.

O jogo possui três fases, que exploram diferentes cenários de Bandeirantes - PR. As três fases têm quatro escalas, permitindo que o jogador explore e colete resíduos dos maiores aos menores, com o objetivo de concientizar sobre a coleta e microplásticos.

## Coleta de dados

Ao clicar em jogar, o jogador é redirecionado para uma página de coleta de dados, que contém informação selecionável sobre escola, nível de escolaridade, cidade e idade do participante. A coleta de dados é feita totalmente para fins acadêmicos.

## Funcionalidades e jogabilidade

O jogo permite explorar o cenário e coletar resíduos em diferentes níveis de zoom, ganhando pontos ao coletar resíduos e o jogo termina quando o tempo acaba ou todos resíduos são coletados.
Cada coleta 10 pontos, com 45 segundos de partida e cinco dicas com desconto de
três segundos cada, podendo alterar os valores em `RegrasJogo`.
Quando o formulário está completo, o cadastro de teste é salvo em SQLite
local (`user://cacaaosresiduos.sqlite3`).

### Modo de teste

A escolha de fases pode ser feita manualmente durante os testes. A seleção automática redireciona para as dificuldades de acordo com a idade da pessoa jogando. Para testar a jogabilidade sem preencher o cadastro, toque em JOGAR segure o título "ANTES DE JOGAR", por 3 segundos.

## Ferramentas

- Godot 4.7.2 Mono
- .NET 8
- SQLite local (`Microsoft.Data.Sqlite` 8.0.31)

### Utilizando Nix

Também é possível utilizar o Nix para configurar o ambiente de desenvolvimento a partir do arquivo flake.nix. No terminal:

```
nix develop
```

## Exportar e instalar no totem (Windows 10)

1. Instale os export templates da versão 4.7.2 mono em
   `~/.local/share/godot/export_templates/4.7.2.stable.mono/` (uma única vez).
2. Gere o pacote:

   ```bash
   dotnet build CacaAosResiduos.csproj -c Release
   godot-mono --headless --path . --export-release "Windows Desktop" export/windows/CacaAosResiduos.exe
   ```

3. Copie a **pasta inteira** `export/windows/` para o totem, por exemplo em
   `D:\CacaAosResiduos\`. Ela contém `CacaAosResiduos.exe` (com os dados do jogo
   embutidos) e a pasta `data_CacaAosResiduos_windows_x86_64`, que precisa ficar ao
   lado do executável. Não renomeie o `.exe` nem copie só ele.
4. No Windows, o jogo abre em tela cheia. O modo quiosque (Acesso Atribuído) e o
   início automático após queda de energia são configurados no próprio Windows.
   O jogo não usa teclado virtual: todo o cadastro é feito por seleção.

Arquivos gerados no totem, em `%APPDATA%\CacaAosResiduos\`:

- `cacaaosresiduos.sqlite3`: banco local (dados fictícios nesta fase);
- `logs\godot.log`: erros, avisos e exceções da última execução (os 10 últimos
  arquivos ficam guardados). Envie esse arquivo quando algo der errado. Ele não
  contém dados de participantes.

## Verificação de desenvolvimento

Compile Debug e execute a suíte dentro do Godot Mono. Os testes usam apenas
dados fictícios, criam bancos temporários próprios e retornam código diferente
de zero quando há falha. O isolamento abaixo também evita abrir o banco do operador:

```bash
dotnet build CacaAosResiduos.csproj
validation_dir=$(mktemp -d /tmp/caca-residuos-validation.XXXXXX)
XDG_DATA_HOME="$validation_dir/data" XDG_CONFIG_HOME="$validation_dir/config" \
  godot-mono --headless --path . res://testes/VerificacoesProjeto.tscn
dotnet build CacaAosResiduos.csproj -c Release
```

Não é uma suíte `dotnet test`: são verificações C# que instanciam as cenas e
usam o motor e SQLite reais, sem adicionar pacotes de teste. O script dos testes
é compilado somente em Debug. O preset de exportação exclui `testes/` do pacote
distribuído e inclui os JSONs de `content/`.

Para importar recursos, execute `godot-mono --headless --path . --editor --quit`
com diretórios temporários equivalentes. Em ambientes restritos, o editor pode
precisar de permissão para seus sockets locais de integração.

Para conferir os recortes e os tamanhos com renderização real, execute a suíte
Debug em uma sessão gráfica com o argumento `--preview`:

```bash
XDG_DATA_HOME="$validation_dir/data" XDG_CONFIG_HOME="$validation_dir/config" \
  godot-mono --path . res://testes/VerificacoesProjeto.tscn -- --preview
```

Ela captura o menu, o cadastro vazio, as instruções, as quatro vistas de cada fase e seus resultados com semente 123 em uma pasta própria
no diretório temporário do sistema e imprime os caminhos. Não funciona com
`--headless`, pois esse modo não renderiza as imagens. A suíte verifica 1.000
sementes por cenário, coleta completa nos níveis disponíveis e regressões de
toque, zonas, perspectiva e sessão.

O jogo continua offline. A primeira restauração de pacotes de desenvolvimento
pode precisar de internet, mas não participa do fluxo do evento.
Para testar tela cheia no desenvolvimento, use `godot-mono --path . --fullscreen`;
isso não configura, por si só, o modo quiosque do Windows.
