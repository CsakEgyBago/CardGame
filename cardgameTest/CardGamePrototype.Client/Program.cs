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
                        service.ExecuteCard(
                            state.SelectedBoardSlot);

                        state.SelectedBoardSlot = -1;
                    }
                }

                int width = Raylib.GetScreenWidth();
                int height = Raylib.GetScreenHeight();

                float boardY = height * 0.40f;
                float handY = height * 0.72f;

                const int slotWidth = 220;
                const int slotHeight = 140;

                const int cardWidth = 180;
                const int cardHeight = 120;

                Vector2 mouse = Raylib.GetMousePosition();

                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    bool handled = false;

                    // HAND SELECTION

                    for (int i = 0; i < state.Hand.Count; i++)
                    {
                        int x = 40 + i * (cardWidth + 20);

                        Rectangle rect =
                            new Rectangle(
                                x,
                                handY,
                                cardWidth,
                                cardHeight);

                        if (Raylib.CheckCollisionPointRec(mouse, rect))
                        {
                            state.SelectedHandCard = i;
                            handled = true;
                            break;
                        }
                    }

                    // BOARD SLOT INTERACTION

                    if (!handled)
                    {
                        int startX =
                            width / 2 -
                            ((slotWidth * 3) + 40) / 2;

                        for (int i = 0; i < 3; i++)
                        {
                            int x =
                                startX +
                                i * (slotWidth + 20);

                            Rectangle slotRect =
                                new Rectangle(
                                    x,
                                    boardY,
                                    slotWidth,
                                    slotHeight);

                            if (!Raylib.CheckCollisionPointRec(
                                mouse,
                                slotRect))
                                continue;

                            // place card

                            if (state.SelectedHandCard >= 0)
                            {
                                bool placed =
                                    service.PlaceCard(
                                        state.SelectedHandCard,
                                        i);

                                if (placed)
                                {
                                    state.SelectedHandCard = -1;
                                }

                                handled = true;
                                break;
                            }

                            // select board card

                            if (state.PlayerBoard[i].IsOccupied)
                            {
                                state.SelectedBoardSlot = i;
                            }
                            else
                            {
                                state.SelectedBoardSlot = -1;
                            }

                            handled = true;
                            break;
                        }
                    }
                }

                Raylib.BeginDrawing();

                Raylib.ClearBackground(Color.DarkGray);

                // =====================
                // PLAYER INFO
                // =====================

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

                // =====================
                // ENEMY INFO
                // =====================

                Raylib.DrawText(
                    $"Enemy HP: {state.Enemy.Hp}/{state.Enemy.MaxHp}",
                    width - 350,
                    20,
                    28,
                    Color.White);

                Raylib.DrawText(
                    $"Position: {state.Enemy.Position}",
                    width - 350,
                    55,
                    24,
                    Color.White);

                Raylib.DrawText(
                    $"Fire: {state.Enemy.ActiveElements.GetStacks(ElementType.Fire)}",
                    width - 350,
                    90,
                    22,
                    Color.Orange);

                Raylib.DrawText(
                    $"Frost: {state.Enemy.ActiveElements.GetStacks(ElementType.Frost)}",
                    width - 350,
                    120,
                    22,
                    Color.SkyBlue);

                Raylib.DrawText(
                    $"Bio: {state.Enemy.ActiveElements.GetStacks(ElementType.Bio)}",
                    width - 350,
                    150,
                    22,
                    Color.Lime);

                // =====================
                // ENEMY LANE
                // =====================

                int laneStart =
                    width / 2 - 300;

                for (int i = 0; i < 5; i++)
                {
                    int x =
                        laneStart +
                        i * 120;

                    Raylib.DrawRectangleLines(
                        x,
                        180,
                        100,
                        80,
                        Color.LightGray);

                    Raylib.DrawText(
                        i.ToString(),
                        x + 8,
                        188,
                        20,
                        Color.LightGray);
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

                // =====================
                // BOARD
                // =====================

                Raylib.DrawText(
                    "BOARD",
                    width / 2 - 60,
                    (int)boardY - 40,
                    24,
                    Color.White);

                int boardStart =
                    width / 2 -
                    ((slotWidth * 3) + 40) / 2;

                for (int i = 0; i < 3; i++)
                {
                    int x =
                        boardStart +
                        i * (slotWidth + 20);

                    Color outline =
                        state.SelectedBoardSlot == i
                            ? Color.Green
                            : Color.LightGray;

                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(
                            x,
                            boardY,
                            slotWidth,
                            slotHeight),
                        3,
                        outline);

                    var slot =
                        state.PlayerBoard[i];

                    if (slot.IsOccupied)
                    {
                        var card =
                            slot.Card!;

                        Raylib.DrawText(
                            card.Name,
                            x + 10,
                            (int)boardY + 10,
                            22,
                            Color.White);

                        Raylib.DrawText(
                            $"Age: {slot.TurnsOnBoard}",
                            x + 10,
                            (int)boardY + 45,
                            18,
                            Color.LightGray);
                    }
                }

                // =====================
                // HAND
                // =====================

                Raylib.DrawText(
                    "HAND",
                    40,
                    (int)handY - 40,
                    24,
                    Color.White);

                for (int i = 0; i < state.Hand.Count; i++)
                {
                    int x =
                        40 +
                        i * (cardWidth + 20);

                    Color border =
                        state.SelectedHandCard == i
                            ? Color.Gold
                            : Color.Black;

                    Raylib.DrawRectangle(
                        x,
                        (int)handY,
                        cardWidth,
                        cardHeight,
                        Color.RayWhite);

                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(
                            x,
                            handY,
                            cardWidth,
                            cardHeight),
                        4,
                        border);

                    var card =
                        state.Hand[i];

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

                    Raylib.DrawText(
                        card.CardType.ToString(),
                        x + 10,
                        (int)handY + 70,
                        18,
                        Color.DarkGray);
                }

                // =====================
                // BURN PILE
                // =====================

                Raylib.DrawText(
                    $"Burned: {state.BurnPile.Count}",
                    20,
                    height - 40,
                    22,
                    Color.Orange);

                // =====================
                // STATUS
                // =====================

                if (state.Phase == TurnPhase.Finished)
                {
                    string msg =
                        state.Player.IsDead
                            ? "Defeat"
                            : "Victory";

                    Raylib.DrawText(
                        msg,
                        width / 2 - 80,
                        40,
                        40,
                        Color.Yellow);
                }

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}