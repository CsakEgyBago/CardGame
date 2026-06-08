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
    public int SkillPoints { get; set; } = 8; // Bumped up so you can test QOL unlocking
    public List<CardDefinition> TotalCollection { get; set; } = new();
    public List<CardDefinition> ActiveDeck { get; set; } = new();
    
    // NEW: Skill Tree Instance
    public SkillTreeManager SkillTree { get; set; } = new();

    // Helper properties to calculate live stats based on unlocked nodes
    public int MaxEnergyBonus => SkillTree.Nodes.Find(n => n.Id == 1 && n.IsUnlocked) != null ? 1 : 0 
                               + (SkillTree.Nodes.Find(n => n.Id == 2 && n.IsUnlocked) != null ? 1 : 0)
                               + (SkillTree.Nodes.Find(n => n.Id == 3 && n.IsUnlocked) != null ? 2 : 0);
                               
    public int MaxHpBonus => (SkillTree.Nodes.Find(n => n.Id == 11 && n.IsUnlocked) != null ? 10 : 0)
                             + (SkillTree.Nodes.Find(n => n.Id == 12 && n.IsUnlocked) != null ? 15 : 0)
                             + (SkillTree.Nodes.Find(n => n.Id == 13 && n.IsUnlocked) != null ? 25 : 0);
                             
    public int StartCardsBonus => (SkillTree.Nodes.Find(n => n.Id == 4 && n.IsUnlocked) != null ? 1 : 0)
                                + (SkillTree.Nodes.Find(n => n.Id == 5 && n.IsUnlocked) != null ? 1 : 0);
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

    static void DrawBattleTile(Rectangle rect, bool highlighted, string laneLabel)
    {
        Raylib.DrawRectangleRounded(rect, 0.08f, 6, new Color(45, 50, 60, 255));
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.08f, 6, highlighted ? 4 : 1, highlighted ? Color.Gold : new Color(80, 85, 95, 255));
        Raylib.DrawText(laneLabel, (int)rect.X + 6, (int)rect.Y + 6, 12, new Color(110, 115, 125, 150));
    }

    // NEW: Handles the positioning math for the 5-Branch Skill Tree
    static Vector2 GetSkillNodePos(SkillNode node, int originX, int originY)
    {
        int colSpacing = 80;
        int rowSpacing = 65;
        
        int x = originX + (node.Column * colSpacing);
        int yOffset = 0;

        switch (node.Branch)
        {
            case SkillBranch.Green: yOffset = -2; break;
            case SkillBranch.Blue: yOffset = -1; break;
            case SkillBranch.Red: yOffset = 0; break;
            case SkillBranch.Yellow: yOffset = 1; break;
            case SkillBranch.White: yOffset = 2; break;
            case SkillBranch.Origin: yOffset = 0; break;
        }

        return new Vector2(x, originY + (yOffset * rowSpacing));
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

            switch (scene)
            {
                case GameScene.BattleView:
                    var bState = battleService.State;
                    if (Raylib.IsKeyPressed(KeyboardKey.Enter) && bState.Phase == TurnPhase.PlayerTurn)
                        battleService.EndTurn();
                    if (Raylib.IsKeyPressed(KeyboardKey.Space) && bState.Phase == TurnPhase.PlayerTurn && bState.SelectedBoardSlot >= 0)
                    {
                        battleService.ExecuteCard(bState.SelectedBoardSlot);
                        bState.SelectedBoardSlot = -1;
                    }
                    
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left) && bState.Phase == TurnPhase.PlayerTurn)
                    {
                        for (int i = 0; i < bState.Hand.Count; i++)
                        {
                            Rectangle cardRect = new(40 + i * (180 + 16), height - 280, 180, 240);
                            if (Raylib.CheckCollisionPointRec(mouse, cardRect))
                            {
                                dragging = true;
                                draggingIndex = i;
                                dragOffset = mouse - new Vector2(cardRect.X, cardRect.Y);
                                break;
                            }
                        }
                        if (!dragging)
                        {
                            for (int i = 0; i < bState.PlayerBoard.Count; i++)
                            {
                                Rectangle slotRect = new((width / 2 - 460) + i * (170 + 18), 440, 170, 150);
                                if (Raylib.CheckCollisionPointRec(mouse, slotRect) && bState.PlayerBoard[i].IsOccupied)
                                    bState.SelectedBoardSlot = i;
                            }
                        }
                    }
                    if (dragging && Raylib.IsMouseButtonReleased(MouseButton.Left))
                    {
                        for (int i = 0; i < bState.PlayerBoard.Count; i++)
                        {
                            Rectangle slotRect = new((width / 2 - 460) + i * (170 + 18), 440, 170, 150);
                            if (Raylib.CheckCollisionPointRec(mouse, slotRect))
                            {
                                battleService.PlaceCard(draggingIndex, i);
                                break;
                            }
                        }
                        dragging = false;
                        draggingIndex = -1;
                    }
                    break;
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
                    int startGridX = width / 2 - 460;
                    int rowEnemyY = 170;
                    int rowPlayerY = 440;

                    for (int i = 0; i < 5; i++)
                    {
                        int currentTrackX = startGridX + i * (170 + 18);
                        Rectangle fullTrack = new Rectangle(currentTrackX, rowEnemyY, 170, (rowPlayerY + 150) - rowEnemyY);
                        Raylib.DrawRectangleRec(fullTrack, i % 2 == 0 ? new Color(34, 37, 44, 255) : new Color(40, 44, 52, 255));
                    }

                    Raylib.DrawRectangle(15, 15, 280, 110, new Color(20, 22, 26, 240));
                    Raylib.DrawText($"PLAYER FRAME HP: {activeBattleState.Player.Hp}/{activeBattleState.Player.MaxHp}", 30, 25, 18, Color.White);
                    Raylib.DrawText($"Turn Limit Energy: {activeBattleState.Player.Energy}", 30, 55, 16, Color.SkyBlue);
                    Raylib.DrawText($"Discard Deck Vol: {activeBattleState.BurnPile.Count}", 30, 85, 16, Color.Orange);

                    Raylib.DrawRectangle(width - 325, 15, 310, 130, new Color(20, 22, 26, 240));
                    Raylib.DrawText($"{activeBattleNode?.Name ?? "Threat Target"}", width - 305, 25, 18, Color.Red);
                    Raylib.DrawText($"Vitals Tracker: {activeBattleState.Enemy.Hp}/{activeBattleState.Enemy.MaxHp}", width - 305, 55, 16, Color.White);

                    Raylib.DrawText($"Fire Burden Stacks: {activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Fire)}", width - 305, 80, 14, Color.Orange);
                    Raylib.DrawText($"Frost Slow Stacks: {activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Frost)}", width - 305, 100, 14, Color.SkyBlue);

                    for (int i = 0; i < 5; i++)
                    {
                        Rectangle cell = new Rectangle(startGridX + i * (170 + 18), rowEnemyY, 170, 150);
                        bool hasEnemy = activeBattleState.Enemy.Position == i;
                        DrawBattleTile(cell, hasEnemy, $"Track Grid {i}");

                        if (hasEnemy)
                        {
                            Rectangle coreBody = new Rectangle(cell.X + 15, cell.Y + 15, cell.Width - 30, cell.Height - 30);
                            
                            if (activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Fire) > 0)
                                Raylib.DrawRectangleLinesEx(new Rectangle(coreBody.X - 4, coreBody.Y - 4, coreBody.Width + 8, coreBody.Height + 8), 3, Color.Orange);
                            if (activeBattleState.Enemy.ActiveElements.GetStacks(ElementType.Frost) > 0)
                                Raylib.DrawRectangleLinesEx(new Rectangle(coreBody.X - 8, coreBody.Y - 8, coreBody.Width + 16, coreBody.Height + 16), 2, Color.SkyBlue);

                            Raylib.DrawRectangleRounded(coreBody, 0.1f, 4, Color.Maroon);
                            Raylib.DrawText("TARGET HOSTILE", (int)coreBody.X + 12, (int)coreBody.Y + 55, 16, Color.White);
                        }
                    }

                    for (int i = 0; i < activeBattleState.PlayerBoard.Count; i++)
                    {
                        Rectangle cell = new Rectangle(startGridX + i * (170 + 18), rowPlayerY, 170, 150);
                        bool selected = activeBattleState.SelectedBoardSlot == i;
                        DrawBattleTile(cell, selected, $"Core Matrix {i}");

                        var slot = activeBattleState.PlayerBoard[i];
                        if (slot.IsOccupied)
                        {
                            var occupant = slot.Occupant!;
                            DrawCard(new Rectangle(cell.X + 6, cell.Y + 6, cell.Width - 12, cell.Height - 12), occupant.SourceCard.Name, occupant.SourceCard.Description, occupant.SourceCard.Cost, new Color(65, 70, 80, 255), Color.SkyBlue, selected);
                            
                            Raylib.DrawText($"HP: {occupant.Hp}/{occupant.MaxHp}", (int)cell.X + 12, (int)(cell.Y + cell.Height - 40), 14, Color.Green);
                            Raylib.DrawText($"T-Count: {slot.TurnsOnBoard}", (int)cell.X + 12, (int)(cell.Y + cell.Height - 24), 13, Color.LightGray);
                        }
                    }

                    for (int i = 0; i < activeBattleState.Hand.Count; i++)
                    {
                        if (dragging && i == draggingIndex) continue;
                        Rectangle pos = new Rectangle(40 + i * (180 + 16), height - 280, 180, 240);
                        var hCard = activeBattleState.Hand[i];
                        DrawCard(pos, hCard.Name, hCard.Description, hCard.Cost, Color.White, Color.Black);
                    }

                    if (dragging && draggingIndex >= 0 && draggingIndex < activeBattleState.Hand.Count)
                    {
                        var targetCard = activeBattleState.Hand[draggingIndex];
                        DrawCard(new Rectangle(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y, 180, 240), targetCard.Name, targetCard.Description, targetCard.Cost, new Color(255, 250, 210, 230), Color.Gold);
                    }

                    Raylib.DrawText("[ENTER] Finalize Operations Block    |    [SPACE] Release Highlighted Core Matrix Cell", 40, height - 35, 15, Color.Gray);

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
                    Raylib.DrawRectangle(0, 0, width, 60, new Color(18, 20, 24, 255));
                    Raylib.DrawText($"COMMERCE HUB AND UPGRADE LAB", 30, 18, 20, Color.White);
                    Raylib.DrawText($"Credits Balance: {profile.Gold}G", width - 420, 20, 18, Color.Gold);
                    Raylib.DrawText($"Skill points: {profile.SkillPoints}SP", width - 200, 20, 18, Color.SkyBlue);

                    if (DrawButton(new Rectangle(40, 90, 160, 40), "< BACK TO MAP", Color.DarkGray, Color.Gray))
                        scene = GameScene.CampaignMap;

                    Raylib.DrawText("AVAILABLE SCHEMA CARDS", 120, 180, 22, Color.Gold);
                    for (int i = 0; i < shopInventory.Count; i++)
                    {
                        int itemX = 120 + i * (220 + 20);
                        Rectangle displayCardArea = new Rectangle(itemX, 230, 220, 280);
                        DrawCard(displayCardArea, shopInventory[i].Name, shopInventory[i].Description, shopInventory[i].Cost, Color.White, Color.DarkBlue);

                        Rectangle buyBtn = new Rectangle(itemX, 530, 220, 40);
                        if (DrawButton(buyBtn, $"Purchase: {cardShopCost}G", new Color(35, 60, 45, 255), new Color(50, 95, 65, 255)))
                        {
                            if (profile.Gold >= cardShopCost)
                            {
                                profile.Gold -= cardShopCost;
                                profile.TotalCollection.Add(shopInventory[i]);
                            }
                        }
                    }

                    // --- NEW 5-BRANCH SKILL TREE RENDERING ---
                    int treeOriginX = width - 650;
                    int treeOriginY = height / 2 + 50;

                    Raylib.DrawText("MAGE SKILL MATRIX (F3 for Debug)", treeOriginX - 100, 180, 22, Color.SkyBlue);
                    Raylib.DrawRectangle(treeOriginX - 150, 220, 750, 450, new Color(15, 18, 22, 200));

                    // 1. Draw connections lines first (so they sit underneath the nodes)
                    foreach (var node in profile.SkillTree.Nodes)
                    {
                        Vector2 pos = GetSkillNodePos(node, treeOriginX, treeOriginY);
                        foreach (var reqId in node.PrerequisiteIds)
                        {
                            var reqNode = profile.SkillTree.Nodes.Find(n => n.Id == reqId);
                            if (reqNode != null)
                            {
                                Vector2 reqPos = GetSkillNodePos(reqNode, treeOriginX, treeOriginY);
                                Color lineColor = node.IsUnlocked ? GetBranchColor(node.Branch) : new Color(50, 50, 50, 255);
                                Raylib.DrawLineEx(reqPos, pos, 4f, lineColor);
                            }
                        }
                    }

                    SkillNode? hoveredNode = null;

                    // 2. Draw the nodes and process hovering
                    foreach (var node in profile.SkillTree.Nodes)
                    {
                        Vector2 pos = GetSkillNodePos(node, treeOriginX, treeOriginY);
                        Color nodeColor = GetBranchColor(node.Branch);
                        
                        bool isHovered = Raylib.CheckCollisionPointCircle(mouse, pos, 20);
                        if (isHovered) hoveredNode = node;

                        // Filled if unlocked, Hollow outline if locked
                        if (node.IsUnlocked)
                        {
                            Raylib.DrawCircleV(pos, 20, nodeColor);
                            Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, 20, Color.White);
                        }
                        else
                        {
                            Raylib.DrawCircleV(pos, 20, new Color(20, 20, 20, 255));
                            Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, 20, profile.SkillTree.IsAvailable(node.Id) ? nodeColor : Color.DarkGray);
                        }

                        // Debug Overlay Details
                        if (profile.SkillTree.DebugMode)
                        {
                            Raylib.DrawText($"ID:{node.Id}", (int)pos.X - 12, (int)pos.Y - 8, 10, Color.Black);
                            Raylib.DrawText(node.Branch.ToString(), (int)pos.X - 25, (int)pos.Y - 35, 10, nodeColor);
                        }
                    }

                    // 3. Render Tooltip for Hovered Node + Handle Clicks (QOL Math applied)
                    if (hoveredNode != null)
                    {
                        Vector2 hoverPos = GetSkillNodePos(hoveredNode, treeOriginX, treeOriginY);
                        Rectangle tooltip = new Rectangle(hoverPos.X - 75, hoverPos.Y + 30, 150, 90);
                        Raylib.DrawRectangleRounded(tooltip, 0.1f, 4, new Color(30, 35, 45, 240));
                        Raylib.DrawRectangleRoundedLinesEx(tooltip, 0.1f, 4, 1, GetBranchColor(hoveredNode.Branch));

                        Raylib.DrawText(hoveredNode.Name, (int)tooltip.X + 10, (int)tooltip.Y + 10, 14, Color.White);
                        Raylib.DrawText(hoveredNode.Description, (int)tooltip.X + 10, (int)tooltip.Y + 30, 12, Color.LightGray);
                        
                        if (hoveredNode.IsUnlocked)
                        {
                            Raylib.DrawText("UNLOCKED", (int)tooltip.X + 10, (int)tooltip.Y + 65, 14, Color.Green);
                        }
                        else
                        {
                            int totalCost = profile.SkillTree.GetTotalCostToUnlock(hoveredNode.Id);
                            Color costColor = profile.SkillPoints >= totalCost ? Color.Yellow : Color.Red;
                            Raylib.DrawText($"Path Cost: {totalCost} SP", (int)tooltip.X + 10, (int)tooltip.Y + 65, 14, costColor);

                            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && profile.SkillPoints >= totalCost)
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
