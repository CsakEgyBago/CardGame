using System;
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

            Raylib.InitWindow(800, 600, "Catalyst -> Executioner Prototype");
            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                // Input
                if (Raylib.IsKeyPressed(KeyboardKey.Escape)) break;
                if (Raylib.IsKeyPressed(KeyboardKey.R)) service.Restart();
                if (Raylib.IsKeyPressed(KeyboardKey.Space)) service.EndTurn();

                // Number keys 1-5 to play cards
                for (int i = 0; i < 5; i++)
                {
                    var key = KeyboardKey.One + i;
                    if (Raylib.IsKeyPressed(key)) service.PlayCard(i);
                }

                // Render
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.DarkGray);

                var s = service.State;
                // Player info
                Raylib.DrawText($"Player HP: {s.Player.Hp}/{s.Player.MaxHp}", 20, 20, 20, Color.White);
                Raylib.DrawText($"Energy: {s.Player.Energy}", 20, 46, 18, Color.LightGray);
                Raylib.DrawText($"Position: {s.Player.Position}", 20, 70, 18, Color.LightGray);

                // Enemy info
                Raylib.DrawText($"Enemy HP: {s.Enemy.Hp}/{s.Enemy.MaxHp}", 400, 20, 20, Color.White);
                Raylib.DrawText($"Position: {s.Enemy.Position}", 400, 46, 18, Color.LightGray);
                Raylib.DrawText($"Fire: {s.Enemy.ActiveElements.GetStacks(ElementType.Fire)}", 400, 70, 18, Color.Orange);
                Raylib.DrawText($"Frost: {s.Enemy.ActiveElements.GetStacks(ElementType.Frost)}", 400, 92, 18, Color.SkyBlue);
                Raylib.DrawText($"Bio: {s.Enemy.ActiveElements.GetStacks(ElementType.Bio)}", 400, 114, 18, Color.Lime);

                // Board positions
                int baseX = 200; int baseY = 200; int cellW = 80;
                for (int i = 0; i < s.BoardSize; i++)
                {
                    int x = baseX + i * (cellW + 10);
                    Raylib.DrawRectangle(x, baseY, cellW, 120, Color.LightGray);
                    Raylib.DrawText(i.ToString(), x + 6, baseY + 6, 20, Color.Black);
                }
                // draw player/enemy in cells
                Raylib.DrawText("P", baseX + s.Player.Position * (cellW + 10) + 32, baseY + 40, 28, Color.Blue);
                Raylib.DrawText("E", baseX + s.Enemy.Position * (cellW + 10) + 32, baseY + 40, 28, Color.Red);

                // Hand
                int handX = 20; int handY = 360; int cw = 140; int ch = 100; int gap = 10;
                for (int i = 0; i < s.Hand.Count; i++)
                {
                    var c = s.Hand[i];
                    int x = handX + i * (cw + gap);
                    Raylib.DrawRectangle(x, handY, cw, ch, Color.RayWhite);
                    Raylib.DrawRectangleLines(x, handY, cw, ch, Color.DarkGray);
                    Raylib.DrawText(c.Name, x + 6, handY + 6, 16, Color.Black);
                    Raylib.DrawText($"Cost: {c.Cost}", x + 6, handY + 28, 14, Color.DarkGray);
                    Raylib.DrawText($"C:{c.CatalystEffects.Count} E:{c.ExecutionerEffects.Count}", x + 6, handY + 48, 12, Color.DarkGray);
                    Raylib.DrawText(c.CardType.ToString(), x + 6, handY + 64, 12, Color.DarkGray);
                    Raylib.DrawText((i + 1).ToString(), x + cw - 24, handY + 6, 20, Color.DarkBlue);
                }

                // Status
                string status = s.Phase == TurnPhase.Finished
                    ? (s.Player.IsDead ? "Defeat - Press R to restart" : "Victory - Press R to restart")
                    : "";
                Raylib.DrawText(status, 20, 520, 20, Color.Yellow);

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}