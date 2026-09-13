# Player do M14.5

Fontes oficiais em Model/ e Animations/: 18 FBXs + Texture.png, preservados sem reexportação. Player.fbx foi inspecionado e excluído por ser uma malha estática extra. SourceInventory.json registra o inventário completo do anexo e hashes.

No Unity 6000.3.24f1, aguarde a preparação automática ou execute **Super Inseto → M14.5 → Build or Rebuild Player Preview**. O Editor verifica o Humanoid antes de criar controller, material, prefab e configuração Resources. Os metadados iniciais dos binários fixam seus GUIDs; as opções completas de importação são serializadas pelo preparador local.

O runtime mantém o Player/CharacterController e o VisualRoot existentes. A representação real só substitui os renderers do placeholder depois que o preview estiver válido. Não copie o modelo por cima do Player da cena.

Relatório completo, ajustes e validação: `MILESTONE_14_5.md`, na raiz do projeto. O milestone aguarda validação local; nenhum teste visual foi declarado executado neste ambiente.
