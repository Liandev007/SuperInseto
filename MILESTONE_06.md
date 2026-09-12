# M6 — drone de segurança

Abra `Assets/Scenes/Nave_TestGym.unity` e dê Play. Entre na sala do atirador do M5; a nova abertura ao sul, em (22,0,3), leva à sala do drone. Ele nasce em (22,2.2,-3), com três waypoints entre 1,8 e 2,8 m. A sala tem teto, pilar e cobertura. Melee, ranged, alvos e percurso/puzzle anteriores permanecem disponíveis.

Prefab: `Assets/SuperInseto/Prefabs/GovernmentSecurityDrone.prefab`. A raiz lógica contém esfera de colisão (centro zero, raio 0,45 m), Health, DroneFlight/DroneBrain, EnemyPerception e RangedWeapon do M5. Eye e FirePoint são substituíveis. VisualRoot pode ser trocado sem alterar lógica. Não há NavMeshAgent, rotor, hover ou animação final; eventos de estado, Health e arma permitem apresentação futura.

Valores iniciais ajustáveis: 60 HP; patrulha 1,4 m/s e espera 1 s; aproximação/recuo 2,1 m/s; altura preferida do centro 1,45 m, mínima 1,1 m e máxima 3,2 m sobre o piso fixo configurado. Alcance de visão 12 m/130°; memória 3 s, busca parada 1 s; perseguição máxima 14 m. Faixa de combate 3–6,5 m, muito perto 2 m; recuo até 1,8 m por 1 s e pausa de 1,2 s. Tiro reutilizado: dano 10, velocidade 8 m/s, duração 3 s, windup 0,75 s, recovery 0,5 s e cooldown adicional 1 s. Ajustes de disparo estão no RangedWeapon do prefab.

Teste: observe a rota e alturas; entre no FOV; amarelo indica alerta/mira. Use pilar/cobertura para quebrar visão, movimente-se e use Q para esquivar. Aproxime-se correndo: o drone desce à altura de combate, recua mais devagar que o jogador e faz pausas, permitindo golpes no chão. Mouse esquerdo = combo; direito = pesado. Em 0 HP ele fica inerte/cinza, cancela tiros pendentes e desativa após 2 s. Reinicie Play para restaurá-lo. Projéteis já lançados terminam normalmente por impacto/lifetime e ignoram Enemy.

Valide localmente colisão com paredes/teto/pilar, descida perto da cobertura, recuo bloqueado, perda/retorno à patrulha, dano único, dodge e morte durante windup. Execute DroneTests e as suítes anteriores (RangedTests cobre cooldown, projétil single-hit, aliados e invulnerabilidade). Teste também M1–M5 e os três inimigos ativos, inclusive morte independente.

Limites: voo restrito ao volume da sala; desvio local com poucas alternativas, sem navegação 3D global. Rotas impossíveis podem exigir waypoints melhores; o drone pausa/troca de waypoint após timeout. Alturas são relativas ao piso configurado, sem acompanhamento de terreno. A movimentação limita o passo de tempo a 0,1 s em frames muito longos para manter estabilidade.

Validação neste ambiente: revisão estática e conferência de YAML, referências, geometria e preservação dos arquivos anteriores. Unity Editor/compilador C# indisponíveis; testes PlayMode, navegação física e sensação do combate dependem da validação local. Scripts/prefabs de M1–M5 e todos os ProjectSettings permanecem intactos; somente a abertura na parede sul do M5 conecta a nova sala. Sem merge na main.
