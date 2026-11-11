using Godot;

namespace CacaAosResiduos.Menu;

internal static class TemaJogo
{
    public static Theme Criar()
    {
        var tema = new Theme
        {
            DefaultFontSize = 22,
            DefaultFont = GD.Load<FontFile>("res://assets/fonts/Fredoka.ttf")
        };
        var textoEscuro = new Color("173d32");
        var textoSuave = new Color("50665b");
        foreach (var tipo in new[] { "Label", "Button" })
        {
            tema.SetColor("font_outline_color", tipo, Colors.Black);
            tema.SetConstant("outline_size", tipo, 1);
        }
        tema.SetTypeVariation("DarkLabel", "Label");
        tema.SetConstant("outline_size", "DarkLabel", 0);
        tema.SetColor("font_color", "Label", new Color("f2f5e9"));
        tema.SetColor("font_color", "Button", Colors.White);
        tema.SetColor("font_hover_color", "Button", Colors.White);
        tema.SetColor("font_pressed_color", "Button", Colors.White);
        // Verde para avançar, terracota para voltar ou sair.
        tema.SetStylebox("normal", "Button", CriarCaixa("245745", 12));
        tema.SetStylebox("hover", "Button", CriarCaixa("30694f", 12));
        tema.SetStylebox("pressed", "Button", CriarCaixa("1b4435", 12));
        tema.SetStylebox("disabled", "Button", CriarCaixa("344b42", 12));
        tema.SetColor("font_disabled_color", "Button", new Color("bbc9c0"));
        var foco = CriarCaixa("00000000", 12);
        foco.DrawCenter = false;
        foco.BorderColor = new Color("f2c15d");
        foco.SetBorderWidthAll(3);
        tema.SetStylebox("focus", "Button", foco);
        tema.SetTypeVariation("PrimaryButton", "Button");
        tema.SetStylebox("normal", "PrimaryButton", CriarCaixa("348052", 12));
        tema.SetStylebox("hover", "PrimaryButton", CriarCaixa("398457", 12));
        tema.SetStylebox("pressed", "PrimaryButton", CriarCaixa("296942", 12));
        VariacaoBotao(tema, "SecondaryButton", 12, Colors.White, "b45429", "aa4b24", "913b20");
        VariacaoBotao(tema, "SuggestionButton", 8, textoEscuro, "f8f4df", "dce9c7", "dce9c7");
        tema.SetConstant("outline_size", "SuggestionButton", 0);
        // O cartão escolhido ganha verde mais claro e borda amarela.
        tema.SetTypeVariation("LevelCard", "Button");
        tema.SetFontSize("font_size", "LevelCard", 22);
        tema.SetStylebox("normal", "LevelCard", CriarCaixa("245745", 16));
        tema.SetStylebox("hover", "LevelCard", CriarCaixa("30694f", 16));
        var nivelEscolhido = CriarCaixa("348052", 16);
        nivelEscolhido.BorderColor = new Color("f2c15d");
        nivelEscolhido.SetBorderWidthAll(6);
        tema.SetStylebox("pressed", "LevelCard", nivelEscolhido);
        tema.SetStylebox("hover_pressed", "LevelCard", nivelEscolhido);
        tema.SetColor("font_hover_pressed_color", "LevelCard", Colors.White);
        tema.SetTypeVariation("SuggestionPanel", "PanelContainer");
        var painelSugestoes = CriarCaixa("f8f4df", 10);
        painelSugestoes.SetContentMarginAll(4);
        painelSugestoes.BorderColor = textoEscuro;
        painelSugestoes.SetBorderWidthAll(2);
        tema.SetStylebox("panel", "SuggestionPanel", painelSugestoes);
        VariacaoBotao(tema, "FieldButton", 10, textoEscuro, "f8f4df", "fffbed", "fffbed");
        tema.SetConstant("outline_size", "FieldButton", 0);
        tema.SetStylebox("disabled", "FieldButton", CriarCaixa("e6e1cc", 10));
        tema.SetColor("font_disabled_color", "FieldButton", textoSuave);
        tema.SetStylebox("panel", "PanelContainer", CriarCaixa("183b32", 16));
        return tema;
    }

    private static void VariacaoBotao(Theme tema, string nome, int raio, Color texto,
        string normal, string hover, string pressionado)
    {
        tema.SetTypeVariation(nome, "Button");
        foreach (var (estado, cor) in new[] { ("normal", normal), ("hover", hover), ("pressed", pressionado) })
        {
            tema.SetStylebox(estado, nome, CriarCaixa(cor, raio));
            tema.SetColor(estado == "normal" ? "font_color" : $"font_{estado}_color", nome, texto);
        }
        tema.SetColor("font_focus_color", nome, texto);
    }

    private static StyleBoxFlat CriarCaixa(string cor, int raio) => new()
    {
        BgColor = new Color(cor),
        CornerRadiusTopLeft = raio,
        CornerRadiusTopRight = raio,
        CornerRadiusBottomLeft = raio,
        CornerRadiusBottomRight = raio,
        ContentMarginLeft = 18,
        ContentMarginRight = 18,
        ContentMarginTop = 8,
        ContentMarginBottom = 8
    };
}
