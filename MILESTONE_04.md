# M4 — agente corpo a corpo

Abra `Assets/Scenes/Nave_TestGym.unity` e pressione Play. A área do M4 ocupa o lado direito do corredor inicial (x=4..11, z=-13..-2); saia do corredor em direção ao salão e vire à direita. O agente aparece no Play, após a geração local do NavMesh. Há três pontos de patrulha, cobertura central e uma caixa baixa. M1–M3 e os dois alvos permanecem no lugar.

- Observe a patrulha, aproxime-se pela frente: amarelo = alerta/antecipação; vermelho = perseguição/combate. Use a cobertura para interromper visão por mais de 3 s e observar retorno à rota.
- Mouse esquerdo = combo leve; direito = pesado; Q = esquiva; demais controles de M1/M2 mantidos. O golpe fica orientado durante a antecipação, permitindo sair do alcance. Verifique perda de 15 HP por acerto, invulnerabilidade da esquiva, dano leve/pesado no agente e morte a 0 HP. Reinicie Play para restaurar jogador/agente.
- Teste perseguição ao redor da cobertura e diante de portas fechadas/abertas; o agente não deve atravessar obstáculos. Teste o jogador sobre plataformas inacessíveis e a perda de visão.
- Refaça escalada/mantle, puzzle, cartão, portas e dano/morte dos dois TargetDummy.

Prefab: `Assets/SuperInseto/Prefabs/GovernmentMeleeAgent.prefab`. Vida: 120; patrulha: 1,7 m/s; perseguição: 3,6 m/s; visão: 8 m/110°; memória: 3 s; limite de perseguição: 13 m. Ataque: início a 1,45 m entre raízes, windup 0,5 s, janela 0,12 s, recuperação 0,55 s, cooldown adicional 0,55 s. Alcance físico 1,25 m e raio 0,45 m a partir de AttackOrigin em MeleeHitbox. Todos ajustáveis no Inspector. Spawn, rota e folhas de portas estão em TestGymNavigation na cena. A layer Enemy (8) foi adicionada à máscara de segurança das quatro portas para impedir fechamento sobre o agente; scripts de M1–M3 não mudaram.

Lógica separada de VisualRoot. Eye e AttackOrigin são referências substituíveis. EnemyPresentation oferece Animator opcional (Speed float, State int conforme EnemyState, Attack/Hit triggers); EnemyAnimationBridge recebe eventos OpenDamageWindow/CloseDamageWindow. Ative useAnimationEvents somente com clips configurados. O temporizador encerra ações mesmo sem eventos.

Validação neste ambiente: revisão estática e verificação de YAML/GUIDs. Unity Editor e compilador C# indisponíveis; compilação real, bake em runtime, PlayMode e sensação de navegação/combate precisam da validação local. Em Window > General > Test Runner > PlayMode, execute EnemyTests e as suítes anteriores. Novos testes cobrem percepção, antecipação, dano único, cooldown, invulnerabilidade e morte. Não foram executados aqui.

Limites do protótipo: um único agente; NavMesh construído uma vez no Play, portas usam carving; sem links de escalada, investigação avançada, respawn ou animação final. Geometria móvel adicional requer configuração de navegação. Main não recebe merge neste milestone.
