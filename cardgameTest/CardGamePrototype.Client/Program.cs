using System.Numerics;
using Raylib_cs;
using CardGamePrototype.Core;

namespace CardGamePrototype.Client;

public enum GameScene { TitleScreen, ModeSelect, CampaignMap, BattleView, MarketShop, DeckBuilder }
public enum NodeType { CombatMinion, CombatElite, CombatBoss }

public class CampaignNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public int EnemyHp { get; set; }
    public int EnemyDefaultPosition { get; set; }
    public bool Completed { get; set; }
}

public class PlayerProfile
{
    public int Gold { get; set; } = 150;
    public int SkillPoints { get; set; } = 8;
    public List<CardDefinition> TotalCollection { get; set; } = new();
    public List<CardDefinition> ActiveDeck { get; set; } = new();
    public SkillTreeManager SkillTree { get; set; } = new();

    bool HasNode(int id) => SkillTree.Nodes.Find(n => n.Id == id)?.IsUnlocked == true;

    public int MaxEnergyBonus  => (HasNode(1) ? 1 : 0) + (HasNode(2) ? 1 : 0) + (HasNode(3) ? 2 : 0);
    public int MaxHpBonus      => (HasNode(11) ? 10 : 0) + (HasNode(12) ? 15 : 0) + (HasNode(13) ? 25 : 0);
    public int StartCardsBonus => (HasNode(4) ? 1 : 0) + (HasNode(5) ? 1 : 0);
}

class Program
{
    static bool DrawButton(Rectangle rect, string text, Color baseColor, Color hoverColor)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        bool hovered = Raylib.CheckCollisionPointRec(mouse, rect);
        Raylib.DrawRectangleRounded(rect, 0.15f, 4, hovered ? hoverColor : baseColor);
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.15f, 4, 2, Color.DarkGray);
        
        int textWidth = Raylib.MeasureText(text, 20);
        Raylib.DrawText(text, (int)(rect.X + rect.Width / 2 - textWidth / 2), (int)(rect.Y + rect.Height / 2 - 10), 20, Color.White);
        
        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    static void DrawCard(Rectangle rect, string title, string description, int cost, Color body, Color border, bool selected = false)
    {
        Raylib.DrawRectangleRounded(rect, 0.08f, 8, body);
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.08f, 8, selected ? 5 : 2, selected ? Color.Gold : border);

        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, 34, new Color(25, 35, 55, 255));
        Raylib.DrawText(title, (int)rect.X + 8, (int)rect.Y + 8, 15, Color.White);
        Raylib.DrawText(description, (int)rect.X + 8, (int)rect.Y + 45, 12, Color.DarkGray);

        Vector2 badgeCenter = new Vector2(rect.X + rect.Width - 20, rect.Y + 17);
        Raylib.DrawCircleV(badgeCenter, 13, new Color(0, 90, 190, 255));
        
        Raylib.DrawTriangle(
            new Vector2(badgeCenter.X + 1, badgeCenter.Y - 7),
            new Vector2(badgeCenter.X - 4, badgeCenter.Y + 1),
            new Vector2(badgeCenter.X + 3, badgeCenter.Y + 1), Color.Yellow);
        Raylib.DrawTriangle(
            new Vector2(badgeCenter.X - 3, badgeCenter.Y - 1),
            new Vector2(badgeCenter.X - 1, badgeCenter.Y + 7),
            new Vector2(badgeCenter.X + 4, badgeCenter.Y - 1), Color.Yellow);

        Raylib.DrawText(cost.ToString(), (int)(badgeCenter.X - 14), (int)(badgeCenter.Y - 6), 12, Color.White);
    }

    static Color CardColorFromName(string name) => name switch
    {
        "Ignite"    => new Color(235, 95, 15, 255),
        "Firebolt"  => new Color(255, 145, 35, 255),
        "Frost Nova" or "FrostNova" => new Color(65, 175, 240, 255),
        "Push"      => new Color(155, 75, 215, 255),
        "Bio Spore" or "BioSpore"   => new Color(45, 200, 65, 255),
        _           => new Color(140, 145, 165, 255)
    };

    static void DrawSummonedUnit(Rectangle zone, string cardName, int hp, int maxHp, bool selected)
    {
        Color col = CardColorFromName(cardName);
        Raylib.DrawRectangleRounded(zone, 0.12f, 6, new Color(col.R / 5, col.G / 5, col.B / 5, 255));
        Raylib.DrawRectangleRoundedLinesEx(zone, 0.12f, 6, selected ? 3 : 2, selected ? Color.Gold : col);

        float cx = zone.X + zone.Width / 2f;
        float cy = zone.Y + zone.Height * 0.42f;
        float r = Math.Min(zone.Width, zone.Height) * 0.27f;

        if (cardName is "Ignite" or "Firebolt")
        {
            // Flame: two stacked upward triangles
            Raylib.DrawTriangle(new Vector2(cx, cy - r), new Vector2(cx - r * 0.55f, cy + r * 0.5f), new Vector2(cx + r * 0.55f, cy + r * 0.5f), col);
            Raylib.DrawTriangle(new Vector2(cx, cy - r * 0.3f), new Vector2(cx - r * 0.28f, cy + r * 0.9f), new Vector2(cx + r * 0.28f, cy + r * 0.9f), new Color(255, 215, 60, 210));
        }
        else if (cardName is "Frost Nova" or "FrostNova")
        {
            // Snowflake: 4-axis cross
            Raylib.DrawLineEx(new Vector2(cx - r, cy), new Vector2(cx + r, cy), 3f, col);
            Raylib.DrawLineEx(new Vector2(cx, cy - r), new Vector2(cx, cy + r), 3f, col);
            Raylib.DrawLineEx(new Vector2(cx - r * 0.72f, cy - r * 0.72f), new Vector2(cx + r * 0.72f, cy + r * 0.72f), 3f, col);
            Raylib.DrawLineEx(new Vector2(cx + r * 0.72f, cy - r * 0.72f), new Vector2(cx - r * 0.72f, cy + r * 0.72f), 3f, col);
        }
        else if (cardName is "Push")
        {
            // Arrow: right-facing chevron + tail
            Raylib.DrawTriangle(new Vector2(cx + r, cy), new Vector2(cx - r * 0.3f, cy - r * 0.7f), new Vector2(cx - r * 0.3f, cy + r * 0.7f), col);
            Raylib.DrawRectangleRec(new Rectangle(cx - r, cy - r * 0.22f, r * 0.78f, r * 0.44f), col);
        }
        else if (cardName is "Bio Spore" or "BioSpore")
        {
            // Bio: concentric rings + nucleus
            Raylib.DrawCircleLines((int)cx, (int)cy, r, col);
            Raylib.DrawCircleLines((int)cx, (int)cy, r * 0.58f, col);
            Raylib.DrawCircleV(new Vector2(cx, cy), r * 0.24f, col);
        }
        else
        {
            // Generic: diamond
            Raylib.DrawTriangle(new Vector2(cx, cy - r), new Vector2(cx - r * 0.55f, cy), new Vector2(cx + r * 0.55f, cy), col);
            Raylib.DrawTriangle(new Vector2(cx, cy + r), new Vector2(cx + r * 0.55f, cy), new Vector2(cx - r * 0.55f, cy), col);
        }

        int nw = Raylib.MeasureText(cardName, 11);
        Raylib.DrawText(cardName, (int)(zone.X + (zone.Width - nw) / 2), (int)(zone.Y + zone.Height - 30), 11, Color.White);

        float bx = zone.X + 5, by = zone.Y + zone.Height - 16, bw = zone.Width - 10, bh = 7;
        Raylib.DrawRectangleRec(new Rectangle(bx, by, bw, bh), new Color(38, 38, 38, 220));
        if (maxHp > 0)
            Raylib.DrawRectangleRec(new Rectangle(bx, by, bw * ((float)hp / maxHp), bh), new Color(50, 215, 80, 255));
    }

    // Radial skill tree layout — 5 branches fan out 72° apart from the origin node
    static Vector2 GetSkillNodePos(SkillNode node, float originX, float originY, float spacing = 78f)
    {
        if (node.Branch == SkillBranch.Origin) return new Vector2(originX, originY);
        float deg = node.Branch switch
        {
            SkillBranch.Green  => 270f,
            SkillBranch.Blue   => 198f,
            SkillBranch.Red    => 342f,
            SkillBranch.Yellow => 126f,
            SkillBranch.White  =>  54f,
            _ => 0f
        };
        float rad = deg * MathF.PI / 180f;
        return new Vector2(originX + node.Column * spacing * MathF.Cos(rad),
                           originY + node.Column * spacing * MathF.Sin(rad));
    }

    static void DrawNodeIcon(Vector2 c, SkillBranch branch, Color col, float r = 9f)
    {
        switch (branch)
        {
            case SkillBranch.Origin:
                Raylib.DrawCircleV(c, r * 0.5f, col); break;
            case SkillBranch.Green:
                Raylib.DrawLineEx(new Vector2(c.X + r * 0.25f, c.Y - r), new Vector2(c.X - r * 0.15f, c.Y + r * 0.05f), 2f, col);
                Raylib.DrawLineEx(new Vector2(c.X - r * 0.15f, c.Y + r * 0.05f), new Vector2(c.X + r * 0.3f, c.Y + r * 0.2f), 2f, col);
                Raylib.DrawLineEx(new Vector2(c.X + r * 0.3f, c.Y + r * 0.2f), new Vector2(c.X - r * 0.25f, c.Y + r), 2f, col);
                break;
            case SkillBranch.Blue:
                Raylib.DrawCircleLines((int)c.X, (int)c.Y, r * 0.72f, col);
                Raylib.DrawCircleV(c, r * 0.3f, col); break;
            case SkillBranch.Red:
                Raylib.DrawTriangle(new Vector2(c.X, c.Y - r), new Vector2(c.X - r * 0.55f, c.Y + r * 0.65f), new Vector2(c.X + r * 0.55f, c.Y + r * 0.65f), col); break;
            case SkillBranch.Yellow:
                Raylib.DrawCircleLines((int)c.X, (int)c.Y, r * 0.68f, col);
                Raylib.DrawLineEx(new Vector2(c.X, c.Y - r * 0.38f), new Vector2(c.X, c.Y + r * 0.38f), 1.5f, col); break;
            case SkillBranch.White:
                Raylib.DrawLineEx(new Vector2(c.X - r, c.Y), new Vector2(c.X + r, c.Y), 2.5f, col);
                Raylib.DrawLineEx(new Vector2(c.X, c.Y - r), new Vector2(c.X, c.Y + r), 2.5f, col); break;
        }
    }

    static Color GetBranchColor(SkillBranch branch)
    {
        return branch switch
        {
            SkillBranch.Green => new Color(50, 200, 90, 255),
            SkillBranch.Blue => Color.SkyBlue,
            SkillBranch.Red => Color.Maroon,
            SkillBranch.Yellow => Color.Gold,
            SkillBranch.White => Color.LightGray,
            _ => Color.Purple
        };
    }

    static void Main()
    {
        GameScene scene = GameScene.TitleScreen;
        PlayerProfile profile = new PlayerProfile();

        for (int i = 0; i < 3; i++) profile.ActiveDeck.Add(CardLibrary.Ignite());
        for (int i = 0; i < 3; i++) profile.ActiveDeck.Add(CardLibrary.Firebolt());
        for (int i = 0; i < 2; i++) profile.ActiveDeck.Add(CardLibrary.Push());
        for (int i = 0; i < 2; i++) profile.ActiveDeck.Add(CardLibrary.FrostNova());

        profile.TotalCollection.AddRange(profile.ActiveDeck);

        List<CampaignNode> campaignNodes = new List<CampaignNode>
        {
            new CampaignNode { Id = 0, Name = "Sector Delta Core", Type = NodeType.CombatMinion, EnemyHp = 35, EnemyDefaultPosition = 1 },
            new CampaignNode { Id = 1, Name = "Outer Perimeter Gate", Type = NodeType.CombatMinion, EnemyHp = 45, EnemyDefaultPosition = 4 },
            new CampaignNode { Id = 2, Name = "Arch-Executioner Frame", Type = NodeType.CombatElite, EnemyHp = 65, EnemyDefaultPosition = 2 },
            new CampaignNode { Id = 3, Name = "The Catalyst Singularity", Type = NodeType.CombatBoss, EnemyHp = 100, EnemyDefaultPosition = 3 }
        };
        int activeNodeIndex = 0;

        BattleService battleService = new BattleService();
        CampaignNode? activeBattleNode = null;

        List<CardDefinition> shopInventory = new List<CardDefinition> { CardLibrary.Ignite(), CardLibrary.Push(), CardLibrary.BioSpore() };
        int cardShopCost = 45;

        Raylib.InitWindow(1600, 900, "Catalyst Architecture");
        Raylib.SetWindowState(ConfigFlags.ResizableWindow);
        Raylib.SetTargetFPS(60);

        bool dragging = false;
        int draggingIndex = -1;
        Vector2 dragOffset = Vector2.Zero;
        bool isDraggingFromCollectionPool = false; 

        while (!Raylib.WindowShouldClose())
        {
            int width = Raylib.GetScreenWidth();
            int height = Raylib.GetScreenHeight();
            Vector2 mouse = Raylib.GetMousePosition();

            // GLOBAL: Listen for F3 to toggle Skill Tree debug
            if (Raylib.IsKeyPressed(KeyboardKey.F3))
            {
                profile.SkillTree.DebugMode = !profile.SkillTree.DebugMode;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(25, 27, 33, 255));

            switch (scene)
            {
                case GameScene.TitleScreen:
                    Raylib.DrawText("CATALYST // ARCHITECTURE", width / 2 - 380, height / 2 - 140, 50, Color.Gold);
                    Raylib.DrawText("Deterministic Tactical Command Interface", width / 2 - 240, height / 2 - 70, 20, Color.LightGray);
                    
                    if (DrawButton(new Rectangle(width / 2 - 150, height / 2 + 20, 300, 50), "INITIALIZE CORE SYSTEM", new Color(40, 50, 70, 255), new Color(60, 80, 120, 255)))
                        scene = GameScene.ModeSelect;
                    break;

                case GameScene.ModeSelect:
                    Raylib.DrawText("SELECT SYSTEM PROTOCOL", width / 2 - 220, 150, 32, Color.White);

                    if (DrawButton(new Rectangle(width / 2 - 320, height / 2 - 40, 280, 120), "STORY MODE\n\n(Sector Campaign)", new Color(30, 80, 50, 255), new Color(45, 115, 75, 255)))
                        scene = GameScene.CampaignMap;
                    
                    Rectangle arcadeFrame = new Rectangle(width / 2 + 40, height / 2 - 40, 280, 120);
                    Raylib.DrawRectangleRec(arcadeFrame, new Color(40, 40, 45, 255));
                    Raylib.DrawRectangleLinesEx(arcadeFrame, 2, Color.DarkGray);
                    Raylib.DrawText("ARCADE MODE", (int)arcadeFrame.X + 60, (int)arcadeFrame.Y + 35, 22, Color.Gray);
                    Raylib.DrawText("[LOCKED IN ALPHA]", (int)arcadeFrame.X + 50, (int)arcadeFrame.Y + 70, 16, Color.Maroon);
                    break;

                case GameScene.CampaignMap:
                    Raylib.DrawRectangle(0, 0, width, 60, new Color(18, 20, 24, 255));
                    Raylib.DrawText($"STORY CAMPAIGN PROGRESSION OVERWORLD", 30, 18, 20, Color.White);
                    Raylib.DrawText($"Credits Pool: {profile.Gold}G", width - 420, 20, 18, Color.Gold);
                    Raylib.DrawText($"Skill Points: {profile.SkillPoints}SP", width - 200, 20, 18, Color.SkyBlue);

                    if (DrawButton(new Rectangle(40, 100, 180, 45), "DECK MANAGER", new Color(65, 75, 95, 255), new Color(85, 105, 135, 255)))
                        scene = GameScene.DeckBuilder;
                    if (DrawButton(new Rectangle(240, 100, 180, 45), "MARKET / SKILLS", new Color(90, 70, 50, 255), new Color(130, 100, 70, 255)))
                        scene = GameScene.MarketShop;

                    Raylib.DrawText("SECTOR RUN TRAILMAP", width / 2 - 130, 220, 24, Color.LightGray);

                    for (int i = 0; i < campaignNodes.Count; i++)
                    {
                        var node = campaignNodes[i];
                        int nodeX = 200 + i * (width - 400) / (campaignNodes.Count - 1);
                        int nodeY = height / 2;

                        if (i < campaignNodes.Count - 1)
                        {
                            int nextX = 200 + (i + 1) * (width - 400) / (campaignNodes.Count - 1);
                            Raylib.DrawLineEx(new Vector2(nodeX, nodeY), new Vector2(nextX, nodeY), 4, Color.DarkGray);
                        }

                        Rectangle nodeBox = new Rectangle(nodeX - 80, nodeY - 50, 160, 100);
                        Color statusColor = node.Completed ? Color.DarkGreen : (i == activeNodeIndex ? Color.Maroon : Color.DarkGray);
                        Color hoverStatusColor = i == activeNodeIndex ? Color.Red : statusColor;

                        if (DrawButton(nodeBox, $"{node.Name}\n\n[Tier {i + 1}]", statusColor, hoverStatusColor) && i == activeNodeIndex)
                        {
                            activeBattleNode = node;
                            battleService.NewBattle();
                            
                            battleService.State.DrawPile.Clear();
                            battleService.State.Hand.Clear();
                            battleService.State.BurnPile.Clear();
                            
                            // HP Bonus logic hooked up to white branch
                            int finalMaxHp = 50 + profile.MaxHpBonus;
                            battleService.State.Player.MaxHp = finalMaxHp; 
                            battleService.State.Player.Hp = finalMaxHp;
                            
                            battleService.State.Enemy.MaxHp = node.EnemyHp;
                            battleService.State.Enemy.Hp = node.EnemyHp;
                            battleService.State.Enemy.Position = node.EnemyDefaultPosition;

                            List<CardDefinition> gameDeckCopy = new List<CardDefinition>(profile.ActiveDeck);
                            for (int k = gameDeckCopy.Count - 1; k > 0; k--)
                            {
                                int idx = battleService.State.Rng.Next(k + 1);
                                var hold = gameDeckCopy[k];
                                gameDeckCopy[k] = gameDeckCopy[idx];
                                gameDeckCopy[idx] = hold;
                            }
                            battleService.State.DrawPile.AddRange(gameDeckCopy);
                            
                            // Starting stats boosted by Skill Tree
                            battleService.State.Player.Energy = 5 + profile.MaxEnergyBonus;
                            int cardsToDraw = 5 + profile.StartCardsBonus;
                            
                            while(battleService.State.Hand.Count < cardsToDraw && battleService.State.DrawPile.Count > 0)
                            {
                                var top = battleService.State.DrawPile[0];
                                battleService.State.DrawPile.RemoveAt(0);
                                battleService.State.Hand.Add(top);
                            }

                            scene = GameScene.BattleView;
                        }
                    }
                    break;

                case GameScene.BattleView:
                    var activeBattleState = battleService.State;

                    // Keyboard input
                    if (activeBattleState.Phase == TurnPhase.PlayerTurn)
                    {
                        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                            battleService.EndTurn();
                        if (Raylib.IsKeyPressed(KeyboardKey.Space) && activeBattleState.SelectedBoardSlot >= 0)
                            battleService.ExecuteCard(activeBattleState.SelectedBoardSlot);
                    }

                    int sideW = 220;
                    int cx = sideW;
                    int cw = width - sideW * 2;
                    int cpH = 72, cpW = sideW - 20;
                    bool showBattleGrid = dragging;

                    int hoveredHandCard = -1;
                    if (!dragging)
                        for (int j = 0; j < activeBattleState.Hand.Count; j++)
                        {
                            Rectangle hcr = new Rectangle(10, 85 + j * (cpH + 6), cpW, cpH);
                            if (Raylib.CheckCollisionPointRec(mouse, hcr)) { hoveredHandCard = j; break; }
                        }

                    // === LEFT SIDEBAR: Player Hand ===
                    Raylib.DrawRectangle(0, 0, sideW, height, new Color(18, 20, 25, 255));
                    Raylib.DrawLine(sideW, 0, sideW, height, new Color(50, 55, 65, 255));
                    Raylib.DrawText("Bf preview for p", 10, 8, 13, Color.Gold);
                    Raylib.DrawText($"HP  {activeBattleState.Player.Hp}/{activeBattleState.Player.MaxHp}", 10, 28, 14, Color.White);
                    Raylib.DrawText($"NRG {activeBattleState.Player.Energy}", 10, 46, 14, Color.SkyBlue);
                    Raylib.DrawText($"DIS {activeBattleState.BurnPile.Count}", 10, 62, 13, Color.Orange);

                    for (int i = 0; i < activeBattleState.Hand.Count; i++)
                    {
                        if (dragging && i == draggingIndex) continue;
                        Rectangle cr = new Rectangle(10, 85 + i * (cpH + 6), cpW, cpH);
                        var hc = activeBattleState.Hand[i];
                        DrawCard(cr, hc.Name, hc.Description, hc.Cost, Color.White, i == hoveredHandCard ? Color.Gold : Color.Black);
                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, cr) && !dragging)
                        {
                            dragging = true;
                            draggingIndex = i;
                            isDraggingFromCollectionPool = false;
                            dragOffset = mouse - new Vector2(cr.X, cr.Y);
                        }
                    }

                    Raylib.DrawText("Cards in hand", 10, height - 75, 16, Color.White);
                    for (int i = 0; i < Math.Min(activeBattleState.Hand.Count, 5); i++)
                    {
                        Rectangle m = new Rectangle(10 + i * 34, height - 48, 28, 40);
                        Raylib.DrawRectangleRec(m, new Color(165, 170, 190, 210));
                        Raylib.DrawRectangleLinesEx(m, 1, Color.Black);
                    }

                    // === RIGHT SIDEBAR: Placed Cards / Enemy ===
                    Raylib.DrawRectangle(width - sideW, 0, sideW, height, new Color(18, 20, 25, 255));
                    Raylib.DrawLine(width - sideW, 0, width - sideW, height, new Color(50, 55, 65, 255));
                    Raylib.DrawText("Bf preview for E", width - sideW + 8, 8, 13, Color.Gold);
                    Raylib.DrawText(activeBattleNode?.Name ?? "Enemy", width - sideW + 8, 28, 12, Color.Red);
                    Raylib.DrawText($"HP  {activeBattleState.Enemy.Hp}/{activeBattleState.Enemy.MaxHp}", width - sideW + 8, 44, 14, Color.White);
                    Raylib.DrawText($"Fire  {activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Fire)}", width - sideW + 8, 62, 12, Color.Orange);
                    Raylib.DrawText($"Frost {activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Frost)}", width - sideW + 90, 62, 12, Color.SkyBlue);

                    for (int i = 0; i < activeBattleState.PlayerBoard.Count; i++)
                    {
                        var bSlot = activeBattleState.PlayerBoard[i];
                        bool bSel = activeBattleState.SelectedBoardSlot == i;
                        Rectangle sr = new Rectangle(width - sideW + 10, 85 + i * (cpH + 6), cpW, cpH);
                        if (bSlot.IsOccupied)
                        {
                            var occ = bSlot.Occupant!;
                            DrawCard(sr, occ.SourceCard.Name, $"HP:{occ.Hp}/{occ.MaxHp} T:{bSlot.TurnsOnBoard}", occ.SourceCard.Cost, new Color(55, 60, 72, 255), Color.SkyBlue, bSel);
                        }
                        else
                        {
                            Raylib.DrawRectangleRec(sr, new Color(28, 31, 38, 200));
                            Raylib.DrawRectangleLinesEx(sr, 1, bSel ? Color.Gold : new Color(50, 55, 65, 255));
                            Raylib.DrawText($"Slot {i}", (int)sr.X + 8, (int)(sr.Y + sr.Height / 2 - 7), 14, new Color(75, 80, 90, 180));
                        }
                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, sr))
                            activeBattleState.SelectedBoardSlot = bSel ? -1 : i;
                    }

                    Raylib.DrawText("Cards on battlefield", width - sideW + 8, height - 75, 14, Color.White);
                    int occupiedCt = 0;
                    for (int i = 0; i < activeBattleState.PlayerBoard.Count; i++)
                        if (activeBattleState.PlayerBoard[i].IsOccupied) occupiedCt++;
                    for (int i = 0; i < Math.Min(occupiedCt, 5); i++)
                    {
                        Rectangle m = new Rectangle(width - sideW + 10 + i * 34, height - 48, 28, 40);
                        Raylib.DrawRectangleRec(m, new Color(90, 130, 195, 210));
                        Raylib.DrawRectangleLinesEx(m, 1, Color.SkyBlue);
                    }

                    // === CENTER: Banner or 2.5D Grid ===
                    if (showBattleGrid)
                    {
                        int fTop = 70, fBot = height - 100;
                        int tL = cx + cw / 10, tR = cx + cw - cw / 10;   // top edge: narrower (enemy/far)
                        int bL = cx + 8,        bR = cx + cw - 8;          // bottom edge: wider (player/near)

                        Raylib.DrawRectangle(cx, fTop, cw, fBot - fTop, new Color(28, 32, 40, 255));

                        // Perspective horizon lines
                        Raylib.DrawLineEx(new Vector2(tL, fTop), new Vector2(tR, fTop), 2, new Color(80, 88, 105, 220));
                        Raylib.DrawLineEx(new Vector2(bL, fBot), new Vector2(bR, fBot), 2, new Color(80, 88, 105, 220));

                        // Converging lane dividers
                        for (int i = 0; i <= 5; i++)
                        {
                            float t = (float)i / 5;
                            int topX = (int)(tL + t * (tR - tL));
                            int botX = (int)(bL + t * (bR - bL));
                            Raylib.DrawLineEx(new Vector2(topX, fTop), new Vector2(botX, fBot), i == 0 || i == 5 ? 2 : 1, new Color(58, 63, 76, 210));
                        }

                        // Mid divider (enemy / player zone separator)
                        int midY = fTop + (int)((fBot - fTop) * 0.48f);
                        int midL = (int)(tL + 0.48f * (bL - tL));
                        int midR = (int)(tR + 0.48f * (bR - tR));
                        Raylib.DrawLineEx(new Vector2(midL, midY), new Vector2(midR, midY), 1, new Color(55, 60, 74, 190));

                        for (int i = 0; i < 5; i++)
                        {
                            float t0 = (float)i / 5, t1 = (float)(i + 1) / 5;
                            int ex0 = (int)(tL + t0 * (tR - tL)) + 4;
                            int ex1 = (int)(tL + t1 * (tR - tL)) - 4;
                            int px0 = (int)(bL + t0 * (bR - bL)) + 4;
                            int px1 = (int)(bL + t1 * (bR - bL)) - 4;
                            int mx0 = (int)(midL + t0 * (midR - midL)) + 4;
                            int mx1 = (int)(midL + t1 * (midR - midL)) - 4;

                            // Player drop zone (bottom half)
                            Rectangle dropZone = new Rectangle(px0, midY + 4, px1 - px0, fBot - midY - 8);
                            bool isTarget = Raylib.CheckCollisionPointRec(mouse, dropZone);
                            Raylib.DrawRectangleLinesEx(dropZone, isTarget ? 3 : 1, isTarget ? Color.Gold : new Color(60, 65, 78, 220));
                            if (isTarget) Raylib.DrawRectangleRec(new Rectangle(dropZone.X + 1, dropZone.Y + 1, dropZone.Width - 2, dropZone.Height - 2), new Color(255, 215, 50, 28));

                            // Enemy zone (top half)
                            Rectangle enemyZone = new Rectangle(ex0, fTop + 4, ex1 - ex0, midY - fTop - 8);
                            bool hasEnemy = activeBattleState.Enemy.Position == i;
                            if (hasEnemy)
                            {
                                if (activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Fire) > 0)
                                    Raylib.DrawRectangleLinesEx(new Rectangle(enemyZone.X - 3, enemyZone.Y - 3, enemyZone.Width + 6, enemyZone.Height + 6), 2, Color.Orange);
                                if (activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Frost) > 0)
                                    Raylib.DrawRectangleLinesEx(new Rectangle(enemyZone.X - 6, enemyZone.Y - 6, enemyZone.Width + 12, enemyZone.Height + 12), 2, Color.SkyBlue);
                                Raylib.DrawRectangleRounded(enemyZone, 0.06f, 4, Color.Maroon);
                                int elw = Raylib.MeasureText("HOSTILE", 13);
                                Raylib.DrawText("HOSTILE", (int)(enemyZone.X + (enemyZone.Width - elw) / 2), (int)(enemyZone.Y + enemyZone.Height / 2 - 7), 13, Color.White);
                            }
                            else
                            {
                                Raylib.DrawRectangleLinesEx(enemyZone, 1, new Color(48, 52, 63, 180));
                            }

                            // Placed unit in this lane
                            if (i < activeBattleState.PlayerBoard.Count && activeBattleState.PlayerBoard[i].IsOccupied)
                            {
                                var occ = activeBattleState.PlayerBoard[i].Occupant!;
                                bool uSel = activeBattleState.SelectedBoardSlot == i;
                                DrawSummonedUnit(new Rectangle(dropZone.X + 2, dropZone.Y + 2, dropZone.Width - 4, dropZone.Height - 4), occ.SourceCard.Name, occ.Hp, occ.MaxHp, uSel);
                            }
                        }

                        // Drop release
                        if (!Raylib.IsMouseButtonDown(MouseButton.Left) && dragging)
                        {
                            int midYd = fTop + (int)((fBot - fTop) * 0.48f);
                            for (int i = 0; i < 5; i++)
                            {
                                float t0 = (float)i / 5, t1 = (float)(i + 1) / 5;
                                int px0d = (int)(bL + t0 * (bR - bL)) + 4;
                                int px1d = (int)(bL + t1 * (bR - bL)) - 4;
                                Rectangle dz = new Rectangle(px0d, midYd + 4, px1d - px0d, fBot - midYd - 8);
                                if (Raylib.CheckCollisionPointRec(mouse, dz))
                                {
                                    battleService.PlaceCard(draggingIndex, i);
                                    break;
                                }
                            }
                            dragging = false;
                            draggingIndex = -1;
                        }
                    }
                    else if (hoveredHandCard >= 0 && hoveredHandCard < activeBattleState.Hand.Count)
                    {
                        // Dim center and show expanded card preview
                        Raylib.DrawRectangle(cx, 0, cw, height - 50, new Color(14, 15, 20, 175));
                        var previewCard = activeBattleState.Hand[hoveredHandCard];
                        int pvW = 240, pvH = 320;
                        Rectangle pvRect = new Rectangle(cx + (cw - pvW) / 2, height / 2 - pvH / 2, pvW, pvH);
                        DrawCard(pvRect, previewCard.Name, previewCard.Description, previewCard.Cost, Color.White, Color.Gold);
                        int pwl = Raylib.MeasureText("drag to play", 14);
                        Raylib.DrawText("drag to play", (int)(pvRect.X + (pvW - pwl) / 2), (int)(pvRect.Y + pvH + 10), 14, Color.Gray);
                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, pvRect) && !dragging)
                        {
                            dragging = true;
                            draggingIndex = hoveredHandCard;
                            isDraggingFromCollectionPool = false;
                            dragOffset = mouse - new Vector2(pvRect.X, pvRect.Y);
                        }
                    }
                    else
                    {
                        // Yellow "Battlefield" banner
                        int banW = (int)(cw * 0.76f);
                        int banH = 215;
                        Rectangle banner = new Rectangle(cx + (cw - banW) / 2, height / 2 - banH / 2, banW, banH);
                        Raylib.DrawRectangleRec(banner, new Color(255, 195, 45, 255));
                        int btw = Raylib.MeasureText("Battlefield", 60);
                        Raylib.DrawText("Battlefield", (int)(banner.X + (banW - btw) / 2), (int)(banner.Y + banH / 2 - 35), 60, Color.Black);
                    }

                    if (activeBattleState.Phase == TurnPhase.PlayerTurn && DrawButton(new Rectangle(cx + cw / 2 - 90, height - 95, 180, 42), "END TURN", new Color(50, 70, 42, 255), new Color(68, 100, 58, 255)))
                        battleService.EndTurn();

                    // Center bottom HUD
                    Raylib.DrawRectangle(cx, height - 50, cw, 50, new Color(12, 13, 17, 220));
                    Raylib.DrawText("[ENTER] End Turn   |   [SPACE] Execute Slot   |   Drag card to play", cx + 18, height - 33, 13, Color.Gray);

                    // Floating drag ghost
                    if (dragging && draggingIndex >= 0 && draggingIndex < activeBattleState.Hand.Count)
                    {
                        var dc = activeBattleState.Hand[draggingIndex];
                        DrawCard(new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, 155, 205), dc.Name, dc.Description, dc.Cost, new Color(255, 250, 210, 230), Color.Gold);
                    }

                    // Win/Lose overlay
                    if (activeBattleState.Phase == TurnPhase.Finished)
                    {
                        Raylib.DrawRectangle(0, 0, width, height, new Color(15, 15, 18, 230));
                        if (activeBattleState.Enemy.IsDead)
                        {
                            Raylib.DrawText("VICTORY CONCLUDED", width / 2 - 250, height / 2 - 50, 45, Color.Gold);
                            Raylib.DrawText($"+60 Node Bounty Core Credits Awarded", width / 2 - 170, height / 2 + 15, 18, Color.LightGray);
                            Raylib.DrawText($"New Schema Card [Bio Spore] Added to Collection Deck Pool", width / 2 - 240, height / 2 + 45, 16, Color.Lime);
                            if (DrawButton(new Rectangle(width / 2 - 120, height / 2 + 95, 240, 50), "RETURN TO OVERWORLD", new Color(40, 75, 45, 255), new Color(60, 110, 65, 255)))
                            {
                                profile.Gold += 60;
                                profile.TotalCollection.Add(CardLibrary.BioSpore());
                                if (activeBattleNode != null)
                                {
                                    activeBattleNode.Completed = true;
                                    if (activeNodeIndex < campaignNodes.Count - 1) activeNodeIndex++;
                                }
                                scene = GameScene.CampaignMap;
                            }
                        }
                        else
                        {
                            Raylib.DrawText("DEFEAT PROTOCOL INITIALIZED", width / 2 - 320, height / 2 - 40, 40, Color.Red);
                            if (DrawButton(new Rectangle(width / 2 - 120, height / 2 + 40, 240, 50), "RESET PROGRESS RUN", new Color(90, 35, 35, 255), new Color(130, 50, 50, 255)))
                            {
                                profile.Gold = 150;
                                profile.SkillPoints = 8;
                                activeNodeIndex = 0;
                                foreach (var node in campaignNodes) node.Completed = false;
                                scene = GameScene.TitleScreen;
                            }
                        }
                    }
                    break;

                case GameScene.MarketShop:
                    // Header
                    Raylib.DrawRectangle(0, 0, width, 58, new Color(15, 17, 22, 255));
                    Raylib.DrawLine(0, 58, width, 58, new Color(38, 43, 52, 255));
                    Raylib.DrawText("MARKET  &  SKILL MATRIX", 30, 15, 22, Color.White);
                    Raylib.DrawText($"{profile.Gold} G", width - 215, 15, 20, Color.Gold);
                    Raylib.DrawText($"{profile.SkillPoints} SP", width - 100, 15, 20, Color.SkyBlue);

                    if (DrawButton(new Rectangle(30, 74, 160, 38), "< BACK TO MAP", new Color(35, 38, 48, 255), new Color(52, 56, 70, 255)))
                        scene = GameScene.CampaignMap;

                    // Vertical divider
                    Raylib.DrawLine(width / 2, 60, width / 2, height, new Color(35, 40, 50, 255));

                    // === LEFT HALF: SHOP ===
                    Raylib.DrawText("SCHEMA CARDS", 110, 132, 18, Color.Gold);
                    Raylib.DrawLine(110, 156, 110 + shopInventory.Count * 233 - 18, 156, new Color(90, 75, 25, 110));

                    for (int i = 0; i < shopInventory.Count; i++)
                    {
                        int sx = 110 + i * 233;
                        DrawCard(new Rectangle(sx, 168, 215, 268), shopInventory[i].Name, shopInventory[i].Description, shopInventory[i].Cost, Color.White, Color.DarkBlue);
                        bool canAfford = profile.Gold >= cardShopCost;
                        if (DrawButton(new Rectangle(sx, 448, 215, 38), $"BUY  {cardShopCost} G",
                            canAfford ? new Color(35, 60, 45, 255) : new Color(42, 36, 36, 255),
                            canAfford ? new Color(50, 95, 65, 255) : new Color(58, 42, 42, 255)) && canAfford)
                        {
                            profile.Gold -= cardShopCost;
                            profile.TotalCollection.Add(shopInventory[i]);
                        }
                    }

                    // === RIGHT HALF: SKILL TREE ===
                    int treeLeft = width / 2 + 20;
                    float treeSpacing = Math.Min(80f, width * 0.044f);
                    float treeOX = width * 0.765f;
                    float treeOY = height * 0.56f;

                    Raylib.DrawText("SKILL MATRIX", treeLeft + 20, 132, 18, Color.SkyBlue);
                    Raylib.DrawLine(treeLeft + 20, 156, treeLeft + 195, 156, new Color(25, 85, 105, 110));
                    Raylib.DrawText(profile.SkillTree.DebugMode ? "DEBUG  [F3]" : "[F3]",
                        treeLeft + 210, 137, 13,
                        profile.SkillTree.DebugMode ? new Color(200, 80, 80, 255) : new Color(52, 60, 72, 255));

                    // Connectors drawn first (underneath nodes)
                    foreach (var sn in profile.SkillTree.Nodes)
                    {
                        Vector2 snPos = GetSkillNodePos(sn, treeOX, treeOY, treeSpacing);
                        foreach (var reqId in sn.PrerequisiteIds)
                        {
                            var req = profile.SkillTree.Nodes.Find(n => n.Id == reqId);
                            if (req == null) continue;
                            Vector2 reqPos = GetSkillNodePos(req, treeOX, treeOY, treeSpacing);
                            Color lc = profile.SkillTree.DebugMode
                                ? (sn.IsUnlocked ? GetBranchColor(sn.Branch) : new Color(40, 42, 48, 255))
                                : (sn.IsUnlocked ? new Color(0, 215, 235, 210) : new Color(32, 125, 145, 50));
                            Raylib.DrawLineEx(reqPos, snPos, sn.IsUnlocked ? 3f : 2f, lc);
                        }
                    }

                    SkillNode? hoveredNode = null;

                    foreach (var sn in profile.SkillTree.Nodes)
                    {
                        Vector2 snPos = GetSkillNodePos(sn, treeOX, treeOY, treeSpacing);
                        float nr = sn.Branch == SkillBranch.Origin ? 24f : 20f;
                        bool avail = profile.SkillTree.IsAvailable(sn.Id);

                        if (Raylib.CheckCollisionPointCircle(mouse, snPos, nr + 8)) hoveredNode = sn;

                        if (profile.SkillTree.DebugMode)
                        {
                            Color dc = GetBranchColor(sn.Branch);
                            Raylib.DrawCircleV(snPos, nr, sn.IsUnlocked ? dc : new Color(15, 17, 23, 255));
                            Raylib.DrawCircleLines((int)snPos.X, (int)snPos.Y, nr,
                                sn.IsUnlocked ? Color.White : (avail ? dc : new Color(46, 48, 56, 255)));
                            Raylib.DrawText($"{sn.Id}", (int)snPos.X - 5, (int)snPos.Y - 7, 13,
                                sn.IsUnlocked ? Color.Black : Color.DarkGray);
                            Raylib.DrawText(sn.Branch.ToString()[..1], (int)snPos.X - 10, (int)snPos.Y - 33, 13, dc);
                        }
                        else
                        {
                            Color outline = sn.IsUnlocked ? new Color(0, 225, 245, 255)
                                          : avail         ? new Color(0, 200, 220, 200)
                                          :                 new Color(55, 148, 168, 105);
                            Color fill = sn.IsUnlocked ? new Color(0, 165, 192, 255) : new Color(11, 15, 22, 255);

                            if (avail && !sn.IsUnlocked)
                                Raylib.DrawCircleV(snPos, nr + 7, new Color(0, 190, 210, 25));

                            Raylib.DrawCircleV(snPos, nr, fill);
                            Raylib.DrawCircleLines((int)snPos.X, (int)snPos.Y, nr, outline);
                            Raylib.DrawCircleLines((int)snPos.X, (int)snPos.Y, nr - 1f,
                                new Color(outline.R, outline.G, outline.B, (byte)(outline.A / 3)));

                            DrawNodeIcon(snPos, sn.Branch, sn.IsUnlocked ? Color.White : outline, nr * 0.52f);
                        }
                    }

                    // Tooltip
                    if (hoveredNode != null)
                    {
                        Vector2 hnPos = GetSkillNodePos(hoveredNode, treeOX, treeOY, treeSpacing);
                        int ttW = 198, ttH = 108;
                        float ttX = hnPos.X + 30, ttY = hnPos.Y - ttH / 2f;
                        if (ttX + ttW > width - 8) ttX = hnPos.X - ttW - 30;
                        if (ttY < 65) ttY = 65;
                        if (ttY + ttH > height - 8) ttY = height - ttH - 8;

                        Color ttBorder = profile.SkillTree.DebugMode
                            ? GetBranchColor(hoveredNode.Branch)
                            : new Color(0, 200, 220, 210);
                        Raylib.DrawRectangleRounded(new Rectangle(ttX, ttY, ttW, ttH), 0.12f, 6, new Color(11, 14, 21, 248));
                        Raylib.DrawRectangleRoundedLinesEx(new Rectangle(ttX, ttY, ttW, ttH), 0.12f, 6, 1.5f, ttBorder);

                        Raylib.DrawText(hoveredNode.Name, (int)ttX + 10, (int)ttY + 10, 14, Color.White);
                        Raylib.DrawText(hoveredNode.Description, (int)ttX + 10, (int)ttY + 30, 12, new Color(180, 185, 198, 255));

                        if (hoveredNode.IsUnlocked)
                        {
                            Raylib.DrawText("UNLOCKED", (int)ttX + 10, (int)ttY + 80, 14, new Color(0, 210, 90, 255));
                        }
                        else
                        {
                            int pathCost = profile.SkillTree.GetTotalCostToUnlock(hoveredNode.Id);
                            bool canUnlock = profile.SkillPoints >= pathCost;
                            Raylib.DrawText($"Cost: {pathCost} SP", (int)ttX + 10, (int)ttY + 55, 13,
                                canUnlock ? Color.Yellow : new Color(210, 72, 72, 255));
                            Raylib.DrawText(canUnlock ? "Click to unlock path" : $"Need {pathCost - profile.SkillPoints} more SP",
                                (int)ttX + 10, (int)ttY + 80, 12,
                                canUnlock ? new Color(0, 190, 210, 200) : new Color(155, 70, 70, 200));

                            if (canUnlock && Raylib.IsMouseButtonPressed(MouseButton.Left))
                            {
                                int sp = profile.SkillPoints;
                                profile.SkillTree.UnlockPath(hoveredNode.Id, ref sp);
                                profile.SkillPoints = sp;
                            }
                        }
                    }
                    break;

                case GameScene.DeckBuilder:
                    Raylib.DrawRectangle(0, 0, width, 60, new Color(18, 20, 24, 255));
                    Raylib.DrawText($"DECK CONFIGURATION CORE MANAGER MATRIX", 30, 18, 20, Color.White);
                    Raylib.DrawText($"Active Core Deck Volume: {profile.ActiveDeck.Count} cards", width - 350, 20, 18, Color.Yellow);

                    if (DrawButton(new Rectangle(40, 90, 160, 40), "< SAVE AND EXIT", Color.DarkGreen, new Color(45, 120, 55, 255)))
                        scene = GameScene.CampaignMap;

                    int colWidth = 150;
                    int colHeight = 200;

                    Raylib.DrawText("ACTIVE ENGAGED LOADOUT DECK (Max 15 Config Cards)", 60, 150, 20, Color.Gold);
                    for (int i = 0; i < profile.ActiveDeck.Count; i++)
                    {
                        Rectangle cardBox = new Rectangle(60 + i * (colWidth + 12), 190, colWidth, colHeight);
                        DrawCard(cardBox, profile.ActiveDeck[i].Name, profile.ActiveDeck[i].Description, profile.ActiveDeck[i].Cost, Color.White, Color.DarkGreen);

                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, cardBox) && !dragging)
                        {
                            profile.ActiveDeck.RemoveAt(i);
                            break;
                        }
                    }

                    Raylib.DrawText("RESERVE UNLOCKED CARD CATALOG SCHEMA POOL (Drag/Drop to load above)", 60, height / 2 + 50, 20, Color.SkyBlue);
                    int itemsPerRow = (width - 120) / (colWidth + 12);
                    for (int i = 0; i < profile.TotalCollection.Count; i++)
                    {
                        int row = i / itemsPerRow;
                        int col = i % itemsPerRow;
                        Rectangle cardBox = new Rectangle(60 + col * (colWidth + 12), (height / 2 + 90) + row * (colHeight + 15), colWidth, colHeight);

                        if (dragging && isDraggingFromCollectionPool && draggingIndex == i) continue;

                        DrawCard(cardBox, profile.TotalCollection[i].Name, profile.TotalCollection[i].Description, profile.TotalCollection[i].Cost, new Color(225, 230, 245, 255), Color.Black);

                        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, cardBox) && !dragging)
                        {
                            dragging = true;
                            draggingIndex = i;
                            isDraggingFromCollectionPool = true;
                            dragOffset = mouse - new Vector2(cardBox.X, cardBox.Y);
                        }
                    }

                    if (dragging && Raylib.IsMouseButtonReleased(MouseButton.Left))
                    {
                        Rectangle dropZone = new Rectangle(40, 140, width - 80, 270);
                        if (Raylib.CheckCollisionPointRec(mouse, dropZone) && isDraggingFromCollectionPool && profile.ActiveDeck.Count < 15)
                        {
                            profile.ActiveDeck.Add(profile.TotalCollection[draggingIndex]);
                        }
                        dragging = false;
                        draggingIndex = -1;
                        isDraggingFromCollectionPool = false;
                    }

                    if (dragging && isDraggingFromCollectionPool && draggingIndex < profile.TotalCollection.Count)
                    {
                        var floatCard = profile.TotalCollection[draggingIndex];
                        DrawCard(new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, colWidth, colHeight), floatCard.Name, floatCard.Description, floatCard.Cost, new Color(255, 240, 170, 230), Color.Gold);
                    }
                    break;
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
