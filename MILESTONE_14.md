# M14 — Save/Load (um slot)

Abra `Assets/Scenes/Nave_TestGym.unity`. `M14 - Save Manager` carrega automaticamente no Start, após os spawners e a inicialização de portas/puzzles/M13. Sem arquivo válido, a cena mantém seus padrões. Não há teclas F5/F9 nem menu.

Arquivo: `Path.Combine(Application.persistentDataPath, "superinseto_save.json")`. Com as configurações atuais, em Windows fica em `%USERPROFILE%\AppData\LocalLow\DefaultCompany\SuperInseto\`; o caminho efetivo é a propriedade `SaveManager.SavePath`. O caminho não depende da pasta Assets/repositório.

JSON versão 1:
- `checkpointId`: `checkpoint_start` ou `checkpoint_02`;
- `hasAccessCard`: credencial `SecurityLevel01`;
- `destroyedObjectIds`: `m11_crate_a`, `m11_crate_b`, `m11_panel`, `m12_sabotage_panel` quando destruídos;
- `poweredReceiverIds`: `m12_node_a`, `m12_node_b` quando ligados.

IDs são serializados: checkpoints no próprio componente; instâncias M11/M12 recebem `PersistentId` dos IDs configurados nos respectivos spawners. Nenhum ID é gerado aleatoriamente. Ao adicionar outro objeto persistível, use um ID único na instância, com DestructibleObject ou BioelectricReceiver. Não coloque o mesmo ID em um prefab reutilizado várias vezes.

Autosave: novo checkpoint aceito, credencial adquirida, objeto persistível destruído ou receptor ligado. Eventos próximos são agrupados em uma escrita após 0,15 s; pausa/saída do Play descarregam uma escrita pendente. Não salva por dano parcial nem por frame. O arquivo `.tmp` é gravado e sincronizado antes de rename/substituição; se a substituição falhar, não apaga o save anterior. Há mensagens temporárias de salvo/carregado/falha.

O load aplica destruição pelo Health existente e energização pelo receptor existente. As condições do M12 derivam `IsSolved`, barreira/colisão e abertura da SlidingDoor (com seu deslocamento normal). 1/2 receptores permanece 1/2. A credencial é restaurada e o cartão já coletado é ocultado. Então M13 recebe o checkpoint carregado e reutiliza busca de posição segura, limpeza de ações, restauração de vida/energia, cooldowns R/F e câmera. Morrer depois continua retornando ao checkpoint carregado.

**Não salvo:** posição livre, HP parcial, energia atual, cooldowns, ações temporárias, câmera, projéteis, inimigos/aggro/mortes, posição das portas ou estado dos botões M2. Inimigos reiniciam com a cena. Portas de credencial usam o cartão salvo; portas M12 derivam dos receptores/painel. Puzzles resolvidos não possuem flag redundante no JSON.

**Limpar:** fora do Play, selecione `M14 - Save Manager`, abra o menu de contexto do componente SaveManager e use `M14/Clear Save (disk only)`. Inicie Play novamente. Também existe `ClearSave()` público. Se limpar durante Play, não reseta o mundo; o próximo novo evento de progresso pode criar outro save.

**Falhas:** JSON vazio/inválido, campos essenciais ausentes ou versão diferente de 1 geram warning e defaults. A inicialização não sobrescreve esse arquivo; um evento posterior de progresso pode gravar novo save. Checkpoint removido usa o inicial, preservando os demais estados conhecidos. IDs ausentes são ignorados. IDs vazios/duplicados na cena desabilitam load/save daquela sessão com warning, sem escolher um objeto arbitrariamente.

## Testar localmente

1. Limpe o save fora do Play. Inicie: checkpoint inicial, objetos intactos, nodes OFF.
2. Passe pelo checkpoint 2 em `(-10,0,12)` e confirme PROGRESSO SALVO. Saia/reabra: deve começar nele, vida/energia máximas, R/F prontos.
3. Destrua uma caixa M11 e o painel de sabotagem M12; ative somente Node A com F, colete o cartão. Saia/reabra: caixa/painel destruídos, colliders desativados, barreira desligada, A ON/B OFF, porta ainda bloqueada, credencial mantida.
4. Ative B e reabra: ambos ON e SlidingDoor abrindo/aberta. Energia/cooldown de F continuam normais durante gameplay.
5. Após load no checkpoint 2, morra na arena: confirme retorno ao mesmo checkpoint. Regresse movimentação, interação, combate e M1–M13.
6. Com Play parado, substitua o conteúdo do JSON por `{broken` e inicie: warning/defaults, sem crash. Limpe depois.
7. Execute `SaveLoadTests` na aba PlayMode do Test Runner: seis testes de arquivo/versão, sessões novas, progresso parcial/completo, respawn, IDs ausentes/duplicados. Usam diretório temporário isolado, nunca o save real.

Validação neste ambiente: revisão estática de C#, YAML/GUIDs/referências, IDs e preservação das áreas/scripts anteriores. Sem Unity Editor/compilador C#: testes automatizados preparados, mas não executados. Persistência real entre Plays, substituição de arquivo no Windows, física/câmera/IA e regressões precisam de validação local. Escopo: uma cena/um slot, sem migração, save de encounters ou UI final.
