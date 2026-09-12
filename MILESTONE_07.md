# M7 — combate integrado

Abra `Assets/Scenes/Nave_TestGym.unity` no Unity 6000.3.24f1 e aperte Play.
Saia do corredor inicial (E na porta; C/Ctrl na passagem baixa), vire à esquerda
e siga a faixa amarela pelo lado esquerdo das escadas até o fundo da sala.
A entrada oeste fica em **(-9, 0, 15)**. A arena ocupa **x -12..12, z 15..33**;
a segunda passagem em **(9, 0, 15)** também permanece aberta.

`M7 - Arena integrada` instancia uma única vez os prefabs aprovados: um melee,
um ranged e um drone. Os três inimigos individuais e os dois TargetDummy permanecem.
Não há respawn, gatilho de ativação ou comunicação entre inimigos; eles usam percepção normal.

- Melee/ranged: prioridades locais de avoidance **40/60**, raio **0,4 m** e demais valores preservados.
- Drone da arena: obstáculo de avoidance sem carving, raio **0,5 m**, removido da avoidance ao morrer.
  Isso permite aos agentes terrestres desviar do drone parado; não controla o voo nem usa NavMeshAgent no drone.
- Drone: `RangedWeapon` no prefab com **windup 0,65 s**, **recovery 0,4 s**, **cooldown 0,8 s**.
  Ciclo mínimo nominal **1,85 s** (antes 2,25 s), condicionado a mira, distância e linha de visão.
  O ajuste também vale para o drone individual M6. Dano 10, velocidade do projétil 8 m/s,
  lifetime 3 s e altura de combate 1,45 m preservados.
- O único bake existente foi ampliado para incluir a arena; não há outro NavMesh ou novas layers.

## Teste local

1. Entre, avance para o centro e observe melee aproximando, ranged disparando e drone descendo à altura de combate.
2. Use as coberturas de 3 m e o pilar; confirme que ambos os atiradores perdem a linha de visão.
3. Use Q contra o melee e ambos os projéteis; durante a janela invulnerável o Health não deve diminuir.
4. Use esquerdo/combo e direito/pesado nos três. Mate-os em ordens diferentes em novas sessões de Play;
   confirme que cada sobrevivente continua agindo e que mortos não reaparecem nem iniciam ataques.
5. Deixe o jogador chegar a 0 HP e verifique o Console. Reinicie Play para repetir (sem respawn).
6. Confira saídas, reposicionamento e ausência de sobreposição persistente; teste também câmera,
   movimentação/escalada/mantle, puzzle M2, TargetDummy e salas individuais M4–M6.
7. Test Runner → PlayMode → `CombatIntegrationTests`: impactos simultâneos, invulnerabilidade/morte única,
   seis ordens de morte, cobertura durante windup, cooldown independente e morte do jogador.

Validação neste ambiente: revisão estática de código, YAML, referências, preservação dos assets anteriores
e geometria de acesso/patrulha. Sem Unity ou compilador C#: compilação, PlayMode, bake/avoidance reais,
câmera, dificuldade e desempenho ainda precisam da validação local. Projéteis já disparados continuam
até impacto ou lifetime após a morte do emissor, conforme M5/M6. Não há novos sistemas de combate.
