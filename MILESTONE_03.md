# Milestone 3 — combate placeholder

Unity 6000.3.24f1. Abra `Assets/Scenes/Nave_TestGym.unity` e aperte Play.
Um alvo de 100 HP está logo após o corredor, no centro da área aberta (x=0, z=3).
Outro alvo de 150 HP fica ao fundo, à direita (x=9, z=12). Não possuem IA.

## Controles e teste local

- Mouse esquerdo: ataque leve (12 de dano). Clique novamente durante a parte final
  do golpe para enfileirar o próximo; no máximo 3 golpes por sequência. Uma pausa
  maior que Combo Window após a recuperação inicia novamente em Leve 1.
- Mouse direito: pesado (30 de dano), mais lento, com recuperação maior.
- Q: esquiva de 2,5 m em 0,25 s; usa WASD em relação à câmera. Parado, vai para
  a frente do personagem. Cooldown de 0,55 s após terminar. Invulnerável entre
  0,04 e 0,16 s da esquiva; parâmetros no Inspector.
- Ataques/esquiva exigem chão e postura em pé; não iniciam agachado, durante pulo,
  aderência, escalada ou mantle. Durante ações, E/pulo/agachar não iniciam outro estado.
  Ao terminar, o controle retorna. E tem prioridade se pressionado junto com um ataque.
- Esc cancela a ação e libera cursor. O clique para recapturar o cursor não ataca.

Teste leve → combo 1/2/3 → pausa → Leve 1 → pesado → Q → movimentação → derrotar alvo.
Observe HP do alvo, flash branco, movimento visual temporário e texto discreto de acerto.
O alvo desativa ao chegar a zero; reinicie Play para repetir. Não há respawn/checkpoint.

Confira também: ataque fora de alcance, alvo atrás/atrás de parede, spam de clique,
Q contra paredes/portas fechadas, Q saindo de plataforma, cooldown e retorno ao controle.
Repita M1 (andar/correr/sprint/pulo/agachar/escalada/mantle) e todo o puzzle do M2.
Durante escalada E continua soltando; ataque e Q devem ser ignorados. Repita a 30/60 FPS.

## Separação do visual

`Health` e `IDamageable` são reutilizáveis; `PlayerCombat` gerencia ações/tempo;
`LightCombo` contém a sequência; `MeleeHitbox` detecta e deduplica receptores.
`PlayerMotor` continua sendo o único responsável por mover o CharacterController,
inclusive durante esquiva. O collider nunca é desativado para deslocar o jogador.

`Player/CombatOrigin - socket substituivel` é a origem temporária dos golpes, independente
do mesh. Substitua a referência Attack Origin por um socket da mão/braço quando existir.
Alcance/raio ficam em MeleeHitbox; danos, janelas, combo, recuperação e esquiva em
PlayerCombat; vida máxima em Health.

`CombatFeedback` é apresentação descartável: pode ser removido junto com os meshes
do placeholder. Ele apenas gira VisualRoot, sem mudar física/hitbox. A lógica funciona
sem esse componente e sem os meshes. Preservar o root físico e CombatOrigin.

Para o Animator futuro: eventos `ActionStarted(action, comboStep)` / `ActionEnded`
e `Health.Damaged` / `Health.Died` permitem selecionar clips de leve 1/2/3, pesado,
esquiva, dano e morte. Coloque `CombatAnimationBridge` no objeto do Animator e aponte
para PlayerCombat. Só então habilite Use Animation Events e coloque os eventos
`OpenDamageWindow` / `CloseDamageWindow` nos clips. Sem clips, deixe desabilitado:
as janelas usam o temporizador. Mesmo com eventos, o timeout de recuperação encerra
a ação e fecha o dano. Reabrir janela no mesmo golpe não repete dano no mesmo alvo.

## Validação e limites

Window > General > Test Runner > PlayMode > Run All inclui testes de vida/morte,
invulnerabilidade, combo, dano único por golpe, pesado e alcance/oclusão, além de M1/M2.
Unity Editor/compilador indisponíveis aqui: testes adicionados, mas não executados;
compilação, sensação dos golpes e integração física exigem validação local.
Nenhum agente agride o jogador neste milestone; Health/TakeDamage e invulnerabilidade
foram preparados e cobertos por testes. Morte do jogador bloqueia ações, sem menu/respawn.
Não há IA, stamina, lock-on, poderes novos, animações ou arte final. Main não foi alterada.
