using System.Numerics;
using Raylib_cs;
using CardGamePrototype.Core;

namespace CardGamePrototype.Client;

class Program
{
    static void DrawCard(Rectangle rect, string title, int cost, Color body, Color border, bool selected = false)
    {
        // Main card body background
        Raylib.DrawRectangleRounded(rect, 0.08f, 8, body);

        // Card border highlighting selection
        Raylib.DrawRectangleRoundedLinesEx(
            rect,
            0.08f,
            8,
            selected ? 5 : 2,
            selected ? Color.Gold : border);

        // Header strip for the title
        Raylib.DrawRectangle(
            (int)rect.X,
            (int)rect.Y,
            (int)rect.Width,
            34,
            new Color(25, 35, 55, 255));

        Raylib.DrawText(
            title,
            (int)rect.X + 8,
            (int)rect.Y + 8,
            16,
            Color.White);

        // --- ENHANCED ENERGY BADGE ---
        // Bigger, glowing blue socket for readability
        int badgeRadius = 15;
        Vector2 badgeCenter = new Vector2(rect.X + rect.Width - 20, rect.Y + 17);
        
        Raylib.DrawCircleV(badgeCenter, badgeRadius, new Color(0, 105, 215, 255));
        Raylib.DrawCircleLinesV(badgeCenter, badgeRadius, Color.SkyBlue);

        string costText = $"⚡{cost}";
        int textWidth = Raylib.MeasureText(costText, 14);
        Raylib.DrawText(
            costText,
            (int)(badgeCenter.X - textWidth / 2),
            (int)(badgeCenter.Y - 7),
            14,
            Color.White);
    }

    static void DrawBattleTile(Rectangle rect, bool highlighted, string laneLabel)
    {
        // Dark background for empty slots
        Raylib.DrawRectangleRounded(
            rect,
            0.08f,
            6,
            new Color(45, 50, 60, 255));

        // Subtle track lines inside slots
        Raylib.DrawRectangleRoundedLinesEx(
            rect,
            0.08f,
            6,
            highlighted ? 4 : 1,
            highlighted ? Color.Gold : new Color(80, 85, 95, 255));

        // Light background grid label
        Raylib.DrawText(
            laneLabel,
            (int)rect.X + 6,
            (int)rect.Y + 6,
            12,
            new Color(110, 115, 125, 150));
    }

    static void Main()
    {
        var service = new BattleService();
        service.NewBattle();

        Raylib.InitWindow(1600, 900, "Catalyst Architecture Prototype");
        Raylib.SetWindowState(ConfigFlags.ResizableWindow);
        Raylib.SetTargetFPS(60);

        bool showDebug = false;
        bool dragging = false;
        int draggingCardIndex = -1;
        Vector2 dragOffset = Vector2.Zero;

        string hoveredElement = "None";
        Rectangle hoveredRect = new();

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
            hoveredRect = new Rectangle();

            const int laneCount = 5;
            const int laneWidth = 170;
            const int laneHeight = 150;
            const int laneSpacing = 18;

            const int handCardWidth = 180;
            const int handCardHeight = 240;

            int battlefieldStartX =
                width / 2 -
                ((laneWidth * laneCount) + (laneSpacing * (laneCount - 1))) / 2;

            int enemyRowY = 170;
            int playerRowY = 440; // Pushed slightly down for cleaner spacing
            int handY = height - 280;

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && state.Phase != TurnPhase.Finished)
            {
                for (int i = 0; i < state.Hand.Count; i++)
                {
                    Rectangle cardRect = new(
                        40 + i * (handCardWidth + 16),
                        handY,
                        handCardWidth,
                        handCardHeight);

                    if (Raylib.CheckCollisionPointRec(mouse, cardRect))
                    {
                        dragging = true;
                        draggingCardIndex = i;
                        dragOffset = mouse - new Vector2(cardRect.X, cardRect.Y);
                        break;
                    }
                }

                if (!dragging)
                {
                    for (int i = 0; i < state.PlayerBoard.Count; i++)
                    {
                        Rectangle slotRect = new(
                            battlefieldStartX + i * (laneWidth + laneSpacing),
                            playerRowY,
                            laneWidth,
                            laneHeight);

                        if (Raylib.CheckCollisionPointRec(mouse, slotRect))
                        {
                            if (state.PlayerBoard[i].IsOccupied)
                                state.SelectedBoardSlot = i;
                        }
                    }
                }
            }

            if (dragging && Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                for (int i = 0; i < state.PlayerBoard.Count; i++)
                {
                    Rectangle slotRect = new(
                        battlefieldStartX + i * (laneWidth + laneSpacing),
                        playerRowY,
                        laneWidth,
                        laneHeight);

                    if (Raylib.CheckCollisionPointRec(mouse, slotRect))
                    {
                        service.PlaceCard(draggingCardIndex, i);
                        break;
                    }
                }
                dragging = false;
                draggingCardIndex = -1;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(30, 32, 38, 255));

            // --- VISUAL FIX: BACKGROUND LANE TRACKS FOR ALIGNMENT ---
            // These tracks draw explicit corridors linking the enemy cells to player cells
            for (int i = 0; i < laneCount; i++)
            {
                int laneX = battlefieldStartX + i * (laneWidth + laneSpacing);
                Rectangle trackRect = new(laneX, enemyRowY, laneWidth, (playerRowY + laneHeight) - enemyRowY);
                
                // Draw zebra striping backgrounds for vertical columns
                Raylib.DrawRectangleRec(trackRect, i % 2 == 0 ? new Color(36, 39, 46, 120) : new Color(42, 45, 53, 120));
                Raylib.DrawRectangleLinesEx(trackRect, 1, new Color(55, 60, 72, 60));
            }

            // PLAYER STATISTICS PANEL
            Raylib.DrawRectangle(12, 12, 300, 120, new Color(22, 22, 26, 220));
            Raylib.DrawRectangleLines(12, 12, 300, 120, Color.DarkGray);
            Raylib.DrawText($"Player HP: {state.Player.Hp}/{state.Player.MaxHp}", 24, 24, 24, Color.White);
            Raylib.DrawText($"Energy: ⚡ {state.Player.Energy}", 24, 60, 22, Color.SkyBlue);
            Raylib.DrawText($"Burned: {state.BurnPile.Count}", 24, 90, 18, Color.Orange);

            // ENEMY STATISTICS PANEL
            int enemyPanelX = width - 340;
            Raylib.DrawRectangle(enemyPanelX, 12, 320, 170, new Color(22, 22, 26, 220));
            Raylib.DrawRectangleLines(enemyPanelX, 12, 320, 170, Color.Maroon);
            Raylib.DrawText("Enemy Nexus", enemyPanelX + 20, 20, 24, Color.Red);
            Raylib.DrawText($"HP: {state.Enemy.Hp}/{state.Enemy.MaxHp}", enemyPanelX + 20, 58, 22, Color.White);
            
            // Highlighted active elements on panel
            Raylib.DrawText($"🔥 Fire: {state.Enemy.ActiveElements.GetStacks(ElementType.Fire)}", enemyPanelX + 20, 92, 18, Color.Orange);
            Raylib.DrawText($"❄️ Frost: {state.Enemy.ActiveElements.GetStacks(ElementType.Frost)}", enemyPanelX + 20, 116, 18, Color.SkyBlue);
            Raylib.DrawText($"🧪 Bio: {state.Enemy.ActiveElements.GetStacks(ElementType.Bio)}", enemyPanelX + 20, 140, 18, Color.Lime);

            // BATTLEFIELD HEADER
            Raylib.DrawText("SPATIAL COMBAT FIELD", width / 2 - 150, 40, 24, new Color(150, 160, 175, 255));

            // ENEMY ROW RENDERING
            Raylib.DrawText("ENEMY FRONT", battlefieldStartX, enemyRowY - 30, 18, Color.Maroon);
            for (int i = 0; i < laneCount; i++)
            {
                Rectangle laneRect = new(
                    battlefieldStartX + i * (laneWidth + laneSpacing),
                    enemyRowY,
                    laneWidth,
                    laneHeight);

                bool occupied = state.Enemy.Position == i;

                if (Raylib.CheckCollisionPointRec(mouse, laneRect))
                {
                    hoveredElement = occupied ? "Enemy Target" : $"Empty Enemy Lane {i}";
                    hoveredRect = laneRect;
                }

                DrawBattleTile(laneRect, occupied, $"Lane {i}");

                if (occupied)
                {
                    Rectangle enemyRect = new(laneRect.X + 15, laneRect.Y + 15, laneRect.Width - 30, laneRect.Height - 30);

                    // --- VISUAL FIX: DYNAMIC ELEMENTAL STATUS AURAS ---
                    int fireStacks = state.Enemy.ActiveElements.GetStacks(ElementType.Fire);
                    int frostStacks = state.Enemy.ActiveElements.GetStacks(ElementType.Frost);
                    int bioStacks = state.Enemy.ActiveElements.GetStacks(ElementType.Bio);

                    // Draw thickness expansion based on stack intensity
                    if (fireStacks > 0)
                        Raylib.DrawRectangleLinesEx(new Rectangle(enemyRect.X - 4, enemyRect.Y - 4, enemyRect.Width + 8, enemyRect.Height + 8), 3, Color.Orange);
                    if (frostStacks > 0)
                        Raylib.DrawRectangleLinesEx(new Rectangle(enemyRect.X - 7, enemyRect.Y - 7, enemyRect.Width + 14, enemyRect.Height + 14), 2, Color.SkyBlue);
                    if (bioStacks > 0)
                        Raylib.DrawRectangleLinesEx(new Rectangle(enemyRect.X - 10, enemyRect.Y - 10, enemyRect.Width + 20, enemyRect.Height + 20), 2, Color.Lime);

                    // Core body token
                    Raylib.DrawRectangleRounded(enemyRect, 0.12f, 6, new Color(130, 20, 40, 255));
                    Raylib.DrawRectangleRoundedLinesEx(enemyRect, 0.12f, 6, 3, Color.Red);

                    Raylib.DrawText("💥 ENEMY 💥", (int)enemyRect.X + 12, (int)enemyRect.Y + 45, 20, Color.White);
                    Raylib.DrawText($"Pos: [Col {i}]", (int)enemyRect.X + 24, (int)enemyRect.Y + 80, 14, Color.Yellow);
                }
            }

            // PLAYER ROW RENDERING (CATALYST MATRIX)
            Raylib.DrawText("YOUR CATALYST SLOTS", battlefieldStartX, playerRowY - 30, 18, Color.SkyBlue);
            for (int i = 0; i < state.PlayerBoard.Count; i++)
            {
                Rectangle tile = new(
                    battlefieldStartX + i * (laneWidth + laneSpacing),
                    playerRowY,
                    laneWidth,
                    laneHeight);

                bool selected = state.SelectedBoardSlot == i;

                if (Raylib.CheckCollisionPointRec(mouse, tile))
                {
                    hoveredElement = $"Catalyst Core Slot {i}";
                    hoveredRect = tile;
                }

                DrawBattleTile(tile, selected, $"Slot {i}");

                var slot = state.PlayerBoard[i];
                if (slot.IsOccupied)
                {
                    DrawCard(
                        new Rectangle(tile.X + 6, tile.Y + 6, tile.Width - 12, tile.Height - 12),
                        slot.Card!.Name,
                        slot.Card.Cost,
                        new Color(70, 75, 85, 255),
                        Color.SkyBlue,
                        selected);

                    // Retained fixed Y calculation from the previous compiler check
                    Raylib.DrawText(
                        $"Turns Active: {slot.TurnsOnBoard}",
                        (int)tile.X + 12,
                        (int)(tile.Y + tile.Height - 26),
                        13,
                        Color.LightGray);
                }
                else
                {
                    Raylib.DrawText("READY FOR DROP", (int)tile.X + 22, (int)tile.Y + 65, 14, new Color(90, 100, 110, 255));
                }
            }

            // CARDS HAND ZONE
            Raylib.DrawText($"HAND ZONE ({state.Hand.Count})", 40, handY - 40, 20, Color.White);
            for (int i = 0; i < state.Hand.Count; i++)
            {
                if (dragging && i == draggingCardIndex)
                    continue;

                Rectangle cardRect = new(
                    40 + i * (handCardWidth + 16),
                    handY,
                    handCardWidth,
                    handCardHeight);

                if (Raylib.CheckCollisionPointRec(mouse, cardRect))
                {
                    hoveredElement = state.Hand[i].Name;
                    hoveredRect = cardRect;
                }

                DrawCard(
                    cardRect,
                    state.Hand[i].Name,
                    state.Hand[i].Cost,
                    new Color(235, 235, 240, 255),
                    new Color(40, 45, 55, 255));
            }

            // FLOATING CARD SELECTION/DRAG DECORATION
            if (dragging && draggingCardIndex >= 0 && draggingCardIndex < state.Hand.Count)
            {
                var card = state.Hand[draggingCardIndex];
                DrawCard(
                    new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, handCardWidth, handCardHeight),
                    card.Name,
                    card.Cost,
                    new Color(255, 245, 200, 230),
                    Color.Gold);
            }

            // HOTKEYS CONTEXT COMPASS
            Raylib.DrawText("[ENTER] End Turn   |   [SPACE] Trigger Selected Catalyst Card   |   [R] Reset Board", 40, height - 35, 16, Color.Gray);

            // RUNTIME SYSTEM METRICS DEBUGGER
            if (showDebug)
            {
                Raylib.DrawRectangle(10, height - 250, 520, 210, new Color(10, 10, 15, 240));
                int localX = (int)(mouse.X - hoveredRect.X);
                int localY = (int)(mouse.Y - hoveredRect.Y);

                Raylib.DrawText("CORE SIM DEBUGGER (F3 Active)", 20, height - 235, 18, Color.Lime);
                Raylib.DrawText($"Cursor Axis Vector: {mouse.X:0}, {mouse.Y:0}", 20, height - 205, 16, Color.White);
                Raylib.DrawText($"Canvas Dimensions: {width} x {height}", 20, height - 180, 16, Color.White);
                Raylib.DrawText($"Collision Element Name: {hoveredElement}", 20, height - 155, 16, Color.Gold);
                Raylib.DrawText($"Localized Intersection Context Offset: {localX}, {localY}", 20, height - 130, 16, Color.White);
                Raylib.DrawText($"Bounding Dimensions: {hoveredRect.Width:0} x {hoveredRect.Height:0}", 20, height - 105, 16, Color.White);
                Raylib.DrawText($"Active Enemy Track Position Matrix ID: {state.Enemy.Position}", 20, height - 80, 16, Color.Orange);
            }

            // --- VISUAL FIX: ENDGAME SCREEN SCENE OVERLAY ---
            if (state.Phase == TurnPhase.Finished)
            {
                // Darken the entire rendering surface with an alpha veil
                Raylib.DrawRectangle(0, 0, width, height, new Color(16, 16, 20, 225));

                if (state.Enemy.IsDead)
                {
                    int winTextWidth = Raylib.MeasureText("VICTORY", 72);
                    Raylib.DrawText("VICTORY", width / 2 - winTextWidth / 2, height / 2 - 60, 72, Color.Gold);
                    
                    int subTextWidth = Raylib.MeasureText("The simulation sector was fully purged.", 20);
                    Raylib.DrawText("The simulation sector was fully purged.", width / 2 - subTextWidth / 2, height / 2 + 20, 20, Color.LightGray);
                }
                else if (state.Player.IsDead)
                {
                    int loseTextWidth = Raylib.MeasureText("DEFEAT", 72);
                    Raylib.DrawText("DEFEAT", width / 2 - loseTextWidth / 2, height / 2 - 60, 72, Color.Red);
                    
                    int subTextWidth = Raylib.MeasureText("Your system was destroyed.", 20);
                    Raylib.DrawText("Your system was destroyed.", width / 2 - subTextWidth / 2, height / 2 + 20, 20, Color.LightGray);
                }

                int restartTextWidth = Raylib.MeasureText("Press [ R ] key to initialize a new run configuration", 18);
                Raylib.DrawText("Press [ R ] key to initialize a new run configuration", width / 2 - restartTextWidth / 2, height / 2 + 90, 18, Color.DarkGray);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}