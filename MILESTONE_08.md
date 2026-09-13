# M8 — Impacto Quitinoso

Abra `Assets/Scenes/Nave_TestGym.unity` no Unity 6000.3.24f1 e aperte Play.
Saia do corredor inicial e siga a faixa amarela à esquerda até a arena M7, entrada em (-9, 0, 15).
**R** ativa o poder enquanto o jogador está em pé, no chão e sem outra ação.
O binding é ajustável em `PlayerInputReader > Chitin Impact Binding`.

Valores no `ChitinImpact` do Player: **35 de dano, esfera de 3 m, cooldown de 5 s,
windup de 0,5 s e recovery de 0,5 s**. O cooldown conta desde a ativação;
cancelar/desabilitar não o restitui. Movimento e outras ações ficam bloqueados durante a execução.
O poder não concede invulnerabilidade. A reação dos inimigos usa o feedback de dano já existente,
sem knockback ou alterações no NavMesh.

`ChitinImpactOrigin` é um Transform substituível na raiz lógica do Player, a 0,9 m dos pés.
O volume é esférico: o drone precisa estar realmente dentro dele. A consulta aceita `IDamageable`,
deduplica colliders e rejeita o próprio jogador. Paredes bloqueiam a linha até o ponto mais próximo
do collider; corpos na hierarquia Enemy não protegem outros inimigos.

O anel provisório é reutilizado e ocultado após 0,3 s. A lógica não depende do visual.
Para animação futura, habilite `Use Animation Events` e chame `CombatAnimationBridge.EmitChitinImpact`.
Eventos repetidos não repetem dano; a recuperação conta após o evento. Sem evento até windup + recovery,
a ação cancela e libera o controle. `PreparationStarted`, `Impacted` e `Finished` são pontos de apresentação.

## Validar localmente

1. Na arena M7, reúna melee/ranged/drone próximos; R deve preparar, emitir um impacto e mostrar quantos alvos receberam dano.
2. Confira redução de 35 HP por alvo, inclusive TargetDummy; fora da esfera ou atrás de parede sólida não deve atingir.
3. Pressione R repetidamente; apenas uma execução a cada 5 s. Confirme dano recebido normalmente durante o poder.
4. Após recovery, teste movimento, combo, pesado, Q e E. Durante dodge, agachamento, climb e mantle, R deve ser bloqueado.
5. Mate inimigos em ordens diferentes; deixe o jogador morrer durante o windup e confira cancelamento, sem desbloquear a morte.
6. Refaça movimentação/escalada/mantle, puzzle e testes individuais M1–M7.
7. Test Runner → PlayMode → `ChitinImpactTests`: dano múltiplo/único, próprio Player, volume/obstrução,
   cooldown, eventos repetidos/ausentes, estados incompatíveis e morte. Os testes do Player exigem captura de cursor disponível.

Neste ambiente foram feitas revisão estática de código, YAML, referências e preservação dos assets.
Sem Unity Editor ou compilador C#: compilação, testes PlayMode, visual e regressões precisam da validação local.
Nenhuma mudança de câmera, inimigos, projéteis, arena, ProjectSettings ou URP.
