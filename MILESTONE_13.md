# M13 — Morte, checkpoint e respawn

Abra `Assets/Scenes/Nave_TestGym.unity` e pressione Play.

- Inicial: `(0, 0, -12)`, índice 0. Fallback: posição/rotação inicial do Player, mesmo sem referência de checkpoint.
- Checkpoint 2: `(-10, 0, 12)`, índice 1, na rota oeste para a entrada da arena M7 `(-9, 0, 15)`.
- Saia do corredor inicial, siga pela esquerda/oeste até x=-10 e avance ao norte. Passe pela base do checkpoint antes de entrar na arena.
- Trigger ativa uma vez; o checkpoint atual fica ciano. Voltar a um índice anterior não regride o progresso.

Morte por `Health.Died` bloqueia input e congela movimento, cancela habilidades/aderência/mantle e mostra **DERROTADO**. Após **2 s**, o coordenador verifica chão e espaço para a cápsula em pé, desabilita temporariamente o CharacterController, reposiciona e restaura os sistemas existentes. Vida e energia voltam ao máximo; atraso de regeneração, combo, dodge, invulnerabilidade antiga e cooldowns R/F são limpos. A câmera reutiliza sua colisão no novo ponto. Nenhuma imunidade adicional é concedida.

O ponto indicado é preferido. Se ocupado, são avaliados até 16 pontos próximos (passo 1 m, dois anéis), depois o ponto inicial. Sem destino seguro, permanece morto e tenta novamente após 0,5 s. Folga configurável: 0,08 m. O prefab `Checkpoint` permite ajustar índice, trigger, SpawnPoint e marcador; o Player referencia o checkpoint inicial e a câmera.

Eventos `DeathStarted`, `Respawned` e `CheckpointActivated` permitem apresentação/animações futuras. `CheckpointPosition` e `CurrentCheckpoint` expõem o estado em memória. Não há save, reset de mundo, KillVolume ou respawn de inimigos.

## Validação local

1. Morra antes de avançar: deve voltar ao início com vida/energia cheias.
2. Ative o checkpoint 2, gaste R/F, entre na arena e deixe os inimigos reduzirem a vida a zero.
3. Durante o delay, tente WASD, E, Q, R, F, pulo e ataques: nada deve iniciar. Após o retorno, confirme posição, rotação, câmera, recursos e poderes disponíveis. Repita a morte.
4. Repita após agachar, dodge, aderência, mantle e preparação de cada poder; confira recuperação dos controles. Volte ao checkpoint inicial e confirme que o segundo permanece atual.
5. Resolva os puzzles/destrua objetos antes de morrer: devem permanecer resolvidos/destruídos. Inimigos vivos devem voltar a detectar; mortos permanecem mortos.
6. Execute `RespawnTests` na aba PlayMode do Test Runner (quatro testes de integração). Regresse movimentação, combate, interação e M1–M12.

Validação neste ambiente: revisão estática de C#, YAML, GUIDs, referências e preservação dos arquivos anteriores. Não há Unity Editor nem compilador C#; compilação, testes PlayMode, física, câmera e combate simultâneo precisam de validação local. Projéteis já disparados continuam seu ciclo normal de impacto/lifetime; inimigos e mundo não são recriados.
