using UnityEngine;
using System.Collections;

public class SeletorDePecas : MonoBehaviour
{
    private GameObject pecaSelecionada = null;
    public float tempoDeMovimento = 0.5f;

    [Header("Som de movimento")]
    [SerializeField] private AudioSource fonteDeAudio;
    [SerializeField] private AudioClip somDeMovimento;

    public Equipe turnoAtual = Equipe.Branca;

   
    void Update()
    {
        // 0 btn esquerdo, 1 bn direito, 2 scroll 
        if (Input.GetMouseButtonDown(0))
        {
            Ray raio = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit acerto;

            
            if (Physics.Raycast(raio, out acerto, 100f))
            {
                GameObject objetoClicado = acerto.collider.gameObject;

                
                if (objetoClicado.CompareTag("piece"))
                {
                    
                    PecaDeXadrez dadosDaPeca = objetoClicado.GetComponent<PecaDeXadrez>();

                    
                    if (dadosDaPeca == null)
                    {
                        Debug.Log("Dados da peca é nulo.");
                    }
                    
                    //verifica se turno é compatível com dados da peça
                    if (dadosDaPeca != null && dadosDaPeca.equipe == turnoAtual)
                    {
                        pecaSelecionada = objetoClicado;
                        Debug.Log("Peça selecionada: " + pecaSelecionada.name);
                    }
   
                    else
                    {
                        Debug.Log("Você não pode selecionar esta peça agora. Turno atual: " + turnoAtual);
                        pecaSelecionada = null; //clicou numa peça que não é do turno
                    }
                }
                
                
                else if (objetoClicado.CompareTag("Tabuleiro") && pecaSelecionada != null)
                {
                    Vector3 pontoClicado = acerto.point;
                    
                    float tamanhoDaCasa = 1.0f; 
                    float offsetX = 0.5f;
                    float offsetZ = 0.5f;

                    float centroX = Mathf.Round((pontoClicado.x - offsetX) / tamanhoDaCasa) * tamanhoDaCasa + offsetX;
                    float centroZ = Mathf.Round((pontoClicado.z - offsetZ) / tamanhoDaCasa) * tamanhoDaCasa + offsetZ;

                    Vector3 destinoCentralizado = new Vector3(centroX, pecaSelecionada.transform.position.y, centroZ);
                    
                    //startCoroutine inicia um processo que se divide em varios frames 
                    StartCoroutine(DeslizarPeca(pecaSelecionada, destinoCentralizado));
                    TrocarTurno();

                    pecaSelecionada = null;
                }
               
                // comer peça
                else if (objetoClicado.CompareTag("piece") && pecaSelecionada != null)
                {
                    PecaDeXadrez peçaAlvo = objetoClicado.GetComponent<PecaDeXadrez>();
                    PecaDeXadrez minhaPeca = pecaSelecionada.GetComponent<PecaDeXadrez>();

                    // verifia se peca clicada é peça inimiga
                    if (peçaAlvo != null && peçaAlvo.equipe != minhaPeca.equipe)
                    {
                        // Salva a coordenada exata onde o inimigo está antes de destruí-lo
                        // Como a peça inimiga já está centralizada (offset 0.5), podemos usar a posição direta dela
                        Vector3 posicaoDoAtaque = objetoClicado.transform.position;
                        posicaoDoAtaque.y = pecaSelecionada.transform.position.y; // Mantém a altura

                        Debug.Log($"{minhaPeca.name} capturou {peçaAlvo.name}!");

                        // remove peça comida
                        Destroy(objetoClicado);

                        
                        StartCoroutine(DeslizarPeca(pecaSelecionada, posicaoDoAtaque));
        
                        TrocarTurno();
                        pecaSelecionada = null;
                    }
                    // troca peça de seleção
                    else if (peçaAlvo != null && peçaAlvo.equipe == minhaPeca.equipe)
                    {
                        pecaSelecionada = objetoClicado;
                        Debug.Log("Trocou a seleção para: " + pecaSelecionada.name);
                    }
                }
             }
             else {
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

        // trigger de som quando peca ja foi deslocada
        if ((destino - posicaoInicial).sqrMagnitude > 0.000001f &&
            fonteDeAudio != null && somDeMovimento != null)
        {
            fonteDeAudio.PlayOneShot(somDeMovimento);
        }
    }
}
