# M5 — agente atirador

Abra `Assets/Scenes/Nave_TestGym.unity` e dê Play. No salão, siga pelo lado direito até o TargetDummy de 150 HP próximo de (9,0,12). A nova abertura na parede leste leva à sala do atirador (x=12..26, z=3..15). O agente roxo aparece no Play em (22,0,7), com três pontos de patrulha e cobertura central. Melee, alvos e percurso/puzzle anteriores permanecem disponíveis.

Controles mantidos: WASD/mouse, Shift sprint, Ctrl/C agachar, Space pulo, E interação; mouse esquerdo combo, direito pesado, Q esquiva. Use a cobertura para quebrar visão; amarelo indica preparação, vermelho breve indica disparo. Saia lateralmente da mira fixada ou use Q. Aproxime-se para provocar recuo e derrotar o atirador. Se não houver recuo seguro, ele para e tenta novamente após uma pausa, sem atirar de perto. Reinicie Play para restaurar vida/agentes.

Prefabs em `Assets/SuperInseto/Prefabs`: `GovernmentRangedAgent` e `GovernmentProjectile`. Valores iniciais ajustáveis: 90 HP; visão 14 m/120°; memória 3 s; Chase Range 18 m; faixa preferida 4–8 m; Too Close 2,7 m; Max Combat 15 m; recuo 3 m, timeout 2 s e nova tentativa após 1 s. Tiro: 12 de dano, velocidade 10 m/s, vida 3 s, raio 0,09 m, preparação 0,65 s, recuperação 0,4 s e cooldown adicional 0,8 s. Socket FirePoint substituível, independente de VisualRoot. Sem root motion ou ossos obrigatórios.

RangedPresentation aceita Animator opcional (Speed float, State int conforme RangedState, Aim bool, Fire/Hit triggers). RangedAnimationBridge.Fire encaminha um único disparo por ação; useAnimationEvents fica desligado até haver clips. Perder visão ou morrer cancela tiro pendente. Projéteis já disparados terminam a trajetória/impacto/tempo de vida. Eles ignoram Enemy, atingem cenário e aplicam dano somente na layer do jogador, usando IDamageable/Health. Invulnerabilidade bloqueia o dano e consome o projétil.

Uma única geração de NavMesh atende às duas salas. Somente a abertura na parede lateral, a extensão do graybox e os bounds serializados de TestGymNavigation foram necessários. Nenhum script aprovado de M1–M4, pacote ou ProjectSettings foi alterado.

Validação local necessária: compilar/importar; observar patrulha/FOV/oclusão; aproximação, mira e recuo sem atravessar paredes; fechar portas entre jogador e inimigo; observar expiração dos projéteis; esquivar, receber dano único, causar dano leve/pesado e matar durante preparação. Testar os dois agentes simultaneamente e refazer M1–M4 (incluindo dois TargetDummy). Execute PlayMode RangedTests e as suítes anteriores no Test Runner.

Neste ambiente, somente revisão estática e verificações de YAML/GUIDs/geometria foram possíveis. Unity Editor/compilador C# indisponíveis: testes automatizados, NavMesh em runtime e sensação do combate não foram executados. Sem pooling, coordenação, cover system ou animações finais. O reposicionamento é limitado a poucos candidatos com caminho completo; não resolve táticas complexas.
