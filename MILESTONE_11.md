# M11 — Objetos destrutíveis

Abra `Assets/Scenes/Nave_TestGym.unity`. Saia do corredor inicial, passando pela porta comum e pela passagem baixa; vire à esquerda ao chegar ao salão. A faixa ciano em **(-4,95; 0; -1,6)** marca a área M11, antes da parede escalável baixa. As áreas anteriores continuam presentes.

`TestGymDestructibles` instancia uma vez, em Awake, os prefabs reutilizáveis `BreakableCrate` e `BreakablePanel`. Selecione a raiz **M11 - Destructible Test Area** para ver os pontos com Gizmos no Editor; os objetos aparecem em Play:

- Duas caixas de **50 HP**, em **(-5,8; 0; 0)** e **(-4,1; 0; 0)**.
- Um painel de **80 HP**, em **(-4,95; 0; 1,5)**.

Todos usam `Health`/`IDamageable`/`DamageInfo` existentes, na layer Default (0). Configure resistência no `Health.maxHealth`. `DestructibleObject` mantém `IsDestroyed` e eventos `Damaged`/`Destroyed`. Em zero, uma única transição desativa os colliders filhos e os NavMeshObstacles quando `disableColliderOnDestroy` está ligado, esconde `intactVisual` quando configurado e mostra uma marca baixa sem collider. A raiz/Health permanecem; Health morto rejeita dano e cura posteriores. O flash de dano é provisório, sem Update por objeto ou fragmentação.

As caixas/painel usam carving de NavMeshObstacle, como já ocorre nas portas: o bake existente ignora raízes com Health. Ao quebrar, o obstacle é desativado; atualização do carving pode levar um ciclo do NavMesh. Não há rebake, alterações na IA ou obrigação de destruir caixas para inimigos alcançarem outras áreas. As laterais M11 são cenário normal, não destrutíveis.

## Teste local

1. Selecione o `Health` da caixa durante Play e acompanhe HP. Leve causa **12**, pesado **30**; ataques só funcionam no alcance normal. Dois pesados destroem uma caixa de 50 HP.
2. Posicione-se perto de **(-4,95; 0; -1,3)**, à frente das duas caixas. R deve retirar **35 de cada uma**, deixando **15 HP**; múltiplos colliders não multiplicam dano. Cooldown e energia continuam valendo.
3. Mire na caixa com F: **25 por tiro**. Um tiro termina nela mesmo se a destruir, sem atingir o painel atrás na mesma execução. Depois de quebrada, um novo projétil pode atravessar seu antigo espaço.
4. Mire no painel com F: quatro acertos o destroem (80 HP). Use também R/leve/pesado. Paredes e objetos sólidos ainda bloqueiam os ataques conforme os sistemas anteriores.
5. Quebre uma caixa e caminhe pelo espaço liberado. Confira visual baixo, raiz persistente, `IsDestroyed = true`, colliders/obstacle desativados e ausência de novas transições em zero. Pode-se contornar ou pular obstáculos: esta passagem não é um puzzle.
6. Confirme R/F consumindo **35/20 de energia**, sem recompensa pela destruição. Teste Q, combos, portas/cartão, escalada/mantle e os três inimigos na arena M7. Tiros inimigos colidem com os objetos, mas mantêm seu filtro atual de dano ao Player.

Test Runner → PlayMode → `DestructibleTests`: quatro testes de quebra única/colisão, leve/pesado, Impacto múltiplo/obstrução e Ferrão sem penetração. Recomeçar Play restaura os objetos; reabilitar o componente não ressuscita um objeto morto.

Validação disponível aqui: revisão estática de C#, YAML, GUIDs, referências e geometria. **Sem Unity Editor/compilador C#**, portanto compilação, PlayMode, passagem física, carving e regressão jogável exigem validação local. Sem loot, explosões, persistência ou conexão com portas/puzzles.
