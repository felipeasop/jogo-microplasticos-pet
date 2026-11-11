using Godot;

namespace CacaAosResiduos.Menu;

// No Windows, cada toque chega de novo como clique de mouse logo depois.
// Este filtro descarta esse clique repetido. Fica por último na árvore para recebê-lo primeiro.
public partial class FiltroCliqueDoToque : Node
{
    public const ulong JanelaMilissegundos = 800;

    private ulong? _ultimoToque;

    public override void _Input(InputEvent evento)
    {
        if (evento is InputEventScreenTouch or InputEventScreenDrag)
            _ultimoToque = Time.GetTicksMsec();
        else if (evento is InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right } &&
            _ultimoToque is { } ultimo && Time.GetTicksMsec() - ultimo <= JanelaMilissegundos)
            GetViewport().SetInputAsHandled();
    }
}
