# M10 — Energia Quitinosa

Abra `Assets/Scenes/Nave_TestGym.unity`. O Player possui `ChitinEnergy` e um texto provisório na Game View: **ENERGIA atual/máxima**, com aviso breve quando falta energia.

Valores no Inspector: máxima/inicial **100**, regeneração **15/s**, delay após gasto **1,5 s**. `energyCost`: **35 em ChitinImpact (R)** e **20 em BioelectricStinger (F)**. Cooldowns continuam **5 s para R** e **2,5 s a partir do disparo para F**.

Só uma ativação válida consome, antes do windup. Estado inválido, ação incompatível, cooldown ou energia insuficiente não consomem; falta de energia não inicia ação/cooldown. Animation Events não gastam novamente. Cancelamento, morte ou FirePoint bloqueado depois de iniciar não devolvem o gasto. O delay reinicia somente após consumo efetivo; morte interrompe regeneração. Desabilitar/reabilitar não recarrega energia.

`CurrentEnergy`, `MaxEnergy`, `CanAfford`, `TrySpend`, `SetCurrentEnergy` e eventos `Changed`, `Spent`, `Depleted`, `Full`, `Insufficient` permitem integração futura sem save/upgrade implementado. `currentEnergy` aparece no Inspector em Play. Movimento, combate comum, Q e E não usam energia.

## Teste local

1. Play: confira **100/100**. R deve cair imediatamente para **65**; após recovery, F deve cair para **45** (antes da regeneração). Mire no Dummy ou nos inimigos da arena M7 e confira que dano/projéteis continuam funcionando.
2. A regeneração altera os números entre usos. Para verificar exatamente **100→80→60→40→20→0**, coloque temporariamente `regenerationRate = 0` e `currentEnergy = 100` no Inspector durante Play. Use F respeitando cooldown. Em zero, F/R não executam nem iniciam cooldown; aparece “Energia insuficiente”.
3. Ainda sem regeneração, teste **R→F→F = 25**; espere os cooldowns. R deve falhar e F deve funcionar, deixando **5**. Tentativas durante Q, escalada, mantle, outro poder e cooldown devem manter energia inalterada.
4. Restaure regeneração **15/s**: após um gasto, espere **1,5 s**, observe recuperação gradual até 100. Novo gasto reinicia delay. Morra com energia incompleta: ela deve parar de regenerar.
5. Teste combo, pesado, Q, movimento, portas/puzzle, R/F, Dummy e os três inimigos na arena. Eles mantêm as regras anteriores. As mudanças temporárias do teste não precisam ser salvas na cena.

Test Runner → PlayMode → `ChitinEnergyTests`: 5 testes focados em consumo, limites, delay, morte, bloqueios e efeitos/eventos únicos. Os testes existentes de M8/M9 continuam presentes; o teste M9 de exclusão de ações repõe energia explicitamente para isolar seu objetivo.

Validação no ambiente: revisão estática de C#, YAML, GUIDs, referências e preservação dos arquivos aprovados. **Unity Editor e compilador C# indisponíveis**: testes PlayMode, compilação e regressão jogável precisam ser executados localmente. Sem HUD final, pickups, stamina, upgrades ou save.
