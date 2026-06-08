using System.Numerics;
using Raylib_cs;
using CardGamePrototype.Core;

namespace CardGamePrototype.Client
{
    class Program
    {
        static void Main()
        {
            var service = new BattleService();
            service.NewBattle();

            Raylib.InitWindow(1600, 900, "Catalyst Prototype");
            Raylib.SetWindowState(ConfigFlags.ResizableWindow);
            Raylib.SetTargetFPS(60);

            int draggingCardIndex = -1;
            bool dragging = false;

            while (!Raylib.WindowShouldClose())
            {
                var state = service.State;

                if (Raylib.IsKeyPressed(KeyboardKey.R))
                    service.Restart();

                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    service.EndTurn();

                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    if (state.SelectedBoardSlot >= 0)
                    {
                        service.ExecuteCard(state.SelectedBoardSlot);
                        state.SelectedBoardSlot = -1;
                    }
                }

                int width = Raylib.GetScreenWidth();
                int height = Raylib.GetScreenHeight();

                float boardY = height * 0.42f;
                float handY = height * 0.75f;

                const int slotWidth = 180;
                const int slotHeight = 130;

                const int cardWidth = 170;
                const int cardHeight = 110;

                Vector2 mouse = Raylib.GetMousePosition();

                int boardStart =
                    width / 2 -
                    ((slotWidth * 5) + (20 * 4)) / 2;

                // START DRAG

                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    for (int i = 0; i < state.Hand.Count; i++)
                    {
                        int x = 40 + i * (cardWidth + 15);

                        Rectangle cardRect =
                            new Rectangle(
                                x,
                                handY,
                                cardWidth,
                                cardHeight);

                        if (Raylib.CheckCollisionPointRec(mouse, cardRect))
                        {
                            dragging = true;
                            draggingCardIndex = i;
                            break;
                        }
                    }

                    if (!dragging)
                    {
                        for (int i = 0; i < state.PlayerBoard.Count; i++)
                        {
                            int x =
                                boardStart +
                                i * (slotWidth + 20);

                            Rectangle slotRect =
                                new Rectangle(
                                    x,
                                    boardY,
                                    slotWidth,
                                    slotHeight);

                            if (!Raylib.CheckCollisionPointRec(mouse, slotRect))
                                continue;

                            if (state.PlayerBoard[i].IsOccupied)
                            {
                                state.SelectedBoardSlot = i;
                            }
                        }
                    }
                }

                // DROP

                if (dragging &&
                    Raylib.IsMouseButtonReleased(MouseButton.Left))
                {
                    bool placed = false;

                    for (int i = 0; i < state.PlayerBoard.Count; i++)
                    {
                        int x =
                            boardStart +
                            i * (slotWidth + 20);

                        Rectangle slotRect =
                            new Rectangle(
                                x,
                                boardY,
                                slotWidth,
                                slotHeight);

                        if (!Raylib.CheckCollisionPointRec(mouse, slotRect))
                            continue;

                        placed =
                            service.PlaceCard(
                                draggingCardIndex,
                                i);

                        break;
                    }

                    dragging = false;
                    draggingCardIndex = -1;
                }

                Raylib.BeginDrawing();

                Raylib.ClearBackground(Color.DarkGray);

                // PLAYER

                Raylib.DrawText(
                    $"Player HP: {state.Player.Hp}/{state.Player.MaxHp}",
                    20,
                    20,
                    28,
                    Color.White);

                Raylib.DrawText(
                    $"Energy: {state.Player.Energy}",
                    20,
                    55,
                    24,
                    Color.Gold);

                Raylib.DrawText(
                    $"Burned: {state.BurnPile.Count}",
                    20,
                    90,
                    24,
                    Color.Orange);

                // ENEMY

                Raylib.DrawText(
                    $"Enemy HP: {state.Enemy.Hp}/{state.Enemy.MaxHp}",
                    width - 350,
                    20,
                    28,
                    Color.White);

                Raylib.DrawText(
                    $"Pos: {state.Enemy.Position}",
                    width - 350,
                    55,
                    22,
                    Color.White);

                Raylib.DrawText(
                    $"Fire {state.Enemy.ActiveElements.GetStacks(ElementType.Fire)}",
                    width - 350,
                    90,
                    22,
                    Color.Orange);

                Raylib.DrawText(
                    $"Frost {state.Enemy.ActiveElements.GetStacks(ElementType.Frost)}",
                    width - 350,
                    120,
                    22,
                    Color.SkyBlue);

                Raylib.DrawText(
                    $"Bio {state.Enemy.ActiveElements.GetStacks(ElementType.Bio)}",
                    width - 350,
                    150,
                    22,
                    Color.Lime);

                // ENEMY LANES

                int laneStart = width / 2 - 300;

                for (int i = 0; i < 5; i++)
                {
                    int x = laneStart + i * 120;

                    Raylib.DrawRectangleLines(
                        x,
                        180,
                        100,
                        80,
                        Color.LightGray);

                    Raylib.DrawText(
                        i.ToString(),
                        x + 10,
                        190,
                        20,
                        Color.White);
                }

                int enemyX =
                    laneStart +
                    state.Enemy.Position * 120 +
                    40;

                Raylib.DrawText(
                    "E",
                    enemyX,
                    210,
                    40,
                    Color.Red);

                // BOARD

                Raylib.DrawText(
                    "CATALYST BOARD",
                    boardStart,
                    (int)boardY - 40,
                    24,
                    Color.White);

                for (int i = 0; i < state.PlayerBoard.Count; i++)
                {
                    int x =
                        boardStart +
                        i * (slotWidth + 20);

                    Rectangle slotRect =
                        new Rectangle(
                            x,
                            boardY,
                            slotWidth,
                            slotHeight);

                    bool hover =
                        Raylib.CheckCollisionPointRec(
                            mouse,
                            slotRect);

                    Color border =
                        state.SelectedBoardSlot == i
                            ? Color.Green
                            : hover
                                ? Color.Yellow
                                : Color.LightGray;

                    Raylib.DrawRectangleLinesEx(
                        slotRect,
                        4,
                        border);

                    var slot =
                        state.PlayerBoard[i];

                    if (slot.IsOccupied)
                    {
                        Raylib.DrawText(
                            slot.Card!.Name,
                            x + 10,
                            (int)boardY + 10,
                            20,
                            Color.White);

                        Raylib.DrawText(
                            $"Turns {slot.TurnsOnBoard}",
                            x + 10,
                            (int)boardY + 40,
                            16,
                            Color.LightGray);
                    }
                }

                // HAND

                Raylib.DrawText(
                    "HAND",
                    40,
                    (int)handY - 40,
                    24,
                    Color.White);

                for (int i = 0; i < state.Hand.Count; i++)
                {
                    if (dragging && i == draggingCardIndex)
                        continue;

                    int x =
                        40 +
                        i * (cardWidth + 15);

                    Raylib.DrawRectangle(
                        x,
                        (int)handY,
                        cardWidth,
                        cardHeight,
                        Color.RayWhite);

                    Raylib.DrawRectangleLines(
                        x,
                        (int)handY,
                        cardWidth,
                        cardHeight,
                        Color.Black);

                    var card = state.Hand[i];

                    Raylib.DrawText(
                        card.Name,
                        x + 10,
                        (int)handY + 10,
                        20,
                        Color.Black);

                    Raylib.DrawText(
                        $"Cost {card.Cost}",
                        x + 10,
                        (int)handY + 40,
                        18,
                        Color.DarkGray);
                }

                // DRAGGING GHOST CARD

                if (dragging &&
                    draggingCardIndex >= 0 &&
                    draggingCardIndex < state.Hand.Count)
                {
                    var card =
                        state.Hand[draggingCardIndex];

                    int ghostX =
                        (int)mouse.X - cardWidth / 2;

                    int ghostY =
                        (int)mouse.Y - cardHeight / 2;

                    Raylib.DrawRectangle(
                        ghostX,
                        ghostY,
                        cardWidth,
                        cardHeight,
                        Color.Beige);

                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(
                            ghostX,
                            ghostY,
                            cardWidth,
                            cardHeight),
                        4,
                        Color.Gold);

                    Raylib.DrawText(
                        card.Name,
                        ghostX + 10,
                        ghostY + 10,
                        20,
                        Color.Black);
                }

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}