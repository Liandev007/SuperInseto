# Milestone 2 — interação

Abra `Assets/Scenes/Nave_TestGym.unity` no Unity 6000.3.24f1 e aperte Play.
Objetos, links e componentes já estão configurados na cena.

## Percurso completo

1. Na saída do ponto inicial, mire no painel cinza da PORTA COMUM, aproxime-se e
   pressione E. Atravesse e use o painel no outro lado para fechá-la/abri-la.
2. Siga pelo corredor e saia na área de testes do M1. Vire à esquerda e entre na
   ala de salas, retornando na direção do início (à esquerda do corredor original).
3. No fundo do hall está o painel vermelho SEGURANÇA — SecurityLevel01. Sem o
   cartão, E deve mostrar “Porta bloqueada — cartão necessário: SecurityLevel01”.
4. Volte um pouco pelo hall e procure o BOTÃO azul na divisória à esquerda.
   E abre a porta lateral da antecâmara. O painel cinza dessa porta só informa
   que ela é controlada remotamente; use o botão azul.
5. Entre na antecâmara. Acione o TERMINAL verde junto à próxima porta para abrir
   a sala do cartão. O botão e o terminal controlam portas diferentes.
6. Entre e pegue o cartão amarelo sobre o pedestal usando E. Ele desaparece e
   o feedback confirma a aquisição de SecurityLevel01.
7. Retorne pelas portas abertas ao hall. E no painel vermelho abre a segurança.
   Entre na sala final. A porta de segurança também tem painel no lado interno.

## Controles e prioridade

M1 preservado: WASD, Alt, Shift, Ctrl/C, Space e mouse.
E interage apenas com objeto a até 2,4 m, dentro de um cone de 18° em relação à
câmera e sem paredes entre objeto/câmera/jogador. Mire com a cruz central; o
prompt confirma o alvo. Distância, ângulo e duração do feedback são ajustáveis.

Durante escalada/mantle, E continua sendo soltar. Fora desses estados, um alvo
válido consome E para interação; sem alvo, o comando mantém a aderência do M1.
Uma tentativa negada por falta de cartão também consome E. Nenhuma lógica de
movimentação, câmera, escalada ou mantle foi reimplementada.

## Testar localmente

- E longe, olhando para trás ou através de paredes não deve interagir.
- Abrir/fechar a porta comum pelos dois lados, inclusive durante movimento.
- Porta fechando com jogador no vão deve parar e prosseguir quando o vão ficar livre.
- Botão e terminal abrem somente as portas vinculadas; a segurança permanece bloqueada.
- Sem cartão, conferir a negativa e o texto. Coletar uma vez, retornar e abrir segurança.
- Completar todo o percurso; não deve haver entrada lateral ou por cima nas salas fechadas.
- Repetir a interação depois de correr, sprintar, pular, agachar, escalar, fazer mantle
  e aterrissar. Conferir que E durante escalada apenas solta, inclusive perto de um alvo.
- Window > General > Test Runner > PlayMode > Run All: regressões de interação e M1.

## Arquivos e limites

Scripts novos em `Assets/SuperInseto/Scripts/Interaction`: `Interactable`,
`InteractionProbe`, `PlayerInteractor`, `AccessCredentials`, `SlidingDoor`,
`DoorHandle`, `ControlPanel` e `AccessCard`. `ControlPanel` usa UnityEvent configurável
para vincular uma ação, sem implementar mecanismos futuros. `DoorHandle` compartilha
o mesmo estado e a mesma checagem de acesso nos dois lados da porta.

A única alteração em script do M1 é o consumo contextual de E no `PlayerInputReader`.
Novos componentes no Player e o grupo `M2 - Interacao e puzzle` estão na cena.
Credenciais e portas reiniciam ao sair de Play; não há inventário completo ou persistência.
Feedback e cores são provisórios. Portas são folhas verticais simples com checagem do
player no fechamento; não foi implementado transporte de objetos ou física de esmagamento.

Unity Editor indisponível no ambiente: testes PlayMode adicionados, mas não executados.
Validação estática cobre referências, ligação dos painéis, geometria do percurso e
preservação dos scripts do M1. Compilação real e jogabilidade ainda exigem o teste local.
Nenhum merge na main ou início do Milestone 3.
