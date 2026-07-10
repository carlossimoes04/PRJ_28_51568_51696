Este diretório contém os scripts associados ao teclado numérico da porta. 
Os scripts originais pertencem ao seguinte asset da Unity Asset Store:
- Asset: "Keypad - Free" (da autoria de NavKeypad)
- Link: https://assetstore.unity.com/packages/3d/props/electronics/keypad-free-262151

1. Scripts Originais (sem alterações)

Os seguintes scripts foram importados do asset e mantidos no seu estado
original, servindo apenas de suporte físico e de interação na cena:
- KeypadButton.cs: Controla a física individual das teclas do teclado.
- KeypadInteractionFPV.cs: Auxiliar de colisão/interação de primeiro plano.

------------------------------------------------------------------------
2. Scripts Modificados (pelo grupo)
------------------------------------------------------------------------
Os seguintes scripts contêm o teu trabalho de desenvolvimento e integração
com a lógica do jogo:

- SlidingDoor.cs (desenvolvido pelo grupo)
  * Script próprio criado para controlar a abertura física e a animação
    da porta de correr quando o acesso é concedido.

- Keypad.cs (modificado pelo grupo)
  * Modificado para integrar o teclado com o fluxo geral da Escape Room:
    * Adicionada a flag booleana 'useEscapeRoomManagerCode'.
    * Adicionado suporte para localizar o 'EscapeRoomManager' na cena.
    * A validação da combinação passou a ser dinâmica: em vez de verificar
      o código fixo do inspetor, valida os dígitos inseridos contra o 
      código gerado aleatoriamente no 'EscapeRoomManager.finalCode'.