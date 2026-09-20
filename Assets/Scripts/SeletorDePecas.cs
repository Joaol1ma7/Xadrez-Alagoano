using UnityEngine;
using System.Collections;

public class SeletorDePecas : MonoBehaviour
{
    private GameObject pecaSelecionada = null;
    public float tempoDeMovimento = 0.5f;

    // Nova variável de estado para controlar de quem é a vez (começa com as Brancas)
    public Equipe turnoAtual = Equipe.Branca;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray raio = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit acerto;

            if (Physics.Raycast(raio, out acerto, 100f))
            {
                GameObject objetoClicado = acerto.collider.gameObject;

                // CENÁRIO 1: O jogador clicou em uma peça
                if (objetoClicado.CompareTag("piece"))
                {
                    // Acessa o script de dados da peça que acabamos de clicar
                    PecaDeXadrez dadosDaPeca = objetoClicado.GetComponent<PecaDeXadrez>();

                    // Verifica se a peça clicada pertence ao jogador do turno atual
                    if (dadosDaPeca == null)
                    {
                        Debug.Log("Dados da peca é nulo.");
                    }
                    if (dadosDaPeca != null && dadosDaPeca.equipe == turnoAtual)
                    {
                        pecaSelecionada = objetoClicado;
                        Debug.Log("Peça selecionada: " + pecaSelecionada.name);
                    }
                    else
                    {
                        Debug.Log("Você não pode selecionar esta peça agora. Turno atual: " + turnoAtual);
                        pecaSelecionada = null; // Cancela seleções anteriores se clicar na peça errada
                    }
                }
                
                // CENÁRIO 2: O jogador tem uma peça válida na memória e clicou no tabuleiro
                else if (objetoClicado.CompareTag("Tabuleiro") && pecaSelecionada != null)
                {
                    Vector3 pontoClicado = acerto.point;
                    
                    float tamanhoDaCasa = 1.0f; 
                    float offsetX = 0.5f;
                    float offsetZ = 0.5f;

                    float centroX = Mathf.Round((pontoClicado.x - offsetX) / tamanhoDaCasa) * tamanhoDaCasa + offsetX;
                    float centroZ = Mathf.Round((pontoClicado.z - offsetZ) / tamanhoDaCasa) * tamanhoDaCasa + offsetZ;

                    Vector3 destinoCentralizado = new Vector3(centroX, pecaSelecionada.transform.position.y, centroZ);
                    
                    StartCoroutine(DeslizarPeca(pecaSelecionada, destinoCentralizado));
                    
                    // O movimento foi feito, então trocamos o turno
                    TrocarTurno();

                    pecaSelecionada = null;
                }// CENÁRIO 3: O jogador tem uma peça na memória e clicou em OUTRA PEÇA
else if (objetoClicado.CompareTag("piece") && pecaSelecionada != null)
{
    PecaDeXadrez peçaAlvo = objetoClicado.GetComponent<PecaDeXadrez>();
    PecaDeXadrez minhaPeca = pecaSelecionada.GetComponent<PecaDeXadrez>();

    // Verifica se a peça clicada é da equipe inimiga
    if (peçaAlvo != null && peçaAlvo.equipe != minhaPeca.equipe)
    {
        // Salva a coordenada exata onde o inimigo está antes de destruí-lo
        // Como a peça inimiga já está centralizada (offset 0.5), podemos usar a posição direta dela
        Vector3 posicaoDoAtaque = objetoClicado.transform.position;
        posicaoDoAtaque.y = pecaSelecionada.transform.position.y; // Mantém a altura

        Debug.Log($"{minhaPeca.name} capturou {peçaAlvo.name}!");

        // Destrói a peça inimiga (remove do jogo)
        Destroy(objetoClicado);

        // Inicia a animação da sua peça deslizando até a casa conquistada
        StartCoroutine(DeslizarPeca(pecaSelecionada, posicaoDoAtaque));
        
        // Finaliza a jogada
        TrocarTurno();
        pecaSelecionada = null;
    }
    // Se a peça clicada for da MESMA equipe, o jogador só quer trocar a seleção
    else if (peçaAlvo != null && peçaAlvo.equipe == minhaPeca.equipe)
    {
        pecaSelecionada = objetoClicado;
        Debug.Log("Trocou a seleção para: " + pecaSelecionada.name);
    }
}
            }
            else
            {
                pecaSelecionada = null;
            }
        }
    }

    // Função auxiliar para inverter o estado do turno
    void TrocarTurno()
    {
        if (turnoAtual == Equipe.Branca)
        {
            turnoAtual = Equipe.Preta;
        }
        else
        {
            turnoAtual = Equipe.Branca;
        }
        
        Debug.Log("Turno trocado! Agora é a vez das: " + turnoAtual);
    }

    IEnumerator DeslizarPeca(GameObject peca, Vector3 destino)
    {
        Vector3 posicaoInicial = peca.transform.position;
        float tempoDecorrido = 0f;

        while (tempoDecorrido < tempoDeMovimento)
        {
            float progresso = tempoDecorrido / tempoDeMovimento;
            peca.transform.position = Vector3.Lerp(posicaoInicial, destino, progresso);
            tempoDecorrido += Time.deltaTime;
            yield return null; 
        }
        peca.transform.position = destino;
    }
}