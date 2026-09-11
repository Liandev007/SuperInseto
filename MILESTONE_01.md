# Milestone 1 — teste local

Unity **6000.3.24f1**, URP 17.3.0, Input System 1.20.0. Abra
`Assets/Scenes/Nave_TestGym.unity` e aperte Play. Aguarde a importação dos pacotes.
Todas as referências estão na cena; não é preciso criar objetos nem executar geradores.

## Controles

- WASD: correr; segurar Alt: andar; segurar Shift: sprint.
- Mouse: câmera; Space: pular no chão, soltar durante escalada.
- Segurar Ctrl ou C: agachar; soltar levanta somente com espaço livre.
- E: aderir à parede à frente / soltar. Paredes laranja aceitam escalada.
- Na parede: W/S sobe/desce, A/D desloca lateralmente. Ctrl/C também solta.
- Esc: libera cursor; clique na Game view: captura novamente.

## Verificação no Editor

1. Console sem erros; caminhar, correr e sprintar têm velocidades distintas.
2. Pular, controlar direção no ar, colidir com paredes e aterrissar sem atravessar chão.
3. Passar agachado sob o teto baixo no corredor. Soltar Ctrl sob o teto deve manter
   a cápsula baixa; sair permite levantar. Sprint e pulo não ignoram o agachamento.
4. Aproximar-se de frente de cada parede laranja, E, subir, parar, descer.
   Inspecionar `PlayerMotor > State`: WallAttached parado, WallClimbing em movimento.
5. E/Space/Ctrl solta; a gravidade retorna e o personagem pode continuar andando.
   Repetir no ar, junto ao chão e várias vezes. S até tocar o chão deve soltar sozinho.
6. Segurar W até o topo das paredes BAIXA/ALTA deve iniciar `Mantling`, levantar a
   cápsula por fora da parede, avançar sobre a borda e terminar em `Grounded`.
   Continuar andando sobre a plataforma. Na parede `Borda BLOQUEADA` (à direita da
   saída do corredor), segurar W deve manter a aderência abaixo do teto: sem cair
   nem atravessar. Testar S e E/Space/Ctrl nessa posição. Remover o teto durante Play
   deve permitir nova tentativa. Sair pela lateral ou desabilitar `ClimbableSurface`
   ainda solta. Paredes cinzas, tetos e faces horizontais não aceitam aderência inicial.
7. Orbitar câmera contra paredes, corredor, teto baixo e durante escalada; verificar
   obstrução em vários ângulos. Esc e retorno de foco não devem deixar entradas presas.
8. As escadas permitem alcançar plataformas e passagem elevada para testar quedas.
   Testar também a 30/60/120 FPS, se possível.

## Estrutura e limites

`Player` contém cápsula física e scripts. `VisualRoot` contém apenas a representação
visual substituível. Preserve o root sem escala e a origem nos pés; substitua os filhos
de VisualRoot pelo modelo futuro. Parâmetros no Inspector dos componentes correspondentes.
O player usa a layer interna Ignore Raycast (2); máscaras de consultas excluem essa layer,
mas o CharacterController continua colidindo com a geometria Default.

Superfícies deste protótipo são estáticas, verticais e marcadas com `ClimbableSurface`.
`LedgeMantle` valida apoio da cápsula, espaço em pé e os dois segmentos do trajeto;
o CharacterController permanece habilitado durante a transição. Altura de busca,
profundidade, margem, elevação e duração são ajustáveis no Inspector. Uma borda
inválida bloqueia somente a subida; não há escalada de teto nem transferência entre
cantos. Obstrução surgindo durante um mantle cancela a transição e devolve a gravidade.
Os materiais simples recebem cor ao entrar em Play; a cena já contém toda a geometria.

Editor Unity e compilador C# não estão disponíveis no ambiente de implementação.
Referências/estrutura foram verificadas estaticamente; compilação real, colisões,
sensação de câmera e transições em Play precisam da verificação acima. Não há alegação
de teste de gameplay aprovado sem executar o Editor.

Testes de regressão: Window > General > Test Runner > PlayMode > Run All.
`LedgeMantleTests` cobre chegada ao chão superior sem salto brusco, continuidade do
deslocamento, teto bloqueado, descida e soltura durante aderência/mantle. Esses testes
foram adicionados, mas não executados aqui por ausência do Unity Editor.

Milestone 1 continua aguardando validação local; nenhum milestone posterior foi iniciado.
