using System.Numerics;
using Raylib_cs;
using CardGamePrototype.Core;

namespace CardGamePrototype.Client
{
    class Program
    {
        static void DrawCard(
            Rectangle rect,
            string title,
            int cost,
            Color bodyColor,
            Color borderColor,
            bool selected = false)
        {
            Raylib.DrawRectangleRounded(rect, 0.08f, 8, bodyColor);

            Raylib.DrawRectangleRoundedLinesEx(
                rect,
                0.08f,
                8,
                selected ? 4 : 2,
                borderColor);

            Raylib.DrawRectangle(
                (int)rect.X,
                (int)rect.Y,
                (int)rect.Width,
                34,
                Color.DarkBlue);

            Raylib.DrawText(
                title,
                (int)rect.X + 10,
                (int)rect.Y + 8,
                20,
                Color.White);

            Raylib.DrawCircle(
                (int)(rect.X + rect.Width - 22),
                (int)(rect.Y + 18),
                14,
                Color.Gold);

            Raylib.DrawText(
                cost.ToString(),
                (int)(rect.X + rect.Width - 28),
                (int)(rect.Y + 8),
                18,
                Color.Black);
        }

        static void Main()
        {
            var service = new BattleService();
            service.NewBattle();

            Raylib.InitWindow(1600, 900, "Catalyst Prototype");
            Raylib.SetWindowState(ConfigFlags.ResizableWindow);
            Raylib.SetTargetFPS(60);

            int draggingCardIndex = -1;
            bool dragging = false;

            bool showDebug = false;

            Vector2 dragOffset = Vector2.Zero;

            string hoveredElement = "None";
            Rectangle hoveredElementRect = new();

            while (!Raylib.WindowShouldClose())
            {
                var state = service.State;

                if (Raylib.IsKeyPressed(KeyboardKey.F3))
                    showDebug = !showDebug;

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

                Vector2 mouse = Raylib.GetMousePosition();

                hoveredElement = "None";
                hoveredElementRect = new Rectangle();

                float boardY = height * 0.46f;
                float handY = height * 0.77f;

                const int slotWidth = 180;
                const int slotHeight = 150;

                const int cardWidth = 180;
                const int cardHeight = 240;

                int boardStart =
                    width / 2 -
                    ((slotWidth * 5) + (20 * 4)) / 2;

                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    for (int i = 0; i < state.Hand.Count; i++)
                    {
                        int x =
                            40 +
                            i * (cardWidth + 20);

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

                            dragOffset =
                                mouse -
                                new Vector2(cardRect.X, cardRect.Y);

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

                            if (Raylib.CheckCollisionPointRec(mouse, slotRect))
                            {
                                if (state.PlayerBoard[i].IsOccupied)
                                {
                                    state.SelectedBoardSlot = i;
                                }
                            }
                        }
                    }
                }

                if (dragging &&
                    Raylib.IsMouseButtonReleased(MouseButton.Left))
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

                        if (Raylib.CheckCollisionPointRec(mouse, slotRect))
                        {
                            service.PlaceCard(
                                draggingCardIndex,
                                i);

                            break;
                        }
                    }

                    dragging = false;
                    draggingCardIndex = -1;
                }

                Raylib.BeginDrawing();

                Raylib.ClearBackground(
                    new Color(
                        42,
                        45,
                        52,
                        255));

                // ====================================================
                // PLAYER PANEL
                // ====================================================

                Raylib.DrawRectangle(
                    12,
                    12,
                    280,
                    120,
                    new Color(25, 25, 30, 220));

                Raylib.DrawText(
                    $"PLAYER HP {state.Player.Hp}/{state.Player.MaxHp}",
                    24,
                    24,
                    28,
                    Color.White);

                Raylib.DrawText(
                    $"Energy: {state.Player.Energy}",
                    24,
                    62,
                    22,
                    Color.Gold);

                Raylib.DrawText(
                    $"Burn Pile: {state.BurnPile.Count}",
                    24,
                    92,
                    20,
                    Color.Orange);

                // ====================================================
                // ENEMY PANEL
                // ====================================================

                int enemyPanelX = width - 340;

                Raylib.DrawRectangle(
                    enemyPanelX,
                    12,
                    320,
                    180,
                    new Color(25, 25, 30, 220));

                Raylib.DrawText(
                    "ENEMY",
                    enemyPanelX + 20,
                    22,
                    28,
                    Color.Red);

                Raylib.DrawText(
                    $"HP {state.Enemy.Hp}/{state.Enemy.MaxHp}",
                    enemyPanelX + 20,
                    58,
                    24,
                    Color.White);

                Raylib.DrawText(
                    $"Lane Position: {state.Enemy.Position}",
                    enemyPanelX + 20,
                    88,
                    20,
                    Color.LightGray);

                Raylib.DrawText(
                    $"Fire: {state.Enemy.ActiveElements.GetStacks(ElementType.Fire)}",
                    enemyPanelX + 20,
                    118,
                    20,
                    Color.Orange);

                Raylib.DrawText(
                    $"Frost: {state.Enemy.ActiveElements.GetStacks(ElementType.Frost)}",
                    enemyPanelX + 20,
                    143,
                    20,
                    Color.SkyBlue);

                Raylib.DrawText(
                    $"Bio: {state.Enemy.ActiveElements.GetStacks(ElementType.Bio)}",
                    enemyPanelX + 20,
                    168,
                    20,
                    Color.Lime);

                // ====================================================
                // ENEMY LANES
                // ====================================================

                Raylib.DrawText(
                    "ENEMY POSITION TRACK",
                    width / 2 - 130,
                    145,
                    22,
                    Color.White);

                int laneStart = width / 2 - 300;

                for (int i = 0; i < 5; i++)
                {
                    int x = laneStart + i * 120;

                    Rectangle laneRect =
                        new Rectangle(
                            x,
                            180,
                            100,
                            90);

                    if (Raylib.CheckCollisionPointRec(mouse, laneRect))
                    {
                        hoveredElement = $"Enemy Lane {i}";
                        hoveredElementRect = laneRect;
                    }

                    Raylib.DrawRectangleRounded(
                        laneRect,
                        0.1f,
                        6,
                        new Color(55, 55, 65, 255));

                    Raylib.DrawRectangleRoundedLinesEx(
                        laneRect,
                        0.1f,
                        6,
                        2,
                        Color.LightGray);

                    Raylib.DrawText(
                        $"Lane {i}",
                        x + 18,
                        214,
                        18,
                        Color.White);
                }

                int enemyX =
                    laneStart +
                    state.Enemy.Position * 120 +
                    16;

                Rectangle enemyRect =
                    new Rectangle(
                        enemyX,
                        190,
                        70,
                        70);

                Raylib.DrawRectangleRounded(
                    enemyRect,
                    0.15f,
                    6,
                    Color.Maroon);

                Raylib.DrawText(
                    "ENEMY",
                    enemyX + 4,
                    218,
                    18,
                    Color.White);

                // ====================================================
                // BOARD
                // ====================================================

                Raylib.DrawText(
                    "CATALYST BOARD",
                    boardStart,
                    (int)boardY - 35,
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

                    if (Raylib.CheckCollisionPointRec(mouse, slotRect))
                    {
                        hoveredElement = $"Board Slot {i}";
                        hoveredElementRect = slotRect;
                    }

                    Color border =
                        state.SelectedBoardSlot == i
                            ? Color.Green
                            : Color.LightGray;

                    Raylib.DrawRectangleRounded(
                        slotRect,
                        0.08f,
                        6,
                        new Color(60, 60, 70, 255));

                    Raylib.DrawRectangleRoundedLinesEx(
                        slotRect,
                        0.08f,
                        6,
                        3,
                        border);

                    var slot =
                        state.PlayerBoard[i];

                    if (slot.IsOccupied)
                    {
                        DrawCard(
                            new Rectangle(
                                x + 6,
                                boardY + 6,
                                slotWidth - 12,
                                slotHeight - 12),
                            slot.Card!.Name,
                            slot.Card.Cost,
                            new Color(80, 80, 90, 255),
                            Color.Gold,
                            state.SelectedBoardSlot == i);

                        Raylib.DrawText(
                            $"Turns: {slot.TurnsOnBoard}",
                            x + 12,
                            (int)boardY + slotHeight - 30,
                            16,
                            Color.White);
                    }
                }

                // ====================================================
                // HAND
                // ====================================================

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
                        i * (cardWidth + 20);

                    Rectangle cardRect =
                        new Rectangle(
                            x,
                            handY,
                            cardWidth,
                            cardHeight);

                    if (Raylib.CheckCollisionPointRec(mouse, cardRect))
                    {
                        hoveredElement =
                            $"Hand Card {i}: {state.Hand[i].Name}";
                        hoveredElementRect = cardRect;
                    }

                    DrawCard(
                        cardRect,
                        state.Hand[i].Name,
                        state.Hand[i].Cost,
                        new Color(220, 220, 225, 255),
                        Color.Black);
                }

                // ====================================================
                // DRAG GHOST
                // ====================================================

                if (dragging &&
                    draggingCardIndex >= 0 &&
                    draggingCardIndex < state.Hand.Count)
                {
                    var card =
                        state.Hand[draggingCardIndex];

                    int ghostX =
                        (int)(mouse.X - dragOffset.X);

                    int ghostY =
                        (int)(mouse.Y - dragOffset.Y);

                    DrawCard(
                        new Rectangle(
                            ghostX,
                            ghostY,
                            cardWidth,
                            cardHeight),
                        card.Name,
                        card.Cost,
                        new Color(245, 235, 190, 220),
                        Color.Gold);
                }

                // ====================================================
                // DEBUG
                // ====================================================

                if (showDebug)
                {
                    Raylib.DrawRectangle(
                        10,
                        height - 220,
                        500,
                        210,
                        new Color(0, 0, 0, 180));

                    int localX =
                        (int)(mouse.X - hoveredElementRect.X);

                    int localY =
                        (int)(mouse.Y - hoveredElementRect.Y);

                    Raylib.DrawText(
                        "DEBUG OVERLAY (F3)",
                        20,
                        height - 205,
                        22,
                        Color.Lime);

                    Raylib.DrawText(
                        $"Cursor Screen: {mouse.X:0}, {mouse.Y:0}",
                        20,
                        height - 170,
                        18,
                        Color.White);

                    Raylib.DrawText(
                        $"Screen Size: {width} x {height}",
                        20,
                        height - 145,
                        18,
                        Color.White);

                    Raylib.DrawText(
                        $"Hovered Element: {hoveredElement}",
                        20,
                        height - 120,
                        18,
                        Color.White);

                    Raylib.DrawText(
                        $"Element Local Pos: {localX}, {localY}",
                        20,
                        height - 95,
                        18,
                        Color.White);

                    Raylib.DrawText(
                        $"Element Size: {hoveredElementRect.Width:0} x {hoveredElementRect.Height:0}",
                        20,
                        height - 70,
                        18,
                        Color.White);

                    Raylib.DrawText(
                        $"Selected Slot: {state.SelectedBoardSlot}",
                        20,
                        height - 45,
                        18,
                        Color.White);
                }

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}