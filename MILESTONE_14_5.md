# M14.5 — Player Humanoid / integração para validação local

**Status: implementação preparada; milestone ainda não declarado concluído.** Este ambiente não possui Unity Editor nem compilador C# para Unity. A importação real, `Avatar.isValid`, aparência e regressões em PlayMode precisam ser verificadas localmente. Nenhum resultado de gameplay ou qualidade visual foi apresentado como teste executado aqui.

Base exclusiva: `main`, commit `d553b619d9e696658885dd0a0d331df3408cc195` (M14). A branch `milestone-14-5-player-animation-preview` já existia apontando exatamente para esse commit, sem commits próprios. A implementação começou em uma cópia limpa dessa base; nenhuma branch, árvore ou implementação anterior do M14.5 foi recuperada. Não há merge na main nem início de M15.

## Primeiro uso no Unity

1. Abra esta branch com **Unity 6000.3.24f1** e aguarde compilação/importação.
2. O preparador de Editor executa uma vez quando os fontes estão disponíveis. Também pode ser chamado em **Super Inseto → M14.5 → Build or Rebuild Player Preview**.
3. Ele importa a T-Pose como Humanoid, verifica Avatar/skin e a hierarquia dos 17 clips, copia o Avatar da T-Pose para eles e só então gera material, controller, prefab e configuração. Um Avatar/clip inválido interrompe a preparação com uma mensagem específica; não há rig forçado.
4. Confira `Assets/SuperInseto/Characters/Player/Animator/ImportValidation.json`, **gerado pelo Unity após sucesso**. Ele contém o resultado real do Avatar, duração/loop dos clips, escala calculada e offset do modelo.
5. Abra `Assets/Scenes/Nave_TestGym.unity` e dê Play. O instalador na raiz Player cria o visual sob o `VisualRoot` existente, antes do Start do SaveManager. A cápsula visual só é ocultada depois de validar as referências.
6. Ajustes ficam em **Super Inseto → M14.5 → Select Animation Settings**. Altura, orientação do modelo e thresholds do Blend Tree requerem rebuild; os demais ajustes são lidos da configuração em runtime.

Os FBXs/PNG e o código estão versionados. **Controller, material, prefab, configuração Resources e relatório de importação são gerados pelo Editor local**; não foram inventados ou declarados como importados neste ambiente. Os `.meta` iniciais dos binários estabelecem GUIDs estáveis; o ModelImporter/TextureImporter serializa as configurações completas no primeiro preparo. Não há referências serializadas a subassets/fileIDs adivinhados. Nenhuma cópia manual de binários é necessária.

Se a preparação falhar, o gameplay continua disponível com o placeholder e um warning de desenvolvimento. Corrija o problema informado e execute o menu de rebuild; não marque o milestone como concluído enquanto houver fallback.

## Inventário efetivamente encontrado e adicionado

O anexo foi recebido como **Player(4).rar**, embora a especificação diga Player(2).rar. Seu inventário coincide com os 20 arquivos esperados. `SourceInventory.json` registra tamanho, SHA-256 e destino de cada um.

Todas as pastas abaixo têm como prefixo `Assets/SuperInseto/Characters/Player/`.

| Arquivo encontrado | Destino versionado | Conteúdo/função |
|---|---|---|
| T-Pose.fbx | Model/T-Pose.fbx | Única malha com skin, esqueleto e Avatar principal |
| Texture.png | Model/Texture.png | Textura original, sem redesenho |
| Idle.fbx | Animations/Idle.fbx | Idle |
| Walk.fbx | Animations/Walk.fbx | Walk |
| Run.fbx | Animations/Run.fbx | Run e Sprint |
| Jump.fbx | Animations/Jump.fbx | Takeoff recortado na importação |
| Floating.fbx | Animations/Floating.fbx | Loop aéreo |
| Fall.fbx | Animations/Fall.fbx | Landing recortado na importação |
| Climb.fbx | Animations/Climb.fbx | Escalada |
| Mantle.fbx | Animations/Mantle.fbx | Mantle |
| LightAtack01.fbx | Animations/LightAtack01.fbx | Combo 1 |
| LightAttack02.fbx | Animations/LightAttack02.fbx | Combo 2, espelhado no Animator |
| LightAttack03.fbx | Animations/LightAttack03.fbx | Combo 3 |
| HeavyAttack.fbx | Animations/HeavyAttack.fbx | Chute pesado, original/espelhado |
| Dodge.fbx | Animations/Dodge.fbx | Esquiva frontal |
| HitReaction.fbx | Animations/HitReaction.fbx | Reação a dano aceito |
| Death.fbx | Animations/Death.fbx | Morte |
| ChitingImpact.fbx | Animations/ChitingImpact.fbx | Impacto Quitinoso |
| BioletricStinger.fbx | Animations/BioletricStinger.fbx | Ferrão Bioelétrico |
| Player.fbx | Não adicionado/não utilizado | Malha estática extra; sem esqueleto, skin ou animação |

Foram adicionados **18 FBXs e 1 PNG**, preservados byte a byte. Os nomes com grafia particular permanecem exatos. Não existem Sprint.fbx, CrouchIdle.fbx ou CrouchWalk.fbx; nenhum asset externo foi buscado.

A inspeção binária encontrou 5.700 vértices na geometria de T-Pose e 65 nós de ossos. Os 17 clips têm os mesmos nomes e relações entre os 65 ossos. Isso indica compatibilidade estrutural; **não substitui a validação Humanoid pelo Unity**. Player.fbx contém uma geometria de 5.700 vértices, quatro materiais e referência a textura, mas nenhum deformer, osso ou AnimationStack. Ele não é usado como segunda malha/skeleton.

## Modelo, Avatar e hierarquia

O Player original, seu CharacterController e todos os componentes continuam na cena. Foi acrescentado somente `PlayerVisualInstaller` à raiz, referenciando o `VisualRoot` já existente. Não foi criado um segundo Player.

Em runtime, sob o VisualRoot existente, o prefab gera `PlayerVisualRoot → ModelFit → SuperInsetoVisual`, com um único Animator no modelo. O primeiro filho permite offsets locais de apresentação; ModelFit acomoda escala/grounding/orientação. O CharacterController continua com altura 1,8 m, raio 0,35 m e raiz em escala unitária.

Configuração do preparador:

- T-Pose: **Human / CreateFromThisModel**, sem importar sua animação de referência como ação jogável. É a única fonte de mesh, skin e Avatar.
- Clips: **Human / CopyFromOther**, `sourceAvatar` obtido do Avatar real importado da T-Pose; rejeição caso a hierarquia ou `AnimationClip.isHumanMotion` não sejam válidos.
- `globalScale = 1`, `useFileScale = true` em todos; o encaixe visual usa a geometria skinned importada. Altura alvo inicial **1,75 m**. A escala uniforme calculada fica no ModelFit, junto ao offset que alinha a menor coordenada Y da malha aos pés. Não há escala numérica inventada antes de executar o importador; o valor real sai em ImportValidation.json.
- `optimizeGameObjects = false`, `preserveHierarchy = true`, sem colliders adicionados, sem câmeras/luzes importadas e sem compressão dos clips neste primeiro passe.
- Material local **URP/Lit**, `_BaseMap = Texture.png`, cor branca, metallic inicial 0,15 e smoothness 0,3. Nenhuma alteração em GraphicsSettings, QualitySettings, TagManager, pacotes ou URP global.
- O instalador desliga apenas os renderers antigos. `CombatFeedback` mantém seu HUD e deixa de girar o placeholder quando o modelo está ativo. Desabilitar o instalador restaura os renderers anteriores e o comportamento original de commit.

## Animator e bridge

Nome gerado: `SuperInseto_Player.controller`. Uma camada Full Body, **Base Layer**, com os estados abaixo. A escolha e as CrossFades vêm de `PlayerAnimationBridge`, lendo os componentes aprovados; não existe outra state machine de gameplay. Não há transitions automáticas por exit-time que possam levar a Idle no ar.

| Estado | Motion | Controle |
|---|---|---|
| Locomotion | Blend Tree Idle/Walk/Run | Velocidade medida pelo CharacterController |
| Takeoff | JumpTakeoff | Tempo visual curto, condicionado ao ar/subida reais |
| Floating | Floating | Loop enquanto fisicamente no ar |
| Landing | FallLanding | Apenas após contato real com o chão |
| Climb | Climb | Loop, pausa parado e reversão ao descer |
| Mantle | Mantle | Progresso real de LedgeMantle |
| Light1 | LightAttack01 | ComboStep 1, original |
| Light2 | LightAttack02 | ComboStep 2, Humanoid mirror |
| Light3 | LightAttack03 | ComboStep 3, original |
| HeavyRight | HeavyAttack | Heavy original |
| HeavyLeft | HeavyAttack | Heavy espelhado |
| Dodge | Dodge | ActionProgress/direção reais do M3 |
| HitReaction | HitReaction | Health.Damaged aceito |
| Death | Death | Morte M13 até respawn |
| ChitinImpact | ChitinImpact | Windup/recuperação reais de M8 |
| BioelectricStinger | BioelectricStinger | Windup/recuperação reais de M9 |

Parâmetros centralizados em hashes:

- Floats: `Speed`, `LocomotionRate`, `ClimbRate`, `VerticalVelocity`.
- Bools: `IsGrounded`, `IsSprinting`, `IsClimbing`, `IsMantling`, `IsDodging`, `IsDead`.
- Int: `AttackIndex`.
- Motion Time floats: `TakeoffTime`, `LandingTime`, `MantleTime`, `Light1Time`, `Light2Time`, `Light3Time`, `HeavyRightTime`, `HeavyLeftTime`, `DodgeTime`, `HitReactionTime`, `DeathTime`, `ChitinImpactTime`, `BioelectricStingerTime`.

Cada ação tem um parâmetro de tempo próprio. Ao trocar Light1 por Light2, por exemplo, a pose de saída não é rebobinada pelo parâmetro do próximo ataque. A arquitetura permite acrescentar uma layer/AvatarMask futuramente; nenhuma UpperBody, IK avançada ou animação procedural foi acrescentada agora.

## Locomoção, ar e root transform

Blend Tree 1D: Idle em **0 m/s**, Walk em **2 m/s**, Run em **4,2 m/s**. `Speed` usa velocidade efetivamente medida, com suavização inicial de 0,10 s; não é apenas WASD pressionado. Sprint usa o mesmo Run, aumentando progressivamente LocomotionRate até **1,15** quando alcança a velocidade real de sprint. Walk/Run começam com rate 1.

Crouch mantém a cápsula e a verificação de espaço do M1. O fallback continua sendo a compressão visual do VisualRoot em Y feita por PlayerMotor, enquanto a locomoção usa Idle/Walk. É uma limitação temporária visível, sem clip crouch inventado.

**`Animator.applyRootMotion = false` em todas as situações**, no prefab, instalação e reset. Nenhum deltaPosition/deltaRotation é aplicado ao Player. OnAnimatorMove apenas restaura o transform local do modelo e os offsets do filho visual; não chama CharacterController.Move nem move a raiz física.

| Clip | Recorte a 30 fps | Root XZ | Root Y | Uso |
|---|---|---|---|---|
| Jump | frames 6–18, 0,20–0,60 s | Extraído, descartado | Extraído, based on feet, descartado | Takeoff de 0,16 s visual |
| Fall | frames 10–32, 0,333–1,067 s | Extraído, descartado | Extraído, based on feet, descartado | Landing após grounded real |
| Mantle | frames 0–116, 0–3,867 s | Extraído, descartado | Extraído, based on feet, descartado | Pose segue Progress físico |
| Dodge | frames 0–30, 0–1 s | Extraído, descartado | Extraído, based on feet, descartado | Pose encaixada nos 0,25 s do M3 |
| Climb | frames 0–67, 0–2,233 s | Extraído, descartado | Extraído, based on feet, descartado | Loop acompanhando velocidade real |
| Floating | frames 0–67, 0–2,233 s | Extraído, descartado | Extraído, based on feet, descartado | Loop aéreo sem altura absoluta do arquivo |

Configuração exata dos seis clips acima: `lockRootPositionXZ = false`, `keepOriginalPositionXZ = true`, `lockRootHeightY = false`, `keepOriginalPositionY = false`, `heightFromFeet = true`. **Bake Into Pose de posição não é usado para esconder a translação desses clips**: ela é extraída como root motion e descartada. Isso evita deixá-la dentro da pose e somá-la ao gameplay.

Todos os clips usam `lockRootRotation = true` e `keepOriginalOrientation = true`. Os demais também extraem/descartam XZ, mas têm `lockRootHeightY = true`, `keepOriginalPositionY = true` e `heightFromFeet = false` para preservar flexão/bobbing e a queda corporal de Death. Essa é a exceção de tratamento Y; não há exceção a applyRootMotion=false.

Jump não executa a parte do arquivo que retorna a Idle. Depois do takeoff, ou assim que a velocidade vertical deixa de ser positiva, a representação vai para Floating enquanto a física continuar no ar. Quedas sem pulo também entram em Floating. Fall só inicia numa transição real ar→chão, sem confundir fim de mantle/load com aterrissagem. Landing dura inicialmente 0,22 s parado, no máximo 0,12 s em deslocamento; não bloqueia input ou movimento e cede a ações/traversal.

Climb congela o loop em WallAttached parado e usa velocidade real/referência **2,2 m/s** em movimento, com reprodução reversa na descida. A análise encontrou translação real nas curvas do Climb apesar da descrição inicial de in-place; por isso ele recebe a mesma extração de deslocamento.

### Mantle

LedgeMantle mantém integralmente validação, trajetória, colisão e cancelamento. A única adição nele é o getter `Progress`. A animação longa do pacote é amostrada de 0 a 1 ao longo da duração física existente de 0,85 s.

Offsets iniciais, em metros no filho visual:

| Ajuste | Valor |
|---|---|
| BaseVisualOffset | (0, 0, 0) |
| MantleStartOffset | (0, -0,18, -0,08) |
| MantleMiddleOffset | (0, -0,08, -0,04) |
| MantleEndOffset | (0, 0, 0) |

SmoothStep interpola início→meio nos primeiros 55% e meio→fim nos 45% finais, acompanhando as duas etapas da trajetória existente. A cada frame a posição é calculada **a partir do valor base**, sem somar ao offset do frame anterior. Ao terminar/cancelar mantle, morrer, desabilitar o visual, carregar ou respawnar, posição/rotação/escala locais voltam aos valores base. A escala crouch do VisualRoot pai continua sob o PlayerMotor.

Os offsets são um ponto inicial de ajuste, não um alinhamento visual aprovado sem render. A remoção da translação Y/XZ trata a elevação do arquivo; o offset pequeno permite corrigir contato com a borda sem mover o CharacterController.

## Combate: Run, commit, mirror e janelas

O combo continua exclusivamente no LightCombo/PlayerCombat M3. Mouse esquerdo, direito e Q mantêm suas funções. Full Body com crossfade inicial de **0,06 s** e retorno de **0,08 s**. Não há pernas correndo sob um soco de Idle.

Ao iniciar Light/Heavy, PlayerCombat captura a velocidade planar medida e, dentro do mesmo action lock, aplica uma contribuição residual decrescente:

`velocidade residual = velocidade de entrada × multiplier × (1 − clamp(elapsed / brakeDuration))`

| Ação | Multiplier | Frenagem | Duração gameplay | Janela de dano gameplay | Dano |
|---|---:|---:|---:|---|---:|
| Light1/2/3 | 0,35 | 0,08 s | 0,38 s | 0,10–0,22 s | 12 |
| Heavy | 0,10 | 0,05 s | 0,80 s | 0,28–0,43 s | 30 |
| Habilidades R/F | 0, sem movimento adicional | Paradas como antes | M8/M9 existentes | Emissão M8/M9 existente | Inalterado |

As durações, janelas, combo window de 0,28 s, danos e cooldowns aprovados não foram alterados. O Heavy planta mais o corpo e mantém sua recuperação original maior. Quando não há modelo válido, o opt-in de commit não é habilitado e o comportamento M3 original permanece.

Mirror: **Light1 direita, Light2 esquerda, Light3 direita**. Heavy alterna **original→mirrored→original** a cada nova ação pesada aceita; respawn/load reiniciam a sequência. O espelhamento é `AnimatorState.mirror`, nunca escala negativa de ossos ou novo combo. A hitbox central/orientada à frente do Player foi preservada; seu alcance e dano não dependem de qual membro está espelhado.

### Sincronização equivalente à bridge de eventos

Foi escolhida a alternativa permitida de **bridge equivalente**, com as fases do gameplay dirigindo o Motion Time da pose. `useAnimationEvents` permanece desligado; os clips não ganham callbacks que possam duplicar dano/energia. O CombatAnimationBridge existente e suas entradas continuam disponíveis, sem substituição do sistema de combate.

Antes da janela M3, a pose avança até o início do contato; durante a janela percorre o trecho do swing/chute; depois completa a recuperação. Intervalos iniciais medidos/estimados a partir das curvas e extensão dos membros:

| Clip | Trecho de contato no fonte | Motion Time normalizado |
|---|---|---|
| LightAtack01 | frames 10–17 de 26 | 0,3846–0,6538 |
| LightAttack02 | frames 19–24 de 41 | 0,4634–0,5854 |
| LightAttack03 | frames 19–23 de 37 | 0,5135–0,6216 |
| HeavyAttack | frames 12–17 de 34 | 0,3529–0,5000 |

Esses intervalos são ajustáveis na configuração e dependem de revisão visual. MeleeHitbox continua abrindo/fechando na janela curta, deduplicando alvos por swing e respeitando obstrução. Nenhuma hitbox fica ligada durante o clip inteiro.

Dodge mantém **direção, distância 2,5 m, duração 0,25 s, cooldown 0,55 s, início de invulnerabilidade 0,04 s e duração 0,12 s**, sem alteração no bloco de execução M3. Somente o filho visual se orienta para DodgeDirection e retorna à rotação base ao terminar. O movimento interno do FBX é descartado.

HitReaction vem apenas de Health.Damaged após dano aceito; invulnerabilidade não produz esse evento. Em locomoção entra com transição curta e dura 0,22 s, sem alterar HP ou criar stun. Ações comprometidas/traversal têm prioridade: a reação pode aguardar até 0,25 s para tocar quando livre; se a ação durar mais, o pedido expira. Isso evita esconder o swing/efeito enquanto o gameplay ainda o executa.

## Habilidades, mãos e mira

R e F continuam validando início/locks, cobrando energia, emitindo efeitos e controlando cooldown em M8/M9/M10. A animação não chama TrySpend, ApplyDamage, Instantiate de projétil ou um segundo cooldown.

| Habilidade | Gameplay preservado | Pose sincronizada |
|---|---|---|
| ChitinImpact (R) | Custo 35, windup 0,50 s, recovery 0,50 s, cooldown 5 s, dano 35 e raio 3 m | Windup atinge frame 12/51 (0,2353) de ChitingImpact; o Emit original dispara após 0,50 s, na primeira atualização elegível |
| BioelectricStinger (F) | Custo 20, windup 0,25 s, recovery 0,30 s, cooldown 2,5 s a partir do disparo, dano 25 | Windup atinge frame 12/41 (0,2927), próximo da menor separação entre mãos medida; Fire original ocorre no LateUpdate elegível após 0,25 s |

A recuperação visual segue RecoveryProgress do sistema correspondente. Cancelamento não restitui custos/cooldowns; pedidos inválidos continuam sem consumir. Impacto conserva RadialDamage e sua obstrução. Ferrão conserva BioelectricProjectile, velocidade 18 m/s, lifetime 3 s e raio 0,1 m.

`BioelectricCastOrigin` obtém **HumanBodyBones.LeftHand** e **HumanBodyBones.RightHand** do Animator da T-Pose (nomes de fonte mixamorig:LeftHand/RightHand). No instante do tiro, depois da avaliação do Animator e da rotação de mira M9:

`CastOrigin = (LeftHand.position + RightHand.position) / 2 + Player.forward × 0,12 m`

Os Transforms são guardados uma vez; as posições mundiais são lidas naquele frame, sem cache de posição antiga. OnAnimatorMove já aplicou os offsets visuais antes de M9.LateUpdate. O ring provisório também usa CastPosition.

Sem as duas mãos válidas, o provider retorna false; M9 usa o FirePoint original/configurável. Há warning somente de desenvolvimento, uma vez por instância. **A mira continua camera-driven**: camera center→raycast→AimPoint, então CastOrigin→AimPoint. MuzzleClear continua verificando o segmento do centro do corpo até a origem; uma mão além de uma parede não permite disparar através dela.

## Morte, respawn e load

Health.Died/M13 permanecem responsáveis por morte, inputs e posicionamento seguro. Death é Full Body, sem loop e sem deslocamento da raiz. Com respawnDelay padrão de 2 s, a pose percorre o clip em aproximadamente **1,7 s** e mantém a pose final pelos 0,3 s restantes. O delay físico não foi alterado. O feedback DERROTADO do M13 permanece; nenhum VFX/HUD/fade final foi acrescentado.

PlayerRespawn.Respawned é usado tanto no respawn normal quanto no caminho de restauração M14. A bridge faz Rebind, zera parâmetros de ações/morte/traversal, restaura offsets/transform do modelo, limpa reação pendente, reinicia alternância Heavy, volta a Locomotion e avalia o Animator em tempo zero. Uma curta supressão de landing evita tratar o teleporte do load como queda.

Saúde/energia, cooldowns, inputs, checkpoint, busca de local seguro e câmera continuam sendo restaurados pelo coordenador original. Não há estado de Animator no JSON; SaveManager/SaveGameData/SaveFileStore não foram modificados.

## Verificação e limites

Verificado neste ambiente:

- RAR extraído sem erro; 20 entradas inventariadas; SHA-256 dos 19 binários copiados coincide com a extração. Nenhum FBX foi editado.
- Leitura de FBX 7700 para modelo/17 clips e FBX 7400 para o extra estático; geometria, ossos/hierarquia, takes, frames e curvas de translação inspecionados.
- Análise sintática dos 16 arquivos C# adicionados/alterados e revisão das APIs de importação/Motion Time usadas. Isso não é compilação Unity.
- GUIDs, alvos `.meta`, referências adicionadas à cena e preservação dos blocos anteriores conferidos. A cena difere apenas pela referência/componente PlayerVisualInstaller.
- Bloco de execução do Dodge, cálculos de dano/mira/obstrução, IDs persistentes, inimigos, destrutíveis, puzzles, save e ProjectSettings preservados conforme diff.

Testes novos: `PlayerAnimationBridgeTests` cobre origem atual das mãos após rotação, obstrução entre corpo e origem, fallback sem mãos com warning único e limpeza de Animator/offsets no respawn usando o prefab real. **Não executados aqui.** O teste com prefab informa Ignore se o preview ainda não foi preparado; isso não é aprovação do milestone.

| Área solicitada | Evidência atual | Confirmação de funcionamento local |
|---|---|---|
| Avatar, textura, scale/grounding | Preparador com gates reais implementado | Pendente |
| Idle/Walk/Run/Sprint/crouch | Bridge + Blend Tree/fallback implementados | Pendente |
| Jump/Floating/Fall/Climb/Mantle | Fases físicas/offsets/import settings implementados | Pendente |
| Lights/Heavy/mirror/dodge/hit/death | Integração e reset implementados | Pendente |
| R/F, mãos, energia/cooldown | Sistemas originais reutilizados; origem estendida | Pendente |
| Melee | EnemyBrain/EnemyAttack/Health e hitbox existentes preservados | Pendente |
| Ranged | RangedBrain/RangedWeapon/projéteis existentes preservados | Pendente |
| Drone | DroneBrain/DroneFlight e filtros M9 existentes preservados | Pendente |
| Destrutíveis M11 | DestructibleObject/Health existentes preservados | Pendente |
| BioelectricReceiver/puzzles M12 | Receiver, projectile e puzzle/door/barrier existentes preservados | Pendente |
| Checkpoint/respawn M13 | Mecânica existente preservada; evento limpa visual | Pendente |
| Save/Load M14 | Código/formato/IDs existentes preservados | Pendente |
| Preservação completa M1–M14 | Mudanças limitadas e diff revisado | **Regressão em PlayMode ainda necessária** |

Limitações visuais conhecidas: crouch comprimido; clips longos adaptados às janelas curtas M3/M1, especialmente Light2/3 e Mantle; direção do dodge representada por um único clip frontal; mão/pé de contato e offsets ainda sem aprovação visual; nenhuma IK de pés/mãos; HitReaction pode expirar durante ações longas. A orientação inicial do modelo e o ajuste dos pés precisam de inspeção no Editor, não foram afirmados como renderizados aqui.

## Roteiro de validação local

1. Verifique o relatório de importação e o Console. Confirme um Animator, um skeleton visual, mesh da T-Pose, Texture.png e placeholder oculto; CharacterController continua habilitado.
2. Idle→Alt/Walk→Run→Shift/Sprint→Run→Walk→Idle. Verifique pés, rotação, sliding e crouch sob o teto baixo.
3. Pule parado e correndo; caia de plataforma. Takeoff não deve voltar a Idle no ar; Floating persiste até contato real; Fall representa apenas pouso. Confira ausência de subida dupla.
4. Escale, pare, desça, solte, faça vários mantles seguidos e cancele um. Compare posição local do PlayerVisualRoot antes/depois; ela deve voltar exatamente à base.
5. Faça Light1/2/3 parado e **saindo de Run/Sprint**; veja commit, direita/esquerda/direita, dano único e recuperação. Faça Heavy repetido saindo de Run, confira alternância do chute e janela curta.
6. Q em frente/lados/trás: distância/duração/invulnerabilidade originais e sem root motion adicional. Receba dano real correndo; durante invulnerabilidade não deve surgir HitReaction.
7. R: pico→onda→dano; F: mãos próximas→projétil entre elas→alvo do centro da câmera. Confira custo único/cooldown, paredes junto às mãos e tiros elevados contra drone.
8. Danifique melee, ranged e drone; quebre caixas/painéis; resolva Puzzle A e ative Nodes A/B com F. Regressão de interação E, portas e cartão.
9. Morra, veja Death e feedback, respawne. Confirme posição/modelo, offsets, parâmetros limpos e funcionamento de Q/R/F/combate.
10. Salve progresso, saia e reentre no Play. Confira checkpoint, credencial, destrutíveis, progresso parcial/completo dos receptores e Animator limpo. Use o fluxo de limpar save documentado em M14 somente se quiser reiniciar o teste.
11. Test Runner → PlayMode: `PlayerAnimationBridgeTests` e as suítes existentes, especialmente CombatIntegrationTests, BioelectricStingerTests, ChitinImpactTests, DestructibleTests, EnvironmentalPuzzleTests, RespawnTests e SaveLoadTests.

Após sua validação, esta etapa visual poderá ser aprovada como M14.5. A implementação para aqui: **sem merge na main e sem iniciar M15**.
