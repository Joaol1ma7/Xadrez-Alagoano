using UnityEngine;

// Criamos as categorias disponíveis
public enum Equipe { Branca, Preta }
public enum TipoPeca { Peao, Torre, Cavalo, Bispo, Rainha, Rei }

public class PecaDeXadrez : MonoBehaviour
{
    // Essas variáveis aparecerão no Inspector para você configurar
    public Equipe equipe;
    public TipoPeca tipo;
}