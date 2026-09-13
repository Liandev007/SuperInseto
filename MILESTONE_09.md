# M9 — Ferrão Bioelétrico

Abra `Assets/Scenes/Nave_TestGym.unity` e aperte Play. F dispara; E/Q/R e os dois botões do mouse mantêm suas funções. A pequena cruz marca o centro da câmera.

- Player / BioelectricStinger: dano **25**, velocidade **18 m/s**, lifetime **3 s**, raio **0,1 m**, cooldown **2,5 s a partir do disparo**, windup **0,25 s**, recovery **0,30 s**, distância de mira **45 m**. Todos ajustáveis no Inspector.
- `Player/BioelectricFirePoint - substituivel`: posição local **(0,3; 1,2; 0,6)**. Troque a referência `firePoint` por um socket/bone futuro; o combate não depende do mesh.
- A consulta central da câmera encontra o primeiro sólido/alvo (ou um ponto a 45 m). O projétil parte do FirePoint até esse ponto, sem homing. A mira é atualizada no windup, após a posição da câmera; mover o alvo após o disparo pode evitar o tiro.
- Um sweep do corpo ao FirePoint impede disparar com o braço atravessando cobertura: nesse caso aparece “Disparo bloqueado” e o uso consome cooldown. O projétil faz spherecasts por todo segmento percorrido, ignora o dono, aplica no máximo um dano e termina no impacto/lifetime. Um flash verde de 0,12 s usa o próprio visual após impacto.
- F/R/combate usam o bloqueio de ação existente. F exige jogador vivo, em pé e no chão; não concede invulnerabilidade. Cancelamento não devolve cooldown. Sem eventos de animação, o timer dispara; futuramente habilite `useAnimationEvents` e chame `CombatAnimationBridge.FireBioelectricStinger`. Evento ausente tem timeout. Eventos `PreparationStarted`, `Fired`, `Blocked`, `Finished` e `BioelectricProjectile.Impacted` permitem substituir o feedback.

## Teste local

1. Saia do corredor inicial (Ctrl/C na passagem baixa). Mire no TargetDummy próximo de **(0, 0, 3)** e pressione F: preparação, esfera verde em movimento, **25 HP por tiro**, flash e recarga. Spam não dispara; após a recarga pode usar novamente. Quatro acertos derrotam o alvo de 100 HP.
2. Para a arena M7, siga a faixa amarela à esquerda, ao fim do corredor, até a entrada oeste em **(-9, 0, 15)**. Mire no ranged e depois no drone no ar; F deve atingir por colisão real. Melee e Dummy também são alvos válidos.
3. Use cobertura entre o Player e os inimigos: tiros devem parar na parede, inclusive quando a câmera enxerga por cima/ao lado mas o FirePoint está bloqueado. Teste tiros junto a paredes e tiros livres até expirarem (3 s + um frame). Player não perde HP pelos próprios disparos.
4. Teste F durante Q, ataques, agachamento, salto, escalada, mantle e R; teste R durante F. Após recovery, movimento, combo, pesado, Q e R devem voltar a funcionar. Teste morte durante windup: nenhum disparo novo.
5. Revalide portas/terminal/cartão, M1 e os inimigos individuais/arena. Projéteis já lançados continuam até impacto/lifetime, mesmo após a morte do Player, como os tiros inimigos.

Test Runner → **PlayMode → BioelectricStingerTests**: 6 testes de física/vida, direção/obstrução, descarte, cooldown, exclusão F/R e cancelamento. Rode também os testes existentes de Ranged/ChitinImpact. Os testes de Player precisam de captura do cursor.

**Validação neste ambiente:** revisão estática de código, YAML, GUIDs e referências. Unity Editor/compilador C# indisponíveis: compilação, testes PlayMode e sensação de mira/combate precisam ser validados localmente. Visual e mira são provisórios; não há energia, aim assist, animações finais ou novas mecânicas além do Ferrão.
