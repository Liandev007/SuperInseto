# M12 — Puzzles ambientais

Abra `Assets/Scenes/Nave_TestGym.unity`. Saia do corredor inicial, vá à esquerda/oeste, passe pela área M11 e siga até a **faixa roxa e a entrada em (-12, 0, 0)**. Uma abertura de 3 m foi feita na parede oeste; seus trechos restantes foram preservados. A sala M12 fica entre **x=-22/-12 e z=-2/12**. Nenhuma área anterior foi removida.

## Puzzle A — sabotagem

Na primeira seção, destrua o painel de controle em **(-20, 0, 1)**, com **80 HP**, instanciado do `BreakablePanel` M11. Leve/pesado/R/F usam dano normal. O evento `DestructibleObject.Destroyed` completa a condição; o `EnvironmentalPuzzle` desativa uma vez o GameObject da barreira em **(-17, 0, 3)**, removendo visual e colisão juntos. A raiz da barreira permanece inativa para depuração. Siga ao norte pela passagem liberada.

## Puzzle B — circuito

Receptores em **(-20, 0, 6)** e **(-14, 0, 6)** começam escuros. Acerte cada um com **F**, em qualquer ordem: ficam ciano. Com 1/2, a porta em **(-17, 0, 8)** permanece fechada; com 2/2, abre automaticamente usando **SlidingDoor.OpenFromControl**, no modo Remote do M2. Atravesse até a marca verde ao norte.

`BioelectricProjectile` envia o contrato exclusivo `IBioelectricReceiver` apenas ao collider realmente atingido, depois de encerrar seu voo. O `BioelectricReceiver` não implementa IDamageable e não usa Health: leve, pesado, R e tiros inimigos não o energizam. Um segundo Ferrão em receptor ligado não repete o evento. Paredes continuam bloqueando tiros; o projétil ainda termina no impacto/lifetime.

## Configuração e feedback

- Novo prefab: `BioelectricReceiver`. `BreakablePanel` é reutilizado sem alterações; barreira e porta são graybox na cena.
- `TestGymEnvironmentalPuzzles` instancia o painel, os dois receptores e dois controllers uma vez em Awake. Selecione a raiz M12 para ver os pontos com Gizmos; durante Play, cada controller mostra `isSolved`, `conditionsMet` e `totalConditions`.
- `EnvironmentalPuzzle` aceita condições de destruição/energização em conjunto e saída `UnityEvent onSolved` configurável. `Configure` permite montar as mesmas referências uma vez num GameObject inativo. Cada saída é executada uma vez; vazio, referência ausente ou condição duplicada não resolve. Reabilitar não reinicia puzzle resolvido.
- Feedback provisório: cores OFF/ON, barreira desaparecendo, porta deslizando e texto 0/2–2/2 visível apenas na sala. Não há polling de condições por frame.
- Energia e cooldowns não mudam: R custa **35**, F custa **20**, com a regeneração e os cooldowns do M10. Disparos de puzzle não são gratuitos nem devolvem energia.

## Validação local

Teste A com as quatro fontes de dano; confira que apenas destruir o painel correto desliga a barreira e permite passar. No B, tente leve/pesado/R nos receptores: devem continuar OFF. Use F no primeiro, confira a porta fechada, espere cooldown, use F no outro e confira abertura. Reinicie Play para testar a ordem inversa. Confira tiros bloqueados por paredes, repetição de acertos sem repetir consequência e energia consumida normalmente.

Test Runner → PlayMode → `EnvironmentalPuzzleTests`: seis testes cobrem sinal exclusivo, obstrução, ambas as ordens, porta, sabotagem, callbacks únicos e configurações inválidas. Os testes existentes de M9/M10/M11 permanecem.

**Limitações:** Unity Editor e compilador C# indisponíveis neste ambiente; houve revisão estática de código/YAML/GUIDs/referências e geometria. Compilação, PlayMode e regressão jogável precisam de validação local. A sala não expande o NavMesh terrestre e não depende de combate. Sem save, sequência, timer ou rede elétrica real; sair e voltar ao Play reinicia os puzzles.
