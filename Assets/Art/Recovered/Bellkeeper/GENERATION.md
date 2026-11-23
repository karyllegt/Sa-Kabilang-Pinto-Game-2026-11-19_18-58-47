# Bellkeeper atlas

Generated using the built-in ImageGen tool on 2026-11-23. Source output copied into this project as `BellkeeperAtlas.png`. Do not credit this as Mary's hand-drawn work.

Final prompt: Production 2D pixel-art sprite atlas on a transparent background, equal 2x2 cells, same full-body Filipino folklore Bellkeeper in four poses: idle, stunned, toll and defeated. Bronze church-bell body, solemn carved stone human face, heavy rope arms, chain tassels, rusted crown hanger; aged bronze, purple shadows, teal eyes and cream highlights. Detailed crisp pixel clusters and ink outline, no UI or text. Identical scale and bottom baseline, separated transparent cells. Red danger energy only on the toll clapper. Target about 96x112 meaningful pixel detail per pose. Preserve character identity, avoid extra creatures and effects crossing cells.

The first three cells are referenced by the existing boss state machine through persistent Sprite assets with a 3.5-unit canvas height. The fourth defeated cell is retained as source art; current death behavior fades the body through the existing BossView logic. Gameplay-scale review is required; generated art alone is not acceptance.
